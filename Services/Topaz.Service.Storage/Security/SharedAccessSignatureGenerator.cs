using Azure.Storage;
using Azure.Storage.Sas;
using Topaz.Shared;

namespace Topaz.Service.Storage.Security;

public sealed class SharedAccessSignatureGenerator(ITopazLogger logger)
{
    public string CreateServiceSasContainer(
        string accountName, string accountKey, string containerName)
    {
            // Create a SAS token that's valid for one day
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                Resource = "c"
            };

            sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.Write);

            var sasUri = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(accountName, accountKey));
            logger.LogDebug(nameof(SharedAccessSignatureGenerator), nameof(CreateServiceSasContainer), "Generated SAS URI: {SasUri}", sasUri);
            
            return sasUri.ToString();
    }
}