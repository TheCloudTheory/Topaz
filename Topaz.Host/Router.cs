using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Topaz.EventPipeline;
using Topaz.Service.Authorization;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Host;

internal sealed class Router(Pipeline eventPipeline, GlobalOptions options, ITopazLogger logger)
{
    private readonly AzureAuthorizationAdapter _authorizationAdapter = new(eventPipeline, logger);
    
    internal async Task MatchAndExecuteEndpoint(IEndpointDefinition[] httpEndpoints, HttpContext context)
    {
        var path = context.Request.Path.ToString();
        var method = context.Request.Method;
        var query = context.Request.QueryString;
        var port = context.Request.Host.Port ?? context.Connection.LocalPort;

        if (method == null)
        {
            logger.LogDebug(nameof(Router), nameof(MatchAndExecuteEndpoint), "Received request with no method.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogInformation($"[{method}][{context.Request.Host}{path}{query}][port:{port}]");

        IEndpointDefinition? endpoint = null;
        var pathParts = path.Split('/');
        
        foreach (var httpEndpoint in httpEndpoints.Where(e => e.PortsAndProtocol.Ports.Any(p => p == port)))
        {
            foreach (var endpointUrl in httpEndpoint.Endpoints)
            {
                var methodAndPath = endpointUrl.Split(" ");
                var endpointMethod = methodAndPath[0];
                var endpointPathAndQuery = methodAndPath[1];

                // Parse optional query-string constraints from pattern (e.g. "GET /{name}?comp=metadata")
                // Skip ?-parsing for regex patterns (paths containing '^') to avoid treating
                // lazy-quantifier '?' in patterns like '^.*?\(PartitionKey=...' as a query separator.
                Dictionary<string, string>? queryConstraint = null;
                var endpointPath = endpointPathAndQuery;
                var queryIndex = endpointPathAndQuery.Contains('^') ? -1 : endpointPathAndQuery.IndexOf('?');
                if (queryIndex >= 0)
                {
                    endpointPath = endpointPathAndQuery[..queryIndex];
                    queryConstraint = endpointPathAndQuery[(queryIndex + 1)..].Split('&')
                        .Select(kv => kv.Split('='))
                        .Where(kv => kv.Length == 2)
                        .ToDictionary(kv => kv[0], kv => kv[1], StringComparer.OrdinalIgnoreCase);
                }

                if(method != endpointMethod) continue;
                                
                var endpointParts = endpointPath.Split('/');
                if (endpointParts.Length != pathParts.Length && IsEndpointWithDynamicRouting(endpointParts) == false) continue;

                if (IsEndpointWithDynamicRouting(endpointParts))
                {
                    var ellipsisIndex = Array.IndexOf(endpointParts, "...");
                    var suffix = endpointParts[(ellipsisIndex + 1)..];

                    if (suffix.Length == 0)
                    {
                        endpoint = httpEndpoint;
                    }
                    else if (pathParts.Length >= suffix.Length)
                    {
                        var pathSuffix = pathParts[^suffix.Length..];
                        var suffixMatches = suffix.Zip(pathSuffix).All(pair =>
                            (pair.First.StartsWith('{') && pair.First.EndsWith('}')) ||
                            string.Equals(pair.First, pair.Second, StringComparison.OrdinalIgnoreCase));
                        if (suffixMatches) endpoint = httpEndpoint;
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

                // If query constraints are declared, all must match the incoming request query
                if (endpoint != null && queryConstraint is { Count: > 0 })
                {
                    var requestQuery = context.Request.Query;
                    if (!queryConstraint.All(kvp =>
                            requestQuery.TryGetValue(kvp.Key, out var v) &&
                            string.Equals(v, kvp.Value, StringComparison.OrdinalIgnoreCase)))
                        endpoint = null;
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
        
        logger.LogDebug(nameof(Router), nameof(MatchAndExecuteEndpoint), "The selected handler for an endpoint will be {0}", endpoint.GetType().Name);
        logger.LogDebug(nameof(Router), nameof(MatchAndExecuteEndpoint), "[{0}] {1}{2}", method, path, query);

        var response = CallEndpoint(endpoint, context);
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        var textResponse = System.Text.Encoding.UTF8.GetString(responseBytes);

        logger.LogInformation($"[{method}][{context.Request.Host}:{path}{query}][{response.StatusCode}] {textResponse}");
        
        context.Response.StatusCode = (int)response.StatusCode;
        
        foreach (var header in response.Headers)
        {
            context.Response.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
        }

        // Also forward content headers (e.g. Content-Length) so endpoints can
        // set them properly via response.Content.Headers. Skip Content-Type because
        // SetResponseContentType handles that separately.
        foreach (var header in response.Content.Headers)
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            context.Response.Headers[header.Key] = new StringValues(header.Value.ToArray());
        }
        
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                // Just returns and let an endpoint prepare a correct response. The reason why there's no
                // generic handler for that kind of situation is because in some scenarios (like when
                // Evert Hub SDK validates a checkpoint), a specific error code is checked.
                if (!HttpMethods.IsHead(context.Request.Method))
                {
                    await context.Response.Body.WriteAsync(responseBytes);
                }
                return;
            case HttpStatusCode.InternalServerError:
                logger.LogError(textResponse);
                break;
        }

        if(response.StatusCode != HttpStatusCode.NoContent)
        {
            SetResponseContentType(context, response);

            // HEAD responses must not include a body; only status and headers are returned.
            if (!HttpMethods.IsHead(context.Request.Method))
            {
                await context.Response.Body.WriteAsync(responseBytes);
            }
        }
    }

    private HttpResponseMessage CallEndpoint(IEndpointDefinition endpoint, HttpContext context)
    {
        var response = new HttpResponseMessage();
        string? requestBodyContent = null;

        try
        {
            // Enable buffering and read body content for potential error logging
            context.Request.EnableBuffering();
            
            if (context.Request.Body.CanSeek && context.Request.ContentLength > 0)
            {
                using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                {
                    requestBodyContent = reader.ReadToEnd();
                }
                
                context.Request.Body.Position = 0; // Reset for endpoint to read
            }
            
            // Yes, this is hacky as heck, but as Topaz acts as a gateway for RPs
            // and actual host, there's no easy way to say when and how to handle
            // all those pesky edge cases.
            var canBypassAuthorization = !context.Request.Headers.ContainsKey("Authorization") &&
                                         (context.Request.Host.Host.EndsWith($".{GlobalSettings.KeyVaultDnsSuffix}") ||
                                          context.Request.Host.Host.EndsWith($".{GlobalSettings.LegacyKeyVaultDnsSuffix}"));
            
            var (isAuthorized, principal) = _authorizationAdapter.IsAuthorized(endpoint.Permissions,
                context.Request.Headers["Authorization"].ToString(), context.Request.Path.Value,
                canBypassAuthorization);

            if (!isAuthorized)
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                return response;
            }

            context.User = principal;
            endpoint.GetResponse(context, response, options);
        }
        catch (JsonException ex)
        {
            // Log the cached request body if available
            if (!string.IsNullOrEmpty(requestBodyContent))
            {
                logger.LogDebug(nameof(Router), nameof(MatchAndExecuteEndpoint), "Request body: {0}", requestBodyContent);
            }
            
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
        catch(Exception ex)
        {
            logger.LogError(ex);
            
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
        
        return response;
    }

    /// <summary>
    /// Sets the content type for the HTTP response based on the response message's content headers.
    /// If no content type is specified in the response, defaults to "application/json".
    /// </summary>
    /// <param name="context">The HTTP context containing the response to modify.</param>
    /// <param name="response">The HTTP response message containing the content type information.</param>
    private void SetResponseContentType(HttpContext context, HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType != null)
        {
            logger.LogDebug(nameof(Router), nameof(MatchAndExecuteEndpoint),
                "Setting content type for response as `{0}`.", response.Content.Headers.ContentType.MediaType);
                
            context.Response.ContentType = response.Content.Headers.ContentType.ToString();
        }
        else
        {
            logger.LogDebug(nameof(Router), nameof(MatchAndExecuteEndpoint),
                "No content type set for response, defaulting to `application/json`.");
                
            context.Response.ContentType = "application/json";
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
        if(!endpointSegment.StartsWith('^')) return false;

        var matches = Regex.Match(pathSegment, endpointSegment, RegexOptions.IgnoreCase);
        return matches.Success;
    }
}
