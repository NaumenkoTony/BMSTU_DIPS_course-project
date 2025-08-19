using Microsoft.IdentityModel.Tokens;

public class JwksService : IJwksService
{
    private readonly RsaSecurityKey _rsaSecurityKey;

    public JwksService(RsaSecurityKey rsaSecurityKey)
    {
        _rsaSecurityKey = rsaSecurityKey;
    }

    public object GetJwks()
    {
        var parameters = _rsaSecurityKey.Rsa.ExportParameters(false);

        var e = Base64UrlEncoder.Encode(parameters.Exponent);
        var n = Base64UrlEncoder.Encode(parameters.Modulus);

        var jwk = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = _rsaSecurityKey.KeyId,
                    e = e,
                    n = n,
                    alg = SecurityAlgorithms.RsaSha256,
                }
            }
        };

        return jwk;
    }
}
