using AskMe.Application.Votes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AskMe.API.Controllers;

[ApiController]
[Route("api/questions")]
[AllowAnonymous]
[EnableRateLimiting("vote")]
public class VotesController : ControllerBase
{
    private readonly VoteService _voteService;

    public VotesController(VoteService voteService) => _voteService = voteService;

    [HttpPost("{id}/vote")]
    public async Task<IActionResult> Vote(Guid id, CancellationToken ct)
    {
        var newCount = await _voteService.VoteAsync(id, ct);
        return Ok(new { voteCount = newCount });
    }

    [HttpDelete("{id}/vote")]
    public async Task<IActionResult> Unvote(Guid id, CancellationToken ct)
    {
        var newCount = await _voteService.UnvoteAsync(id, ct);
        return Ok(new { voteCount = newCount });
    }
}
