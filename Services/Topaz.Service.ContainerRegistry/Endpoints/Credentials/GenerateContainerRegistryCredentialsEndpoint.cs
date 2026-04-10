using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.ContainerRegistry.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Credentials;

internal sealed class GenerateContainerRegistryCredentialsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/generateCredentials"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/generateCredentials/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupName = path.ExtractValueFromPath(4);
        var registryName = path.ExtractValueFromPath(8);

        var operation = _controlPlane.Get(
            subscriptionIdentifier,
            ResourceGroupIdentifier.From(resourceGroupName),
            registryName!);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, registryName, resourceGroupName);
            return;
        }

        GenerateCredentialsRequest? request = null;
        if (context.Request.ContentLength > 0)
        {
            request = JsonSerializer.Deserialize<GenerateCredentialsRequest>(
                context.Request.Body, GlobalSettings.JsonOptions);
        }

        var tokenName = ExtractTokenName(request?.TokenId) ?? registryName!;
        var expiry = request?.Expiry ?? DateTimeOffset.UtcNow.AddYears(1);

        var passwords = BuildPasswords(request?.Name, expiry);

        response.CreateJsonContentResponse(new GenerateCredentialsResponse
        {
            Username = tokenName,
            Passwords = passwords
        }, HttpStatusCode.OK);
    }

    private static string? ExtractTokenName(string? tokenId)
    {
        if (string.IsNullOrEmpty(tokenId))
            return null;

        var lastSlash = tokenId.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < tokenId.Length - 1)
            return tokenId[(lastSlash + 1)..];

        return null;
    }

    private static GenerateCredentialsResponse.TokenPasswordEntry[] BuildPasswords(string? name, DateTimeOffset expiry)
    {
        if (string.Equals(name, "password1", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new GenerateCredentialsResponse.TokenPasswordEntry
                {
                    Name = "password1",
                    Value = Guid.NewGuid().ToString("N"),
                    Expiry = expiry
                }
            ];
        }

        if (string.Equals(name, "password2", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new GenerateCredentialsResponse.TokenPasswordEntry
                {
                    Name = "password2",
                    Value = Guid.NewGuid().ToString("N"),
                    Expiry = expiry
                }
            ];
        }

        return
        [
            new GenerateCredentialsResponse.TokenPasswordEntry
            {
                Name = "password1",
                Value = Guid.NewGuid().ToString("N"),
                Expiry = expiry
            },
            new GenerateCredentialsResponse.TokenPasswordEntry
            {
                Name = "password2",
                Value = Guid.NewGuid().ToString("N"),
                Expiry = expiry
            }
        ];
    }
}
