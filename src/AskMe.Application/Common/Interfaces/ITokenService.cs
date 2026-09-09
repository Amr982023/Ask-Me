using AskMe.Domain.Entities;

namespace AskMe.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
