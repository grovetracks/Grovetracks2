using Grovetracks.Api.Models;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Grovetracks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EngagementsController(
    IDoodleEngagementRepository engagementRepository) : ControllerBase
{
    private static readonly HashSet<double> ValidScores = [0.0, 0.25, 1.0];

    [HttpPost(Name = "create-engagement")]
    public async Task<ActionResult<EngagementResponse>> CreateEngagement(
        [FromBody] CreateEngagementRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.KeyId))
            return BadRequest("KeyId is required.");

        if (!ValidScores.Contains(request.Score))
            return BadRequest("Score must be 0.0, 0.25, or 1.0.");

        var engagement = new DoodleEngagement
        {
            KeyId = request.KeyId,
            Score = request.Score,
            EngagedAt = DateTime.UtcNow
        };

        var result = await engagementRepository.UpsertAsync(engagement, cancellationToken);

        return CreatedAtAction(null, new EngagementResponse
        {
            KeyId = result.KeyId,
            Score = result.Score,
            EngagedAt = result.EngagedAt
        });
    }
}
