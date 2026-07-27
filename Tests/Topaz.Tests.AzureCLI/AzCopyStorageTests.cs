namespace Topaz.Tests.AzureCLI;

/// <summary>
/// Tests azcopy against the Topaz blob storage emulator.
/// Scenario 1: Upload a local file to a blob container via SAS URL.
/// Scenario 2: Server-side copy (S2S) between two storage accounts in different regions.
/// </summary>
public class AzCopyStorageTests : AzCopyFixture
{
    private const string ResourceGroup = "rg-azcopy-storage";
    private const string BlobPort = "8891";

    // azcopy needs --trusted-microsoft-suffixes so it treats the custom domain
    // as an Azure Storage endpoint (enables proper auth/header handling).
    private const string TrustedSuffixes = "blob.storage.topaz.local.dev";

    [Test]
    public async Task AzCopy_UploadLocalFile_BlobAppearsInContainer()
    {
        const string account = "topazazcopyupload01";
        const string container = "uploads";
        const string blobName = "hello.txt";
        const string localContent = "Hello from Topaz azcopy test!";

        await RunAzureCliCommand($"az group create -n {ResourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create -n {account} -g {ResourceGroup} -l westeurope --sku Standard_LRS");

        var key = await GetAzureCliRawOutput(
            $"az storage account keys list -n {account} -g {ResourceGroup} --query '[0].value' -o tsv");
        var connStr = BuildConnStr(account, key, BlobPort);

        await EnsureAzureCliBlobHostMapping(account);

        await RunAzureCliCommand(
            $"az storage container create -n {container} --connection-string '{connStr}'");

        // Generate container-level SAS (write + create permissions, far future expiry)
        var sas = await GetAzureCliRawOutput(
            $"az storage container generate-sas -n {container} --permissions rwdl " +
            $"--expiry 2099-01-01T00:00:00Z --connection-string '{connStr}' -o tsv");

        await EnsureAzCopyBlobHostMapping(account);

        // Create a local file and upload it
        var uploadUrl =
            $"https://{account}.blob.storage.topaz.local.dev:{BlobPort}/{container}/{blobName}?{sas}";

        await RunAzCopyCommand(
            $"echo '{localContent}' > /tmp/{blobName} && " +
            $"azcopy copy /tmp/{blobName} '{uploadUrl}' " +
            $"--trusted-microsoft-suffixes={TrustedSuffixes}");

        // Verify the blob exists via Azure CLI
        await RunAzureCliCommand(
            $"az storage blob exists -c {container} -n {blobName} --connection-string '{connStr}'",
            resp => Assert.That(resp["exists"]!.GetValue<bool>(), Is.True));
    }

    [Test]
    public async Task AzCopy_ServerSideCopy_BlobCopiedBetweenAccountsInDifferentRegions()
    {
        const string accountSrc = "topazazcopysrc01";
        const string accountDst = "topazazcopydst01";
        const string containerSrc = "source-container";
        const string containerDst = "dest-container";
        const string blobName = "cross-account.txt";

        // Create source account (West Europe) and destination account (East US)
        await RunAzureCliCommand($"az group create -n {ResourceGroup}-s2s -l westeurope");
        await RunAzureCliCommand(
            $"az storage account create -n {accountSrc} -g {ResourceGroup}-s2s -l westeurope --sku Standard_LRS");
        await RunAzureCliCommand(
            $"az storage account create -n {accountDst} -g {ResourceGroup}-s2s -l eastus --sku Standard_LRS");

        var keySrc = await GetAzureCliRawOutput(
            $"az storage account keys list -n {accountSrc} -g {ResourceGroup}-s2s --query '[0].value' -o tsv");
        var keyDst = await GetAzureCliRawOutput(
            $"az storage account keys list -n {accountDst} -g {ResourceGroup}-s2s --query '[0].value' -o tsv");

        var connSrc = BuildConnStr(accountSrc, keySrc, BlobPort);
        var connDst = BuildConnStr(accountDst, keyDst, BlobPort);

        await EnsureAzureCliBlobHostMapping(accountSrc);
        await EnsureAzureCliBlobHostMapping(accountDst);

        await RunAzureCliCommand($"az storage container create -n {containerSrc} --connection-string '{connSrc}'");
        await RunAzureCliCommand($"az storage container create -n {containerDst} --connection-string '{connDst}'");

        // Upload a seed blob to the source account via azcopy
        var sasSrc = await GetAzureCliRawOutput(
            $"az storage container generate-sas -n {containerSrc} --permissions rl " +
            $"--expiry 2099-01-01T00:00:00Z --connection-string '{connSrc}' -o tsv");
        var sasDst = await GetAzureCliRawOutput(
            $"az storage container generate-sas -n {containerDst} --permissions rwdl " +
            $"--expiry 2099-01-01T00:00:00Z --connection-string '{connDst}' -o tsv");

        await EnsureAzCopyBlobHostMapping(accountSrc);
        await EnsureAzCopyBlobHostMapping(accountDst);

        // Seed source blob
        var seedUrl =
            $"https://{accountSrc}.blob.storage.topaz.local.dev:{BlobPort}/{containerSrc}/{blobName}?{sasSrc.Replace("rl", "rwdl")}";

        // Re-generate write SAS for upload seed step
        var sasSrcWrite = await GetAzureCliRawOutput(
            $"az storage container generate-sas -n {containerSrc} --permissions rwdl " +
            $"--expiry 2099-01-01T00:00:00Z --connection-string '{connSrc}' -o tsv");
        var uploadSeedUrl =
            $"https://{accountSrc}.blob.storage.topaz.local.dev:{BlobPort}/{containerSrc}/{blobName}?{sasSrcWrite}";

        await RunAzCopyCommand(
            $"echo 'S2S cross-account blob' > /tmp/{blobName} && " +
            $"azcopy copy /tmp/{blobName} '{uploadSeedUrl}' " +
            $"--trusted-microsoft-suffixes={TrustedSuffixes}");

        // Server-side copy: source → destination (azcopy sends PUT x-ms-copy-source to dst)
        var srcBlobUrl =
            $"https://{accountSrc}.blob.storage.topaz.local.dev:{BlobPort}/{containerSrc}/{blobName}?{sasSrc}";
        var dstBlobUrl =
            $"https://{accountDst}.blob.storage.topaz.local.dev:{BlobPort}/{containerDst}/{blobName}?{sasDst}";

        await RunAzCopyCommand(
            $"azcopy copy '{srcBlobUrl}' '{dstBlobUrl}' " +
            $"--trusted-microsoft-suffixes={TrustedSuffixes}");

        // Verify blob exists in destination account
        await RunAzureCliCommand(
            $"az storage blob exists -c {containerDst} -n {blobName} --connection-string '{connDst}'",
            resp => Assert.That(resp["exists"]!.GetValue<bool>(), Is.True));
    }

    private static string BuildConnStr(string accountName, string accountKey, string blobPort) =>
        $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};" +
        $"BlobEndpoint=https://{accountName}.blob.storage.topaz.local.dev:{blobPort};";
}
