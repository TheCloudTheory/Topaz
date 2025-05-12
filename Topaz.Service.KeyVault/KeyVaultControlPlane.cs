namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultControlPlane(ResourceProvider provider)
{
    private readonly ResourceProvider provider = provider;

    public Models.KeyVault Create(string name, string resourceGroup, string location, string subscriptionId)
    {
        var model = new Models.KeyVault(name, resourceGroup, location, subscriptionId);

        this.provider.Create(name, model);

        return model;
    }
}
