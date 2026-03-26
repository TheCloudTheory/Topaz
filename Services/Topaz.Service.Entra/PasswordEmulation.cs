namespace Topaz.Service.Entra;

using System;
using System.Security.Cryptography;

public static class PasswordEmulation
{
    // Excludes easily-confused chars like: O/0, I/l/1
    private const string Upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower   = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits  = "23456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{}:,.?";

    private const string All = Upper + Lower + Digits + Symbols;

    /// <summary>
    /// Emulates a "strong password" (16–64 chars) using a CSPRNG.
    /// Ensures at least one char from each category (upper/lower/digit/symbol).
    /// </summary>
    public static string GenerateEntraLikeStrongPassword(int length = 32)
    {
        if (length is < 16 or > 64)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be in the range 16..64.");

        Span<char> buffer = stackalloc char[length];

        // Guarantee category coverage
        buffer[0] = Pick(Upper);
        buffer[1] = Pick(Lower);
        buffer[2] = Pick(Digits);
        buffer[3] = Pick(Symbols);

        // Fill the rest uniformly from the full allowed alphabet
        for (var i = 4; i < length; i++)
            buffer[i] = Pick(All);

        // Shuffle so the first 4 aren't predictable positions
        Shuffle(buffer);

        return new string(buffer);
    }

    private static char Pick(string alphabet)
        => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

    private static void Shuffle(Span<char> span)
    {
        for (var i = span.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (span[i], span[j]) = (span[j], span[i]);
        }
    }
}