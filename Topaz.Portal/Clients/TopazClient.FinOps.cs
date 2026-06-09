using System.Text.Json;
using Topaz.Portal.Models.FinOps;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<EstimatedCostsResponse?> GetEstimatedCosts(
        Guid subscriptionId,
        string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        var url = $"/topaz/subscriptions/{subscriptionId:D}/estimatedCosts?currency={Uri.EscapeDataString(currency)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<EstimatedCostsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
