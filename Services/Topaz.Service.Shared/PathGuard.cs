namespace Topaz.Service.Shared;

/// <summary>
/// Provides path traversal protection utilities for use in data-plane and control-plane classes.
/// Every user-supplied value that is incorporated into a filesystem path must be validated with
/// <see cref="ValidateName"/> before the path is constructed, and optionally confirmed with
/// <see cref="EnsureWithinDirectory"/> afterwards as a second layer of defence.
/// </summary>
public static class PathGuard
{
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if <paramref name="name"/> contains
    /// characters that could be used to traverse directories (e.g. <c>..</c>, <c>/</c>, <c>\</c>).
    /// Call this before using any user-supplied value as part of a filesystem path.
    /// </summary>
    /// <param name="name">The resource name or identifier to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="name"/> contains path-traversal characters.
    /// </exception>
    public static void ValidateName(string name)
    {
        if (name.Contains("..") || name.Contains('/') || name.Contains('\\'))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
    }

    /// <summary>
    /// Sanitizes a user-supplied resource name by stripping directory components via
    /// <see cref="Path.GetFileName"/>, then validates that no traversal characters remain.
    /// Returns the sanitized name, which should replace the raw user value in all
    /// subsequent path construction. Use this instead of <see cref="ValidateName"/> so
    /// that static analysis tools (e.g. CodeQL) can recognise the taint as cleared.
    /// </summary>
    /// <param name="name">The user-supplied resource name to sanitize.</param>
    /// <returns>The sanitized name, safe to embed in a file path.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is null/empty after stripping, or still contains forbidden characters.
    /// </exception>
    public static string SanitizeName(string name)
    {
        // Path.GetFileName is recognised by CodeQL as a path-traversal sanitizer.
        var sanitized = Path.GetFileName(name);
        if (string.IsNullOrEmpty(sanitized))
            throw new InvalidOperationException("Identifier is empty after sanitization.");
        ValidateName(sanitized);
        return sanitized;
    }

    /// <summary>
    /// Ensures that <paramref name="candidatePath"/> resolves to a location inside
    /// <paramref name="baseDirectory"/> after path canonicalization.
    /// Provides a second layer of defence after <see cref="ValidateName"/>.
    /// </summary>
    /// <param name="candidatePath">The fully-constructed path to validate.</param>
    /// <param name="baseDirectory">The root directory that the path must stay within.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resolved path escapes <paramref name="baseDirectory"/>.
    /// </exception>
    public static void EnsureWithinDirectory(string candidatePath, string baseDirectory)
    {
        var fullCandidate = Path.GetFullPath(candidatePath);
        var fullBase = Path.GetFullPath(baseDirectory);

        // Append separator so a directory named "baseXYZ" is not mistakenly accepted
        // as a prefix for paths that should live under "base/XYZ".
        if (!fullBase.EndsWith(Path.DirectorySeparatorChar))
            fullBase += Path.DirectorySeparatorChar;

        if (!fullCandidate.StartsWith(fullBase, StringComparison.Ordinal))
            throw new InvalidOperationException("Computed path escapes the permitted emulator directory.");
    }
}
