using System.Collections.Concurrent;
using CartonCaps.Infrastructure.Mocks;
using CartonCaps.Application.Common.Interfaces;
using CartonCaps.Application.DTO;

namespace CartonCaps.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of referral repository for testing.
/// TODO: Replace with database implementation.
/// </summary>
public class InMemoryReferralRepository : IInMemoryReferralRepository
{
    private static readonly ConcurrentDictionary<string, MockAttribution> _attributions = new();
    private static readonly ConcurrentDictionary<string, MockReferralLink> _referralLinks = new();
    private static readonly HashSet<string> _claimedUsers = new();

    public InMemoryReferralRepository()
    {
        _ = SeedMockData();
    }

    public Task<ReferralLinkDto?> GetReferralLinkByCodeAsync(string code)
    {
        _referralLinks.TryGetValue(code, out var link);
        return Task.FromResult(MapToDto(link));
    }

    public Task<ReferralLinkDto?> GetReferralLinkByUserIdAsync(Guid userId)
    {
        var link = _referralLinks.Values.FirstOrDefault(l => l.UserId == userId.ToString());
        return Task.FromResult(MapToDto(link));
    }

    public Task AddReferralLinkAsync(ReferralLinkDto link)
    {
        _referralLinks.TryAdd(link.ReferralCode, MapToMock(link));
        return Task.CompletedTask;
    }

    public Task<AttributionResponse?> GetAttributionByDeviceIdAsync(string deviceId)
    {
        _attributions.TryGetValue(deviceId, out var attribution);
        return Task.FromResult(MapToDto(attribution));
    }

    public Task AddAttributionAsync(AttributionResponse attribution)
    {
        _attributions.TryAdd(attribution.DeviceId, MapToMock(attribution));
        return Task.CompletedTask;
    }

    public Task RemoveAttributionAsync(string deviceId)
    {
        _attributions.TryRemove(deviceId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> HasUserClaimedAsync(string userId)
    {
        return Task.FromResult(_claimedUsers.Contains(userId));
    }

    public Task MarkUserAsClaimedAsync(string userId)
    {
        _claimedUsers.Add(userId);
        return Task.CompletedTask;
    }

    private async Task SeedMockData()
    {
        // Create a test referral link
        _referralLinks.TryAdd("CARTON_2026", new MockReferralLink
        {
            ReferralCode = "CARTON_2026",
            UserId = "user-123",
            UserName = "TestUser",
            ShortUrl = "https://cartoncaps.com/r/CARTON_2026",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            ExpiresAt = DateTime.UtcNow.AddMonths(6)
        });
    }

    private static ReferralLinkDto? MapToDto(MockReferralLink? mock)
    {
        if (mock == null) return null;
        return new ReferralLinkDto(
            mock.ReferralCode,
            mock.UserId,
            mock.ExpiresAt,
            mock.CreatedAt,
            mock.UserName,
            mock.ShortUrl
        );
    }

    private static MockReferralLink MapToMock(ReferralLinkDto dto)
    {
        return new MockReferralLink
        {
            ReferralCode = dto.ReferralCode,
            UserId = dto.UserId,
            CreatedAt = dto.CreatedAt,
            ExpiresAt = dto.ExpiresAt,
            UserName = dto.UserName,
            ShortUrl = dto.ShortUrl
        };
    }

    private static AttributionResponse? MapToDto(MockAttribution? mock)
    {
        if (mock == null) return null;
        return new AttributionResponse
        {
            DeviceId = mock.DeviceId,
            ReferralCode = mock.ReferralCode,
            Token = mock.Token,
            AttributedAt = mock.AttributedAt,
            ExpiresAt = mock.ExpiresAt
        };
    }

    private static MockAttribution MapToMock(AttributionResponse response)
    {
        return new MockAttribution
        {
            DeviceId = response.DeviceId,
            ReferralCode = response.ReferralCode,
            Token = response.Token,
            AttributedAt = response.AttributedAt,
            ExpiresAt = response.ExpiresAt
        };
    }
}