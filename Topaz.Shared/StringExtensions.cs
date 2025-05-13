namespace Topaz.Shared;

public static class StringExtensions
{
    public static string? ExtractValueFromPath(this string path, int index, string? defaultValue = null)
    {
        var requestParts = path.Split('/');

        return requestParts.Length > index ? requestParts[index] : defaultValue;
    }
}