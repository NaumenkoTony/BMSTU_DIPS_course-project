using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

public interface IJwksService
{
    JwksResult GetJwks();
}