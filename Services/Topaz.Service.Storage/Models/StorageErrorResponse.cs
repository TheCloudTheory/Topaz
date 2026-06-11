using System.Xml.Linq;

namespace Topaz.Service.Storage.Models;

/// <summary>
/// XML error response model for Azure Storage data-plane operations (Blob, Queue).
/// Converts to standard Azure Storage XML error format.
/// </summary>
internal sealed record StorageErrorResponse(string Code, string Message)
{
    /// <summary>
    /// Renders the error response as XML conforming to Azure Storage error format.
    /// </summary>
    public string ToXml()
    {
        var element = new XElement("Error",
            new XElement("Code", Code),
            new XElement("Message", Message));
        
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            element);
        
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>
    /// Factory method for permission mismatch errors (403 Forbidden).
    /// </summary>
    public static StorageErrorResponse AuthorizationPermissionMismatch() =>
        new("AuthorizationPermissionMismatch",
            "This request is not authorized to perform this operation.");

    /// <summary>
    /// Factory method for authentication failure errors (401 Unauthorized).
    /// </summary>
    public static StorageErrorResponse AuthenticationFailed() =>
        new("AuthenticationFailed",
            "Server failed to authenticate the request. Make sure the value of the Authorization header is formed correctly including the signature.");

    /// <summary>
    /// Factory method for secondary region write rejection (403 Forbidden).
    /// </summary>
    public static StorageErrorResponse WriteOperationNotSupportedOnSecondary() =>
        new("WriteOperationNotSupportedOnSecondary",
            "The account being accessed does not support writes from the secondary region.");
}
