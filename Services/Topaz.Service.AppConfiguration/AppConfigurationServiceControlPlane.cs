using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.AppConfiguration.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration;

internal sealed class AppConfigurationServiceControlPlane(
    AppConfigurationResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    public static AppConfigurationServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(new AppConfigurationResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        var store = resource.As<ConfigurationStoreResource, ConfigurationStoreResourceProperties>();
        if (store == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a ConfigurationStore instance.");
            return OperationResult.Failed;
        }

        if (string.IsNullOrWhiteSpace(store.Location))
        {
            logger.LogError($"ConfigurationStore resource `{resource.Id}` is missing required location.");
            return OperationResult.Failed;
        }

        try
        {
            var properties = ConfigurationStoreResourceProperties.FromRequest(store.Properties, store.Name);
            var typed = new ConfigurationStoreResource(
                store.GetSubscription(),
                store.GetResourceGroup(),
                store.Name,
                store.Location,
                store.Tags,
                store.Sku,
                properties);

            provider.CreateOrUpdate(store.GetSubscription(), store.GetResourceGroup(), store.Name, typed);
            return OperationResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }
}
