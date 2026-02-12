using FluentAssertions;
using Grovetracks.DataAccess.Generation;

namespace Grovetracks.Test.Unit.Generation;

public class SceneComposerTests
{
    private readonly SceneComposer _composer = new();

    [Fact]
    public void ComposeScene_DuoHorizontal_CreatesTwoFragments()
    {
        var templates = SceneTemplateProvider.GetAll();
        var duoTemplate = templates.First(t => t.Name == "duo-horizontal");

        var cat = TestCompositionFactory.CreateMultiStroke(word: "cat");
        var dog = TestCompositionFactory.CreateMultiStroke(word: "dog");

        var result = _composer.ComposeScene(duoTemplate, [(cat, "cat"), (dog, "dog")]);

        result.DoodleFragments.Count.Should().Be(2);
    }

    [Fact]
    public void ComposeScene_TagsIncludeGeneratedAndSceneAndCategories()
    {
        var templates = SceneTemplateProvider.GetAll();
        var duoTemplate = templates.First(t => t.Name == "duo-horizontal");

        var cat = TestCompositionFactory.CreateMultiStroke(word: "cat");
        var dog = TestCompositionFactory.CreateMultiStroke(word: "dog");

        var result = _composer.ComposeScene(duoTemplate, [(cat, "cat"), (dog, "dog")]);

        result.Tags.Should().Contain("generated");
        result.Tags.Should().Contain("scene");
        result.Tags.Should().Contain("duo-horizontal");
        result.Tags.Should().Contain("cat");
        result.Tags.Should().Contain("dog");
    }

    [Fact]
    public void ComposeScene_AllCoordinatesInBounds()
    {
        var templates = SceneTemplateProvider.GetAll();

        foreach (var template in templates)
        {
            var assignments = template.Slots
                .Select((_, i) => (TestCompositionFactory.CreateMultiStroke(word: $"word{i}"), $"word{i}"))
                .ToList();

            var result = _composer.ComposeScene(template, assignments);

            foreach (var fragment in result.DoodleFragments)
            {
                foreach (var stroke in fragment.Strokes)
                {
                    foreach (var x in stroke.Data[0])
                        x.Should().BeInRange(0.0, 1.0, $"X out of bounds in template {template.Name}");
                    foreach (var y in stroke.Data[1])
                        y.Should().BeInRange(0.0, 1.0, $"Y out of bounds in template {template.Name}");
                }
            }
        }
    }

    [Fact]
    public void ComposeScene_QuadGrid_CreatesFourFragments()
    {
        var templates = SceneTemplateProvider.GetAll();
        var quadTemplate = templates.First(t => t.Name == "quad-grid");

        var comps = Enumerable.Range(0, 4)
            .Select(i => (TestCompositionFactory.CreateMultiStroke(word: $"w{i}"), $"w{i}"))
            .ToList();

        var result = _composer.ComposeScene(quadTemplate, comps);

        result.DoodleFragments.Count.Should().Be(4);
    }

    [Fact]
    public void ComposeScene_WrongSlotCount_Throws()
    {
        var templates = SceneTemplateProvider.GetAll();
        var duoTemplate = templates.First(t => t.Name == "duo-horizontal");

        var act = () => _composer.ComposeScene(duoTemplate,
            [(TestCompositionFactory.CreateMultiStroke(), "cat")]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ComposeScene_WidthAndHeight_Are255()
    {
        var templates = SceneTemplateProvider.GetAll();
        var duoTemplate = templates.First(t => t.Name == "duo-horizontal");

        var cat = TestCompositionFactory.CreateMultiStroke(word: "cat");
        var dog = TestCompositionFactory.CreateMultiStroke(word: "dog");

        var result = _composer.ComposeScene(duoTemplate, [(cat, "cat"), (dog, "dog")]);

        result.Width.Should().Be(255);
        result.Height.Should().Be(255);
    }
}
