using Microsoft.AspNetCore.Mvc;
using CartonCaps.Application.Common.Interfaces;
using CartonCaps.Application.DTO;

namespace CartonCaps.Api.Controllers;

/// <summary>
/// Manages referral feature endpoints including link generation, device attribution, and reward claiming.
/// </summary>
[ApiController]
[Route("referrals")]
[Produces("application/json")]
public class ReferralController : ControllerBase
{
    private readonly IReferralService _referralService;
    private readonly ILogger<ReferralController> _logger;

    /// <param name="referralService">The service handling referral business logic.</param>
    /// <param name="logger">Logger for tracking operations and errors.</param>
    public ReferralController(
        IReferralService referralService,
        ILogger<ReferralController> logger)
    {
        _referralService = referralService ?? throw new ArgumentNullException(nameof(referralService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a unique shareable referral link for the authenticated user.
    /// </summary>
    /// <param name="campaign">Optional marketing campaign identifier (e.g., 'whatsapp', 'twitter').</param>
    /// <returns>A newly created referral link with expiry date.</returns>
    /// <response code="201">Referral link created successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("new-link")]
    [ProducesResponseType(typeof(ReferralLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserReferralLink([FromQuery] string? campaign)
    {
        try
        {
            // TODO: Replace with actual user ID using JWT claims
            var mockUserId = Guid.NewGuid();
            
            _logger.LogInformation(
                "Generating referral link for user {UserId} with campaign {Campaign}",
                mockUserId,
                campaign ?? "general");

            var result = await _referralService.CreateReferralLinkAsync(mockUserId, campaign);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to generate referral link for user {UserId}: {Error}",
                    mockUserId,
                    result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Created($"/referrals/{result.Value!.ReferralCode}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while generating referral link");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Matches a new device installation to a referral code during app onboarding.
    /// This endpoint enables deferred deep linking attribution.
    /// </summary>
    /// <param name="request">The attribution request containing device fingerprint and referral code.</param>
    /// <returns>Attribution response if attribution succeeds.</returns>
    /// <response code="200">Device successfully attributed to referral.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Referral code not found or expired.</response>
    [HttpPost("attribute")]
    [ProducesResponseType(typeof(AttributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MatchDeviceToReferral([FromBody] AttributionRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid attribution request received");
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation(
                "Processing attribution for device {DeviceId} with referral code {ReferralCode}",
                request.DeviceId,
                request.ReferralCode);

            var result = await _referralService.MatchDeviceAsync(request);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Attribution failed for device {DeviceId}: {Error}",
                    request.DeviceId,
                    result.Error);
                    
                return NotFound(new { message = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during device attribution for device {DeviceId}", request.DeviceId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Finalizes a referral and processes reward distribution after successful user registration.
    /// </summary>
    /// <param name="request">The claim request containing the attribution token.</param>
    /// <returns>Confirmation of reward processing.</returns>
    /// <response code="200">Reward processed successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="409">Reward already claimed or invalid token.</response>
    [HttpPost("claim")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimReferral([FromBody] ClaimRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid claim request received");
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Processing reward claim with token {Token}", request.AttributionToken);
            
            var result = await _referralService.ClaimReferralAsync(request);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Reward claimed successfully");
                return Ok(new { message = "Reward processed successfully." });
            }

            _logger.LogWarning("Invalid or already claimed token: {Token}", request.AttributionToken);
            return Conflict(new { message = "Invalid token or reward already claimed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during claim processing");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Retrieves all referrals made by the authenticated user.
    /// </summary>
    /// <param name="status">Optional filter by referral status (pending, completed, rewarded).</param>
    /// <returns>List of referrals with their current status.</returns>
    /// <response code="200">Successfully retrieved referrals.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("my-referrals")]
    [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyReferrals([FromQuery] string? status = null)
    {
        try
        {
            // TODO: Replace with actual user ID from JWT claims
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            _logger.LogInformation("Fetching referrals with status filter: {Status}", status ?? "all");
            
            // TODO: Implement actual retrieval logic in service
            var referrals = new[] {
                new { inviteeName = "John D.", status = "completed", dateJoined = "2026-01-20", rewardIssued = true },
                new { inviteeName = "Sarah W.", status = "pending", dateJoined = "2026-01-25", rewardIssued = false }
            };

            // Apply status filter if provided
            var filteredReferrals = string.IsNullOrWhiteSpace(status)
                ? referrals
                : referrals.Where(r => r.status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToArray();

            return Ok(filteredReferrals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching referrals");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}
