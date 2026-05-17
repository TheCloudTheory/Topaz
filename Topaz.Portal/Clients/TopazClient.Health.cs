using System.Text.Json;
using Topaz.Portal.Models;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<HostInfoDto?> GetHostInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<HostInfoDto>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}
