using CartonCaps.Application.Services;
using CartonCaps.Application.DTO;
using CartonCaps.Application.Common.Interfaces;
using CartonCaps.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CartonCaps.Tests.Services;

public class ReferralServiceTests
{
    private readonly ReferralService _sut;
    private readonly IInMemoryReferralRepository _repository;
    private readonly Mock<ILogger<ReferralService>> _loggerMock;

    public ReferralServiceTests()
    {
        _repository = new InMemoryReferralRepository();
        _loggerMock = new Mock<ILogger<ReferralService>>();
        _sut = new ReferralService(_loggerMock.Object, _repository);
    }

    #region CreateReferralLinkAsync Tests

    [Fact]
    public async Task CreateReferralLinkAsync_WithValidUserId_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaign = "summer_promo";

        // Act
        var result = await _sut.CreateReferralLinkAsync(userId, campaign);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReferralCode.Should().NotBeNullOrEmpty();
        result.Value.ShortUrl.Should().Contain(campaign);
    }

    [Fact]
    public async Task CreateReferralLinkAsync_WithNullCampaign_ReturnsSuccessWithGeneralShare()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.CreateReferralLinkAsync(userId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ShortUrl.Should().Contain("general_share");
    }

    [Fact]
    public async Task CreateReferralLinkAsync_WithEmptyGuid_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.Empty;

        // Act
        var result = await _sut.CreateReferralLinkAsync(userId, "test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User ID");
    }

    [Fact]
    public async Task CreateReferralLinkAsync_ReturnsExpiryDateInFuture()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.CreateReferralLinkAsync(userId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region MatchDeviceAsync Tests

    [Fact]
    public async Task MatchDeviceAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange - First create a referral link to get a valid code
        var userId = Guid.NewGuid();
        var createResult = await _sut.CreateReferralLinkAsync(userId, "test");
        var validCode = createResult.Value!.ReferralCode;
        
        var request = new AttributionRequest("device123", validCode, "ios");

        // Act
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.DeviceId.Should().Be("device123");
    }

    [Fact]
    public async Task MatchDeviceAsync_WithEmptyDeviceId_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new AttributionRequest("", "SOME_CODE", "ios");

        // Act
        Func<Task> act = async () => await _sut.MatchDeviceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MatchDeviceAsync_WithNullDeviceId_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new AttributionRequest(null!, "SOME_CODE", "ios");

        // Act
        Func<Task> act = async () => await _sut.MatchDeviceAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MatchDeviceAsync_WithInvalidReferralCode_ReturnsFailure()
    {
        // Arrange
        var request = new AttributionRequest("device123", "INVALID_CODE", "ios");

        // Act
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found or expired");
    }

    [Fact]
    public async Task MatchDeviceAsync_WithValidRequest_ReturnsCorrectReferralCode()
    {
        // Arrange 
        var userId = Guid.NewGuid();
        var createResult = await _sut.CreateReferralLinkAsync(userId, "test");
        var validCode = createResult.Value!.ReferralCode;
        
        var request = new AttributionRequest("device456", validCode, "android");

        // Act
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ReferralCode.Should().Be(validCode);
    }

    [Fact]
    public async Task MatchDeviceAsync_WithAlreadyAttributedDevice_ReturnsFailure()
    {
        // Arrange - Create referral and attribute a device
        var userId = Guid.NewGuid();
        var createResult = await _sut.CreateReferralLinkAsync(userId, "test");
        var validCode = createResult.Value!.ReferralCode;
        
        var request = new AttributionRequest("device789", validCode, "ios");
        await _sut.MatchDeviceAsync(request); // First attribution

        // Act - Try to attribute the same device again
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already attributed");
    }

    #endregion
}