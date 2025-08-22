using System.Net.Http;
using System.Net.Http.Headers;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;

class LoyaltyQueueProcessor(IHttpClientFactory httpClientFactory, IInternalTokenService internalTokenService, IConnectionMultiplexer redis) : BackgroundService
{
    private readonly IConnectionMultiplexer redis = redis;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IInternalTokenService internalTokenService = internalTokenService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = redis.GetDatabase();
        while (!stoppingToken.IsCancellationRequested)
        {
            string? accessToken = await db.ListLeftPopAsync("loyalty-queue");
            try
            {
                if (accessToken == null)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                if (IsTokenExpired(accessToken))
                {
                    Console.WriteLine($"Token expired: {accessToken}. Removing from queue.");
                    continue;
                }

                var loyaltyClient = httpClientFactory.CreateClient("LoyaltyQueueService");
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties/degrade");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await loyaltyClient.SendAsync(request, stoppingToken);
                
                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                {
                    await db.ListRightPushAsync("loyalty-queue", accessToken);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine($"Token invalid: {accessToken}. Removing from queue.");
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(accessToken) && !IsTokenExpired(accessToken))
                {
                    await db.ListRightPushAsync("loyalty-queue", accessToken);
                    Console.WriteLine($"LoyaltyQueueProcessor error {accessToken}. Return to queue: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"LoyaltyQueueProcessor error. Token expired or invalid: {ex.Message}");
                }
            }
        }
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}