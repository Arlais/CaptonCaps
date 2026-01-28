namespace CartonCaps.Application.DTO;

/// <summary>
/// Response after successfully attributing a device to a referral.
/// </summary>
public record AttributionResponse
{
    public string DeviceId { get; set; }  = string.Empty;
    public string ReferralCode { get; init; } = string.Empty;
    public required string Token { get; init; }
    public DateTime? AttributedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}