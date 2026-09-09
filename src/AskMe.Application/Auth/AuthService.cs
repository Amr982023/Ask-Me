using System.Security.Cryptography;
using AskMe.Application.Common.Exceptions;
using AskMe.Application.Common.Interfaces;
using AskMe.Domain.Entities;
using AskMe.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskMe.Application.Auth;

public class AuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IOtpHasher _otpHasher;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IFacebookTokenValidator _facebookValidator;
    private readonly IRequestContextService _requestContext;

    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    // Server-side floor under the frontend's 60s cooldown UI - never trust a
    // client-only timer to actually stop rapid-fire resend abuse.
    private static readonly TimeSpan MinResendInterval = TimeSpan.FromSeconds(45);

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IOtpHasher otpHasher,
        ITokenService tokens,
        IEmailSender email,
        IGoogleTokenValidator googleValidator,
        IFacebookTokenValidator facebookValidator,
        IRequestContextService requestContext)
    {
        _db = db;
        _hasher = hasher;
        _otpHasher = otpHasher;
        _tokens = tokens;
        _email = email;
        _googleValidator = googleValidator;
        _facebookValidator = facebookValidator;
        _requestContext = requestContext;
    }

    // ---------------- Registration / email verification ----------------

    /// Creates a PendingRegistration and emails a code - NO User row exists
    /// yet. This is deliberate: until the code is verified, the username and
    /// email must stay available for someone else to actually claim, rather
    /// than being permanently squatted by an abandoned signup attempt.
    public async Task<RegisterResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var username = NormalizeUsername(request.Username);
        var email = request.Email.Trim().ToLowerInvariant();

        ValidateUsername(username);
        if (request.Password.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");

        await EnsureIpNotBlockedAsync(ct);

        var taken = await _db.Users.AnyAsync(u => u.Username == username || u.Email == email, ct);
        if (taken)
            throw new ValidationException("Username or email is already taken.");

        // A repeat registration attempt for the same email overwrites the
        // previous pending row (and is itself subject to the resend cooldown
        // below) rather than creating a duplicate.
        var existingPending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
        if (existingPending is not null)
            EnsureResendAllowed(existingPending.ExpiresAt);

        var code = GenerateCode();
        var expiresAt = DateTime.UtcNow.Add(OtpLifetime);

        if (existingPending is not null)
        {
            existingPending.Username = username;
            existingPending.PasswordHash = _hasher.Hash(request.Password);
            existingPending.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? username : request.DisplayName.Trim();
            existingPending.RegistrationIp = _requestContext.IpAddress;
            existingPending.CodeHash = _otpHasher.Hash(code);
            existingPending.ExpiresAt = expiresAt;
        }
        else
        {
            _db.PendingRegistrations.Add(new PendingRegistration
            {
                Username = username,
                Email = email,
                PasswordHash = _hasher.Hash(request.Password),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? username : request.DisplayName.Trim(),
                RegistrationIp = _requestContext.IpAddress,
                CodeHash = _otpHasher.Hash(code),
                ExpiresAt = expiresAt,
            });
        }

        await _db.SaveChangesAsync(ct);
        await SendOtpEmailAsync(email, code, "Verify your Ask Me account");

        return new RegisterResultDto(email, "Almost done. Check your email for a verification code.");
    }

    /// The ONLY place a User row gets created via the password-registration
    /// path - only reached once the code has actually been verified.
    public async Task<AuthResultDto> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var pending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);

        if (pending is null || pending.ExpiresAt < DateTime.UtcNow || !_otpHasher.Verify(request.Code, pending.CodeHash))
            throw new ValidationException("Invalid or expired code.");

        // Final uniqueness check - guards the rare race where someone else
        // claimed the same username/email while this one sat unverified.
        var stillAvailable = !await _db.Users.AnyAsync(u => u.Username == pending.Username || u.Email == pending.Email, ct);
        if (!stillAvailable)
        {
            _db.PendingRegistrations.Remove(pending);
            await _db.SaveChangesAsync(ct);
            throw new ValidationException("That username or email was just taken. Please register again.");
        }

        var user = new User
        {
            Username = pending.Username,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            DisplayName = pending.DisplayName,
            Role = UserRole.User,
            EmailConfirmed = true,
            RegistrationIp = pending.RegistrationIp,
        };
        _db.Users.Add(user);
        _db.PendingRegistrations.Remove(pending);
        await _db.SaveChangesAsync(ct);

        var (token, expires) = _tokens.GenerateToken(user);
        return new AuthResultDto(token, expires, user.Id, user.Username, user.Role.ToString());
    }

    public async Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var pending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);

        // Same response whether or not a pending registration exists, to
        // avoid leaking which emails are mid-signup.
        if (pending is null) return;

        EnsureResendAllowed(pending.ExpiresAt);

        var code = GenerateCode();
        pending.CodeHash = _otpHasher.Hash(code);
        pending.ExpiresAt = DateTime.UtcNow.Add(OtpLifetime);
        await _db.SaveChangesAsync(ct);

        await SendOtpEmailAsync(email, code, "Verify your Ask Me account");
    }

    // ---------------- Login ----------------

    public async Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same error for "no such user", "wrong password", and "Google/Facebook-only
        // account" - never reveal which one it was (avoids account enumeration).
        // Note: every User row that exists is already EmailConfirmed = true,
        // since verification is now the only way one gets created - no
        // separate "please verify" check is needed here anymore.
        if (user is null || user.PasswordHash is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new ValidationException("Invalid email or password.");

        var (token, expires) = _tokens.GenerateToken(user);
        return new AuthResultDto(token, expires, user.Id, user.Username, user.Role.ToString());
    }

    // ---------------- Password reset (forgot password) ----------------

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same response whether or not the account exists, to avoid leaking
        // which emails are registered.
        if (user is not null)
            await GenerateAndSendOtpAsync(user, OtpPurpose.PasswordReset, "Reset your Ask Me password", ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new ValidationException("Invalid or expired code.");

        if (request.NewPassword.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");

        await ConsumeOtpOrThrowAsync(user.Id, OtpPurpose.PasswordReset, request.Code, ct);

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ---------------- OAuth (Google / Facebook) ----------------
    // No OTP here by design - the provider has already verified the email
    // address on our behalf, so there's nothing left to confirm.

    public async Task<AuthResultDto> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var info = await _googleValidator.ValidateAsync(request.IdToken, ct)
            ?? throw new ValidationException("Invalid Google sign-in token.");

        var user = await FindOrCreateExternalUserAsync("Google", info, ct);
        var (token, expires) = _tokens.GenerateToken(user);
        return new AuthResultDto(token, expires, user.Id, user.Username, user.Role.ToString());
    }

    public async Task<AuthResultDto> FacebookLoginAsync(FacebookLoginRequest request, CancellationToken ct = default)
    {
        var info = await _facebookValidator.ValidateAsync(request.AccessToken, ct)
            ?? throw new ValidationException("Invalid Facebook sign-in token.");

        var user = await FindOrCreateExternalUserAsync("Facebook", info, ct);
        var (token, expires) = _tokens.GenerateToken(user);
        return new AuthResultDto(token, expires, user.Id, user.Username, user.Role.ToString());
    }

    // ---------------- Change password (logged-in user, OTP-gated) ----------------

    /// Step 1 of changing your password from Settings: emails a code to the
    /// account's own address before anything can actually change.
    public async Task RequestPasswordChangeOtpAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        await GenerateAndSendOtpAsync(user, OtpPurpose.PasswordChange, "Confirm your Ask Me password change", ct);
    }

    /// Step 2: requires the emailed code AND (if one exists) the current
    /// password - both must check out.
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (request.NewPassword.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");

        if (user.PasswordHash is not null)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || !_hasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new ValidationException("Current password is incorrect.");
        }
        // else: OAuth-only account setting its first local password - no current password to check.

        await ConsumeOtpOrThrowAsync(userId, OtpPurpose.PasswordChange, request.Code, ct);

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ---------------- Helpers ----------------

    private async Task EnsureIpNotBlockedAsync(CancellationToken ct)
    {
        var ip = _requestContext.IpAddress;
        var blocked = await _db.BlockedIps.AnyAsync(b => b.IpAddress == ip, ct);
        if (blocked)
            throw new ValidationException("Registration is not allowed from this network.");
    }

    private async Task<User> FindOrCreateExternalUserAsync(
        string provider, Common.Models.ExternalUserInfo info, CancellationToken ct)
    {
        var email = info.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.ExternalProvider == provider && u.ExternalId == info.ProviderUserId, ct);

        if (user is null)
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        if (user is not null)
        {
            user.ExternalProvider = provider;
            user.ExternalId = info.ProviderUserId;
            user.EmailConfirmed = true;
            await _db.SaveChangesAsync(ct);
            return user;
        }

        var username = await GenerateUniqueUsernameAsync(info.Name ?? email, ct);
        await EnsureIpNotBlockedAsync(ct);

        user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = null,
            DisplayName = string.IsNullOrWhiteSpace(info.Name) ? username : info.Name,
            Role = UserRole.User,
            EmailConfirmed = true,
            ExternalProvider = provider,
            ExternalId = info.ProviderUserId,
            RegistrationIp = _requestContext.IpAddress,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string seed, CancellationToken ct)
    {
        var baseName = new string(seed.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrEmpty(baseName)) baseName = "user";
        if (baseName.Length > 24) baseName = baseName[..24];

        var candidate = baseName;
        var attempt = 0;
        while (await _db.Users.AnyAsync(u => u.Username == candidate, ct))
        {
            attempt++;
            candidate = $"{baseName}{RandomNumberGenerator.GetInt32(1000, 9999)}";
            if (attempt > 10) candidate = $"{baseName}{Guid.NewGuid():N}"[..30];
        }
        return candidate;
    }

    private static string GenerateCode() => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    private async Task SendOtpEmailAsync(string toEmail, string code, string subject)
    {
        var body = $"""
            <p>Your verification code is:</p>
            <h2 style="letter-spacing:4px">{code}</h2>
            <p>This code expires in {OtpLifetime.TotalMinutes:0} minutes. If you didn't request this, you can ignore this email.</p>
            """;
        await _email.SendAsync(toEmail, subject, body, default);
    }

    /// Generates+sends an OTP for an existing User (password reset / password
    /// change) - includes the same server-side resend-interval floor as the
    /// pending-registration path.
    private async Task GenerateAndSendOtpAsync(User user, OtpPurpose purpose, string subject, CancellationToken ct)
    {
        var recent = await _db.OtpCodes
            .Where(o => o.UserId == user.Id && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (recent is not null)
            EnsureResendAllowed(recent.ExpiresAt);

        var code = GenerateCode();
        _db.OtpCodes.Add(new OtpCode
        {
            UserId = user.Id,
            CodeHash = _otpHasher.Hash(code),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.Add(OtpLifetime),
        });
        await _db.SaveChangesAsync(ct);

        await SendOtpEmailAsync(user.Email, code, subject);
    }

    private async Task ConsumeOtpOrThrowAsync(Guid userId, OtpPurpose purpose, string code, CancellationToken ct)
    {
        var candidates = await _db.OtpCodes
            .Where(o => o.UserId == userId && o.Purpose == purpose && o.ConsumedAt == null && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var match = candidates.FirstOrDefault(o => _otpHasher.Verify(code, o.CodeHash));
        if (match is null)
            throw new ValidationException("Invalid or expired code.");

        match.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// Both PendingRegistration and OtpCode store ExpiresAt but not
    /// "sent at" directly - since ExpiresAt = sentAt + OtpLifetime, this
    /// derives sentAt from it to check the resend cooldown without an extra column.
    private static void EnsureResendAllowed(DateTime expiresAt)
    {
        var sentAt = expiresAt - OtpLifetime;
        var elapsed = DateTime.UtcNow - sentAt;
        if (elapsed < MinResendInterval)
        {
            var waitSeconds = (int)Math.Ceiling((MinResendInterval - elapsed).TotalSeconds);
            throw new ValidationException($"Please wait {waitSeconds}s before requesting another code.");
        }
    }

    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    private static void ValidateUsername(string username)
    {
        if (username.Length is < 3 or > 30)
            throw new ValidationException("Username must be between 3 and 30 characters.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-z0-9_]+$"))
            throw new ValidationException("Username may only contain lowercase letters, numbers and underscores.");
    }
}
