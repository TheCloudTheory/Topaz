using System.Text.Json;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Topaz.Identity;
using Topaz.Portal.Models.Auth;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.Subscriptions;
using Topaz.ResourceManager;

namespace Topaz.Portal;

public class TopazClient
{
    private readonly ArmClient _armClient;
    private readonly HttpClient _httpClient;

    public TopazClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        _armClient = new ArmClient(credentials, Guid.Empty.ToString(), TopazArmClientOptions.New);

        _httpClient = httpClientFactory.CreateClient();

        var armBaseUrl = configuration["Topaz:ArmBaseUrl"];
        if (!string.IsNullOrWhiteSpace(armBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(armBaseUrl);
        }
    }

    public async Task<ListSubscriptionsResponse> ListSubscriptions()
    {
        var subscriptions = new List<SubscriptionResource>();

        await foreach (var subscription in _armClient.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(subscription);
        }

        return new ListSubscriptionsResponse
        {
            Value = subscriptions.Select(sub => new SubscriptionDto
            {
                DisplayName = sub.Data.DisplayName,
                SubscriptionId = sub.Data.SubscriptionId,
                Id = sub.Id.ToString()
            }).ToArray()
        };
    }

    public async Task CreateSubscription(Guid subscriptionId, string subscriptionName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Subscription name is required.", nameof(subscriptionName));

        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("Topaz:ArmBaseUrl is not configured.");

        var payload = new
        {
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName
        };

        using var resp =
            await _httpClient.PostAsJsonAsync($"/subscriptions/{subscriptionId}", payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Create subscription failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<ListResourceGroupsResponse> ListResourceGroups()
    {
        var subscriptions = await ListSubscriptions();
        var resourceGroups = new List<ResourceGroupResource>();

        foreach (var subscription in subscriptions.Value)
        {
            await foreach (var rg in _armClient
                               .GetSubscriptionResource(
                                   new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"))
                               .GetResourceGroups().GetAllAsync())
            {
                resourceGroups.Add(rg);
            }
        }
        
        return new ListResourceGroupsResponse
        {
            Value = resourceGroups.Select(rg => new ResourceGroupDto
            {
                Id = rg.Id.ToString(),
                Name = rg.Data.Name,
                Location = rg.Data.Location
            }).ToArray()
        };       
    }

    public async Task<TokenResponse?> GetAuthToken(string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://topaz.local.dev:8899/organizations/oauth2/v2.0/token?grant_type=password&client_id={Guid.NewGuid()}&username={username}&password={password}");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<TokenResponse>(content);

        return token;
    }
}