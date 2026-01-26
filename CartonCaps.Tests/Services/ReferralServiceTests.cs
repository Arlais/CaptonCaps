using CartonCaps.Application.Services;
using CartonCaps.Application.DTO;
using FluentAssertions;
using Xunit;

namespace CartonCaps.Tests.Services;

public class ReferralServiceTests
{
    private readonly ReferralService _sut;

    public ReferralServiceTests()
    {
        _sut = new ReferralService();
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
        result.Value!.ExpiryDate.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region MatchDeviceAsync Tests

    [Fact]
    public async Task MatchDeviceAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new AttributionRequest("device123", "CARTON_2026", "ios");

        // Act
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.DeviceId.Should().Be("device123");
    }

    [Fact]
    public async Task MatchDeviceAsync_WithEmptyDeviceId_ReturnsFailure()
    {
        // Arrange
        var request = new AttributionRequest("", "CARTON_2026", "ios");

        // Act
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Device ID");
    }

    [Fact]
    public async Task MatchDeviceAsync_WithWhitespaceDeviceId_ReturnsFailure()
    {
        // Arrange
        var request = new AttributionRequest("   ", "CARTON_2026", "ios");

        // Act
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Device ID");
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
    public async Task MatchDeviceAsync_WithValidRequest_ReturnsCorrectOs()
    {
        // Arrange
        var request = new AttributionRequest("device123", "CARTON_2026", "android");

        // Act
        var result = await _sut.MatchDeviceAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Os.Should().Be("android");
    }

    #endregion
}