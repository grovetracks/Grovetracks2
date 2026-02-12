using System.Text.Json;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Services;

public class SimpleCompositionMapper : ISimpleCompositionMapper
{
    private const double MaxCoordinate = 255.0;
    private const int CoordinatePrecision = 3;

    public Composition MapToComposition(QuickdrawSimpleDoodle doodle)
    {
        var rawStrokes = JsonSerializer.Deserialize<List<List<List<int>>>>(doodle.Drawing)
            ?? throw new InvalidOperationException($"Failed to deserialize drawing for {doodle.KeyId}");

        var strokes = rawStrokes
            .Select(stroke => new Stroke
            {
                Data = new List<IReadOnlyList<double>>
                {
                    stroke[0]
                        .Select(x => Math.Round(x / MaxCoordinate, CoordinatePrecision))
                        .ToList()
                        .AsReadOnly(),
                    stroke[1]
                        .Select(y => Math.Round(y / MaxCoordinate, CoordinatePrecision))
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
            Width = 255,
            Height = 255,
            DoodleFragments = [doodleFragment],
            Tags = ["quickdraw-simple", doodle.Word]
        };
    }
}
