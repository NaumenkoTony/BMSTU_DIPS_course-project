using System.Security.Claims;
using System.Text.Json;
using GatewayService.TokenService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<AuthorizationHandler>();
builder.Services.AddScoped<IInternalTokenService, InternalTokenService>();

builder.Services.AddHttpClient("LoyaltyService", client =>
{
    client.BaseAddress = new Uri("http://loyalty_service:8050");
}).AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient("LoyaltyQueueService", client =>
{
    client.BaseAddress = new Uri("http://loyalty_service:8050");
});

builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri("http://payment_service:8060");
}).AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient("ReservationService", client =>
{
    client.BaseAddress = new Uri("http://reservation_service:8070");
}).AddHttpMessageHandler<AuthorizationHandler>();

builder.Services.AddHttpClient("IdentityService", client =>
{
    client.BaseAddress = new Uri("http://identity_service:8000");
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity_service:8000";
        options.Audience = "resource_server";
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

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("redis:6379,abortConnect=false"));
builder.Services.AddHostedService<LoyaltyQueueProcessor>();

var app = builder.Build();

// app.Use(async (context, next) =>
// {
//     if (context.Request.Cookies.TryGetValue("access_token", out var token) &&
//         string.IsNullOrEmpty(context.Request.Headers["Authorization"]))
//     {
//         context.Request.Headers.Authorization = $"Bearer {token}";
//         var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
//         logger.LogDebug("Added Authorization header from cookie for: {Path}", context.Request.Path);
//     }
//     await next();
// });


app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();