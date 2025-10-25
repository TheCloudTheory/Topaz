using System.Net;
using System.Xml.Linq;
using System.Xml.Serialization;
using Azure.Data.Tables.Models;
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
    public TableProperties[] GetTables(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var tables = provider.ListAs<TableItem>(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, 10);

        return tables.Where(table => table != null).Select(table => new TableProperties
        {
            Name = table!.Name
        }).ToArray()!;
    }

    public TableItem CreateTable(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, CreateTableRequest request)
    {
        var model = new TableItem(request.TableName);;

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, request.TableName!, storageAccountName, model);

        return model;
    }

    public CreateTableResponse CreateTable(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
        var model = new TableItem(tableName);;

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName, model);

        return new CreateTableResponse()
        {
            Name = model.Name
        };
    }

    public void DeleteTable(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string tableName, string storageAccountName)
    {
       provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);
    }

    internal bool CheckIfTableExists(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName)
    {
        return provider.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);
    }

    public TableServiceProperties GetTableProperties(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var storageControlPlane = new AzureStorageControlPlane(new ResourceProvider(logger), logger);
        var path = storageControlPlane.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var propertiesFilePath = Path.Combine(path, "properties.xml");

        if (!File.Exists(propertiesFilePath)) throw new InvalidOperationException();
        
        var document = XDocument.Load(File.OpenRead(propertiesFilePath), LoadOptions.PreserveWhitespace);
        var properties = TableServicePropertiesSerialization.DeserializeTableServiceProperties(document.Root);
        
        return properties;
    }

    public HttpStatusCode SetAcl(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName, Stream input)
    {
        using var sr = new StreamReader(input);
        
        var aclPath = provider.GetTableAclPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);
        var document = XDocument.Load(input, LoadOptions.PreserveWhitespace);
        
        if (document.Element("SignedIdentifiers") is {} signedIdentifiersElement)
        {
            var acls = signedIdentifiersElement.Elements("SignedIdentifier")
                .Select(TableSignedIdentifier.DeserializeTableSignedIdentifier).ToList();

            if (acls.Count > 5) return HttpStatusCode.BadRequest;

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
            return HttpStatusCode.BadRequest;
        }

        return HttpStatusCode.NoContent;
    }

    public SignedIdentifiers GetAcl(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string tableName)
    {
        var aclPath = provider.GetTableAclPath(subscriptionIdentifier, resourceGroupIdentifier, tableName, storageAccountName);
        var files = Directory.EnumerateFiles(aclPath, "*.xml", SearchOption.TopDirectoryOnly);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(TableSignedIdentifier));

        var acls = files.Select(file => (TableSignedIdentifier)serializer.Deserialize(File.OpenRead(file))!).ToList();
        var response = new SignedIdentifiers(acls.ToArray());

        return response;
    }
}
