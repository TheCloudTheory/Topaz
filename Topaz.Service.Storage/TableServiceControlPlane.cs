using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using Azure.Data.Tables.Models;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;
using TableSignedIdentifier = Topaz.Service.Storage.Serialization.TableSignedIdentifier;

namespace Topaz.Service.Storage;

internal sealed class TableServiceControlPlane(TableResourceProvider provider, ITopazLogger logger)
{
    private readonly TableResourceProvider provider = provider;
    private readonly ITopazLogger _topazLogger = logger;

    public TableProperties[] GetTables(string storageAccountName)
    {
        var tables = this.provider.List(storageAccountName);

        return [.. tables.Select(t => {
            var di = new DirectoryInfo(t);
            return new TableProperties(di.Name);
        })];
    }

    public TableItem CreateTable(Stream input, string storageAccountName)
    {
        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var content = JsonSerializer.Deserialize<TableProperties>(rawContent, GlobalSettings.JsonOptions) 
            ?? throw new Exception();
        var model = new TableItem(content.TableName);;

        this.provider.Create(content.TableName, storageAccountName, model);

        return model;
    }

    public CreateTableResponse CreateTable(string tableName, string storageAccountName)
    {
        var model = new TableItem(tableName);;

        this.provider.Create(tableName, storageAccountName, model);

        return new CreateTableResponse()
        {
            Name = model.Name
        };
    }

    public void DeleteTable(string tableName, string storageAccountName)
    {
       this.provider.Delete(tableName, storageAccountName);
    }

    internal bool CheckIfTableExists(string tableName, string storageAccountName)
    {
        return this.provider.CheckIfTableExists(tableName, storageAccountName);
    }

    internal string GetTableDataPath(string tableName, string storageAccountName)
    {
        return this.provider.GetTableDataPath(tableName, storageAccountName);
    }

    public TableServiceProperties GetTableProperties(string storageAccountName)
    {
        var storageControlPlane = new AzureStorageControlPlane(new ResourceProvider(this._topazLogger), this._topazLogger);
        var path = storageControlPlane.GetServiceInstancePath(storageAccountName);
        var propertiesFilePath = Path.Combine(path, "properties.xml");

        if (File.Exists(propertiesFilePath) == false) throw new InvalidOperationException();
        
        var document = XDocument.Load(File.OpenRead(propertiesFilePath), LoadOptions.PreserveWhitespace);
        var properties = TableServicePropertiesSerialization.DeserializeTableServiceProperties(document.Root);
        
        return properties;
    }

    public HttpStatusCode SetAcl(string storageAccountName, string tableName, Stream input)
    {
        using var sr = new StreamReader(input);
        
        var aclPath = this.provider.GetTableAclPath(tableName, storageAccountName);
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

    public SignedIdentifiers GetAcl(string storageAccountName, string tableName)
    {
        var aclPath = this.provider.GetTableAclPath(tableName, storageAccountName);
        var files = Directory.EnumerateFiles(aclPath, "*.xml", SearchOption.TopDirectoryOnly);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(TableSignedIdentifier));

        var acls = files.Select(file => (TableSignedIdentifier)serializer.Deserialize(File.OpenRead(file))!).ToList();
        var response = new SignedIdentifiers(acls.ToArray());

        return response;
    }
}
