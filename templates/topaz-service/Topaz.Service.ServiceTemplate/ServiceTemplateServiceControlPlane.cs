using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceTemplate;

internal sealed class ServiceTemplateServiceControlPlane(
    Pipeline eventPipeline,
    ServiceTemplateResourceProvider provider,
    ITopazLogger logger) : IControlPlane
{
    private const string NotFoundCode = "ResourceNotFound";
    private const string NotFoundMessage = "ServiceTemplate resource '{0}' could not be found.";

    public static ServiceTemplateServiceControlPlane New(Pipeline eventPipeline, ITopazLogger logger) =>
        new(eventPipeline, new ServiceTemplateResourceProvider(logger), logger);

    public OperationResult Deploy(GenericResource resource)
    {
        // TODO: replace MyResource / MyResourceProperties with your actual model types
        // var typed = resource.As<MyResource, MyResourceProperties>();
        // if (typed == null)
        // {
        //     logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a ServiceTemplate instance.");
        //     return OperationResult.Failed;
        // }

        try
        {
            // TODO: call CreateOrUpdate / other provider methods here
            return OperationResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return OperationResult.Failed;
        }
    }
}
