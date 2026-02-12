using FluentAssertions;
using Grovetracks.Api.Controllers;
using Grovetracks.Api.Models;
using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Grovetracks.Test.Unit.Controllers;

public class EngagementsControllerTests
{
    private readonly IDoodleEngagementRepository _mockRepository;
    private readonly EngagementsController _controller;

    public EngagementsControllerTests()
    {
        _mockRepository = Substitute.For<IDoodleEngagementRepository>();
        _controller = new EngagementsController(_mockRepository);
    }

    [Fact]
    public async Task CreateEngagement_WithValidRequest_ReturnsCreated()
    {
        var request = new CreateEngagementRequest { KeyId = "test-key", Score = 1.0 };
        _mockRepository.UpsertAsync(Arg.Any<DoodleEngagement>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DoodleEngagement>());

        var result = await _controller.CreateEngagement(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result.Result!;
        var response = created.Value as EngagementResponse;
        response.Should().NotBeNull();
        response!.KeyId.Should().Be("test-key");
        response.Score.Should().Be(1.0);
    }

    [Fact]
    public async Task CreateEngagement_WithEmptyKeyId_ReturnsBadRequest()
    {
        var request = new CreateEngagementRequest { KeyId = "", Score = 1.0 };

        var result = await _controller.CreateEngagement(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateEngagement_WithWhitespaceKeyId_ReturnsBadRequest()
    {
        var request = new CreateEngagementRequest { KeyId = "   ", Score = 0.25 };

        var result = await _controller.CreateEngagement(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateEngagement_WithInvalidScore_ReturnsBadRequest()
    {
        var request = new CreateEngagementRequest { KeyId = "test-key", Score = 0.5 };

        var result = await _controller.CreateEngagement(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(1.0)]
    public async Task CreateEngagement_WithValidScores_ReturnsCreated(double score)
    {
        var request = new CreateEngagementRequest { KeyId = "test-key", Score = score };
        _mockRepository.UpsertAsync(Arg.Any<DoodleEngagement>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DoodleEngagement>());

        var result = await _controller.CreateEngagement(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result.Result!;
        var response = created.Value as EngagementResponse;
        response!.Score.Should().Be(score);
    }

    [Fact]
    public async Task CreateEngagement_CallsUpsertOnRepository()
    {
        var request = new CreateEngagementRequest { KeyId = "test-key", Score = 0.25 };
        _mockRepository.UpsertAsync(Arg.Any<DoodleEngagement>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<DoodleEngagement>());

        await _controller.CreateEngagement(request, CancellationToken.None);

        await _mockRepository.Received(1).UpsertAsync(
            Arg.Is<DoodleEngagement>(e => e.KeyId == "test-key" && e.Score == 0.25),
            Arg.Any<CancellationToken>());
    }
}
