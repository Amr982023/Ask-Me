using AskMe.Application.Common.Interfaces;

namespace AskMe.Infrastructure.Identity;

// Same BCrypt approach as passwords, but a lower work factor since OTP codes
// are short-lived (10 minutes) and low-entropy numeric strings anyway - the
// real protection is the expiry + single-use consumption, not hash cost.
public class BCryptOtpHasher : IOtpHasher
{
    public string Hash(string code) => BCrypt.Net.BCrypt.HashPassword(code, workFactor: 10);

    public bool Verify(string code, string hash) => BCrypt.Net.BCrypt.Verify(code, hash);
}
