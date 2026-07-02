namespace Topaz.Service.Storage.Security;

/// <summary>
/// Result of a storage data-plane authorization check. Distinguishes between permission mismatches
/// (403 AuthorizationPermissionMismatch) and authentication failures (401 AuthenticationFailed).
/// </summary>
internal readonly record struct StorageAuthorizationResult(bool IsAuthorized, string? ErrorCode)
{
    /// <summary>
    /// Authorization succeeded.
    /// </summary>
    public static StorageAuthorizationResult Authorized() => new(true, null);

    /// <summary>
    /// Authorization failed with the specified error code (e.g. "AuthenticationFailed", "AuthorizationPermissionMismatch").
    /// </summary>
    public static StorageAuthorizationResult Denied(string errorCode) => new(false, errorCode);

    /// <summary>
    /// Shorthand for a 401 authentication failure (bad signature, expired token, etc.).
    /// </summary>
    public static StorageAuthorizationResult AuthenticationFailed() => Denied("AuthenticationFailed");

    /// <summary>
    /// Shorthand for a 403 permission mismatch (valid token but wrong sp= permissions for the HTTP method).
    /// </summary>
    public static StorageAuthorizationResult PermissionMismatch() => Denied("AuthorizationPermissionMismatch");

    /// <summary>
    /// Shorthand for a 403 source IP mismatch (caller IP falls outside the sip= range in the SAS token).
    /// </summary>
    public static StorageAuthorizationResult SourceIPMismatch() => Denied("AuthorizationSourceIPMismatch");

    /// <summary>
    /// No authentication information was provided and the target container is private.
    /// Maps to 401 with the standard Azure Storage WWW-Authenticate challenge.
    /// </summary>
    public static StorageAuthorizationResult NoAuthenticationInformation() => Denied("NoAuthenticationInformation");
}
