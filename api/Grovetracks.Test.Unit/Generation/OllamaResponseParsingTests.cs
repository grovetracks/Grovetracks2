using System.Text.Json;
using FluentAssertions;
using Grovetracks.Etl.Models;

namespace Grovetracks.Test.Unit.Generation;

public class OllamaResponseParsingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Deserialize_ValidChatResponse_ParsesCorrectly()
    {
        var json = """
            {
                "message": { "role": "assistant", "content": "hello world" },
                "done": true
            }
            """;

        var result = JsonSerializer.Deserialize<OllamaChatResponse>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.Message.Role.Should().Be("assistant");
        result.Message.Content.Should().Be("hello world");
        result.Done.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_ResponseNotDone_ParsesDoneAsFalse()
    {
        var json = """
            {
                "message": { "role": "assistant", "content": "partial" },
                "done": false
            }
            """;

        var result = JsonSerializer.Deserialize<OllamaChatResponse>(json, JsonOptions);

        result.Should().NotBeNull();
        result!.Done.Should().BeFalse();
    }

    [Fact]
    public void ExtractContent_AndDeserializeAsBatch_ParsesCompositions()
    {
        var compositionJson = """
            {
                "compositions": [
                    {
                        "subject": "cat",
                        "strokes": [
                            { "xs": [0.1, 0.3, 0.5], "ys": [0.2, 0.4, 0.6] },
                            { "xs": [0.6, 0.8], "ys": [0.7, 0.9] }
                        ]
                    }
                ]
            }
            """;

        var chatResponseJson = JsonSerializer.Serialize(new
        {
            message = new { role = "assistant", content = compositionJson },
            done = true
        });

        var chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(chatResponseJson, JsonOptions);
        chatResponse.Should().NotBeNull();

        var batch = JsonSerializer.Deserialize<AiCompositionBatch>(chatResponse!.Message.Content, JsonOptions);

        batch.Should().NotBeNull();
        batch!.Compositions.Should().HaveCount(1);
        batch.Compositions[0].Subject.Should().Be("cat");
        batch.Compositions[0].Strokes.Should().HaveCount(2);
        batch.Compositions[0].Strokes[0].Xs.Should().BeEquivalentTo(new[] { 0.1, 0.3, 0.5 });
        batch.Compositions[0].Strokes[0].Ys.Should().BeEquivalentTo(new[] { 0.2, 0.4, 0.6 });
    }

    [Fact]
    public void ExtractContent_MultipleCompositions_ParsesAll()
    {
        var compositionJson = """
            {
                "compositions": [
                    {
                        "subject": "tree",
                        "strokes": [{ "xs": [0.1, 0.5], "ys": [0.2, 0.8] }]
                    },
                    {
                        "subject": "tree",
                        "strokes": [{ "xs": [0.3, 0.7], "ys": [0.1, 0.9] }]
                    }
                ]
            }
            """;

        var batch = JsonSerializer.Deserialize<AiCompositionBatch>(compositionJson, JsonOptions);

        batch.Should().NotBeNull();
        batch!.Compositions.Should().HaveCount(2);
        batch.Compositions[0].Strokes[0].Xs[0].Should().Be(0.1);
        batch.Compositions[1].Strokes[0].Xs[0].Should().Be(0.3);
    }

    [Fact]
    public void Deserialize_EmptyCompositionsArray_ParsesAsEmptyList()
    {
        var json = """{ "compositions": [] }""";

        var batch = JsonSerializer.Deserialize<AiCompositionBatch>(json, JsonOptions);

        batch.Should().NotBeNull();
        batch!.Compositions.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_MalformedContent_ReturnsNull()
    {
        var malformed = "this is not json at all";

        var act = () => JsonSerializer.Deserialize<AiCompositionBatch>(malformed, JsonOptions);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_EmptyStrokes_ParsesCorrectly()
    {
        var json = """
            {
                "compositions": [
                    {
                        "subject": "dot",
                        "strokes": []
                    }
                ]
            }
            """;

        var batch = JsonSerializer.Deserialize<AiCompositionBatch>(json, JsonOptions);

        batch.Should().NotBeNull();
        batch!.Compositions[0].Subject.Should().Be("dot");
        batch.Compositions[0].Strokes.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_HighPrecisionCoordinates_PreservesValues()
    {
        var json = """
            {
                "compositions": [
                    {
                        "subject": "test",
                        "strokes": [
                            { "xs": [0.123456789, 0.987654321], "ys": [0.111111111, 0.999999999] }
                        ]
                    }
                ]
            }
            """;

        var batch = JsonSerializer.Deserialize<AiCompositionBatch>(json, JsonOptions);

        batch.Should().NotBeNull();
        batch!.Compositions[0].Strokes[0].Xs[0].Should().BeApproximately(0.123456789, 1e-9);
        batch.Compositions[0].Strokes[0].Ys[1].Should().BeApproximately(0.999999999, 1e-9);
    }
}
