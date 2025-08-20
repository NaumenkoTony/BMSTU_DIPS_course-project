using Microsoft.IdentityModel.Tokens;

public class JwksResult
{
    public List<JwkKey> Keys { get; set; } = new();
}

public class JwkKey
{
    public string Kty { get; set; } = string.Empty;
    public string Use { get; set; } = string.Empty;
    public string Kid { get; set; } = string.Empty;
    public string E { get; set; } = string.Empty;
    public string N { get; set; } = string.Empty;
    public string Alg { get; set; } = string.Empty;
}

public class JwksService : IJwksService
{
    private readonly RsaSecurityKey _rsaSecurityKey;

    public JwksService(RsaSecurityKey rsaSecurityKey)
    {
        _rsaSecurityKey = rsaSecurityKey ?? throw new ArgumentNullException(nameof(rsaSecurityKey));
    }

    public JwksResult GetJwks()
    {
        if (_rsaSecurityKey.Rsa == null)
            throw new InvalidOperationException("RSA instance is not available");

        var parameters = _rsaSecurityKey.Rsa.ExportParameters(false);

        if (parameters.Exponent == null || parameters.Modulus == null)
            throw new InvalidOperationException("RSA parameters are invalid");

        var e = Base64UrlEncoder.Encode(parameters.Exponent);
        var n = Base64UrlEncoder.Encode(parameters.Modulus);

        var jwk = new JwkKey
        {
            Kty = "RSA",
            Use = "sig",
            Kid = _rsaSecurityKey.KeyId ?? Guid.NewGuid().ToString("N"),
            E = e,
            N = n,
            Alg = SecurityAlgorithms.RsaSha256
        };

        return new JwksResult
        {
            Keys = new List<JwkKey> { jwk }
        };
    }
}
