using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints;

/// <summary>
/// Serves the browser-based device code authorisation page at <c>/devicelogin</c>.
/// <list type="bullet">
///   <item><c>GET  /devicelogin</c> — returns an HTML sign-in form (user_code + username).</item>
///   <item><c>POST /devicelogin</c> — validates the submitted user_code and username, moves
///     the code from <see cref="DeviceCodeEndpoint.PendingDeviceCodes"/> to
///     <see cref="DeviceCodeEndpoint.AuthorizedDeviceCodes"/> so that the next token-endpoint
///     poll succeeds.</item>
/// </list>
/// </summary>
internal sealed class DeviceLoginEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _userDataPlane = UserDataPlane.New(logger);

    public string[] Endpoints => ["GET /devicelogin", "POST /devicelogin"];
    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    // Public browser endpoint — no auth required.
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker) => (true, null);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            HandlePost(context, response);
        }
        else
        {
            response.Content = new StringContent(DeviceLoginHtmlBuilder.Form(userCode: null, error: null), Encoding.UTF8, "text/html");
            response.StatusCode = HttpStatusCode.OK;
        }
    }

    private void HandlePost(HttpContext context, HttpResponseMessage response)
    {
        var userCode = context.Request.Form["user_code"].ToString().Trim().ToUpperInvariant();
        var username = context.Request.Form["username"].ToString().Trim();

        if (!DeviceCodeEndpoint.PendingDeviceCodes.TryGetValue(userCode, out var deviceCode))
        {
            logger.LogDebug(nameof(DeviceLoginEndpoint), nameof(HandlePost),
                "Device login failed — unknown user_code: {0}", userCode);
            response.Content = new StringContent(
                DeviceLoginHtmlBuilder.Form(userCode, "Unknown device code. Please check the code and try again."),
                Encoding.UTF8, "text/html");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var userOperation = _userDataPlane.Get(UserIdentifier.From(username));
        if (userOperation.Resource == null || userOperation.Result != OperationResult.Success)
        {
            logger.LogDebug(nameof(DeviceLoginEndpoint), nameof(HandlePost),
                "Device login failed — unknown username: {0}", username);
            response.Content = new StringContent(
                DeviceLoginHtmlBuilder.Form(userCode, $"User \u2018{HttpUtility.HtmlEncode(username)}\u2019 was not found."),
                Encoding.UTF8, "text/html");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        DeviceCodeEndpoint.PendingDeviceCodes.TryRemove(userCode, out _);
        DeviceCodeEndpoint.AuthorizedDeviceCodes[deviceCode] = userOperation.Resource.Id;

        logger.LogDebug(nameof(DeviceLoginEndpoint), nameof(HandlePost),
            "Device login succeeded. user_code: {0}, username: {1}, objectId: {2}",
            userCode, username, userOperation.Resource.Id);

        response.Content = new StringContent(DeviceLoginHtmlBuilder.Success(username), Encoding.UTF8, "text/html");
        response.StatusCode = HttpStatusCode.OK;
    }
}
