using AskMe.Application.Common.Exceptions;
using AskMe.Application.Common.Interfaces;
using AskMe.Application.Common.Models;
using AskMe.Domain.Entities;
using AskMe.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskMe.Application.Admin;

// Every method here assumes the caller has already been verified as Admin at
// the API layer (via [Authorize(Roles = "Admin")]) - this service additionally
// writes an audit entry for every sensitive action or view, per requirement.
public class AdminService
{
    private readonly IApplicationDbContext _db;

    public AdminService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AdminQuestionDto>> GetAllQuestionsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var baseQuery = _db.Questions.AsNoTracking().Include(q => q.TargetUser);
        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new AdminQuestionDto(
                q.Id, q.TargetUser!.Username, q.Content, q.IsAnonymous,
                q.IsVisible, q.IsDeleted, q.VoteCount, q.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminQuestionDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task SetVisibilityAsync(Guid adminId, Guid questionId, bool isVisible, CancellationToken ct = default)
    {
        var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, ct)
            ?? throw new NotFoundException("Question", questionId);

        q.IsVisible = isVisible;
        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            AdminUserId = adminId,
            QuestionId = questionId,
            Action = isVisible ? SecurityAction.UnhideQuestion : SecurityAction.HideQuestion,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid adminId, Guid questionId, CancellationToken ct = default)
    {
        var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, ct)
            ?? throw new NotFoundException("Question", questionId);

        q.IsDeleted = true;
        q.IsVisible = false;
        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            AdminUserId = adminId,
            QuestionId = questionId,
            Action = SecurityAction.DeleteQuestion,
        });
        await _db.SaveChangesAsync(ct);
    }

    /// The one place in the whole system that returns SourceIp/UserAgent -
    /// never reachable from public or profile-owner endpoints. Recording the
    /// audit entry is mandatory and happens atomically with the read being
    /// "granted" (same SaveChanges), so every access is provably logged.
    public async Task<SecurityMetadataDto> InvestigateAsync(Guid adminId, Guid questionId, CancellationToken ct = default)
    {
        var metadata = await _db.QuestionSecurityMetadata.AsNoTracking()
            .FirstOrDefaultAsync(m => m.QuestionId == questionId, ct)
            ?? throw new NotFoundException("QuestionSecurityMetadata", questionId);

        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            AdminUserId = adminId,
            QuestionId = questionId,
            Action = SecurityAction.ViewSecurityMetadata,
        });
        await _db.SaveChangesAsync(ct);

        return new SecurityMetadataDto(
            metadata.QuestionId, metadata.SourceIp, metadata.UserAgent,
            metadata.AuthenticatedUserId, metadata.SubmittedAt);
    }

    public async Task<PlatformStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        return new PlatformStatsDto(
            await _db.Users.CountAsync(ct),
            await _db.Questions.CountAsync(q => !q.IsDeleted, ct),
            await _db.Answers.CountAsync(ct),
            await _db.Questions.CountAsync(q => !q.IsVisible && !q.IsDeleted, ct),
            await _db.Questions.CountAsync(q => q.CreatedAt >= since && !q.IsDeleted, ct));
    }

    // ---------------- User management ----------------

    public async Task<List<AdminUserDto>> GetUsersAsync(string? search, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.Username.Contains(s) || u.Email.Contains(s));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).Take(100).ToListAsync(ct);
        var userIds = users.Select(u => u.Id).ToList();

        var questionCounts = await _db.Questions
            .Where(q => userIds.Contains(q.TargetUserId) && !q.IsDeleted)
            .GroupBy(q => q.TargetUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countsByUser = questionCounts.ToDictionary(x => x.UserId, x => x.Count);

        return users.Select(u => new AdminUserDto(
            u.Id, u.Username, u.DisplayName, u.Email, u.Role.ToString(),
            countsByUser.GetValueOrDefault(u.Id, 0), u.CreatedAt, u.RegistrationIp
        )).ToList();
    }

    /// Deletes a user account. Questions SENT TO them cascade-delete (with
    /// their answers/votes/security metadata/follow-ups) via the FK
    /// configuration; questions THEY asked other people are kept, with the
    /// author link set to null (SetNull), same as any other account deletion.
    public async Task DeleteUserAsync(Guid adminId, Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (user.Role == UserRole.Admin)
            throw new ValidationException("Admin accounts can't be deleted from this panel.");

        _db.Users.Remove(user);
        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            AdminUserId = adminId,
            Action = SecurityAction.DeleteUser,
            Notes = $"Deleted user '{user.Username}' ({user.Id})",
        });
        await _db.SaveChangesAsync(ct);
    }

    // ---------------- IP blocking ----------------

    public async Task<List<BlockedIpDto>> GetBlockedIpsAsync(CancellationToken ct = default)
    {
        var blocked = await _db.BlockedIps.AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
        return blocked.Select(b => new BlockedIpDto(b.Id, b.IpAddress, b.Reason, b.CreatedAt)).ToList();
    }

    public async Task BlockIpAsync(Guid adminId, BlockIpRequest request, CancellationToken ct = default)
    {
        var ip = request.IpAddress.Trim();
        if (string.IsNullOrEmpty(ip))
            throw new ValidationException("An IP address is required.");

        var alreadyBlocked = await _db.BlockedIps.AnyAsync(b => b.IpAddress == ip, ct);
        if (alreadyBlocked) return;

        _db.BlockedIps.Add(new BlockedIp { IpAddress = ip, Reason = request.Reason, BlockedByAdminId = adminId });
        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            AdminUserId = adminId,
            Action = SecurityAction.BlockIp,
            Notes = $"Blocked IP {ip}",
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnblockIpAsync(Guid adminId, Guid blockedIpId, CancellationToken ct = default)
    {
        var entry = await _db.BlockedIps.FirstOrDefaultAsync(b => b.Id == blockedIpId, ct)
            ?? throw new NotFoundException("BlockedIp", blockedIpId);

        _db.BlockedIps.Remove(entry);
        _db.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            AdminUserId = adminId,
            Action = SecurityAction.UnblockIp,
            Notes = $"Unblocked IP {entry.IpAddress}",
        });
        await _db.SaveChangesAsync(ct);
    }
}
