using System.ComponentModel.DataAnnotations;

namespace CartonCaps.Application.DTO;

public record AttributionRequest(
    [Required] string DeviceId, 
    
    [Required] 
    [StringLength(20, MinimumLength = 5)] 
    string ReferralCode, 
    
    [Required] 
    [RegularExpression("^(ios|android)$", ErrorMessage = "Platform must be 'ios' or 'android'")]
    string Os
);