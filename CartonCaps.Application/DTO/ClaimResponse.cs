namespace CartonCaps.Application.DTO;
public record ClaimResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ReferralCode { get; init; }
}