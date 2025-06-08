using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints;

public sealed class KeyVaultEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new ResourceProvider(logger));
    public (int Port, Protocol Protocol) PortAndProtocol => (8898, Protocol.Https);

    public string[] Endpoints => [
        "PUT /{vaultName}/secrets/{secretName}",
        "GET /{vaultName}/secrets/{secretName}",
        "GET /{vaultName}/secrets/{secretName}/",
        "GET /{vaultName}/secrets/{secretName}/{secretVersion}",
        "DELETE /{vaultName}/secrets/{secretName}",
        "GET /{vaultName}/secrets/",
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            if(method == "PUT")
            {
                var vaultName = path.ExtractValueFromPath(1);
                var secretName = path.ExtractValueFromPath(3);
                var (data, code) = this._dataPlane.SetSecret(input, vaultName!, secretName!);

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
                var vaultName = path.ExtractValueFromPath(1);
                var secretName = path.ExtractValueFromPath(3);

                if (string.IsNullOrEmpty(vaultName)) throw new InvalidOperationException();
                if (string.IsNullOrEmpty(secretName))
                {
                    HandleGetSecretsRequest(vaultName, response);
                }
                else
                {
                    HandleGetSecretRequest(path, vaultName, secretName, response);
                }
            }

            if (method == "DELETE")
            {
                var vaultName = path.ExtractValueFromPath(1);
                var secretName = path.ExtractValueFromPath(3);

                if (string.IsNullOrEmpty(vaultName)) throw new InvalidOperationException();
                if (string.IsNullOrEmpty(secretName))
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    return response;
                }
                
                HandleDeleteSecretRequest(vaultName, secretName, response);
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;

            return response;
        }
        
        return response;
    }

    private void HandleDeleteSecretRequest(string vaultName, string secretName, HttpResponseMessage response)
    {
        var (data, code) = this._dataPlane.DeleteSecret(vaultName, secretName);
        if (data == null)
        {
            response.StatusCode = code;
            return;
        }
        
        var content = DeleteSecretResponse.New(data.Id, vaultName, secretName, data.Attributes);
        
        response.StatusCode = code;
        response.Content = JsonContent.Create(content, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
    }

    private void HandleGetSecretsRequest(string vaultName, HttpResponseMessage response)
    {
        var (data, code) = this._dataPlane.GetSecrets(vaultName);
        var content = new GetSecretsResponse()
        {
            Value = data.Select(s => new GetSecretsResponse.Secret()
            {
                Id = s.Id,
                Attributes = new GetSecretsResponse.Secret.SecretAttributes()
                {
                    Created = s.Attributes.Created,
                    Enabled =  s.Attributes.Enabled,
                    Updated = s.Attributes.Updated
                },
                ContentType = "plainText" // TODO: Add support for setting this value
            }).ToArray(),
        };
        
        response.StatusCode = code;
        response.Content = JsonContent.Create(content, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
    }

    private void HandleGetSecretRequest(string path, string vaultName, string? secretName, HttpResponseMessage response)
    {
        var version = path.ExtractValueFromPath(4);
        var (data, code) = this._dataPlane.GetSecret(vaultName, secretName!, version);

        response.StatusCode = code;
        response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
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
}
