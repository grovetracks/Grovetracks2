using Grovetracks.DataAccess.Models;

namespace Grovetracks.Test.Unit.Generation;

internal static class TestCompositionFactory
{
    internal static Composition CreateSimple(
        double[] xs,
        double[] ys,
        string word = "cat",
        int width = 255,
        int height = 255)
    {
        var stroke = new Stroke
        {
            Data = new List<IReadOnlyList<double>>
            {
                xs.ToList().AsReadOnly(),
                ys.ToList().AsReadOnly(),
                new List<double> { 0 }.AsReadOnly()
            }.AsReadOnly()
        };

        return new Composition
        {
            Width = width,
            Height = height,
            DoodleFragments = new List<DoodleFragment>
            {
                new() { Strokes = new List<Stroke> { stroke }.AsReadOnly() }
            }.AsReadOnly(),
            Tags = new List<string> { "test", word }.AsReadOnly()
        };
    }

    internal static Composition CreateMultiStroke(
        int strokeCount = 7,
        int pointsPerStroke = 12,
        string word = "cat")
    {
        var rng = new Random(42);
        var strokes = new List<Stroke>();

        for (var i = 0; i < strokeCount; i++)
        {
            var xs = Enumerable.Range(0, pointsPerStroke)
                .Select(_ => Math.Round(rng.NextDouble() * 0.8 + 0.1, 3))
                .ToList();
            var ys = Enumerable.Range(0, pointsPerStroke)
                .Select(_ => Math.Round(rng.NextDouble() * 0.8 + 0.1, 3))
                .ToList();

            strokes.Add(new Stroke
            {
                Data = new List<IReadOnlyList<double>>
                {
                    xs.AsReadOnly(),
                    ys.AsReadOnly(),
                    new List<double> { 0 }.AsReadOnly()
                }.AsReadOnly()
            });
        }

        return new Composition
        {
            Width = 255,
            Height = 255,
            DoodleFragments = new List<DoodleFragment>
            {
                new() { Strokes = strokes.AsReadOnly() }
            }.AsReadOnly(),
            Tags = new List<string> { "test", word }.AsReadOnly()
        };
    }
}
