namespace AskMe.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
