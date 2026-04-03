using Azure.ResourceManager;
using Microsoft.Graph;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly AccountSession _session;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    
    private ArmClient? _armClient;
    private GraphServiceClient? _graphClient;
    private bool _initialized;

    public TopazClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, AccountSession session)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _session = session;

        _httpClient = httpClientFactory.CreateClient();

        var armBaseUrl = configuration["Topaz:ArmBaseUrl"];
        if (!string.IsNullOrWhiteSpace(armBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(armBaseUrl);
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            // Load session if not already loaded
            if (_session.Token is null)
            {
                await _session.LoadAsync();
            }

            if (_session.Token is null)
                throw new InvalidOperationException("Session not loaded. User must be authenticated.");

            var credentials = new AzureFixedTokenLocalCredential(_session.Token);
            _armClient = new ArmClient(credentials, Guid.Empty.ToString(), TopazArmClientOptions.New);

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.Token);

            _graphClient = new GraphServiceClient(_httpClientFactory.CreateClient(),
                new LocalGraphFixedTokenAuthenticationProvider(_session.Token),
                _configuration["Topaz:ArmBaseUrl"]);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }




}