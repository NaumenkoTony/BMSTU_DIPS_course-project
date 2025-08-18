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
            _issuer = config["Issuer"] ?? "https://localhost:8000";
        }

        public Task<string> CreateIdTokenAsync(string userId, string clientId, IEnumerable<Claim> userClaims, IEnumerable<string> scopes)
        {
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, _issuer),
                new Claim(JwtRegisteredClaimNames.Aud, clientId),
                new Claim("scope", string.Join(" ", scopes))
            };

            claims.AddRange(userClaims);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: clientId,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(60),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            return Task.FromResult(tokenString);
        }

        public Task<string> CreateAccessTokenAsync(string userId, string clientId, IEnumerable<Claim> userClaims, IEnumerable<string> scopes)
        {
            // Для простоты access_token сделаем похожим, но без подробной информации профиля
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, _issuer),
                new Claim(JwtRegisteredClaimNames.Aud, clientId),
                new Claim("scope", string.Join(" ", scopes))
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: clientId,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(60),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            return Task.FromResult(tokenString);
        }
    }
}
