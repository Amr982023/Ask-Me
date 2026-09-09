namespace AskMe.Application.Common.Interfaces;

// Same shape as IPasswordHasher, kept separate since OTP codes are short
// numeric strings with a much shorter lifetime/threat model than passwords.
public interface IOtpHasher
{
    string Hash(string code);
    bool Verify(string code, string hash);
}
