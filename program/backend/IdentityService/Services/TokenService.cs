using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using IdentityService.Models;

namespace IdentityService.Services
{
    public class TokenService : ITokenService
    {
        private readonly RsaSecurityKey _key;
        private readonly string _issuer;

        public TokenService(RsaSecurityKey key, IConfiguration config)
        {
            _key = key;
            _issuer = config["Authentication:Issuer"] ?? "identityService";
        }

        private string CreateToken(string subject, string audience, IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var now = DateTime.UtcNow;

            var jwtClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, subject),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iss, _issuer),
                new(JwtRegisteredClaimNames.Aud, audience),
                new(JwtRegisteredClaimNames.Iat,
                        new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64)
            };

            jwtClaims.AddRange(claims);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                claims: jwtClaims,
                notBefore: now,
                expires: now.Add(lifetime),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> CreateIdTokenAsync(string userId, string audience, IEnumerable<Claim> userClaims, string[] scopes)
        {
            var claims = new List<Claim>
            {
                new("name", userClaims.FirstOrDefault(c => c.Type == "name")?.Value ?? ""),
                new("email", userClaims.FirstOrDefault(c => c.Type == "email")?.Value ?? ""),
                new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            return Task.FromResult(CreateToken(userId, audience, claims, TimeSpan.FromMinutes(60)));
        }

        public Task<string> CreateAccessTokenAsync(string userId, string audience, IEnumerable<Claim> userClaims, string[] scopes)
        {
            var claims = new List<Claim>();
            claims.AddRange(userClaims);

            return Task.FromResult(CreateToken(userId, audience, claims, TimeSpan.FromMinutes(15)));
        }

    }
}