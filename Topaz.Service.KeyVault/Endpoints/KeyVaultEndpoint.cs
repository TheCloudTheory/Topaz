using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints;

public sealed class KeyVaultEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultKeyVaultPort, Protocol.Https);

    public string[] Endpoints => [
        "PUT /secrets/{secretName}",
        "GET /secrets/{secretName}",
        "GET /secrets/{secretName}/",
        "GET /secrets/{secretName}/{secretVersion}",
        "DELETE /secrets/{secretName}",
        "GET /secrets/",
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query, GlobalOptions options, Guid correlationId)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            var vaultName = headers["Host"].ToString().Split(".")[0];
            var secretName = path.ExtractValueFromPath(2);
            var identifiers = GlobalDnsEntries.GetEntry(KeyVaultService.UniqueName, vaultName!);

            if (identifiers == null)
            {
                throw new Exception("Identifiers for Azure Key Vault not found.");
            }

            var subscriptionIdentifier = SubscriptionIdentifier.From(identifiers.Value.subscription);
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(identifiers.Value.resourceGroup);
            
            switch (method)
            {
                case "PUT":
                {
                   
                    var (data, code) = _dataPlane.SetSecret(input,
                        subscriptionIdentifier,
                        resourceGroupIdentifier, vaultName!, secretName!);

                    if(code == HttpStatusCode.Unauthorized)
                    {
                        EnforceAuthenticationChallenge(response, code);
                        return response;
                    }

                    response.StatusCode = code;
                    response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
                    break;
                }
                case "GET":
                {
                    if (string.IsNullOrEmpty(vaultName)) throw new InvalidOperationException();
                    if (string.IsNullOrEmpty(secretName))
                    {
                        HandleGetSecretsRequest(subscriptionIdentifier, resourceGroupIdentifier, vaultName, response);
                    }
                    else
                    {
                        HandleGetSecretRequest(path, subscriptionIdentifier, resourceGroupIdentifier, vaultName, secretName, response);
                    }

                    break;
                }
                case "DELETE":
                {
                    if (string.IsNullOrEmpty(vaultName)) throw new InvalidOperationException();
                    if (string.IsNullOrEmpty(secretName))
                    {
                        response.StatusCode = HttpStatusCode.NotFound;
                        return response;
                    }
                
                    HandleDeleteSecretRequest(subscriptionIdentifier, resourceGroupIdentifier, vaultName, secretName, response);
                    break;
                }
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

    private void HandleDeleteSecretRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName,
        HttpResponseMessage response)
    {
        var (data, code) = _dataPlane.DeleteSecret(subscriptionIdentifier, resourceGroupIdentifier, vaultName, secretName);
        if (data == null)
        {
            response.StatusCode = code;
            return;
        }

        var content = DeleteSecretResponse.New(data.Id, vaultName, secretName, data.Attributes);

        response.StatusCode = code;
        response.Content = JsonContent.Create(content, new MediaTypeHeaderValue("application/json"),
            GlobalSettings.JsonOptions);
    }

    private void HandleGetSecretsRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, HttpResponseMessage response)
    {
        var (data, code) = _dataPlane.GetSecrets(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
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

    private void HandleGetSecretRequest(string path, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string? secretName, HttpResponseMessage response)
    {
        var version = path.ExtractValueFromPath(3);
        var (data, code) = _dataPlane.GetSecret(subscriptionIdentifier, resourceGroupIdentifier, vaultName, secretName!, version);

        if (code == HttpStatusCode.NotFound)
        {
            response.StatusCode = code;
            response.Content = CreateErrorResponse($"Secret {secretName} not found.");
            return;
        }

        response.StatusCode = code;
        response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
    }
    
    private static JsonContent CreateErrorResponse(string errorMessage)
    {
        return JsonContent.Create(new ErrorResponse
        {
            Error = new ErrorResponse.ErrorData()
            {
                Message = errorMessage
            }
        }, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
    }

    /// <summary>
    /// Handles the situation when Key Vault SDK sends first request with empty body and
    /// empty authorization header to avoid sending secrets to unauthorized service.
    /// </summary>
    private static void EnforceAuthenticationChallenge(HttpResponseMessage response, HttpStatusCode code)
    {
        response.Headers.Add("WWW-Authenticate",
            $"Bearer authorization=\"https://keyvault.topaz.local.dev:{GlobalSettings.DefaultKeyVaultPort}/{Guid.Empty}\", resource=\"https://keyvault.topaz.local.dev:{GlobalSettings.DefaultKeyVaultPort}\"");
        response.StatusCode = code;
    }
}
