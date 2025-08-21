using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public interface IInternalTokenService
{
    string GenerateServiceToken();
}

public class InternalTokenService(IConfiguration configuration) : IInternalTokenService
{
    private readonly IConfiguration configuration = configuration;

    public string GenerateServiceToken()
    {
        var secret = configuration["InternalJwt:Secret"];
        var issuer = configuration["InternalJwt:Issuer"];
        var audience = configuration["InternalJwt:Audience"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "system-processor"),
                new Claim("role", "System")
            },
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
