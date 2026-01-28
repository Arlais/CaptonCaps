using Microsoft.AspNetCore.Mvc.Testing;
using CartonCaps.Application.DTO;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CartonCaps.Tests.Controllers;

/// <summary>
/// Integration tests for the ReferralController endpoints.
/// Tests the full request/response cycle including validation, error handling, and business logic.
/// </summary>
public class ReferralControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReferralControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    #region GetUserReferralLink Tests

    [Fact]
    public async Task GetUserReferralLink_WithValidRequest_ReturnsCreated()
    {
        // Act
        var response = await _client.GetAsync("/referrals/new-link?campaign=test_campaign");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ReferralLinkDto>();
        result.Should().NotBeNull();
        result!.ReferralCode.Should().NotBeNullOrEmpty();
        result.ShortUrl.Should().Contain("test_campaign");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetUserReferralLink_WithoutCampaign_ReturnsCreatedWithDefaultCampaign()
    {
        // Act
        var response = await _client.GetAsync("/referrals/new-link");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ReferralLinkDto>();
        result.Should().NotBeNull();
        result!.ShortUrl.Should().Contain("general_share");
    }

    [Fact]
    public async Task GetUserReferralLink_WithSpecialCharactersInCampaign_SanitizesInput()
    {
        // Act
        var response = await _client.GetAsync("/referrals/new-link?campaign=test<script>alert(1)</script>");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ReferralLinkDto>();
        result.Should().NotBeNull();
        // The sanitized campaign should not contain script tags
        result!.ShortUrl.Should().NotContain("<script>");
    }

    #endregion

    #region MatchDeviceToReferral Tests

    [Fact]
    public async Task MatchDeviceToReferral_WithValidRequest_ReturnsOk()
    {
        // Arrange - First create a referral link to get a valid code
        var createResponse = await _client.GetAsync("/referrals/new-link?campaign=test");
        var referralLink = await createResponse.Content.ReadFromJsonAsync<ReferralLinkDto>();
        var validCode = referralLink!.ReferralCode;
        
        var request = new AttributionRequest($"device_{Guid.NewGuid()}", validCode, "ios");

        // Act
        var response = await _client.PostAsJsonAsync("/referrals/attribute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AttributionResponse>();
        result.Should().NotBeNull();
        result!.DeviceId.Should().Be(request.DeviceId);
        result.ReferralCode.Should().Be(validCode);
    }

    [Fact]
    public async Task MatchDeviceToReferral_WithInvalidReferralCode_ReturnsNotFound()
    {
        // Arrange
        var request = new AttributionRequest("device_12345", "INVALID_CODE", "android");

        // Act
        var response = await _client.PostAsJsonAsync("/referrals/attribute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("", "CARTON_2026", "ios")] // Empty device ID
    [InlineData("device123", "ABC", "ios")] // Too short referral code
    [InlineData("device123", "CARTON_2026", "windows")] // Invalid OS
    public async Task MatchDeviceToReferral_WithInvalidData_ReturnsBadRequest(
        string deviceId, string referralCode, string os)
    {
        // Arrange
        var request = new AttributionRequest(deviceId, referralCode, os);

        // Act
        var response = await _client.PostAsJsonAsync("/referrals/attribute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MatchDeviceToReferral_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = "{ \"deviceId\": \"test123\" }"; // Missing required fields

        // Act
        var response = await _client.PostAsync("/referrals/attribute", 
            new StringContent(invalidJson, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ClaimReferral Tests

    [Fact]
    public async Task ClaimReferral_WithValidToken_ReturnsOk()
    {
        // Arrange - First create a referral and attribute a device to get a valid token
        var createResponse = await _client.GetAsync("/referrals/new-link?campaign=test");
        var referralLink = await createResponse.Content.ReadFromJsonAsync<ReferralLinkDto>();
        var validCode = referralLink!.ReferralCode;
        
        var deviceId = $"device_{Guid.NewGuid()}";
        var attributeRequest = new AttributionRequest(deviceId, validCode, "ios");
        var attributeResponse = await _client.PostAsJsonAsync("/referrals/attribute", attributeRequest);
        var attribution = await attributeResponse.Content.ReadFromJsonAsync<AttributionResponse>();
        
        var request = new ClaimRequest{
            AttributionToken = attribution!.Token,
            UserId = Guid.NewGuid().ToString(),
            DeviceId = deviceId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/referrals/claim", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClaimReferral_WithInvalidToken_ReturnsConflict()
    {
        // Arrange
        var request = new ClaimRequest{
            AttributionToken = "invalid_token",
            UserId = "device123",
            DeviceId = Guid.NewGuid().ToString()
        };
        // Act
        var response = await _client.PostAsJsonAsync("/referrals/claim", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ClaimReferral_WithEmptyToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ClaimRequest{
            AttributionToken = "",
            UserId = "device123",
            DeviceId = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/referrals/claim", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetMyReferrals Tests

    [Fact]
    public async Task GetMyReferrals_WithoutFilter_ReturnsAllReferrals()
    {
        // Act
        var response = await _client.GetAsync("/referrals/my-referrals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMyReferrals_WithStatusFilter_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/referrals/my-referrals?status=completed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        result.Should().NotBeNull();
        // All results should have status "completed"
        foreach (var item in result!)
        {
            item.GetProperty("status").GetString().Should().Be("completed");
        }
    }

    [Fact]
    public async Task GetMyReferrals_WithInvalidStatus_ReturnsEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/referrals/my-referrals?status=invalid_status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task Endpoints_WithMalformedJson_ReturnsBadRequest()
    {
        // Arrange
        var malformedJson = "{ this is not valid json }";

        // Act
        var response = await _client.PostAsync("/referrals/attribute",
            new StringContent(malformedJson, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Endpoints_WithUnsupportedMediaType_ReturnsUnsupportedMediaType()
    {
        // Arrange
        var content = new StringContent("plain text", System.Text.Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync("/referrals/attribute", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion
}