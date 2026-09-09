using System.Net;
using System.Text.Json;
using AskMe.Application.Common.Exceptions;

namespace AskMe.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, message) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ForbiddenException => (HttpStatusCode.Forbidden, ex.Message),
                DuplicateVoteException => (HttpStatusCode.Conflict, ex.Message),
                ValidationException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred."),
            };

            if (status == HttpStatusCode.InternalServerError)
                // Never leak internal exception details/IPs/stack traces to clients -
                // full detail goes to the server log only.
                _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
