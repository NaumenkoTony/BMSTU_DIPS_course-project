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
            _issuer = config["Issuer"] ?? "http://identity_service:8000";
        }

        private string CreateToken(string subject, string audience, IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var now = DateTime.UtcNow;

            var jwtClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, _issuer),
                new Claim(JwtRegisteredClaimNames.Aud, audience),
                new Claim(JwtRegisteredClaimNames.Iat, 
                          new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                          ClaimValueTypes.Integer64)
            };

            jwtClaims.AddRange(claims);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: audience,
                claims: jwtClaims,
                notBefore: now,
                expires: now.Add(lifetime),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> CreateIdTokenAsync(string userId, string clientId, IEnumerable<Claim> userClaims, string[] scopes)
        {
            var claims = new List<Claim>
            {
                new Claim("scope", string.Join(" ", scopes)),
                new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            claims.AddRange(userClaims);

            return Task.FromResult(CreateToken(userId, clientId, claims, TimeSpan.FromMinutes(60)));
        }

        public Task<string> CreateAccessTokenAsync(string userId, string audience, IEnumerable<Claim> userClaims, string[] scopes)
        {
            var claims = new List<Claim>
            {
                new Claim("scope", string.Join(" ", scopes))
            };

            claims.AddRange(userClaims);

            return Task.FromResult(CreateToken(userId, audience, claims, TimeSpan.FromMinutes(60)));
        }
    }
}