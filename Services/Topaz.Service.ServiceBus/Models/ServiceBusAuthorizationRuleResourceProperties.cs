using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusAuthorizationRuleResourceProperties
{
    public List<string> Rights { get; set; } = [];
    public string PrimaryKey { get; set; } = string.Empty;
    public string SecondaryKey { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;

    [JsonIgnore]
    public string ClaimType => "SharedAccessKey";

    [JsonIgnore]
    public string ClaimValue => "None";

    public static ServiceBusAuthorizationRuleResourceProperties Create(string ruleName, IEnumerable<string> rights) =>
        new()
        {
            Rights = rights.ToList(),
            PrimaryKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            SecondaryKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            KeyName = ruleName
        };

    public static void UpdateFromRequest(ServiceBusAuthorizationRuleResource resource,
        CreateOrUpdateServiceBusAuthorizationRuleRequest request)
    {
        if (request.Properties?.Rights is { } rights)
            resource.Properties.Rights = rights;
    }
}
