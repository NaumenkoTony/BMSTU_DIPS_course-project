using System.Net.Http;
using System.Net.Http.Headers;
using StackExchange.Redis;

class LoyaltyQueueProcessor(IHttpClientFactory httpClientFactory, IConnectionMultiplexer redis) : BackgroundService
{
    private readonly IConnectionMultiplexer redis = redis;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

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
                    Console.WriteLine("Invalid token in queue");
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                var loyaltyClient = httpClientFactory.CreateClient("LoyaltyService");
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties/degrade");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await loyaltyClient.SendAsync(request, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    await db.ListRightPushAsync("loyalty-queue", accessToken);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await db.ListRightPushAsync("loyalty-queue", accessToken);
                    Console.WriteLine($"LoyaltyQueueProcessor error {accessToken}. Return to queue: {ex.Message}");
                }
            }
        }
    }
}