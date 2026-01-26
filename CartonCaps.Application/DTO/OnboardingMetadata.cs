namespace CartonCaps.Application.DTO;

public record OnboardingMetadata(
    string DeviceId,
    string Os,
    string AppVersion,
    string Locale,
    string Timezone
);