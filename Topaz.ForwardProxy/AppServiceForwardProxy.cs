using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.AppService;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.ForwardProxy;

public sealed class AppServiceForwardProxy(HttpClient httpClient, ITopazLogger logger)
{
    private readonly AppServiceSiteControlPlane _appServiceSiteControlPlane = AppServiceSiteControlPlane.New(logger);
    public bool CanForward(string host)
    {
        logger.LogDebug(nameof(AppServiceForwardProxy), nameof(CanForward), "Checking if host `{0}` can be forwarded.", host);
        
        // SCM/Kudu hostnames (.scm.azurewebsites.topaz.local.dev) must not be forwarded;
        // they are served by the dedicated Kudu endpoints, not by Docker containers.
        return !host.Contains(".scm.azurewebsites.topaz.local.dev") && host.EndsWith(".azurewebsites.topaz.local.dev");
    }
    
    public async Task<HttpResponseMessage> Send(HttpContext context)
    {
        logger.LogDebug(nameof(AppServiceForwardProxy), nameof(Send), "Forwarding request to target.");
        
        // Note that assumption here is that the host is in the format of <siteName>.azurewebsites.topaz.local.dev
        // and the name of the site is the same as the Docker Compose service.
        var siteName = context.Request.Host.Host.Split(".")[0];
        var path = context.Request.Path.Value;
        var query = context.Request.QueryString.Value;
        
        // We will fetch port from the WEBSITES_PORT, assuming 80 is the default.
        const string websitesPort = "80";
        var config = _appServiceSiteControlPlane.GetSiteConfig(siteName);
        
        if (config.Result != OperationResult.Success || config.Resource == null)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        var setting =
            config.Resource.Properties?.AppSettings?.FirstOrDefault(setting => setting.Name == "WEBSITES_PORT");
        
        // Define the target URL as we have all the data.
        var targetUrl = $"http://{siteName}:{setting?.Value ?? websitesPort}{path}{query}";
        
        // Now we will construct a new request to the target URL.
        var targetRequest = CreateProxyHttpRequest(context, new Uri(targetUrl));
        
        // We will also propagate X-Forwarded-For and X-Forwarded-Host headers
        // to the target.
        targetRequest.Headers.Add("X-Forwarded-For", context.Connection.RemoteIpAddress.ToString());
        targetRequest.Headers.Add("X-Forwarded-Host", context.Request.Host.Host);
        
        // The last part - we will send the request to the target.
        var response = await httpClient.SendAsync(targetRequest);

        return response;
    }
    
    /// <summary>
    /// Taken from https://github.com/aspnet/Proxy/blob/master/src/Microsoft.AspNetCore.Proxy/ProxyAdvancedExtensions.cs
    /// </summary>
    private static HttpRequestMessage CreateProxyHttpRequest(HttpContext context, Uri uri)
    {
        var request = context.Request;

        var requestMessage = new HttpRequestMessage();
        var requestMethod = request.Method;
        if (!HttpMethods.IsGet(requestMethod) &&
            !HttpMethods.IsHead(requestMethod) &&
            !HttpMethods.IsDelete(requestMethod) &&
            !HttpMethods.IsTrace(requestMethod))
        {
            var streamContent = new StreamContent(request.Body);
            requestMessage.Content = streamContent;
        }

        // Copy the request headers
        foreach (var header in request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        requestMessage.Headers.Host = uri.Authority;
        requestMessage.RequestUri = uri;
        requestMessage.Method = new HttpMethod(request.Method);

        return requestMessage;
    }
}