using MatchMaking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController(
    IMatchSearchService matchSearchService,
    IMatchQueryService matchQueryService) : ControllerBase
{
    /// <summary>
    /// Request to search for a new match.
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new ProblemDetails { Title = "userId is required", Status = 400 });

        var result = await matchSearchService.RequestMatchSearchAsync(userId.Trim(), cancellationToken);

        return result.Status switch
        {
            MatchSearchStatus.Accepted => NoContent(),
            MatchSearchStatus.AlreadyPending => NoContent(),
            _ => NoContent()
        };
    }

    /// <summary>
    /// Retrieve match information for the user's last successful search request.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MatchInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMatch([FromQuery] string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new ProblemDetails { Title = "userId is required", Status = 400 });

        var match = await matchQueryService.GetMatchByUserIdAsync(userId.Trim(), cancellationToken);
        if (match is null)
            return NotFound();

        return Ok(new MatchInfoResponse(match.MatchId, match.UserIds));
    }
}

public sealed record MatchInfoResponse(string MatchId, IReadOnlyList<string> UserIds);
