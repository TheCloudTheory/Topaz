using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints;

/// <summary>
/// Base class for all Key Vault data-plane endpoints (secrets, keys, certificates).
/// Moves the vault lookup and authorization check out of each endpoint's GetResponse
/// into a single <see cref="Authorize"/> override, keyed by the abstract
/// <see cref="AccessPolicyPermission"/> (and optional <see cref="AccessPolicyScope"/>)
/// properties each concrete endpoint declares.
/// </summary>
internal abstract class KeyVaultDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger)
{
    protected readonly KeyVaultAuthorizationChecker AuthChecker = new(eventPipeline, logger);
    protected readonly ITopazLogger Logger = logger;
    protected readonly KeyVaultControlPlane ControlPlane = KeyVaultControlPlane.New(eventPipeline, logger);

    /// <summary>The Key Vault access-policy permission name for this endpoint (e.g. "get", "set").</summary>
    protected abstract string? AccessPolicyPermission { get; }

    /// <summary>The Key Vault access-policy scope. Defaults to "secrets".</summary>
    protected virtual string AccessPolicyScope => "secrets";

    /// <summary>Retrieves the vault resource stored by <see cref="Authorize"/> for this request.</summary>
    protected static KeyVaultFullResource GetVault(HttpContext context)
        => (KeyVaultFullResource)context.Items[typeof(KeyVaultFullResource)]!;

    /// <inheritdoc cref="IEndpointDefinition.Authorize"/>
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker)
    {
        var hostSegments = context.Request.Host.Host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (hostSegments.Length == 0)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return (false, null);
        }

        var vaultName = PathGuard.SanitizeName(hostSegments[0]);
        var vaultOperation = ControlPlane.FindByName(vaultName!);
        if (vaultOperation.Result == OperationResult.NotFound || vaultOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return (false, null);
        }

        context.Items[typeof(KeyVaultFullResource)] = vaultOperation.Resource;

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            response.Headers.Add("WWW-Authenticate", KeyVaultAuthorizationChecker.WwwAuthenticateChallenge);
            // Real Azure Key Vault always returns a parseable JSON error body on 401.
            // Old go-autorest (azurerm Terraform provider) calls azure.WithErrorUnlessStatusCode()
            // which tries to decode the response body; an empty body causes an EOF error that
            // prevents the bearer-challenge retry from ever being attempted.
            response.Content = new StringContent(
                "{\"error\":{\"code\":\"Unauthorized\",\"message\":\"Request is missing a Bearer or PoP token.\"}}",
                System.Text.Encoding.UTF8,
                "application/json");
            return (false, null);
        }

        if (!AuthChecker.IsAuthorized(authHeader, vaultOperation.Resource, Permissions!, AccessPolicyPermission, AccessPolicyScope))
        {
            response.StatusCode = HttpStatusCode.Forbidden;
            return (false, null);
        }

        return (true, null);
    }

    // Permissions is declared on the concrete IEndpointDefinition implementation;
    // expose it here so Authorize can read it without casting.
    private string[]? Permissions => (this as IEndpointDefinition)?.Permissions;
}
