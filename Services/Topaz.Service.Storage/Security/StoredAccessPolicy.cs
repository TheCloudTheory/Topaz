namespace Topaz.Service.Storage.Security;

internal sealed record StoredAccessPolicy(string? Permissions, string? StartsOn, string? ExpiresOn);
