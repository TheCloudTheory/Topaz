using System.Net;
using System.Text;
using System.Xml;
using Azure.Storage.Blobs.Models;

namespace Topaz.Service.Storage.Utils;

internal static class HttpResponseMessageExtensions
{
    public static void CreateBlobErrorResponse(this HttpResponseMessage response, BlobErrorCode code, string errorMessage, HttpStatusCode statusCode)
        => response.CreateBlobErrorResponse(code.ToString(), errorMessage, statusCode);

    public static void CreateBlobErrorResponse(this HttpResponseMessage response, string code, string errorMessage,
        HttpStatusCode statusCode)
    {
        response.StatusCode = statusCode;
        response.Headers.Add("x-ms-error-code", code);
        response.Content = new StringContent(CreateXmlErrorResponse(code, errorMessage), Encoding.UTF8, "application/xml");
    }

    private static string CreateXmlErrorResponse(string code, string errorMessage)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8,
        };

        using var stringWriter = new EncodingAwareStringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        xmlWriter.WriteStartDocument();
        xmlWriter.WriteStartElement("Error");
        xmlWriter.WriteElementString("Code", code);
        xmlWriter.WriteElementString("Message", errorMessage);
        xmlWriter.WriteEndElement();
        xmlWriter.WriteEndDocument();
        xmlWriter.Flush();

        return stringWriter.ToString();
    }
}
