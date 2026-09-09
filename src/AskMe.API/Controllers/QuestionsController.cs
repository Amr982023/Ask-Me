using System.Security.Claims;
using AskMe.Application.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AskMe.API.Controllers;

[ApiController]
[Route("api")]
public class QuestionsController : ControllerBase
{
    private readonly QuestionService _questionService;

    public QuestionsController(QuestionService questionService) => _questionService = questionService;

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string ViewerIp() =>
        Request.Headers.TryGetValue("X-Forwarded-For", out var f) && !string.IsNullOrWhiteSpace(f)
            ? f.ToString().Split(',')[0].Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    [HttpPost("users/{username}/questions")]
    [AllowAnonymous]
    [EnableRateLimiting("submit")]
    public async Task<IActionResult> Submit(string username, SubmitQuestionRequest request, CancellationToken ct)
    {
        var id = await _questionService.SubmitQuestionAsync(username, request, CurrentUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("users/{username}/questions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeed(
        string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] QuestionSort sort = QuestionSort.Votes, [FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await _questionService.GetPublicFeedAsync(username, page, pageSize, ViewerIp(), sort, search, ct));

    [HttpGet("questions/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<QuestionDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _questionService.GetQuestionDetailAsync(id, ViewerIp(), ct));

    [HttpGet("me/questions")]
    [Authorize]
    public async Task<IActionResult> GetMyQuestions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] QuestionSort sort = QuestionSort.Votes, CancellationToken ct = default)
        => Ok(await _questionService.GetMyQuestionsAsync(CurrentUserId()!.Value, page, pageSize, sort, ct));

    [HttpGet("me/answers")]
    [Authorize]
    public async Task<IActionResult> GetMyAnswers([FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await _questionService.GetMyAnswersAsync(CurrentUserId()!.Value, search, ct));

    [HttpPatch("questions/{id}/hide")]
    [Authorize]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct)
    {
        await _questionService.HideQuestionAsync(id, CurrentUserId()!.Value, ct);
        return NoContent();
    }

    [HttpPatch("questions/{id}/unhide")]
    [Authorize]
    public async Task<IActionResult> Unhide(Guid id, CancellationToken ct)
    {
        await _questionService.UnhideQuestionAsync(id, CurrentUserId()!.Value, ct);
        return NoContent();
    }

    [HttpDelete("questions/{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _questionService.DeleteQuestionAsync(id, CurrentUserId()!.Value, ct);
        return NoContent();
    }

    [HttpPost("questions/{id}/answer")]
    [Authorize]
    public async Task<IActionResult> Answer(Guid id, SubmitAnswerRequest request, CancellationToken ct)
    {
        await _questionService.AnswerQuestionAsync(id, CurrentUserId()!.Value, request, ct);
        return NoContent();
    }

    [HttpPost("questions/answer-image")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAnswerImage(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "A file is required." });

        await using var stream = file.OpenReadStream();
        var key = await _questionService.UploadAnswerImageAsync(stream, file.FileName, file.ContentType, ct);
        return Ok(new { imageKey = key });
    }

    [HttpPost("questions/{id}/follow-up")]
    [AllowAnonymous]
    [EnableRateLimiting("submit")]
    public async Task<IActionResult> SubmitFollowUp(Guid id, SubmitFollowUpRequest request, CancellationToken ct)
        => Ok(await _questionService.SubmitFollowUpAsync(id, request, ct));
}
