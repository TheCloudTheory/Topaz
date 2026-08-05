using System.Text.Json.Nodes;
using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ApiManagementTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
    private const string SubscriptionName = "apim-sub";
    private const string ResourceGroupName = "apim-rg";
    private const string ServiceName = "my-apim";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "apim", "delete",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "create",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString(),
            "--publisher-email", "admin@example.com",
            "--publisher-name", "Test Publisher"
        ]);
    }

    private static string MetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription",
        SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
        ".apim", ServiceName, "metadata.json");

    [Test]
    public void ApiManagement_Create_ResourceIsPersistedToDisk()
    {
        Assert.That(File.Exists(MetadataPath), Is.True);
    }

    [Test]
    public async Task ApiManagement_Show_ReturnsExistingService()
    {
        var code = await Program.RunAsync([
            "apim", "show",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_List_ByResourceGroup_ReturnsServices()
    {
        var code = await Program.RunAsync([
            "apim", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_List_BySubscription_ReturnsServices()
    {
        var code = await Program.RunAsync([
            "apim", "list",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Update_ReturnsUpdatedService()
    {
        var code = await Program.RunAsync([
            "apim", "update",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--publisher-email", "updated@example.com",
            "--publisher-name", "Updated Publisher"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_CheckName_WhenNameIsTaken_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "check-name",
            "--name", ServiceName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_CheckName_WhenNameIsAvailable_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "check-name",
            "--name", "nonexistent-apim-service",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Delete_SoftDeletes()
    {
        var code = await Program.RunAsync([
            "apim", "delete",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(code, Is.Zero);
            Assert.That(File.Exists(MetadataPath), Is.True);
        }
    }

    [Test]
    public async Task ApiManagement_Create_WhenServiceAlreadyExists_UpdatesAndSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "create",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString(),
            "--publisher-email", "updated@example.com",
            "--publisher-name", "Updated Publisher"
        ]);

        Assert.That(code, Is.Zero);
    }

    // --- API DataPlane ---

    [Test]
    public async Task ApiManagement_Api_Create_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--protocols", "https",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Api_Show_ReturnsExistingApi()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "api", "show",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Api_List_ReturnsApis()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "api", "list",
            "--service-name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Api_Update_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);
        
        var etagFile = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".apim", ServiceName, "apis-etag", "test-api", "metadata.json");
        
        Assert.That(File.Exists(etagFile), Is.True);
        
        var etag = JsonNode.Parse(await File.ReadAllTextAsync(etagFile))!["value"];

        var code = await Program.RunAsync([
            "apim", "api", "update",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--if-match", $"\"{etag!}\"",
            "--display-name", "Updated API",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Api_GetEntityTag_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "api", "get-entity-tag",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Api_ListRevisions_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "api", "list-revisions",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Api_Delete_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "apim", "api", "get-entity-tag",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var etagFile = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".apim", ServiceName, "apis-etag", "test-api", "metadata.json");
        
        Assert.That(File.Exists(etagFile), Is.True);
        
        var etag = JsonNode.Parse(await File.ReadAllTextAsync(etagFile))!["value"];

        var code = await Program.RunAsync([
            "apim", "api", "delete",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--if-match", $"\"{etag!}\"",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    // --- Product DataPlane ---

    [Test]
    public async Task ApiManagement_Product_Create_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_Show_ReturnsExistingProduct()
    {
        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "product", "show",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_List_ReturnsProducts()
    {
        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "product", "list",
            "--service-name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_Update_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);
        
        var etagFile = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".apim", ServiceName, "products-etag", "test-product", "metadata.json");
        
        Assert.That(File.Exists(etagFile), Is.True);
        
        var etag = JsonNode.Parse(await File.ReadAllTextAsync(etagFile))!["value"];

        var code = await Program.RunAsync([
            "apim", "product", "update",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Updated Product",
            "--if-match", $"\"{etag}\"",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_GetEntityTag_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "product", "get-entity-tag",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_AddApi_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "product", "add-api",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_CheckApi_WhenAssigned_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "product", "add-api",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "product", "check-api",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_ListApis_ReturnsAssignedApis()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "product", "add-api",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "product", "list-apis",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_RemoveApi_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "api", "create",
            "--service-name", ServiceName,
            "--api-id", "test-api",
            "--display-name", "Test API",
            "--path", "/test",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "product", "add-api",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "product", "remove-api",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--api-id", "test-api",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Product_Delete_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "product", "create",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--display-name", "Test Product",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);
        
        var etagFile = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".apim", ServiceName, "products-etag", "test-product", "metadata.json");
        
        Assert.That(File.Exists(etagFile), Is.True);
        
        var etag = JsonNode.Parse(await File.ReadAllTextAsync(etagFile))!["value"];

        var code = await Program.RunAsync([
            "apim", "product", "delete",
            "--service-name", ServiceName,
            "--product-id", "test-product",
            "--if-match", $"\"{etag}\"",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    // --- Backend DataPlane ---

    [Test]
    public async Task ApiManagement_Backend_Create_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "backend", "create",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://backend.example.com",
            "--protocol", "http",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Backend_Show_ReturnsExistingBackend()
    {
        await Program.RunAsync([
            "apim", "backend", "create",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://backend.example.com",
            "--protocol", "http",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "backend", "show",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Backend_List_ReturnsBackends()
    {
        await Program.RunAsync([
            "apim", "backend", "create",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://backend.example.com",
            "--protocol", "http",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "backend", "list",
            "--service-name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Backend_Update_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "backend", "create",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://backend.example.com",
            "--protocol", "http",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var etagFile = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".apim", ServiceName, "backends-etag", "test-backend", "metadata.json");

        Assert.That(File.Exists(etagFile), Is.True);

        var etag = JsonNode.Parse(await File.ReadAllTextAsync(etagFile))!["value"];

        var code = await Program.RunAsync([
            "apim", "backend", "update",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://new-backend.example.com",
            "--if-match", $"\"{etag}\"",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Backend_GetEntityTag_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "backend", "create",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://backend.example.com",
            "--protocol", "http",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "backend", "get-entity-tag",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Backend_Reconnect_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "backend", "create",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://backend.example.com",
            "--protocol", "http",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "backend", "reconnect",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Backend_Delete_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "backend", "create",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--url", "https://backend.example.com",
            "--protocol", "http",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var etagFile = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
            ".apim", ServiceName, "backends-etag", "test-backend", "metadata.json");

        Assert.That(File.Exists(etagFile), Is.True);

        var etag = JsonNode.Parse(await File.ReadAllTextAsync(etagFile))!["value"];

        var code = await Program.RunAsync([
            "apim", "backend", "delete",
            "--service-name", ServiceName,
            "--backend-id", "test-backend",
            "--if-match", $"\"{etag}\"",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    // --- Policy DataPlane ---

    private const string PolicyId = "policy";
    private const string PolicyValue = "<policies><inbound><base /></inbound><backend><base /></backend><outbound><base /></outbound></policies>";

    private string PolicyEtagFilePath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription",
        SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
        ".apim", ServiceName, "policies-etag", PolicyId, "metadata.json");

    [Test]
    public async Task ApiManagement_Policy_Create_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "policy", "create",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--value", PolicyValue,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Policy_Show_ReturnsExistingPolicy()
    {
        await Program.RunAsync([
            "apim", "policy", "create",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--value", PolicyValue,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "policy", "show",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Policy_List_ReturnsPolicies()
    {
        await Program.RunAsync([
            "apim", "policy", "create",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--value", PolicyValue,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "policy", "list",
            "--service-name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Policy_GetEntityTag_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "policy", "create",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--value", PolicyValue,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var code = await Program.RunAsync([
            "apim", "policy", "get-entity-tag",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Policy_Delete_CommandSucceeds()
    {
        await Program.RunAsync([
            "apim", "policy", "create",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--value", PolicyValue,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(PolicyEtagFilePath), Is.True);

        var etag = JsonNode.Parse(await File.ReadAllTextAsync(PolicyEtagFilePath))!["value"];

        var code = await Program.RunAsync([
            "apim", "policy", "delete",
            "--service-name", ServiceName,
            "--policy-id", PolicyId,
            "--if-match", $"\"{etag}\"",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }
}
