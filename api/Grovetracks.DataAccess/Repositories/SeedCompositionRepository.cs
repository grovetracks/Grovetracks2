using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grovetracks.DataAccess.Repositories;

public class SeedCompositionRepository(AppDbContext dbContext) : ISeedCompositionRepository
{
    public async Task<IReadOnlyList<SeedComposition>> GetByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SeedCompositions
            .Where(s => s.Word == word)
            .OrderByDescending(s => s.QualityScore)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeedComposition>> GetByWordAsync(
        string word,
        string? sourceType,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.Where(s => s.Word == word);

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        return await query
            .OrderByDescending(s => s.QualityScore)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeedComposition>> GetByWordAsync(
        string word,
        string? sourceType,
        string? generationMethod,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.Where(s => s.Word == word);

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        if (!string.IsNullOrEmpty(generationMethod))
            query = query.Where(s => s.GenerationMethod == generationMethod);

        return await query
            .OrderByDescending(s => s.QualityScore)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<SeedComposition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SeedCompositions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SeedCompositions
            .Select(s => s.Word)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        string? sourceType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.AsQueryable();

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        return await query
            .Select(s => s.Word)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctWordsAsync(
        string? sourceType,
        string? generationMethod,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.AsQueryable();

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        if (!string.IsNullOrEmpty(generationMethod))
            query = query.Where(s => s.GenerationMethod == generationMethod);

        return await query
            .Select(s => s.Word)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByWordAsync(
        string word,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SeedCompositions
            .Where(s => s.Word == word)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByWordAsync(
        string word,
        string? sourceType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.Where(s => s.Word == word);

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByWordAsync(
        string word,
        string? sourceType,
        string? generationMethod,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.Where(s => s.Word == word);

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        if (!string.IsNullOrEmpty(generationMethod))
            query = query.Where(s => s.GenerationMethod == generationMethod);

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyList<SeedComposition> compositions,
        CancellationToken cancellationToken = default)
    {
        dbContext.SeedCompositions.AddRange(compositions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SeedCompositions
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        string? sourceType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.AsQueryable();

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        string? sourceType,
        string? generationMethod,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.AsQueryable();

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        if (!string.IsNullOrEmpty(generationMethod))
            query = query.Where(s => s.GenerationMethod == generationMethod);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctGenerationMethodsAsync(
        string? sourceType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SeedCompositions.AsQueryable();

        if (!string.IsNullOrEmpty(sourceType))
            query = query.Where(s => s.SourceType == sourceType);

        return await query
            .Where(s => s.GenerationMethod != null)
            .Select(s => s.GenerationMethod!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeedComposition>> GetCuratedByWordAsync(
        string word,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SeedCompositions
            .Where(s => s.Word == word && s.SourceType == "curated")
            .OrderByDescending(s => s.QualityScore)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteBySourceTypeAsync(
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SeedCompositions
            .Where(s => s.SourceType == sourceType)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
