using System.Security.Claims;
using AskMe.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AskMe.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => _accessor.HttpContext?.User?.IsInRole("Admin") == true;
}
