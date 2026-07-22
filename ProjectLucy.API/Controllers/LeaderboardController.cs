using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLucy.Application.Leaderboard.Queries.GetMentorLeaderboard;

namespace ProjectLucy.API.Controllers;

[ApiController]
[Route("api/leaderboard/mentors")]
[Authorize(Policy = "MentorLeaderboardAccess")]
public sealed class LeaderboardController : ControllerBase
{
    private readonly ISender _sender;

    public LeaderboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetMentors(
        [FromQuery] string period = "weekly",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var normalizedPeriod = period.Trim().ToLowerInvariant();
        if (normalizedPeriod is not ("weekly" or "alltime"))
        {
            return BadRequest(new { message = "period must be weekly or alltime" });
        }

        if (page < 1 || pageSize is < 1 or > 50)
        {
            return BadRequest(new { message = "page must be at least 1 and pageSize must be between 1 and 50" });
        }

        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var viewerId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(
            new GetMentorLeaderboardQuery(viewerId, normalizedPeriod, page, pageSize),
            ct);
        return StatusCode(result.Status, result);
    }
}
