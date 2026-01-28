using System.Collections.Concurrent;
using CartonCaps.Application.Common.Interfaces;
using CartonCaps.Application.DTO;
using Microsoft.Extensions.Logging;

namespace CartonCaps.Application.Services;

/// <summary>
/// Implements referral services including device matching and referral link creation.
/// </summary>
public class ReferralService : IReferralService 
{
    private readonly ILogger<ReferralService> _logger;
    private readonly IInMemoryReferralRepository _repository;

    public ReferralService(ILogger<ReferralService> logger, IInMemoryReferralRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Validates the referral attempt and generates a secure attribution token. 
    /// </summary>
    public async Task<Result<AttributionResponse>> MatchDeviceAsync(AttributionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DeviceId);
        ArgumentNullException.ThrowIfNull(request.ReferralCode);
        var validationResult = await ValidateAttributionRequestAsync(request);
        if (!validationResult.IsSuccess)
            return Result<AttributionResponse>.Failure(validationResult.Error);

        // 4. Generate mock attribution token
        var attributionToken = GenerateMockToken(request.DeviceId, request.ReferralCode);
        var attributionResponse = CreateAttributionResponse(request.DeviceId, request.ReferralCode, attributionToken);
        await _repository.AddAttributionAsync(attributionResponse);

        _logger.LogInformation(
            "✅ Attribution successful: DeviceId={DeviceId}, Code={Code}", 
            request.DeviceId, request.ReferralCode);
        
        return Result<AttributionResponse>.Success(attributionResponse);
    }

    private async Task<Result<ReferralLinkDto>> ValidateAttributionRequestAsync(AttributionRequest request)
    {
        _logger.LogInformation(
            "Validating attribution: DeviceId={DeviceId}, Code={Code}", 
            request.DeviceId, request.ReferralCode);

        // TODO: Replace with database lookup
        // 1. Validate referral code exists (mock DB lookup)
        var referralLink = await _repository.GetReferralLinkByCodeAsync(request.ReferralCode);
        if (referralLink == null)
        {
            _logger.LogWarning("Invalid referral code: {Code}", request.ReferralCode);
            return Result<ReferralLinkDto>.Failure("Referral code not found or expired.");
        }
        if (referralLink.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired referral code: {Code}", request.ReferralCode);
            return Result<ReferralLinkDto>.Failure("Referral code has expired.");
        }
        var attributions = await _repository.GetAttributionByDeviceIdAsync(request.DeviceId);
        if (attributions != null)
        {
            _logger.LogWarning("No attribution: {DeviceId}", request.DeviceId);
            return Result<ReferralLinkDto>.Failure("This device is already attributed to a referral.");
        }
        return Result<ReferralLinkDto>.Success(referralLink);
    }

    private static AttributionResponse CreateAttributionResponse(string deviceId, string referralCode, string token)
    {
        return new AttributionResponse
        {
            DeviceId = deviceId,
            ReferralCode = referralCode,
            Token = token,
            AttributedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Token valid for 1 hour
        };
    }

    /// <summary>
    /// Claims a referral using the attribution token after user registration.
    /// </summary>
    public async Task<Result<ClaimResponse>> ClaimReferralAsync(ClaimRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.UserId);
        ArgumentNullException.ThrowIfNull(request.AttributionToken);

        _logger.LogInformation("Claim attempt: UserId={UserId}", request.UserId);
        
        var validationResult = await ValidateClaimRequestAsync(request);
        if (!validationResult.IsSuccess)
            return Result<ClaimResponse>.Failure(validationResult.Error);


        // 8. Mark as claimed (mock DB operation)
        await _repository.MarkUserAsClaimedAsync(request.UserId);
        await _repository.RemoveAttributionAsync(validationResult.Value.Attribution.DeviceId);

        _logger.LogInformation(
            "✅ Referral claimed: Referrer={ReferrerId}, Referee={RefereeId}", 
            validationResult.Value.Link.UserId, request.UserId);
        var response = new ClaimResponse
        {
            Success = true,
            Message = $"Referral bonus claimed! You and referral both received rewards.",
            ReferralCode = validationResult.Value.Attribution.ReferralCode
        };

