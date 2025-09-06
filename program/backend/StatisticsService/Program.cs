using Microsoft.EntityFrameworkCore;
using StatisticsService.Data;
using StatisticsService.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddDbContext<StatisticsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<KafkaConsumerService>();

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

builder.Services.AddAuthorization();

var kafkaConfig = builder.Configuration.GetSection("Kafka");
Console.WriteLine("Trying to coonect to kafka: " + kafkaConfig["BootstrapServers"]);
var bootstrapServers = kafkaConfig["BootstrapServers"] ?? "localhost:9092";
using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
try
{
    await admin.CreateTopicsAsync(
    [
        new TopicSpecification { Name = "user-actions", NumPartitions = 5, ReplicationFactor = 1 }
    ]);
}
catch (CreateTopicsException e) when (e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
{
    Console.WriteLine("Topic already exists");
}

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
