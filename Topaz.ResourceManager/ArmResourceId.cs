using System.Text;

namespace Topaz.ResourceManager;

public static class ArmResourceId
{
    /// <summary>
    /// Builds the ARM id for a resource from its type and name, which interleave: type
    /// <c>Microsoft.ServiceBus/namespaces/topics</c> with name <c>ns/topic</c> gives
    /// <c>{scope}/providers/Microsoft.ServiceBus/namespaces/ns/topics/topic</c>.
    ///
    /// <para>
    /// Concatenating the two instead puts a type keyword where a resource name belongs, so every
    /// child lookup resolves against the wrong parent. Only resources whose type has a single
    /// segment after the provider are unaffected, which is why this is invisible until a template
    /// declares a child resource.
    /// </para>
    /// </summary>
    /// <param name="scope">
    /// The id prefix the resource hangs off, with no trailing slash — for example
    /// <c>/subscriptions/{id}/resourceGroups/{name}</c>, or an empty string at tenant scope.
    /// </param>
    public static string Build(string scope, string? type, string? name)
    {
        var typeSegments = (type ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var nameSegments = (name ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

        // typeSegments[0] is the provider namespace; the rest pair up with the name segments.
        // Anything that does not pair is left as it was rather than guessed at.
        if (typeSegments.Length < 2 || typeSegments.Length - 1 != nameSegments.Length)
            return $"{scope}/providers/{type}/{name}";

        var id = new StringBuilder(scope).Append("/providers/").Append(typeSegments[0]);
        for (var i = 0; i < nameSegments.Length; i++)
            id.Append('/').Append(typeSegments[i + 1]).Append('/').Append(nameSegments[i]);

        return id.ToString();
    }
}
