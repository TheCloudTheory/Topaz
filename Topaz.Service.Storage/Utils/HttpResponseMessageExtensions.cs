using System.Net;
using System.Net.Http.Json;
using Azure.Storage.Blobs.Models;

namespace Topaz.Service.Storage.Utils;

internal static class HttpResponseMessageExtensions
{
    public static void CreateBlobErrorResponse(this HttpResponseMessage response, BlobErrorCode code, string errorMessage, HttpStatusCode statusCode)
    {
        var error = new ErrorResponse(code.ToString(), errorMessage);

        response.StatusCode = statusCode;
        response.Headers.Add("x-ms-error-code", code.ToString());
        response.Content = JsonContent.Create(error);
    }
}