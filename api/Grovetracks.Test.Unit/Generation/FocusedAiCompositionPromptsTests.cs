using FluentAssertions;
using Grovetracks.Etl.Data;

namespace Grovetracks.Test.Unit.Generation;

public class FocusedAiCompositionPromptsTests
{
    [Fact]
    public void BuildMessages_NoFewShot_ReturnsSystemAndUserMessages()
    {
        var pairs = new List<(string, string)>();

        var result = FocusedAiCompositionPrompts.BuildMessages("angel", 3, pairs);

        result.Should().HaveCount(2);
        GetRole(result[0]).Should().Be("system");
        GetRole(result[1]).Should().Be("user");
        GetContent(result[1]).Should().Contain("angel");
    }

    [Fact]
    public void BuildMessages_OneFewShotPair_ReturnsCorrectMessageOrder()
    {
        var pairs = new List<(string, string)>
        {
            ("Draw 2 variations of: angel", "{\"compositions\":[]}")
        };

        var result = FocusedAiCompositionPrompts.BuildMessages("angel", 3, pairs);

        result.Should().HaveCount(4);
        GetRole(result[0]).Should().Be("system");
        GetRole(result[1]).Should().Be("user");
        GetContent(result[1]).Should().Be("Draw 2 variations of: angel");
        GetRole(result[2]).Should().Be("assistant");
        GetContent(result[2]).Should().Contain("compositions");
        GetRole(result[3]).Should().Be("user");
        GetContent(result[3]).Should().Contain("angel");
    }

    [Fact]
    public void BuildMessages_MultipleFewShotPairs_IncludesAllInOrder()
    {
        var pairs = new List<(string, string)>
        {
            ("first-prompt", "first-response"),
            ("second-prompt", "second-response"),
            ("third-prompt", "third-response")
        };

        var result = FocusedAiCompositionPrompts.BuildMessages("cat", 2, pairs);

        result.Should().HaveCount(8);
        GetRole(result[0]).Should().Be("system");
        GetContent(result[1]).Should().Be("first-prompt");
        GetContent(result[2]).Should().Be("first-response");
        GetContent(result[3]).Should().Be("second-prompt");
        GetContent(result[4]).Should().Be("second-response");
        GetContent(result[5]).Should().Be("third-prompt");
        GetContent(result[6]).Should().Be("third-response");
        GetRole(result[7]).Should().Be("user");
    }

    [Fact]
    public void BuildMessages_SystemPromptIsFirst()
    {
        var pairs = new List<(string, string)>
        {
            ("prompt", "response")
        };

        var result = FocusedAiCompositionPrompts.BuildMessages("tree", 1, pairs);

        GetRole(result[0]).Should().Be("system");
        GetContent(result[0]).Should().Contain("focused on drawing one specific subject");
    }

    [Fact]
    public void BuildMessages_LastMessageContainsSubject()
    {
        var pairs = new List<(string, string)>
        {
            ("prompt", "response")
        };

        var result = FocusedAiCompositionPrompts.BuildMessages("dragon", 5, pairs);

        var lastMessage = result[^1];
        GetRole(lastMessage).Should().Be("user");
        GetContent(lastMessage).Should().Contain("dragon");
        GetContent(lastMessage).Should().Contain("5");
    }

    [Fact]
    public void BuildUserPrompt_SingularForOne()
    {
        var result = FocusedAiCompositionPrompts.BuildUserPrompt("angel", 1);

        result.Should().StartWith("Draw 1 distinct variation of: angel");
    }

    [Fact]
    public void BuildUserPrompt_PluralForMultiple()
    {
        var result = FocusedAiCompositionPrompts.BuildUserPrompt("angel", 3);

        result.Should().StartWith("Draw 3 distinct variations of: angel");
    }

    [Fact]
    public void FocusedSystemPrompt_ContainsKeyInstructions()
    {
        FocusedAiCompositionPrompts.FocusedSystemPrompt.Should().Contain("Study the example drawings carefully");
        FocusedAiCompositionPrompts.FocusedSystemPrompt.Should().Contain("0.0 and 1.0");
        FocusedAiCompositionPrompts.FocusedSystemPrompt.Should().Contain("DO NOT");
    }

    private static string GetRole(object message) =>
        message.GetType().GetProperty("role")!.GetValue(message)!.ToString()!;

    private static string GetContent(object message) =>
        message.GetType().GetProperty("content")!.GetValue(message)!.ToString()!;
}
