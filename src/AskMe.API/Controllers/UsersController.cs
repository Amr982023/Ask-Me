using System.Security.Claims;
using AskMe.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskMe.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService) => _userService = userService;

    [HttpGet("{username}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicProfileDto>> GetPublicProfile(string username, CancellationToken ct)
        => Ok(await _userService.GetPublicProfileAsync(username, ct));

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
        => Ok(await _userService.SearchProfilesAsync(query ?? string.Empty, ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> GetMe(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _userService.GetMeAsync(userId, ct));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.UpdateProfileAsync(userId, request, ct);
        return NoContent();
    }

    [HttpPost("me/profile-image")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB
    public async Task<IActionResult> UploadProfileImage(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "A file is required." });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var stream = file.OpenReadStream();
        await _userService.UpdateProfileImageAsync(userId, stream, file.FileName, file.ContentType, ct);
        return NoContent();
    }
}
