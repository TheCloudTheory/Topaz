using System.Net;
using System.Net.Http.Json;

namespace Topaz.Service.Shared;

public static class HttpResponseMessageExtensions
{
    public const string ResourceNotFoundCode = "ResourceNotFound";
    
    private const string ResourceNotFoundMessage = "The Resource '{0}' under resource group '{1}' was not found";
    
    public static void CreateErrorResponse(this HttpResponseMessage response, string code, params object[] args)
    {
        switch (code)
        {
            case ResourceNotFoundCode:
                var message = string.Format(ResourceNotFoundMessage, args);
                var error = new GenericErrorResponse(ResourceNotFoundCode, message);
                
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = JsonContent.Create(error);
                break;
            default:
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = new StringContent("Unknown error");
                break;
        }
    }
}