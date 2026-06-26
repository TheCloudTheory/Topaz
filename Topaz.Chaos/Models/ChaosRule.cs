using System.Net.Http.Headers;
using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Chaos.Models;

public sealed class ChaosRule
{
    public required string Id { get; init; }
    public required string ServiceNamespace { get; init; }
    public required FaultType FaultType { get; init; }
    public required double FaultRate { get; init; }
    public int? HttpStatusCode { get; init; }
    public bool Enabled { get; set; } = true;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public async Task<HttpResponseMessage> GetResponse()
    {
        switch(FaultType)
        {
            case FaultType.Timeout:
                // Timeout will be configurable at some point
                await Task.Delay(TimeSpan.FromSeconds(30));
                return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout);
            case FaultType.TransientError:
                var error = new ChaosErrorResponse()
                {
                    Error = new ChaosErrorResponse.ChaosErrorDetail()
                    {
                        Code = "InternalServerError",
                        Message = "An unexpected error occurred."
                    }
                };
                
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(error.ToString(), System.Text.Encoding.UTF8, "application/json")
                };
            case FaultType.Throttle:
                return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
                {
                    Headers = { RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5)) }
                };
            case FaultType.ServiceUnavailable:
                // Timeout will be configurable at some point
                await Task.Delay(TimeSpan.FromSeconds(60));
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}