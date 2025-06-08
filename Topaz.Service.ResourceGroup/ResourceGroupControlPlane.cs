using System.Net;
using System.Text.Json;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

internal sealed class ResourceGroupControlPlane(ResourceProvider provider)
{
    private readonly ResourceProvider provider = provider;

    public Models.ResourceGroup Get(string name)
    {
        var data = this.provider.Get(name);
        var model = JsonSerializer.Deserialize<Models.ResourceGroup>(data, GlobalSettings.JsonOptions);

        return model!;
    }

    public Models.ResourceGroup Create(string name, string subscriptionId, string location)
    {
        var model = new Models.ResourceGroup(name, subscriptionId, location);

        this.provider.Create(name, model);

        return model;
    }

    public (Models.ResourceGroup data, HttpStatusCode code) CreateOrUpdate(string name, string subscriptionId, Stream input)
    {
        var model = this.provider.CreateOrUpdate<Models.ResourceGroup, CreateOrUpdateRequest>(name, input, (req) 
            => new Models.ResourceGroup(name, subscriptionId, req.Location!));

        return model;
    }
}
