namespace Topaz.Service.Storage.Utils;

/// <summary>
/// Utility for generating and validating pop receipts.
/// Pop receipts are opaque tokens used to delete or update message visibility.
/// They are regenerated on each update and must match for the operation to succeed.
/// </summary>
internal static class PopReceiptGenerator
{
    /// <summary>
    /// Generate a new pop receipt token.
    /// Pop receipts are unique per message update and are required for delete/visibility update operations.
    /// </summary>
    /// <returns>A unique pop receipt token</returns>
    public static string Generate()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Validate that a pop receipt is in the expected format.
    /// </summary>
    /// <param name="popReceipt">The pop receipt to validate</param>
    /// <returns>True if the pop receipt is valid, false otherwise</returns>
    public static bool IsValid(string? popReceipt)
    {
        if (string.IsNullOrWhiteSpace(popReceipt))
        {
            return false;
        }

        // Pop receipts should be non-empty strings
        return popReceipt.Length > 0;
    }
}
