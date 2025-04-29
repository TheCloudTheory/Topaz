namespace Azure.Local.Service.Storage;

internal sealed class ResourceProvider
{
    public void Create(string name)
    {
        var accountPath = Path.Combine(AzureStorageService.LocalDirectoryPath, name);
        if(Directory.Exists(accountPath)) return;

        Directory.CreateDirectory(accountPath);
    }
}
