using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Host;

internal sealed class Router(GlobalOptions options, ITopazLogger logger)
{
    internal async Task MatchAndExecuteEndpoint(IEndpointDefinition[] httpEndpoints, HttpContext context)
    {
        var path = context.Request.Path.ToString();
        var method = context.Request.Method;
        var query = context.Request.QueryString;
        var port = context.Request.Host.Port;

        if (method == null)
        {
            logger.LogDebug($"Received request with no method.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogInformation($"Received request: {method} {context.Request.Host}{path}{query}");

        IEndpointDefinition? endpoint = null;
        var pathParts = path.Split('/');
        foreach (var httpEndpoint in httpEndpoints.Where(e => e.PortAndProtocol.Port == port))
        {
            foreach (var endpointUrl in httpEndpoint.Endpoints)
            {
                var methodAndPath = endpointUrl.Split(" ");
                var endpointMethod = methodAndPath[0];
                var endpointPath = methodAndPath[1];
                                
                if(method != endpointMethod) continue;
                                
                var endpointParts = endpointPath.Split('/');
                if (endpointParts.Length != pathParts.Length && IsEndpointWithDynamicRouting(endpointParts) == false) continue;

                if (IsEndpointWithDynamicRouting(endpointParts))
                {
                    foreach (var part in endpointParts)
                    {
                        if (part.StartsWith('{') && part.EndsWith('}')) continue;
                        if (part.Equals("...")) endpoint = httpEndpoint;
                    }
                }
                else
                {
                    for (var i = 0; i < endpointParts.Length; i++)
                    {
                        if (endpointParts[i].StartsWith('{') && endpointParts[i].EndsWith('}')) continue;
                        if (MatchesRegexExpressionForEndpoint(endpointParts[i], pathParts[i])) continue;
                        if (string.Equals(endpointParts[i], pathParts[i], StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            endpoint = null; // We need to reset the endpoint as it doesn't look correct now
                            break;
                        }

                        endpoint = httpEndpoint;
                    }
                }

                // If we have endpoint assigned after validating the URL, we don't need to process other endpoints
                if (endpoint != null) break;
            }

            // If we have endpoint assigned after validating the URL, we don't need to process other endpoints
            if (endpoint != null) break;
        }

        if (endpoint == null)
        {
            await CreateNotFoundResponse(context, method, path);
            return;
        }

        var response = endpoint.GetResponse(path, method, context.Request.Body, context.Request.Headers, query, options);
        var textResponse = await response.Content.ReadAsStringAsync();

        logger.LogInformation($"Response: [{endpoint.GetType()}][{response.StatusCode}] [{path}] {textResponse}");
        
        context.Response.StatusCode = (int)response.StatusCode;
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Just returns and let an endpoint prepare a correct response. The reason why there's no
            // generic handler for that kind of situation is because in some scenarios (like when
            // Evert Hub SDK validates a checkpoint), a specific error code is checked.
            await context.Response.WriteAsync(textResponse);
            return;
        }

        foreach (var header in response.Headers)
        {
            context.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
        }

        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(textResponse);
        }

        if(response.StatusCode != HttpStatusCode.NoContent)
        {
            await context.Response.WriteAsync(textResponse);
        }
    }

    private async Task CreateNotFoundResponse(HttpContext context, string method, string path)
    {
        logger.LogError($"Request {method} {path} has no corresponding endpoint assigned.");

        var failedResponse = new HttpResponseMessage();
        failedResponse.CreateErrorResponse(
            HttpResponseMessageExtensions.EndpointNotFoundCode, method, path);
                            
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        await context.Response.WriteAsync(await failedResponse.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Checks if the provided endpoint allows dynamic routing. Dynamic routing is a concept
    /// when endpoint accepts multiple paths which point to a specific resource. An example
    /// of such an endpoint is UploadBlob endpoint for Blob Storage where path will differ depending
    /// on the blob location.
    /// </summary>
    /// <param name="endpointParts">An array of parts of the endpoint.</param>
    /// <returns>True if dynamic routing is allowed.</returns>
    private bool IsEndpointWithDynamicRouting(string[] endpointParts)
    {
        return endpointParts.Contains("...");
    }

    /// <summary>
    /// Determines whether a given path segment matches a specified endpoint segment using a regular expression.
    /// </summary>
    /// <param name="endpointSegment">The endpoint segment, which may contain a regular expression.</param>
    /// <param name="pathSegment">The path segment to compare against the endpoint segment.</param>
    /// <returns>
    /// True if the path segment matches the endpoint segment's regular expression; otherwise, false.
    /// </returns>
    private bool MatchesRegexExpressionForEndpoint(string endpointSegment, string pathSegment)
    {
        if(string.IsNullOrEmpty(endpointSegment) || string.IsNullOrEmpty(pathSegment)) return false;
        if(endpointSegment.StartsWith('^') == false) return false;

        var matches = Regex.Match(pathSegment, endpointSegment, RegexOptions.IgnoreCase);
        return matches.Success;
    }
}