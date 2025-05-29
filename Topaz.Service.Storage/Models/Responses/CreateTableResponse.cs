using System.Text.Json;
using Topaz.Service.Shared;

namespace Topaz.Service.Storage.Models.Responses;

public class CreateTableResponse
{
    public string? Name { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}