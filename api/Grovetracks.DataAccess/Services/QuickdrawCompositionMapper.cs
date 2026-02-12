using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Services;

public class QuickdrawCompositionMapper(IDrawingStorageService drawingStorageService) : ICompositionMapper
{
    private const double MarginFactor = 1.2;
    private const int CoordinatePrecision = 3;

    public async Task<Composition> MapToCompositionAsync(
        QuickdrawDoodle doodle,
        CancellationToken cancellationToken = default)
    {
        var rawDrawing = await drawingStorageService.GetDrawingAsync(
            doodle.DrawingReference, cancellationToken);

        return MapToComposition(doodle, rawDrawing);
    }

    private static Composition MapToComposition(QuickdrawDoodle doodle, RawDrawing rawDrawing)
    {
        var maxX = rawDrawing.Strokes.SelectMany(s => s.Xs).Max();
        var maxY = rawDrawing.Strokes.SelectMany(s => s.Ys).Max();

        var width = (int)maxX;
        var height = (int)maxY;

        var strokes = rawDrawing.Strokes
            .Select(rawStroke => new Stroke
            {
                Data = new List<IReadOnlyList<double>>
                {
                    rawStroke.Xs
                        .Select(x => Math.Round(x / (maxX * MarginFactor), CoordinatePrecision))
                        .ToList()
                        .AsReadOnly(),
                    rawStroke.Ys
                        .Select(y => Math.Round(y / (maxY * MarginFactor), CoordinatePrecision))
                        .ToList()
                        .AsReadOnly(),
                    new List<double> { 0 }.AsReadOnly()
                }.AsReadOnly()
            })
            .ToList()
            .AsReadOnly();

        var doodleFragment = new DoodleFragment
        {
            Strokes = strokes
        };

        return new Composition
        {
            Width = width,
            Height = height,
            DoodleFragments = [doodleFragment],
            Tags = ["quickdraw", doodle.Word]
        };
    }
}
