namespace AskMe.Application.Common.Interfaces;

// Wraps the details of the current HTTP request needed for security
// metadata capture and rate limiting, without Application depending on
// ASP.NET Core's HttpContext directly.
public interface IRequestContextService
{
    string IpAddress { get; }
    string? UserAgent { get; }
}
