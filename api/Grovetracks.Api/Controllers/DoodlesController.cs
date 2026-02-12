using Grovetracks.Api.Models;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grovetracks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoodlesController(
    IQuickdrawDoodleRepository doodleRepository,
    ICompositionMapper compositionMapper,
    IDoodleEngagementRepository engagementRepository) : ControllerBase
{
    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 72;
    private const int MaxConcurrentS3Fetches = 6;

    [HttpGet("words", Name = "get-distinct-words")]
    public async Task<IReadOnlyList<string>> GetDistinctWords(
        CancellationToken cancellationToken)
    {
        return await doodleRepository.GetDistinctWordsAsync(cancellationToken);
    }

    [HttpGet("word/{word}", Name = "get-gallery-page")]
    public async Task<GalleryPageResponse> GetGalleryPage(
        string word,
        [FromQuery] int limit = DefaultPageSize,
        [FromQuery] bool excludeEngaged = false,
        CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, MaxPageSize);
        var fetchLimit = clampedLimit + 1;

        IReadOnlyList<QuickdrawDoodle> doodles;

        if (excludeEngaged)
        {
            var engagedKeys = await engagementRepository.GetEngagedKeyIdsAsync(cancellationToken);
            doodles = await doodleRepository.GetByWordExcludingKeysAsync(
                word, fetchLimit, engagedKeys, cancellationToken);
        }
        else
        {
            doodles = await doodleRepository.GetByWordAsync(
                word, fetchLimit, cancellationToken);
        }

        var hasMore = doodles.Count > clampedLimit;
        var pageItems = hasMore ? doodles.Take(clampedLimit).ToList() : doodles;

        using var semaphore = new SemaphoreSlim(MaxConcurrentS3Fetches);

        var compositionTasks = pageItems.Select(async doodle =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var composition = await compositionMapper.MapToCompositionAsync(
                    doodle, cancellationToken);
                return new DoodleWithCompositionResponse
                {
                    Doodle = MapToSummary(doodle),
                    Composition = MapToResponse(composition)
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var items = await Task.WhenAll(compositionTasks);

        return new GalleryPageResponse
        {
            Items = items.ToList().AsReadOnly(),
            TotalCount = items.Length,
            HasMore = hasMore
        };
    }

    [HttpGet("{keyId}/composition", Name = "get-doodle-composition")]
    public async Task<ActionResult<DoodleWithCompositionResponse>> GetComposition(
        string keyId,
        CancellationToken cancellationToken)
    {
        var doodle = await doodleRepository.GetByKeyIdAsync(keyId, cancellationToken);
        if (doodle is null)
            return NotFound();

        var composition = await compositionMapper.MapToCompositionAsync(
            doodle, cancellationToken);

        return new DoodleWithCompositionResponse
        {
            Doodle = MapToSummary(doodle),
            Composition = MapToResponse(composition)
        };
    }

    private static DoodleSummaryResponse MapToSummary(QuickdrawDoodle doodle) => new()
    {
        KeyId = doodle.KeyId,
        Word = doodle.Word,
        CountryCode = doodle.CountryCode,
        Timestamp = doodle.Timestamp,
        Recognized = doodle.Recognized
    };

    private static CompositionResponse MapToResponse(Composition composition) => new()
    {
        Width = composition.Width,
        Height = composition.Height,
        DoodleFragments = composition.DoodleFragments
            .Select(f => new DoodleFragmentResponse
            {
                Strokes = f.Strokes
                    .Select(s => new StrokeResponse { Data = s.Data })
                    .ToList()
                    .AsReadOnly()
            })
            .ToList()
            .AsReadOnly(),
        Tags = composition.Tags
    };
}
