using System.Net;
using System.Net.Http.Json;

namespace Topaz.Service.Shared;

public static class HttpResponseMessageExtensions
{
    public const string ResourceNotFoundCode = "ResourceNotFound";
    public const string ResourceGroupNotFoundCode = "ResourceGroupNotFound";
    public const string EndpointNotFoundCode = "EndpointNotFound";
    public const string InternalErrorCode = "InternalError";
    
    private const string ResourceNotFoundMessage = "Resource '{0}' under resource group '{1}' was not found";
    private const string ResourceGroupNotFoundMessage = "Resource group '{0}' was not found";
    private const string EndpointNotFoundMessage = "Endpoint was not found for the request '{0}' '{1}'";
    private const string InternalErrorMessage = "Internal error: {0}";
    
    public static void CreateErrorResponse(this HttpResponseMessage response, string code, params object[] args)
    {
        string message;
        GenericErrorResponse error;
        
        switch (code)
        {
            case ResourceNotFoundCode:
                message = string.Format(ResourceNotFoundMessage, args);
                error = new GenericErrorResponse(ResourceNotFoundCode, message);
                
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = JsonContent.Create(error);
                break;
            case EndpointNotFoundCode:
                message = string.Format(EndpointNotFoundMessage, args);
                error = new GenericErrorResponse(EndpointNotFoundCode, message);
                
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = JsonContent.Create(error);
                break;
            case ResourceGroupNotFoundCode:
                message = string.Format(ResourceGroupNotFoundMessage, args);
                error = new GenericErrorResponse(ResourceGroupNotFoundCode, message);
                
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = JsonContent.Create(error);
                break;
            case InternalErrorCode:
                message = string.Format(InternalErrorMessage, args);
                error = new GenericErrorResponse(InternalErrorCode, message);
                
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = JsonContent.Create(error);
                break;
            default:
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = new StringContent("Unknown error");
                break;
        }
    }
}