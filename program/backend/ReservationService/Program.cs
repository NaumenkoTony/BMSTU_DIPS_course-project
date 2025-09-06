using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReservationService.Data;
using ReservationService.Data.RepositoriesPostgreSQL;
using ReservationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddDbContext<ReservationsContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReservationService")));

builder.Services.AddScoped<IHotelRepository, HotelRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

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
            ValidIssuer = authConfig["Issuer"],
            ValidateAudience = true,
            ValidAudience = authConfig["Audience"],
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

builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();