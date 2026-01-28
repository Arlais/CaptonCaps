namespace CartonCaps.Application.DTO;

/// <summary>
/// Represents a request to claim a referral reward after successful user registration.
/// This finalizes the referral process and triggers reward distribution.
/// </summary>
/// <param name="AttributionToken">Secure token generated during device attribution that validates the referral chain.</param>
public record ClaimRequest
{
    public string UserId { get; init; } = string.Empty;
    public required string AttributionToken { get; init; }
    public string? DeviceId { get; init; }

    public ClaimRequest() { }
    public ClaimRequest(string attributionToken, string userId, string? deviceId)
    {
        AttributionToken = attributionToken;
        UserId = userId;
        DeviceId = deviceId;
    }  
};