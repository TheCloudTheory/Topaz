using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Topaz.Identity;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public sealed class TopazArmClient(AzureLocalCredential credentials) : IDisposable
{
    private const ushort EmulatorPort = GlobalSettings.DefaultResourceManagerPort;

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri($"https://topaz.local.dev:{EmulatorPort}/")
    };

    /// <summary>
    /// Creates a subscription using REST interface exposed by Topaz
    /// </summary>
    public async Task CreateSubscriptionAsync(Guid subscriptionId, string subscriptionName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"subscriptions/{subscriptionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
           (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);
        
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

    public async Task PurgeKeyVault(Guid subscriptionId, string keyVaultName, string location)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/locations/{location}/deletedVaults/{keyVaultName}/purge");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);
        var response = await _httpClient.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateSubscriptionAsync(Guid subscriptionId, string subscriptionName,
        Dictionary<string, string> tags)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"subscriptions/{subscriptionId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);
        
        var payload = new
        {
            SubscriptionName = subscriptionName,
            Tags = tags
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

    public async Task CancelSubscriptionAsync(Guid subscriptionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"subscriptions/{subscriptionId}/providers/Microsoft.Subscription/cancel");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);
        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(message);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task EnableSubscriptionAsync(Guid subscriptionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"subscriptions/{subscriptionId}/providers/Microsoft.Subscription/enable");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);
        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(message);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<JsonNode> ExportTemplateAsync(Guid subscriptionId, string resourceGroupName, string? options = null, string[]? resources = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/exportTemplate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);

        var payload = new { options, resources };
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, GlobalSettings.JsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Resource group not found: {message}", null, HttpStatusCode.NotFound);
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }

    public async Task<JsonNode> ExportDeploymentTemplateAsync(Guid subscriptionId, string resourceGroupName, string deploymentName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}/exportTemplate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);
        request.Content = new ByteArrayContent([]);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Deployment not found: {message}", null, HttpStatusCode.NotFound);
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }

    public async Task CreateManagementGroupAsync(string groupId, string displayName)
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            $"providers/Microsoft.Management/managementGroups/{groupId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);

        var payload = new { properties = new { displayName } };
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, GlobalSettings.JsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<JsonNode> ListDeploymentsAtManagementGroupScopeAsync(string groupId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            (await credentials.GetTokenAsync(new TokenRequestContext(), CancellationToken.None)).Token);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Management group not found: {message}", null, HttpStatusCode.NotFound);
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}