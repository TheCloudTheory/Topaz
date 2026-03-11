using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Resources;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Topaz.Identity;
using Topaz.Portal.Models.Rbac;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.ResourceManager;
using Topaz.Portal.Models.Subscriptions;
using Topaz.Portal.Models.Tenant;
using Topaz.ResourceManager;

namespace Topaz.Portal;

internal sealed class TopazClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AccountSession _session;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    
    private ArmClient? _armClient;
    private GraphServiceClient? _graphClient;
    private bool _initialized;

    public TopazClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, AccountSession session)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _session = session;

        _httpClient = httpClientFactory.CreateClient();

        var armBaseUrl = configuration["Topaz:ArmBaseUrl"];
        if (!string.IsNullOrWhiteSpace(armBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(armBaseUrl);
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            // Load session if not already loaded
            if (_session.Token is null)
            {
                await _session.LoadAsync();
            }

            if (_session.Token is null)
                throw new InvalidOperationException("Session not loaded. User must be authenticated.");

            var credentials = new AzureFixedTokenLocalCredential(_session.Token);
            _armClient = new ArmClient(credentials, Guid.Empty.ToString(), TopazArmClientOptions.New);

            _graphClient = new GraphServiceClient(_httpClientFactory.CreateClient(),
                new LocalGraphFixedTokenAuthenticationProvider(_session.Token),
                _configuration["Topaz:ArmBaseUrl"]);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<ListSubscriptionsResponse> ListSubscriptions()
    {
        await EnsureInitializedAsync();
        
        var subscriptions = new List<SubscriptionResource>();

        await foreach (var subscription in _armClient!.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(subscription);
        }

        return new ListSubscriptionsResponse
        {
            Value = subscriptions.Select(sub => new SubscriptionDto
            {
                DisplayName = sub.Data.DisplayName,
                SubscriptionId = sub.Data.SubscriptionId,
                Id = sub.Id.ToString(),
                Tags = sub.Data.Tags is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(sub.Data.Tags, StringComparer.OrdinalIgnoreCase)
            }).ToArray()
        };
    }
    
    public async Task<SubscriptionDto?> GetSubscription(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"))
            .GetAsync(cancellationToken);

        return new SubscriptionDto
        {
            DisplayName = subscription.Value.Data.DisplayName,
            SubscriptionId = subscription.Value.Data.SubscriptionId,
            Id = subscription.Value.Id.ToString(),
            Tags = subscription.Value.Data.Tags is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(subscription.Value.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }
    
    public async Task CreateOrUpdateSubscriptionTag(
        Guid subscriptionId,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value is required.", nameof(tagValue));
        
        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"))
            .GetAsync(cancellationToken);
        
        var payload = new
        {
            SubscriptionName = subscription.Value.Data.DisplayName,
            Tags = subscription.Value.Data.Tags is null
                ? new Dictionary<string, string> { { tagName, tagValue } }
                : new Dictionary<string, string>(subscription.Value.Data.Tags, StringComparer.OrdinalIgnoreCase) { { tagName, tagValue } }
        };

        using var resp =
            await _httpClient.PatchAsJsonAsync($"/subscriptions/{subscriptionId}", payload, cancellationToken);
        
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating subscription failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task CreateSubscription(Guid subscriptionId, string subscriptionName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
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
        await EnsureInitializedAsync();
        
        var subscriptions = await ListSubscriptions();
        var resourceGroups = new List<ResourceGroupDto>();

        foreach (var subscription in subscriptions.Value)
        {
            await foreach (var rg in _armClient!
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
        await EnsureInitializedAsync();
        
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);
        var rgResponse = await _armClient!
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
        await EnsureInitializedAsync();
        
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        var rg = await _armClient!.GetSubscriptionResource(
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
        await EnsureInitializedAsync();
        
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var subscription = _armClient!.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var rgCollection = subscription.GetResourceGroups();

        var rgData = new ResourceGroupData(new AzureLocation(location));

        _ = await rgCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            resourceGroupName,
            rgData,
            cancellationToken);
    }
    
    public async Task<IReadOnlyList<User>> ListUsers(
        int top = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        var resp = await _graphClient!.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = top;

            // Keep payload small and UI-focused
            cfg.QueryParameters.Select =
            [
                "id",
                "displayName",
                "userPrincipalName",
                "mail",
                "accountEnabled",
                "userType"
            ];

            cfg.QueryParameters.Orderby = ["displayName"];
        }, cancellationToken);

        return resp?.Value ?? [];
    }

    public async Task CreateUser(
        string displayName,
        string userPrincipalName,
        string? mail,
        string password,
        bool accountEnabled = true,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        if (string.IsNullOrWhiteSpace(userPrincipalName))
            throw new ArgumentException("User principal name is required.", nameof(userPrincipalName));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var newUser = new User
        {
            DisplayName = displayName,
            UserPrincipalName = userPrincipalName,
            Mail = mail,
            AccountEnabled = accountEnabled,
            MailNickname = userPrincipalName.Split('@')[0],
            PasswordProfile = new PasswordProfile
            {
                Password = password,
                ForceChangePasswordNextSignIn = false
            }
        };

        await _graphClient!.Users.PostAsync(newUser, cancellationToken: cancellationToken);
    }

    public async Task<TenantInformationResponse> GetDirectoryInfo()
    {
        await EnsureInitializedAsync();
        
        var directory = await _graphClient!.Directory.GetAsync();
        var tenantInformation =
            await _graphClient!.TenantRelationships.FindTenantInformationByTenantIdWithTenantId(directory!.Id)
                .GetAsync();

        var users = await _graphClient!.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var groups = await _graphClient!.Groups.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var servicePrincipals = await _graphClient!.ServicePrincipals.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var applications = await _graphClient!.Applications.GetAsync(cfg =>
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
    
    public async Task<ListRoleDefinitionsResponse?> ListRoleDefinitions(
        Guid subscriptionId,
        string? roleNameFilter = null,
        int pageSize = 5,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"));

        var filter = string.IsNullOrWhiteSpace(roleNameFilter)
            ? null
            : $"roleName eq '{roleNameFilter.Replace("'", string.Empty)}'";

        var pageable = subscription.GetAuthorizationRoleDefinitions().GetAllAsync(
            filter: filter,
            cancellationToken: cancellationToken);

        await using var pageEnumerator = pageable
            .AsPages(continuationToken, pageSizeHint: pageSize)
            .GetAsyncEnumerator(cancellationToken);

        if (!await pageEnumerator.MoveNextAsync())
            return new ListRoleDefinitionsResponse { Value = [], ContinuationToken = null };

        var page = pageEnumerator.Current;

        var items = page.Values.Select(rd => new RoleDefinitionDto
        {
            Id = rd.Id.ToString(),
            Name = rd.Data.Name,
            Type = rd.Data.ResourceType.ToString(),
            Properties = new RoleDefinitionPropertiesDto
            {
                RoleName = rd.Data.RoleName,
                Description = rd.Data.Description,
                RoleType = rd.Data.RoleType.ToString()
            }
        }).ToArray();

        return new ListRoleDefinitionsResponse
        {
            Value = items,
            ContinuationToken = page.ContinuationToken
        };
    }

    public async Task<ListRoleAssignmentsResponse?> ListRoleAssignments(
        Guid subscriptionId,
        string? roleNameFilter = null,
        int pageSize = 5,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId:D}"));

        var filter = string.IsNullOrWhiteSpace(roleNameFilter)
            ? null
            : $"roleName eq '{roleNameFilter.Replace("'", string.Empty)}'";

        var pageable = subscription.GetRoleAssignments().GetAllAsync(
            filter: filter,
            cancellationToken: cancellationToken);

        await using var pageEnumerator = pageable
            .AsPages(continuationToken, pageSizeHint: pageSize)
            .GetAsyncEnumerator(cancellationToken);

        if (!await pageEnumerator.MoveNextAsync())
            return new ListRoleAssignmentsResponse { Value = [], ContinuationToken = null };

        var page = pageEnumerator.Current;

        var items = page.Values.Select(ra => new RoleAssignmentDto
        {
            Id = ra.Id.ToString(),
            Name = ra.Data.Name,
            Type = ra.Data.ResourceType.ToString(),
            Properties = new RoleAssignmentPropertiesDto
            {
                RoleDefinitionId = ra.Data.RoleDefinitionId?.ToString(),
                PrincipalId = ra.Data.PrincipalId?.ToString(),
                PrincipalType = ra.Data.PrincipalType?.ToString(),
                Scope = ra.Data.Scope?.ToString()
            }
        }).ToArray();

        return new ListRoleAssignmentsResponse
        {
            Value = items,
            ContinuationToken = page.ContinuationToken
        };
    }
}