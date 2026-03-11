using System.Text.Json;
using Topaz.Portal.Models.Auth;

namespace Topaz.Portal;

internal sealed class AuthenticationClient
{
    private readonly HttpClient _httpClient;
    
    public AuthenticationClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();

        var armBaseUrl = configuration["Topaz:ArmBaseUrl"];
        if (!string.IsNullOrWhiteSpace(armBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(armBaseUrl);
        }
    }
    
    public async Task<(TokenResponse? Token, string? ErrorMessage)> GetAuthTokenWithError(string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://topaz.local.dev:8899/organizations/oauth2/v2.0/token?grant_type=password&client_id={Guid.NewGuid()}&username={username}&password={password}");

        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var apiError = JsonSerializer.Deserialize<AuthErrorResponse>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var message = apiError?.GetBestDescription();
                if (!string.IsNullOrWhiteSpace(message))
                    return (null, message);
            }
            catch
            {
                // Ignore parse errors; fall back below.
            }

            // Fallback: don’t leak raw body to UI; keep it simple.
            return (null, "Sign-in failed. Please check your credentials and try again.");
        }

        var token = JsonSerializer.Deserialize<TokenResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (string.IsNullOrWhiteSpace(token?.AccessToken))
            return (null, "Sign-in failed: token was missing.");

        return (token, null);
    }
}