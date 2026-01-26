using System.ComponentModel.DataAnnotations;

namespace CartonCaps.Application.DTO;

public record ClaimRequest(
    [Required] string AttributionToken
);