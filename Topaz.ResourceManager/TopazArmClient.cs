using System.Text.Json;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public sealed class TopazArmClient : IDisposable
{
    private const short EmulatorPort = 8899;

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri($"https://localhost:{EmulatorPort}/"),
    };

    /// <summary>
    /// Creates a subscription using REST interface exposed by Topaz
    /// </summary>
    public async Task CreateSubscriptionAsync(Guid subscriptionId, string subscriptionName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"subscriptions/{subscriptionId}");
        var payload = new
        {
            SubscriptionName = subscriptionName,
            SubscriptionId = subscriptionId.ToString()
        };
        var content = new StringContent(JsonSerializer.Serialize(payload, GlobalSettings.JsonOptions));
        request.Content = content;
        var response = await _httpClient.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}