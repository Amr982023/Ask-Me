namespace AskMe.Application.Auth;

public record RegisterRequest(string Username, string Email, string Password, string DisplayName);

// Registration no longer returns a token directly - the account can't log in
// until the emailed code is verified.
public record RegisterResultDto(string Email, string Message);

public record VerifyEmailRequest(string Email, string Code);
public record ResendVerificationRequest(string Email);

public record LoginRequest(string Email, string Password);
public record AuthResultDto(string Token, DateTime ExpiresAtUtc, Guid UserId, string Username, string Role);

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);

public record GoogleLoginRequest(string IdToken);
public record FacebookLoginRequest(string AccessToken);

// CurrentPassword is only required when the account already has one -
// OAuth-only accounts are allowed to set their first password without it.
// Code is the OTP sent via /auth/request-password-change-otp.
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword, string Code);
