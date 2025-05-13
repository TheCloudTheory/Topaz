using System.Net.Http.Headers;
using System.Net.Http.Json;
using Topaz.Service.Shared;
using Topaz.Shared;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Topaz.Service.KeyVault;

public sealed class KeyVaultEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;
    private readonly KeyVaultDataPlane dataPlane = new(logger, new ResourceProvider(logger));
    public (int Port, Protocol Protocol) PortAndProtocol => (8898, Protocol.Https);

    public string[] Endpoints => [
        "/{vaultName}/secrets/{secretName}"
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            if(method == "PUT")
            {
                var vaultName = ExtractVaultNameFromPath(path);
                var secretName = ExtractSecretNameFromPath(path);
                var (data, code) = this.dataPlane.SetSecret(input, vaultName, secretName);

                if(code == HttpStatusCode.Unauthorized)
                {
                    EnforceAuthenticationChallenge(response, code);

                    return response;
                }

                response.StatusCode = code;
                response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
            }

            if (method == "GET")
            {
                var vaultName = ExtractVaultNameFromPath(path);
                var secretName = ExtractSecretNameFromPath(path);
                var (data, code) = this.dataPlane.GetSecret(vaultName, secretName);

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

    /// <summary>
    /// Handles the situation when Key Vault SDK sends first request with empty body and
    /// empty authorization header to avoid sending secrets to unauthorized service.
    /// </summary>
    private static void EnforceAuthenticationChallenge(HttpResponseMessage response, HttpStatusCode code)
    {
        response.Headers.Add("WWW-Authenticate", $"Bearer authorization=\"http://localhost:8898/{Guid.Empty}\", resource=\"https://localhost:8898\"");
        response.StatusCode = code;
    }

    private string ExtractVaultNameFromPath(string path)
    {
        var requestParts = path.Split('/');
        var secretName = requestParts[1];

        return secretName;
    }

    private string ExtractSecretNameFromPath(string path)
    {
        var requestParts = path.Split('/');
        var secretName = requestParts[3];

        return secretName;
    }
}
