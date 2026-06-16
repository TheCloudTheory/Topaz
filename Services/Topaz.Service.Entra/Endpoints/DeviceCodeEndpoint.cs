using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints;

internal sealed class DeviceCodeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    /// <summary>
    /// Maps user_code → device_code. Populated when a device code is issued and cleared by
    /// <see cref="DeviceLoginEndpoint"/> once the user has completed browser sign-in.
    /// </summary>
    internal static readonly ConcurrentDictionary<string, string> PendingDeviceCodes = new();

    /// <summary>
    /// Maps device_code → objectId. Populated by <see cref="DeviceLoginEndpoint"/> after the user
    /// signs in at /devicelogin, and consumed (removed) by <see cref="TokenEndpoint"/> when
    /// handling the <c>urn:ietf:params:oauth:grant-type:device_code</c> grant.
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

        PendingDeviceCodes[userCode] = deviceCode;

        logger.LogDebug(nameof(DeviceCodeEndpoint), nameof(GetResponse),
            "Issued device code. user_code: {0}. Waiting for /devicelogin.", userCode);

        response.CreateJsonContentResponse(DeviceCodeResponse.Create(deviceCode, userCode), HttpStatusCode.OK);
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
