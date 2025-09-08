using System.Net;
using System.Text.Json;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public sealed class TopazArmClient : IDisposable
{
    private const ushort EmulatorPort = GlobalSettings.DefaultResourceManagerPort;

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri($"https://topaz.local.dev:{EmulatorPort}/"),
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
        
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            // Throw a meaningful exception rather than a vague `Response status code does not indicate success: 400 (Bad Request).`
            // This helps in debugging.
            var message = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(message);
        }
        
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}