using System.Security.Claims;
using System.Text.Json;
using GatewayService.TokenService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<AuthorizationHandler>();
builder.Services.AddScoped<IInternalTokenService, InternalTokenService>();

var servicesConfig = builder.Configuration.GetSection("Services");
builder.Services.AddHttpClient("LoyaltyService", client =>
{
    client.BaseAddress = new Uri(servicesConfig["Loyalty"]);
}).AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient("LoyaltyQueueService", client =>
{
    client.BaseAddress = new Uri(servicesConfig["Loyalty"]);
});

builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri(servicesConfig["Payment"]);
}).AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient("ReservationService", client =>
{
    client.BaseAddress = new Uri(servicesConfig["Reservation"]);
}).AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient("IdentityService", client =>
{
    client.BaseAddress = new Uri(servicesConfig["Identity"]);
}).AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient("StatisticsService", client =>
{
    client.BaseAddress = new Uri(servicesConfig["Statistics"]);
}).AddHttpMessageHandler<AuthorizationHandler>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddAutoMapper(typeof(Program));

var authConfig = builder.Configuration.GetSection("Authentication");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authConfig["Authority"];
        options.Audience = authConfig["Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication challenge triggered: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated successfully.");
                return Task.CompletedTask;
            }
        };
    });
    
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var redis_con_str = builder.Configuration.GetConnectionString("redis");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redis_con_str));
builder.Services.AddScoped<LoyaltyQueueProcessor>();

var app = builder.Build();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();