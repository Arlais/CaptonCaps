namespace CartonCaps.Infrastructure.Mocks;
public class MockReferralLink
    {
        public required string ReferralCode { get; init; }
        public required string UserId { get; init; }
        public required string ShortUrl { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
    }