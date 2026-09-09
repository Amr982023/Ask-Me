using System.Security.Claims;
using AskMe.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AskMe.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResultDto>> Register(RegisterRequest request, CancellationToken ct)
        => Ok(await _authService.RegisterAsync(request, ct));

    [HttpPost("verify-email")]
    public async Task<ActionResult<AuthResultDto>> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
        => Ok(await _authService.VerifyEmailAsync(request, ct));

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken ct)
    {
        await _authService.ResendVerificationAsync(request, ct);
        return Ok(new { message = "If an account with that email exists, a new code has been sent." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken ct)
        => Ok(await _authService.LoginAsync(request, ct));

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(request, ct);
        return Ok(new { message = "If an account with that email exists, a reset code has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return Ok(new { message = "Password updated. You can now log in." });
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResultDto>> Google(GoogleLoginRequest request, CancellationToken ct)
        => Ok(await _authService.GoogleLoginAsync(request, ct));

    [HttpPost("facebook")]
    public async Task<ActionResult<AuthResultDto>> Facebook(FacebookLoginRequest request, CancellationToken ct)
        => Ok(await _authService.FacebookLoginAsync(request, ct));

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.ChangePasswordAsync(userId, request, ct);
        return NoContent();
    }

    [HttpPost("request-password-change-otp")]
    [Authorize]
    public async Task<IActionResult> RequestPasswordChangeOtp(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.RequestPasswordChangeOtpAsync(userId, ct);
        return Ok(new { message = "Verification code sent to your email." });
    }
}
