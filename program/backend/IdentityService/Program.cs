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

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
var rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(true))
{
    KeyId = Guid.NewGuid().ToString()
};
builder.Services.AddSingleton<RsaSecurityKey>(rsaSecurityKey);

builder.Services.AddSingleton<IJwksService, JwksService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IClientStore, ClientStore>();
builder.Services.AddScoped<IAuthorizationCodeStore, AuthorizationCodeStore>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.Cookie.Name = "idsrv.session";
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Issuer"] ?? "http://identity_service:8000",
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = rsaSecurityKey,
            ValidateIssuerSigningKey = true
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

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
