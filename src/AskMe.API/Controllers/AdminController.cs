using System.Security.Claims;
using AskMe.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskMe.API.Controllers;

// Every action here requires the Admin role, enforced server-side - never
// reachable through the normal user-facing UI or its API surface.
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService) => _adminService = adminService;

    private Guid CurrentAdminId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("questions")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _adminService.GetAllQuestionsAsync(page, pageSize, ct));

    [HttpPatch("questions/{id}/visibility")]
    public async Task<IActionResult> SetVisibility(Guid id, [FromQuery] bool isVisible, CancellationToken ct)
    {
        await _adminService.SetVisibilityAsync(CurrentAdminId(), id, isVisible, ct);
        return NoContent();
    }

    [HttpDelete("questions/{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _adminService.DeleteAsync(CurrentAdminId(), id, ct);
        return NoContent();
    }

    [HttpGet("questions/{id}/security")]
    public async Task<IActionResult> Investigate(Guid id, CancellationToken ct)
        => Ok(await _adminService.InvestigateAsync(CurrentAdminId(), id, ct));

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
        => Ok(await _adminService.GetStatsAsync(ct));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await _adminService.GetUsersAsync(search, ct));

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        await _adminService.DeleteUserAsync(CurrentAdminId(), id, ct);
        return NoContent();
    }

    [HttpGet("blocked-ips")]
    public async Task<IActionResult> GetBlockedIps(CancellationToken ct)
        => Ok(await _adminService.GetBlockedIpsAsync(ct));

    [HttpPost("blocked-ips")]
    public async Task<IActionResult> BlockIp(BlockIpRequest request, CancellationToken ct)
    {
        await _adminService.BlockIpAsync(CurrentAdminId(), request, ct);
        return NoContent();
    }

    [HttpDelete("blocked-ips/{id}")]
    public async Task<IActionResult> UnblockIp(Guid id, CancellationToken ct)
    {
        await _adminService.UnblockIpAsync(CurrentAdminId(), id, ct);
        return NoContent();
    }
}
