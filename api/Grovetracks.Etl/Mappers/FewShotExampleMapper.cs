using System.Text.Json;
using Grovetracks.DataAccess.Models;
using Grovetracks.Etl.Models;

namespace Grovetracks.Etl.Mappers;

public static class FewShotExampleMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static AiCompositionBatch MapToAiBatch(
        string subject,
        IReadOnlyList<Composition> compositions)
    {
        var aiCompositions = compositions
            .Select(c => new AiComposition
            {
                Subject = subject,
                Strokes = c.DoodleFragments
                    .SelectMany(f => f.Strokes)
                    .Where(s => s.Data.Count >= 2 && s.Data[0].Count >= 2)
                    .Select(s => new AiStroke
                    {
                        Xs = s.Data[0],
                        Ys = s.Data[1]
                    })
                    .ToList()
                    .AsReadOnly()
            })
            .ToList()
            .AsReadOnly();

        return new AiCompositionBatch { Compositions = aiCompositions };
    }

    public static string SerializeToJson(AiCompositionBatch batch) =>
        JsonSerializer.Serialize(batch, JsonOptions);
}
