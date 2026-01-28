using CartonCaps.Application.DTO;

namespace CartonCaps.Application.Common.Interfaces;

public interface IReferralService
{
    /// <summary>
    /// Validates the referral attempt and generates a secure attribution token.
    /// </summary>
    /// <param name="request">The data captured by the mobile SDK post-install.</param>
    /// <returns>A Result containing onboarding metadata and a secure attribution token.</returns>
    Task<Result<AttributionResponse>> MatchDeviceAsync(AttributionRequest request);

    /// <summary>
    /// Creates a unique, trackable short link for a user's referral code.
    /// </summary>
    /// <param name="userId">The unique identifier of the requesting user.</param>
    /// <param name="campaign">An optional identifier for the marketing channel.</param>
    /// <returns>A Result containing the referral link details.</returns>
    Task<Result<ReferralLinkDto>> CreateReferralLinkAsync(Guid userId, string? campaign);

    /// <summary>
    /// Finalizes the referral process by claiming rewards based on a valid attribution token.
    /// </summary>
    /// <param name="request">Information required to claim the referral reward.</param>
    /// <returns>A Result indicating the success or failure of the claim operation.</returns>
    Task<Result<ClaimResponse>> ClaimReferralAsync(ClaimRequest request);
}