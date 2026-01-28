using CartonCaps.Application.Common.Interfaces;
using CartonCaps.Application.Services;
using CartonCaps.Api.Middleware;
using CartonCaps.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IInMemoryReferralRepository, InMemoryReferralRepository>();
builder.Services.AddControllers();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() 
    {
        Title = "CartonCaps Referral Service API",
        Version = "v1.0.0",
        Description = "API for generating shareable referral links and handling deferred attribution for mobile app referrals."
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddOpenApi();

// Add exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApps", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CartonCaps API v1");
        options.RoutePrefix = "swagger"; // Serve Swagger UI at root
    });
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors("AllowMobileApps");
app.UseCorrelationId();
app.MapControllers();

app.Run();

public partial class Program { }