        return Result<ClaimResponse>.Success(response);
    }
    
    private async Task<Result<(AttributionResponse Attribution, ReferralLinkDto Link)>> ValidateClaimRequestAsync(ClaimRequest request)
    {
        // Token validation
        var tokenData = ValidateMockToken(request.AttributionToken);
        if (tokenData == null)
        {
            _logger.LogWarning("Invalid token for user {UserId}", request.UserId);
            return Result<(AttributionResponse, ReferralLinkDto)>.Failure("Invalid or expired attribution token.");
        }

        var attribution = await _repository.GetAttributionByDeviceIdAsync(tokenData.Value.DeviceId);
        if (attribution == null)
        {
            _logger.LogWarning("No attribution found for device: {DeviceId}", tokenData.Value.DeviceId);
            return Result<(AttributionResponse, ReferralLinkDto)>.Failure("Attribution not found.");
        }

        if (attribution.Token != request.AttributionToken)
        {
            _logger.LogWarning("Token mismatch for device {DeviceId}", tokenData.Value.DeviceId);
            return Result<(AttributionResponse, ReferralLinkDto)>.Failure("Token verification failed.");
        }

        if (attribution.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired attribution for device {DeviceId}", tokenData.Value.DeviceId);
            await _repository.RemoveAttributionAsync(tokenData.Value.DeviceId);
            return Result<(AttributionResponse, ReferralLinkDto)>.Failure("Attribution has expired.");
        }

        var referralLink = await _repository.GetReferralLinkByCodeAsync(attribution.ReferralCode);
        if (referralLink == null)
        {
            _logger.LogWarning("Referral link not found: {Code}", attribution.ReferralCode);
            return Result<(AttributionResponse, ReferralLinkDto)>.Failure("Referral link not found.");
        }

        if (referralLink.UserId == request.UserId)
        {
            _logger.LogWarning("Self-referral attempt: UserId={UserId}", request.UserId);
            return Result<(AttributionResponse, ReferralLinkDto)>.Failure("You cannot refer yourself.");
        }

        var hasClaimed = await _repository.HasUserClaimedAsync(request.UserId);
        if (hasClaimed)
        {
            _logger.LogWarning("User already claimed: {UserId}", request.UserId);
            return Result<(AttributionResponse, ReferralLinkDto)>.Failure("You have already claimed a referral bonus.");
        }

        return Result<(AttributionResponse, ReferralLinkDto)>.Success((attribution, referralLink));
    }

    /// <summary>
    /// Creates a unique referral link for a user to share with potential referees.
    /// </summary>
    public async Task<Result<ReferralLinkDto>> CreateReferralLinkAsync(Guid userId, string? campaign)
    {
        // Validate user ID
        if (userId == Guid.Empty)
            return Result<ReferralLinkDto>.Failure("A valid User ID is required to generate a link.");

        var newLink = await CreateReferralLink(userId, campaign);
        await _repository.AddReferralLinkAsync(newLink);

        return Result<ReferralLinkDto>.Success(newLink);
    }

    private async Task<ReferralLinkDto> CreateReferralLink(Guid userId, string? campaign)
    {
        var sanitizedCampaign = SanitizeCampaign(campaign);
        var source = sanitizedCampaign ?? "general_share";
        // Needs to call user referral code and third party deep link service
        var referralCode = await GenerateUniqueCode();
        var shortUrl = $"https://cartoncaps.com/r/{referralCode}?utm_source={source}";

        return new ReferralLinkDto(
            referralCode,
            shortUrl,
            DateTime.UtcNow.AddMonths(6),
            DateTime.UtcNow,
            userId.ToString()
        );
    }

    /// <summary>
    /// Sanitizes the campaign parameter to prevent injection attacks and ensure URL safety.
    /// </summary>
    /// <param name="campaign">The raw campaign string.</param>
    /// <returns>A sanitized campaign string or null if input was invalid.</returns>
    private static string? SanitizeCampaign(string? campaign)
    {
        if (string.IsNullOrWhiteSpace(campaign))
            return null;

        // Remove potentially dangerous characters and limit length
        var sanitized = new string(campaign
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
            .Take(50)
            .ToArray());

        return string.IsNullOrEmpty(sanitized) ? null : sanitized;
    }

    private string GenerateMockToken(string deviceId, string referralCode)
    {
        var payload = $"{deviceId}|{referralCode}|{DateTime.UtcNow:O}";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        return $"MOCK_{encoded}";
    }

    private (string DeviceId, string ReferralCode, DateTime Timestamp)? ValidateMockToken(string token)
    {
        try
        {
            if (!token.StartsWith("MOCK_"))
                return null;

            var encoded = token.Substring(5);
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts = decoded.Split('|');

            if (parts.Length != 3)
                return null;

            return (parts[0], parts[1], DateTime.Parse(parts[2]));
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> GenerateUniqueCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        string code;
        ReferralLinkDto? existingLink;
        
        do
        {
            code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
            existingLink = await _repository.GetReferralLinkByCodeAsync(code);
        }
        while (existingLink != null);

        return code;
    }
}