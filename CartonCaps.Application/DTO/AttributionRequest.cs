using System.ComponentModel.DataAnnotations;

namespace CartonCaps.Application.DTO;

/// <summary>
/// Represents a request to attribute a new app installation to a referral code.
/// </summary>
/// <param name="DeviceId">Unique hardware identifier (IDFV/GAID) to identify the installation.</param>
/// <param name="ReferralCode">The referral code extracted from the deep link (5-20 characters).</param>
/// <param name="Os">The mobile operating system platform (ios or android).</param>
public record AttributionRequest(
    [Required] string DeviceId, 
    
    [Required] 
    [StringLength(20, MinimumLength = 5)] 
    string ReferralCode, 
    
    [Required] 
    [RegularExpression("^(ios|android)$", ErrorMessage = "Platform must be 'ios' or 'android'")]
    string Os
);