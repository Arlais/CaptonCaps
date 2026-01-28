namespace CartonCaps.Application.DTO;

/// <summary>
/// Data Transfer Object representing a newly generated shareable referral link.
/// </summary>
public record ReferralLinkDto(
    string ReferralCode, 
    string ShortUrl, 
    DateTime ExpiresAt,
    DateTime CreatedAt,
    string UserId
);