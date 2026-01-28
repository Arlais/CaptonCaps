namespace CartonCaps.Infrastructure.Mocks;
 
public class MockAttribution
    {
        public required string DeviceId { get; init; }
        public required string ReferralCode { get; init; }
        public required string Token { get; init; }
        public DateTime? AttributedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
    }