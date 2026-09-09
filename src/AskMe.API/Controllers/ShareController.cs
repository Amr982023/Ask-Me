using System.Net;
using AskMe.Application.Common.Exceptions;
using AskMe.Application.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AskMe.API.Controllers;

// A React SPA can't produce per-question Open Graph tags for link previews:
// Facebook/WhatsApp/Twitter's crawlers fetch the raw HTML and never execute
// JavaScript, so anything React would render is invisible to them. This
// controller serves a tiny server-rendered page instead - crawlers read its
// static <meta property="og:..."> tags directly, while real browsers get an
// instant redirect into the actual interactive app. This is the URL the
// "Share" button should hand out, not the SPA's own /q/{id} route.
[ApiController]
[Route("share/q")]
[AllowAnonymous]
public class ShareController : ControllerBase
{
    private readonly QuestionService _questionService;
    private readonly IConfiguration _config;

    public ShareController(QuestionService questionService, IConfiguration config)
    {
        _questionService = questionService;
        _config = config;
    }

    [HttpGet("{id}")]
    public async Task<ContentResult> Get(Guid id, CancellationToken ct)
    {
        QuestionDto? question;
        try
        {
            question = await _questionService.GetQuestionDetailAsync(id, viewerIp: null, ct);
        }
        catch (NotFoundException)
        {
            question = null;
        }

        var frontendOrigin = (_config["AllowedFrontendOrigin"] ?? string.Empty).TrimEnd('/');
        var targetUrl = $"{frontendOrigin}/q/{id}";

        var who = question is null
            ? "Ask Me"
            : question.IsAnonymous
                ? "Someone asked anonymously"
                : $"{question.AuthorDisplayName ?? question.AuthorUsername} asked";

        var description = question is null
            ? "This question isn't available."
            : Truncate(question.Content, 200);

        // Only set og:image when the answer actually has one - a broken/missing
        // image tag looks worse than no image tag on most platforms' previews.
        var imageTag = question?.Answer?.ImageUrl is { } imageUrl
            ? $"""<meta property="og:image" content="{Encode(imageUrl)}" />"""
            : string.Empty;

        var html = $$"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8" />
              <title>{{Encode(who)}} · Ask Me</title>
              <meta property="og:type" content="article" />
              <meta property="og:site_name" content="Ask Me" />
              <meta property="og:title" content="{{Encode(who)}}" />
              <meta property="og:description" content="{{Encode(description)}}" />
              <meta property="og:url" content="{{Encode(targetUrl)}}" />
              {{imageTag}}
              <meta name="twitter:card" content="summary_large_image" />
              <meta http-equiv="refresh" content="0; url={{Encode(targetUrl)}}" />
            </head>
            <body>
              <p>Redirecting to <a href="{{Encode(targetUrl)}}">{{Encode(targetUrl)}}</a>...</p>
              <script>window.location.replace({{System.Text.Json.JsonSerializer.Serialize(targetUrl)}});</script>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
    private static string Encode(string s) => WebUtility.HtmlEncode(s);
}
