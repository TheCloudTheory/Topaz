using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Identity;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints;

internal sealed class DeviceCodeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _userDataPlane = UserDataPlane.New(logger);

    /// <summary>
    /// Maps device_code → objectId. Populated by <see cref="DeviceCodeEndpoint"/> and consumed (and removed) by
    /// <see cref="TokenEndpoint"/> when handling the <c>urn:ietf:params:oauth:grant-type:device_code</c> grant.
    /// </summary>
    internal static readonly ConcurrentDictionary<string, string> AuthorizedDeviceCodes = new();

    public string[] Endpoints =>
    [
        "POST /organizations/oauth2/v2.0/devicecode",
        "POST /{tenantId}/oauth2/v2.0/devicecode",
        "POST /common/oauth2/v2.0/devicecode",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var deviceCode = $"Topaz{Guid.NewGuid():N}";
        var userCode = GenerateUserCode();

        // MSAL sends login_hint in the POST body when the caller already has a specific identity
        // in mind (e.g. az login --use-device-code after a prior sign-in, or az login -u <upn>).
        // Honour it so the resulting token carries the correct user identity.
        // When no hint is present we deviate from real Azure: real Azure keeps the code in a
        // "pending" state until the user visits the verification_uri, enters the user_code, and
        // signs in. Topaz could support that too (a GET/POST /devicelogin page is feasible), but
        // that endpoint does not exist yet. Until it does, we pre-bind to the global admin so the
        // first token poll succeeds immediately.
        var objectId = ResolveObjectId(context.Request.Form["login_hint"]);
        AuthorizedDeviceCodes[deviceCode] = objectId;

        logger.LogDebug(nameof(DeviceCodeEndpoint), nameof(GetResponse),
            "Issued device code. user_code: {0}, objectId: {1}", userCode, objectId);

        response.CreateJsonContentResponse(DeviceCodeResponse.Create(deviceCode, userCode), HttpStatusCode.OK);
    }

    private string ResolveObjectId(string? loginHint)
    {
        if (string.IsNullOrWhiteSpace(loginHint))
        {
            logger.LogDebug(nameof(DeviceCodeEndpoint), nameof(ResolveObjectId),
                "No login_hint — pre-binding device code to global admin (placeholder until /devicelogin is implemented).");
            return Globals.GlobalAdminId;
        }

        var userOperation = _userDataPlane.Get(UserIdentifier.From(loginHint));
        if (userOperation.Resource != null && userOperation.Result == OperationResult.Success)
        {
            return userOperation.Resource.Id;
        }

        logger.LogWarning($"login_hint '{loginHint}' did not match any user — defaulting to global admin.");
        return Globals.GlobalAdminId;
    }

    private static string GenerateUserCode()
    {
        const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new char[9];
        var random = new Random();
        for (var i = 0; i < 4; i++) buffer[i] = Chars[random.Next(Chars.Length)];
        buffer[4] = '-';
        for (var i = 5; i < 9; i++) buffer[i] = Chars[random.Next(Chars.Length)];
        return new string(buffer);
    }
}
