using System.Text.Json;
using FluentAssertions;
using Grovetracks.DataAccess.Models;
using Grovetracks.Etl.Mappers;
using Grovetracks.Etl.Models;

namespace Grovetracks.Test.Unit.Generation;

public class FewShotExampleMapperTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void MapToAiBatch_SingleComposition_ExtractsXsAndYs()
    {
        var compositions = new List<Composition>
        {
            CreateComposition(
                CreateStroke(new[] { 0.1, 0.3, 0.5 }, new[] { 0.2, 0.4, 0.6 }),
                CreateStroke(new[] { 0.7, 0.9 }, new[] { 0.8, 0.95 }))
        };

        var result = FewShotExampleMapper.MapToAiBatch("cat", compositions);

        result.Compositions.Should().HaveCount(1);
        result.Compositions[0].Subject.Should().Be("cat");
        result.Compositions[0].Strokes.Should().HaveCount(2);
        result.Compositions[0].Strokes[0].Xs.Should().BeEquivalentTo(new[] { 0.1, 0.3, 0.5 });
        result.Compositions[0].Strokes[0].Ys.Should().BeEquivalentTo(new[] { 0.2, 0.4, 0.6 });
        result.Compositions[0].Strokes[1].Xs.Should().BeEquivalentTo(new[] { 0.7, 0.9 });
        result.Compositions[0].Strokes[1].Ys.Should().BeEquivalentTo(new[] { 0.8, 0.95 });
    }

    [Fact]
    public void MapToAiBatch_MultipleCompositions_MapsAll()
    {
        var compositions = new List<Composition>
        {
            CreateComposition(CreateStroke(new[] { 0.1, 0.2 }, new[] { 0.3, 0.4 })),
            CreateComposition(CreateStroke(new[] { 0.5, 0.6 }, new[] { 0.7, 0.8 }))
        };

        var result = FewShotExampleMapper.MapToAiBatch("house", compositions);

        result.Compositions.Should().HaveCount(2);
        result.Compositions[0].Subject.Should().Be("house");
        result.Compositions[1].Subject.Should().Be("house");
        result.Compositions[0].Strokes[0].Xs[0].Should().Be(0.1);
        result.Compositions[1].Strokes[0].Xs[0].Should().Be(0.5);
    }

    [Fact]
    public void MapToAiBatch_MultipleFragments_FlattensStrokes()
    {
        var fragment1 = new DoodleFragment
        {
            Strokes = new List<Stroke>
            {
                CreateStroke(new[] { 0.1, 0.2 }, new[] { 0.3, 0.4 }),
                CreateStroke(new[] { 0.5, 0.6 }, new[] { 0.7, 0.8 })
            }.AsReadOnly()
        };
        var fragment2 = new DoodleFragment
        {
            Strokes = new List<Stroke>
            {
                CreateStroke(new[] { 0.9, 0.95 }, new[] { 0.1, 0.15 })
            }.AsReadOnly()
        };

        var composition = new Composition
        {
            Width = 255,
            Height = 255,
            DoodleFragments = new List<DoodleFragment> { fragment1, fragment2 }.AsReadOnly(),
            Tags = new List<string> { "test" }.AsReadOnly()
        };

        var result = FewShotExampleMapper.MapToAiBatch("tree", new List<Composition> { composition });

        result.Compositions[0].Strokes.Should().HaveCount(3);
        result.Compositions[0].Strokes[0].Xs[0].Should().Be(0.1);
        result.Compositions[0].Strokes[1].Xs[0].Should().Be(0.5);
        result.Compositions[0].Strokes[2].Xs[0].Should().Be(0.9);
    }

    [Fact]
    public void MapToAiBatch_IgnoresTimingData()
    {
        var stroke = new Stroke
        {
            Data = new List<IReadOnlyList<double>>
            {
                new List<double> { 0.1, 0.2, 0.3 }.AsReadOnly(),
                new List<double> { 0.4, 0.5, 0.6 }.AsReadOnly(),
                new List<double> { 100, 200, 300 }.AsReadOnly()
            }.AsReadOnly()
        };

        var composition = new Composition
        {
            Width = 255,
            Height = 255,
            DoodleFragments = new List<DoodleFragment>
            {
                new() { Strokes = new List<Stroke> { stroke }.AsReadOnly() }
            }.AsReadOnly(),
            Tags = new List<string> { "test" }.AsReadOnly()
        };

        var result = FewShotExampleMapper.MapToAiBatch("fish", new List<Composition> { composition });

        result.Compositions[0].Strokes[0].Xs.Should().BeEquivalentTo(new[] { 0.1, 0.2, 0.3 });
        result.Compositions[0].Strokes[0].Ys.Should().BeEquivalentTo(new[] { 0.4, 0.5, 0.6 });
    }

    [Fact]
    public void MapToAiBatch_FiltersStrokesWithTooFewPoints()
    {
        var compositions = new List<Composition>
        {
            CreateComposition(
                CreateStroke(new[] { 0.1 }, new[] { 0.2 }),
                CreateStroke(new[] { 0.3, 0.5 }, new[] { 0.4, 0.6 }),
                CreateStroke(Array.Empty<double>(), Array.Empty<double>()))
        };

        var result = FewShotExampleMapper.MapToAiBatch("bird", compositions);

        result.Compositions[0].Strokes.Should().HaveCount(1);
        result.Compositions[0].Strokes[0].Xs[0].Should().Be(0.3);
    }

    [Fact]
    public void SerializeToJson_ProducesCamelCasePropertyNames()
    {
        var compositions = new List<Composition>
        {
            CreateComposition(CreateStroke(new[] { 0.1, 0.2 }, new[] { 0.3, 0.4 }))
        };

        var batch = FewShotExampleMapper.MapToAiBatch("dog", compositions);
        var json = FewShotExampleMapper.SerializeToJson(batch);

        json.Should().Contain("\"compositions\":");
        json.Should().Contain("\"subject\":");
        json.Should().Contain("\"strokes\":");
        json.Should().Contain("\"xs\":");
        json.Should().Contain("\"ys\":");
        json.Should().NotContain("\"Compositions\":");
        json.Should().NotContain("\"Subject\":");
    }

    [Fact]
    public void SerializeToJson_RoundTripsAsAiCompositionBatch()
    {
        var compositions = new List<Composition>
        {
            CreateComposition(
                CreateStroke(new[] { 0.1, 0.3 }, new[] { 0.2, 0.4 }),
                CreateStroke(new[] { 0.5, 0.7 }, new[] { 0.6, 0.8 }))
        };

        var batch = FewShotExampleMapper.MapToAiBatch("star", compositions);
        var json = FewShotExampleMapper.SerializeToJson(batch);

        var deserialized = JsonSerializer.Deserialize<AiCompositionBatch>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Compositions.Should().HaveCount(1);
        deserialized.Compositions[0].Subject.Should().Be("star");
        deserialized.Compositions[0].Strokes.Should().HaveCount(2);
        deserialized.Compositions[0].Strokes[0].Xs.Should().BeEquivalentTo(new[] { 0.1, 0.3 });
    }

    [Fact]
    public void MapToAiBatch_EmptyCompositionList_ReturnsEmptyBatch()
    {
        var result = FewShotExampleMapper.MapToAiBatch("empty", new List<Composition>());

        result.Compositions.Should().BeEmpty();
    }

    private static Composition CreateComposition(params Stroke[] strokes) =>
        new()
        {
            Width = 255,
            Height = 255,
            DoodleFragments = new List<DoodleFragment>
            {
                new() { Strokes = strokes.ToList().AsReadOnly() }
            }.AsReadOnly(),
            Tags = new List<string> { "test" }.AsReadOnly()
        };

    private static Stroke CreateStroke(double[] xs, double[] ys) =>
        new()
        {
            Data = new List<IReadOnlyList<double>>
            {
                xs.ToList().AsReadOnly(),
                ys.ToList().AsReadOnly(),
                new List<double> { 0 }.AsReadOnly()
            }.AsReadOnly()
        };
}
