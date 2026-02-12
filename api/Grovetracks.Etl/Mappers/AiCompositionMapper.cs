using Grovetracks.DataAccess.Generation;
using Grovetracks.DataAccess.Models;
using Grovetracks.Etl.Models;

namespace Grovetracks.Etl.Mappers;

public static class AiCompositionMapper
{
    private const int CanvasSize = 255;

    public static Composition MapToComposition(AiComposition source, string generationMethod)
    {
        var strokes = source.Strokes
            .Where(s => s.Xs.Count >= 2 && s.Xs.Count == s.Ys.Count)
            .Select(s => new Stroke
            {
                Data = new List<IReadOnlyList<double>>
                {
                    s.Xs.Select(x => CompositionGeometry.RoundCoordinate(CompositionGeometry.ClampCoordinate(x)))
                        .ToList().AsReadOnly(),
                    s.Ys.Select(y => CompositionGeometry.RoundCoordinate(CompositionGeometry.ClampCoordinate(y)))
                        .ToList().AsReadOnly(),
                    new List<double> { 0 }.AsReadOnly()
                }.AsReadOnly()
            })
            .ToList();

        return new Composition
        {
            Width = CanvasSize,
            Height = CanvasSize,
            DoodleFragments = new List<DoodleFragment>
            {
                new() { Strokes = strokes.AsReadOnly() }
            }.AsReadOnly(),
            Tags = new List<string> { "ai-generated", generationMethod, source.Subject }.AsReadOnly()
        };
    }
}
