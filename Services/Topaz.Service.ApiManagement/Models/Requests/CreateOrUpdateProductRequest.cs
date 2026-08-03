using System.Text.Json.Serialization;

namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdateProductRequest
{
    public ProductContractResourceProperties? Properties { get; set; }
}