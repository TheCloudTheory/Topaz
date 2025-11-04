using Topaz.Service.Shared;

namespace Topaz.ResourceManager;

public interface IControlPlane
{
    OperationResult Deploy(GenericResource resource);
}