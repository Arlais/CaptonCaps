using CartonCaps.Application.DTO;

namespace CartonCaps.Application.Common.Interfaces;

public interface IReferralService
{
    /// <summary>
    /// Processes a device fingerprint and referral code to determine onboarding context.
    /// </summary>
    /// <param name="request">The data captured by the mobile SDK post-install.</param>
    /// <returns>A Result containing onboarding metadata and a secure attribution token.</returns>
    Task<Result<OnboardingMetadata>> MatchDeviceAsync(AttributionRequest request);

    /// <summary>
    /// Creates a unique, trackable short link for a user's referral code.
    /// </summary>
    /// <param name="userId">The unique identifier of the requesting user.</param>
    /// <param name="campaign">An optional identifier for the marketing channel.</param>
    /// <returns>A Result containing the referral link details.</returns>
    Task<Result<ReferralLinkDto>> CreateReferralLinkAsync(Guid userId, string? campaign);
}