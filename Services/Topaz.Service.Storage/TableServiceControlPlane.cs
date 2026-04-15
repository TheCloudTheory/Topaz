using System.Xml.Linq;
using System.Xml.Serialization;
using Azure.Data.Tables.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;
using TableSignedIdentifier = Topaz.Service.Storage.Serialization.TableSignedIdentifier;

namespace Topaz.Service.Storage;

internal sealed class TableServiceControlPlane(TableResourceProvider provider, ITopazLogger logger)
{
    public ControlPlaneOperationResult<TableProperties[]> GetTables(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var tables =
            provider.ListAs<TableItem>(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, 10);
        var result = tables.Select(table => new TableProperties
        {
            Name = table.Name
        }).ToArray()!;

        return new ControlPlaneOperationResult<TableProperties[]>(OperationResult.Success, result, null, null);
    }

    public ControlPlaneOperationResult<TableItem> CreateTable(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, CreateTableRequest request)
    {
        var model = new TableItem(request.TableName);
        ;

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, request.TableName!, storageAccountName, model);

        return new ControlPlaneOperationResult<TableItem>(OperationResult.Created, model, null, null);
    }

    public ControlPlaneOperationResult<CreateTableResponse> CreateTable(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        var model = new TableItem(tableName);
        ;

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName, model);

        return new ControlPlaneOperationResult<CreateTableResponse>(OperationResult.Created, new CreateTableResponse
        {
            Name = model.Name
        }, null, null);
    }

    public ControlPlaneOperationResult DeleteTable(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);
        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    internal bool CheckIfTableExists(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName)
    {
        return provider.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier, tableName,
            storageAccountName);
    }

    public ControlPlaneOperationResult<TableServiceProperties> GetTableProperties(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName)
    {
        var storageControlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var path = storageControlPlane.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName);
        var propertiesFilePath = Path.Combine(path, "properties.xml");

        if (!File.Exists(propertiesFilePath))
            return new ControlPlaneOperationResult<TableServiceProperties>(OperationResult.NotFound, null,
                "Table service properties file not found.", "TableServicePropertiesNotFound");

        var document = XDocument.Load(File.OpenRead(propertiesFilePath), LoadOptions.PreserveWhitespace);
        var properties = TableServicePropertiesSerialization.DeserializeTableServiceProperties(document.Root);

        return new ControlPlaneOperationResult<TableServiceProperties>(OperationResult.Success, properties, null, null);
    }

    public ControlPlaneOperationResult<string> GetTablePropertiesXml(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var storageControlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var path = storageControlPlane.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName);
        var propertiesFilePath = Path.Combine(path, "properties.xml");

        if (!File.Exists(propertiesFilePath))
            return new ControlPlaneOperationResult<string>(OperationResult.NotFound, null,
                "Table service properties file not found.", "TableServicePropertiesNotFound");

        return new ControlPlaneOperationResult<string>(OperationResult.Success, File.ReadAllText(propertiesFilePath),
            null, null);
    }

    public static string GetTableServiceStatsXml()
    {
        var lastSyncTime = DateTimeOffset.UtcNow.ToString("R");
        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <StorageServiceStats>
                  <GeoReplication>
                    <Status>live</Status>
                    <LastSyncTime>{lastSyncTime}</LastSyncTime>
                  </GeoReplication>
                </StorageServiceStats>
                """;
    }

    public ControlPlaneOperationResult SetTableProperties(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, Stream input)
    {
        var storageControlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var path = storageControlPlane.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName);
        var propertiesFilePath = Path.Combine(path, "properties.xml");

        var document = XDocument.Load(input, LoadOptions.PreserveWhitespace);

        // Ensure the <Cors> element is always present; the Azure Data Tables SDK
        // iterates over it and throws if it is null (i.e. absent from the XML).
        if (document.Root?.Element("Cors") == null)
        {
            document.Root?.Add(new XElement("Cors"));
        }

        document.Save(propertiesFilePath);
        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public ControlPlaneOperationResult SetAcl(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName, Stream input)
    {
        using var sr = new StreamReader(input);

        var aclPath = provider.GetTableAclPath(subscriptionIdentifier, resourceGroupIdentifier, tableName,
            storageAccountName);
        var document = XDocument.Load(input, LoadOptions.PreserveWhitespace);

        if (document.Element("SignedIdentifiers") is { } signedIdentifiersElement)
        {
            var acls = signedIdentifiersElement.Elements("SignedIdentifier")
                .Select(TableSignedIdentifier.DeserializeTableSignedIdentifier).ToList();

            if (acls.Count > 5)
                return new ControlPlaneOperationResult(OperationResult.BadRequest,
                    "Too many signed identifiers (max 5).", "TooManySignedIdentifiers");

            foreach (var acl in acls)
            {
                using var sw = new EncodingAwareStringWriter();
                var serializer = new XmlSerializer(typeof(TableSignedIdentifier));
                serializer.Serialize(sw, acl);

                // Note that per Table Service functionality, if a client wants to update
                // ACL, they should first get it, make the changes and then set it once again.
                // Below we follow the standard approach for Table Service, which just replaces
                // ACLs matching the ID
                File.WriteAllText(Path.Combine(aclPath, acl.Id + ".xml"), sw.ToString());
            }
        }
        else
        {
            return new ControlPlaneOperationResult(OperationResult.BadRequest, "Missing SignedIdentifiers element.",
                "InvalidRequestBody");
        }

        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public ControlPlaneOperationResult<SignedIdentifiers> GetAcl(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName)
    {
        var aclPath = provider.GetTableAclPath(subscriptionIdentifier, resourceGroupIdentifier, tableName,
            storageAccountName);
        var files = Directory.EnumerateFiles(aclPath, "*.xml", SearchOption.TopDirectoryOnly);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(TableSignedIdentifier));

        var acls = files.Select(file => (TableSignedIdentifier)serializer.Deserialize(File.OpenRead(file))!).ToList();
        var result = new SignedIdentifiers(acls.ToArray());

        return new ControlPlaneOperationResult<SignedIdentifiers>(OperationResult.Success, result, null, null);
    }
}