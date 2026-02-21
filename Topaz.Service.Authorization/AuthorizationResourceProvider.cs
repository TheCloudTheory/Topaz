using System.Text.Json;
using Topaz.Service.Authorization.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

internal sealed class ResourceAuthorizationResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceAuthorizationService>(logger);
internal sealed class ResourceGroupAuthorizationResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceGroupAuthorizationService>(logger);
internal sealed class SubscriptionAuthorizationResourceProvider(ITopazLogger logger) : ResourceProviderBase<SubscriptionAuthorizationService>(logger)
{
    private readonly ITopazLogger _logger = logger;

    public RoleDefinitionResource[] ListBuiltInRoles(SubscriptionIdentifier subscriptionIdentifier)
    {
        _logger.LogDebug(nameof(SubscriptionAuthorizationResourceProvider), nameof(ListBuiltInRoles), "List built-in roles for `{0}` subscription.", subscriptionIdentifier);
        
        var definitions = new List<RoleDefinitionResource>();
        var rawFiles = Directory.EnumerateFiles("Data", "*.json", SearchOption.AllDirectories);
        foreach (var file in rawFiles)
        {
            _logger.LogDebug(nameof(SubscriptionAuthorizationResourceProvider), nameof(ListBuiltInRoles), "Loading contents of a `{0}` file as role definition.", file);
            
            var content = File.ReadAllText(file);
            var fileModel = JsonSerializer.Deserialize<RoleDefinition>(content, GlobalSettings.JsonOptions);

            if (fileModel == null)
            {
                _logger.LogError($"Could not deserialize `{file}` file as `{nameof(RoleDefinition)}`.");
                continue;
            }

            try
            {
                var definition = fileModel.ToRoleDefinitionResource(subscriptionIdentifier);
                definitions.Add(definition);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Could not convert `{file}` to `{nameof(RoleDefinitionResource)}`: {ex.Message}");
            }
        }
        
        return definitions.ToArray();
    }
}