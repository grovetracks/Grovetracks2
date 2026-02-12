using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation;

public static class CompositionValidator
{
    private const double MinBoundingBoxCoverage = 0.10;
    private const int MinTotalPoints = 5;

    public static (bool IsValid, double QualityScore) Validate(Composition composition)
    {
        if (composition.DoodleFragments.Count == 0)
            return (false, 0);

        var totalStrokes = CompositionGeometry.CountTotalStrokes(composition);
        if (totalStrokes == 0)
            return (false, 0);

        var totalPoints = CompositionGeometry.CountTotalPoints(composition);
        if (totalPoints < MinTotalPoints)
            return (false, 0);

        foreach (var fragment in composition.DoodleFragments)
        {
            foreach (var stroke in fragment.Strokes)
            {
                var xs = stroke.Data[0];
                var ys = stroke.Data[1];
                for (var i = 0; i < xs.Count; i++)
                {
                    if (xs[i] < 0 || xs[i] > 1 || ys[i] < 0 || ys[i] > 1)
                        return (false, 0);
                }
            }
        }

        var (minX, minY, maxX, maxY) = CompositionGeometry.ComputeBoundingBox(composition);
        var bboxCoverage = (maxX - minX) * (maxY - minY);
        if (bboxCoverage < MinBoundingBoxCoverage)
            return (false, 0);

        var strokeScore = totalStrokes <= 30
            ? 1.0 - Math.Abs(totalStrokes - 7.0) / 20.0
            : 0.8;
        var pointScore = totalPoints <= 200
            ? 1.0 - Math.Abs(totalPoints - 80.0) / 500.0
            : Math.Min(1.0, 0.7 + totalPoints / 5000.0);
        var coverageScore = Math.Min(bboxCoverage / 0.6, 1.0);
        var balanceScore = 1.0 - Math.Abs((maxX - minX) - (maxY - minY));

        var score = Math.Max(0, (strokeScore * 0.15) + (pointScore * 0.15) + (coverageScore * 0.40) + (balanceScore * 0.30));
        return (true, Math.Round(score, 4));
    }
}
