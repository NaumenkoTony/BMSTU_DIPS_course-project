using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityService")));

builder.Services.AddIdentity<User, Role>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<IdentityContext>()
    .AddDefaultTokenProviders();

var rsa = RSA.Create(2048);
var rsaKey = new RsaSecurityKey(rsa)
{
    KeyId = Guid.NewGuid().ToString("N")
};

builder.Services.AddSingleton<RsaSecurityKey>(rsaKey);
builder.Services.AddSingleton<IJwksService, JwksService>();

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IClientStore, ClientStore>();
builder.Services.AddScoped<IAuthorizationCodeStore, AuthorizationCodeStore>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "smart"; // Используем умную схему
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddPolicyScheme("smart", "Smart authentication scheme", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return "Bearer";
        }
        
        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.Cookie.Name = "idsrv.session";
    
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/admin"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/admin"))
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
})
.AddJwtBearer("Bearer", options =>
{
    options.Authority = builder.Configuration["Issuer"] ?? "http://identity_service:8000";
    options.Audience = "resource_server";
    options.RequireHttpsMetadata = false;
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Issuer"] ?? "http://identity_service:8000",
        ValidateAudience = true,
        ValidAudience = "resource_server",
        ValidateLifetime = true,
        IssuerSigningKey = rsaKey,
        ValidateIssuerSigningKey = true,
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("JWT Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Authentication challenge: {Error}", context.Error);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("JWT Token validated for user: {User}", 
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection()
    .SetApplicationName("HotelBookingIdentityProvider");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityContext>();
    db.Database.Migrate();

    db = scope.ServiceProvider.GetRequiredService<IdentityContext>();
    await ClientSeeder.EnsureSeededAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityContext>();
    db.Database.Migrate();

    await ClientSeeder.EnsureSeededAsync(db);
}

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    await IdentitySeeder.SeedRolesAndAdminAsync(userManager, roleManager);
}

app.UseCors("AllowAll");

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
