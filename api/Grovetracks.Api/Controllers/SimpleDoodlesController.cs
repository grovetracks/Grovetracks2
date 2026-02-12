using Grovetracks.Api.Models;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grovetracks.Api.Controllers;

[ApiController]
[Route("api/simple-doodles")]
public class SimpleDoodlesController(
    IQuickdrawSimpleDoodleRepository repository,
    ISimpleCompositionMapper compositionMapper) : ControllerBase
{
    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 48;

    [HttpGet("words", Name = "get-simple-distinct-words")]
    public async Task<IReadOnlyList<string>> GetDistinctWords(
        CancellationToken cancellationToken)
    {
        return await repository.GetDistinctWordsAsync(cancellationToken);
    }

    [HttpGet("word/{word}", Name = "get-simple-gallery-page")]
    public async Task<GalleryPageResponse> GetGalleryPage(
        string word,
        [FromQuery] int limit = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, MaxPageSize);
        var doodles = await repository.GetByWordAsync(word, clampedLimit, cancellationToken);

        var items = doodles
            .Select(doodle => new DoodleWithCompositionResponse
            {
                Doodle = MapToSummary(doodle),
                Composition = MapToResponse(compositionMapper.MapToComposition(doodle))
            })
            .ToList()
            .AsReadOnly();

        return new GalleryPageResponse
        {
            Items = items,
            TotalCount = items.Count,
            HasMore = false
        };
    }

    [HttpGet("{keyId}/composition", Name = "get-simple-doodle-composition")]
    public async Task<ActionResult<DoodleWithCompositionResponse>> GetComposition(
        string keyId,
        CancellationToken cancellationToken)
    {
        var doodle = await repository.GetByKeyIdAsync(keyId, cancellationToken);
        if (doodle is null)
            return NotFound();

        return new DoodleWithCompositionResponse
        {
            Doodle = MapToSummary(doodle),
            Composition = MapToResponse(compositionMapper.MapToComposition(doodle))
        };
    }

    [HttpGet("word/{word}/count", Name = "get-simple-word-count")]
    public async Task<int> GetWordCount(
        string word,
        CancellationToken cancellationToken)
    {
        return await repository.GetCountByWordAsync(word, cancellationToken);
    }

    private static DoodleSummaryResponse MapToSummary(QuickdrawSimpleDoodle doodle) => new()
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
