using FluentAssertions;
using Grovetracks.Etl.Data;

namespace Grovetracks.Test.Unit.Generation;

public class ComposableSubjectsTests
{
    [Fact]
    public void Subjects_HasExpectedCount()
    {
        ComposableSubjects.All.Count.Should().BeGreaterThanOrEqualTo(180);
        ComposableSubjects.All.Count.Should().BeLessThanOrEqualTo(220);
    }

    [Fact]
    public void Subjects_HasNoDuplicates()
    {
        var duplicates = ComposableSubjects.All
            .GroupBy(s => s)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty($"found duplicate subjects: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Subjects_AreNonEmptyStrings()
    {
        foreach (var subject in ComposableSubjects.All)
        {
            subject.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Subjects_AreLowercase()
    {
        foreach (var subject in ComposableSubjects.All)
        {
            subject.Should().Be(subject.ToLowerInvariant(),
                $"subject '{subject}' should be lowercase");
        }
    }
}
