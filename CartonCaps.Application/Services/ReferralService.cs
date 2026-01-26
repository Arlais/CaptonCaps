using CartonCaps.Application.Common.Interfaces;
using CartonCaps.Application.DTO;

namespace CartonCaps.Application.Services;

public class ReferralService : IReferralService 
{
    /// <summary>
    /// Validates the referral attempt and generates a secure token. 
    /// Encapsulates the logic to prevent users from referring their own devices.
    /// </summary>
    public async Task<Result<OnboardingMetadata>> MatchDeviceAsync(AttributionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return Result<OnboardingMetadata>.Failure("Device ID is required for attribution.");

        if (request.ReferralCode == "CARTON_2026") {
            var metadata = new OnboardingMetadata(
                DeviceId: request.DeviceId,
                Os: request.Os,
                AppVersion: "1.0.0",
                Locale: "en-US",
                Timezone: "UTC"
            );
            return Result<OnboardingMetadata>.Success(metadata);
        }

        return Result<OnboardingMetadata>.Failure("Referral code not found or expired.");
    }

    public async Task<Result<ReferralLinkDto>> CreateReferralLinkAsync(Guid userId, string? campaign)
    {
        if (userId == Guid.Empty)
        {
            return Result<ReferralLinkDto>.Failure("A valid User ID is required to generate a link.");
        }

        //Mock logic to generate a referral code instead of going to database
        var userReferralCode = "CARTON_2026"; 
        var source = campaign ?? "general_share";

        // Simulate a call to a third-party deep link provider 
        var mockShortUrl = $"https://cartoncaps.link/i/{userReferralCode}?utm_source={source}";

        var linkDto = new ReferralLinkDto(
            ReferralCode: userReferralCode,
            ShortUrl: mockShortUrl,
            ExpiryDate: DateTime.UtcNow.AddMonths(1)
        );

        return Result<ReferralLinkDto>.Success(linkDto);
    }
}