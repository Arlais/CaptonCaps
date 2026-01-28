using CartonCaps.Application.DTO;

namespace CartonCaps.Application.Common.Interfaces;

public interface IInMemoryReferralRepository
{
    Task<ReferralLinkDto?> GetReferralLinkByCodeAsync(string code);
    Task<ReferralLinkDto?> GetReferralLinkByUserIdAsync(Guid userId);
    Task AddReferralLinkAsync(ReferralLinkDto link);
    
    Task<AttributionResponse?> GetAttributionByDeviceIdAsync(string deviceId);
    Task AddAttributionAsync(AttributionResponse attribution);
    Task RemoveAttributionAsync(string deviceId);
    
    Task<bool> HasUserClaimedAsync(string userId);
    Task MarkUserAsClaimedAsync(string userId);
}