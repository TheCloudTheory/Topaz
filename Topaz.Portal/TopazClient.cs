using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Graph;
using Topaz.Identity;
using Topaz.Portal.Models.Auth;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.ResourceManager;
using Topaz.Portal.Models.Subscriptions;
using Topaz.Portal.Models.Tenant;
using Topaz.ResourceManager;

namespace Topaz.Portal;

public class TopazClient
{
    private readonly ArmClient _armClient;
    private readonly HttpClient _httpClient;
    private readonly GraphServiceClient _graphClient;

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

        _graphClient = new GraphServiceClient(httpClientFactory.CreateClient(),
            new LocalGraphAuthenticationProvider(), armBaseUrl);
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
        var resourceGroups = new List<ResourceGroupDto>();

        foreach (var subscription in subscriptions.Value)
        {
            await foreach (var rg in _armClient
                               .GetSubscriptionResource(
                                   new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"))
                               .GetResourceGroups().GetAllAsync())
            {
                resourceGroups.Add(new ResourceGroupDto
                {
                    Id = rg.Id.ToString(),
                    Name = rg.Data.Name,
                    Location = rg.Data.Location,
                    SubscriptionId = subscription.SubscriptionId,
                    SubscriptionName = subscription.DisplayName
                });
            }
        }

        return new ListResourceGroupsResponse
        {
            Value = resourceGroups.ToArray()
        };
    }

    public async Task<ResourceGroupDto?> GetResourceGroup(Guid subscriptionId, string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        var subscription = await _armClient
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);
        var rgResponse = await _armClient
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var rg = rgResponse.Value;

        return new ResourceGroupDto
        {
            Id = rg.Id.ToString(),
            Name = rg.Data.Name,
            Location = rg.Data.Location,
            SubscriptionId = subscriptionId.ToString(),
            SubscriptionName = subscription.Value.Data.DisplayName
        };
    }

    public async Task<ListDeploymentsResponse> ListDeployments(Guid subscriptionId, string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        var rg = await _armClient.GetSubscriptionResource(
                new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);
        var deployments = new List<DeploymentDto>();
        await foreach (var deployment in rg.Value.GetArmDeployments()
                           .GetAllAsync(cancellationToken: cancellationToken))
        {
            deployments.Add(new DeploymentDto
            {
                Id = deployment.Id.ToString(),
                Name = deployment.Data.Name,
                Type = deployment.Data.ResourceType,
                Location = deployment.Data.Location,
                Properties = new DeploymentPropertiesDto
                {
                    Mode = deployment.Data.Properties.Mode.HasValue
                        ? deployment.Data.Properties.Mode.Value.ToString()
                        : string.Empty,
                    ProvisioningState = deployment.Data.Properties.ProvisioningState.HasValue
                        ? deployment.Data.Properties.ProvisioningState.Value.ToString()
                        : string.Empty,
                    Timestamp = deployment.Data.Properties.Timestamp,
                }
            });
        }

        return new ListDeploymentsResponse()
        {
            Value = deployments.ToArray()
        };
    }

    public async Task CreateResourceGroup(Guid subscriptionId, string resourceGroupName, string location,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var rgCollection = subscription.GetResourceGroups();

        var rgData = new ResourceGroupData(new AzureLocation(location));

        _ = await rgCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            resourceGroupName,
            rgData,
            cancellationToken);
    }
    
    public async Task<(TokenResponse? Token, string? ErrorMessage)> GetAuthTokenWithError(string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://topaz.local.dev:8899/organizations/oauth2/v2.0/token?grant_type=password&client_id={Guid.NewGuid()}&username={username}&password={password}");

        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var apiError = JsonSerializer.Deserialize<AuthErrorResponse>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var message = apiError?.GetBestDescription();
                if (!string.IsNullOrWhiteSpace(message))
                    return (null, message);
            }
            catch
            {
                // Ignore parse errors; fall back below.
            }

            // Fallback: don’t leak raw body to UI; keep it simple.
            return (null, "Sign-in failed. Please check your credentials and try again.");
        }

        var token = JsonSerializer.Deserialize<TokenResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (string.IsNullOrWhiteSpace(token?.AccessToken))
            return (null, "Sign-in failed: token was missing.");

        return (token, null);
    }

    public async Task<TenantInformationResponse> GetDirectoryInfo()
    {
        var directory = await _graphClient.Directory.GetAsync();
        var tenantInformation =
            await _graphClient.TenantRelationships.FindTenantInformationByTenantIdWithTenantId(directory!.Id)
                .GetAsync();

        var users = await _graphClient.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var groups = await _graphClient.Groups.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var servicePrincipals = await _graphClient.ServicePrincipals.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var applications = await _graphClient.Applications.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        return TenantInformationResponse.FromGraph(
            tenantInformation!,
            users?.OdataCount,
            groups?.OdataCount,
            servicePrincipals?.OdataCount,
            applications?.OdataCount);
    }
}