using System.Net.Http.Headers;
using System.Net.Http.Json;
using Topaz.Service.Shared;
using Topaz.Shared;
using Microsoft.AspNetCore.Http;

namespace Topaz.Service.KeyVault;

public sealed class KeyVaultEndpoint : IEndpointDefinition
{
    private readonly ILogger logger;
    private readonly KeyVaultDataPlane dataPlane;

    public Protocol Protocol => Protocol.Https;

    public string[] Endpoints => [
        "/secrets/{secretName}"
    ];

    public KeyVaultEndpoint(ILogger logger)
    {
        this.logger = logger;
        this.dataPlane = new KeyVaultDataPlane(logger, new ResourceProvider(logger));
    }

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            if(method == "PUT")
            {
                var secretName = ExtractSecretNameFromPath(path);
                var (data, code) = this.dataPlane.SetSecret(input, "", "");

                response.StatusCode = code;
                response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
            }
        }
        catch(Exception ex)
        {
            this.logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = System.Net.HttpStatusCode.InternalServerError;

            return response;
        }
        
        return response;
    }

    private string ExtractSecretNameFromPath(string path)
    {
        var requestParts = path.Split('/');
        var secretName = requestParts[2];

        return secretName;
    }
}
