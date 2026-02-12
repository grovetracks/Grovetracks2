using System.Text.Json;
using Grovetracks.Api.Models;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grovetracks.Api.Controllers;

[ApiController]
[Route("api/seed-compositions")]
public class SeedCompositionsController(ISeedCompositionRepository repository) : ControllerBase
{
    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 48;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [HttpGet("words", Name = "get-seed-distinct-words")]
    public async Task<IReadOnlyList<string>> GetDistinctWords(
        [FromQuery] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetDistinctWordsAsync(sourceType, cancellationToken);
    }

    [HttpGet("word/{word}", Name = "get-seed-gallery-page")]
    public async Task<SeedCompositionPageResponse> GetByWord(
        string word,
        [FromQuery] int limit = DefaultPageSize,
        [FromQuery] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, MaxPageSize);
        var seeds = await repository.GetByWordAsync(word, sourceType, clampedLimit, cancellationToken);
        var totalCount = await repository.GetCountByWordAsync(word, sourceType, cancellationToken);

        var items = seeds
            .Select(MapToResponse)
            .ToList()
            .AsReadOnly();

        return new SeedCompositionPageResponse
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    [HttpGet("{id:guid}", Name = "get-seed-composition")]
    public async Task<ActionResult<SeedCompositionWithDataResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var seed = await repository.GetByIdAsync(id, cancellationToken);
        if (seed is null)
            return NotFound();

        return MapToResponse(seed);
    }

    [HttpGet("word/{word}/count", Name = "get-seed-word-count")]
    public async Task<int> GetWordCount(
        string word,
        CancellationToken cancellationToken)
    {
        return await repository.GetCountByWordAsync(word, cancellationToken);
    }

    [HttpGet("count", Name = "get-seed-total-count")]
    public async Task<int> GetTotalCount(
        [FromQuery] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetTotalCountAsync(sourceType, cancellationToken);
    }

    private static SeedCompositionWithDataResponse MapToResponse(SeedComposition seed)
    {
        var composition = JsonSerializer.Deserialize<Composition>(seed.CompositionJson, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize composition for {seed.Id}");

        return new SeedCompositionWithDataResponse
        {
            Summary = new SeedCompositionSummaryResponse
            {
                Id = seed.Id,
                Word = seed.Word,
                QualityScore = seed.QualityScore,
                StrokeCount = seed.StrokeCount,
                TotalPointCount = seed.TotalPointCount,
                CuratedAt = seed.CuratedAt,
                SourceType = seed.SourceType,
                GenerationMethod = seed.GenerationMethod
            },
            Composition = new CompositionResponse
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
            }
        };
    }
}
