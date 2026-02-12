using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Generation;

public class SceneComposer
{
    public Composition ComposeScene(
        SceneTemplate template,
        IReadOnlyList<(Composition Source, string Word)> slotAssignments)
    {
        if (slotAssignments.Count != template.Slots.Count)
            throw new ArgumentException(
                $"Expected {template.Slots.Count} slot assignments for template '{template.Name}', got {slotAssignments.Count}");

        var allFragments = new List<DoodleFragment>();
        var tags = new List<string> { "generated", "scene", template.Name };

        for (var i = 0; i < template.Slots.Count; i++)
        {
            var slot = template.Slots[i];
            var (source, word) = slotAssignments[i];

            var placed = CompositionGeometry.PlaceInRegion(
                source,
                slot.X,
                slot.Y,
                slot.Width,
                slot.Height,
                slot.FillFactor);

            allFragments.AddRange(placed.DoodleFragments);

            if (!tags.Contains(word))
                tags.Add(word);
        }

        return new Composition
        {
            Width = 255,
            Height = 255,
            DoodleFragments = allFragments.AsReadOnly(),
            Tags = tags.AsReadOnly()
        };
    }
}
