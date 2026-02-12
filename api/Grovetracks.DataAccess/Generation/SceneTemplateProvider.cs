namespace Grovetracks.DataAccess.Generation;

public static class SceneTemplateProvider
{
    private static readonly IReadOnlyList<SceneTemplate> Templates =
    [
        new SceneTemplate
        {
            Name = "duo-horizontal",
            Slots =
            [
                new SceneSlot { X = 0.0, Y = 0.05, Width = 0.48, Height = 0.9 },
                new SceneSlot { X = 0.52, Y = 0.05, Width = 0.48, Height = 0.9 }
            ]
        },
        new SceneTemplate
        {
            Name = "duo-vertical",
            Slots =
            [
                new SceneSlot { X = 0.05, Y = 0.0, Width = 0.9, Height = 0.48 },
                new SceneSlot { X = 0.05, Y = 0.52, Width = 0.9, Height = 0.48 }
            ]
        },
        new SceneTemplate
        {
            Name = "trio-triangle",
            Slots =
            [
                new SceneSlot { X = 0.25, Y = 0.0, Width = 0.5, Height = 0.48 },
                new SceneSlot { X = 0.0, Y = 0.52, Width = 0.48, Height = 0.48 },
                new SceneSlot { X = 0.52, Y = 0.52, Width = 0.48, Height = 0.48 }
            ]
        },
        new SceneTemplate
        {
            Name = "quad-grid",
            Slots =
            [
                new SceneSlot { X = 0.0, Y = 0.0, Width = 0.48, Height = 0.48 },
                new SceneSlot { X = 0.52, Y = 0.0, Width = 0.48, Height = 0.48 },
                new SceneSlot { X = 0.0, Y = 0.52, Width = 0.48, Height = 0.48 },
                new SceneSlot { X = 0.52, Y = 0.52, Width = 0.48, Height = 0.48 }
            ]
        },
        new SceneTemplate
        {
            Name = "featured-with-accents",
            Slots =
            [
                new SceneSlot { X = 0.15, Y = 0.15, Width = 0.7, Height = 0.7, FillFactor = 0.85 },
                new SceneSlot { X = 0.0, Y = 0.0, Width = 0.28, Height = 0.28, FillFactor = 0.65 },
                new SceneSlot { X = 0.72, Y = 0.0, Width = 0.28, Height = 0.28, FillFactor = 0.65 }
            ]
        }
    ];

    public static IReadOnlyList<SceneTemplate> GetAll() => Templates;

    public static SceneTemplate GetRandom(Random rng) => Templates[rng.Next(Templates.Count)];
}
