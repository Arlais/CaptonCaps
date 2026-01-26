using Microsoft.AspNetCore.Mvc;
using CartonCaps.Application.Common.Interfaces;
using CartonCaps.Application.Services;
using CartonCaps.Application.DTO;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IReferralService, ReferralService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();    
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/referrals/new-link", async (
    [FromQuery] string? campaign, 
    IReferralService referralService) =>
{
    // Craft: We simulate the User ID usually found in the JWT/ClaimsPrincipal
    var mockUserId = Guid.NewGuid(); 
    
    var result = await referralService.CreateReferralLinkAsync(mockUserId, campaign);

    return result.IsSuccess 
        ? Results.Created($"/referrals/{result.Value!.ReferralCode}", result.Value)
        : Results.BadRequest(new { error = result.Error });
})
.WithName("GetUserReferralLink");

app.MapPost("/referrals/attribute", async ([FromBody] AttributionRequest request,
IReferralService service) =>
{
    var result = await service.MatchDeviceAsync(request);
    if (request.ReferralCode == "|")
    {
        return result.IsSuccess ?
        Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
    }

    return Results.NotFound(new { message = "Referral code is invalid or expired." });
})
.WithName("MatchDeviceToReferral");

app.MapPost("/referrals/claim", ([FromBody] ClaimRequest request) =>
{
    if (request.AttributionToken == "secure_b64_encoded_handshake_token")
    {
        return Results.Ok(new { message = "Reward processed successfully." });
    }
    
    return Results.Conflict(new { message = "Invalid token or reward already claimed." });
})
.WithName("ClaimReferral");

app.MapGet("/referrals/my-referrals", () =>
{
    return Results.Ok(new[] {
        new { inviteeName = "John D.", status = "completed", dateJoined = "2026-01-20", rewardIssued = true },
        new { inviteeName = "Sarah W.", status = "pending", dateJoined = "2026-01-25", rewardIssued = false }
    });
})
.WithName("GetMyReferrals");

app.Run();

public partial class Program { }