using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Services;

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
var keyParameters = rsa.ExportParameters(true);
var rsaSecurityKey = new RsaSecurityKey(keyParameters)
{
    KeyId = Guid.NewGuid().ToString()
};
builder.Services.AddSingleton<RsaSecurityKey>(rsaSecurityKey);

builder.Services.AddSingleton<IJwksService, JwksService>(); // ➡ JWKS endpoint для публикации публичного ключа
builder.Services.AddScoped<ITokenService, TokenService>(); // ➡ Формирование ID и Access токенов
builder.Services.AddScoped<IClientStore, ClientStore>(); // ➡ Валидация client_id, redirect_uri, scopes
builder.Services.AddScoped<IAuthorizationCodeStore, AuthorizationCodeStore>(); // ➡ Создание и проверка authorization code
builder.Services.AddScoped<IProfileService, ProfileService>(); // ➡ Формирование claims для токенов и /userinfo
builder.Services.AddScoped<IConsentService, ConsentService>(); // ➡ Логика экрана согласия на выдачу scopes

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
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDataProtection()
    .SetApplicationName("HotelBookingIdentityProvider");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityContext>();
    db.Database.Migrate();

    var clientStore = scope.ServiceProvider.GetRequiredService<IClientStore>();
    await ClientSeeder.EnsureSeededAsync(clientStore, db);
}

// 9️⃣ Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
