using System.Security.Cryptography;
using System.Text;

namespace Topaz.Service.KeyVault;

internal static class KeyVaultBackupCipher
{
    // AES-256-CBC key used to encrypt backup blobs. This is the emulator's vault-specific master key.
    // Structure of an encrypted blob: [9 magic][1 version][16 IV][n ciphertext], then base64url-encoded.
    private static readonly byte[] BackupEncryptionKey = Convert.FromHexString("546F70617A4B565F42434B5F56312E30546F70617A4B565F42434B5F56312E30");
    private static readonly byte[] BackupMagic = Encoding.UTF8.GetBytes("TOPAZKVBK");
    private const byte BackupVersion = 0x01;

    internal static string EncryptBackup(byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = BackupEncryptionKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var blob = new byte[BackupMagic.Length + 1 + aes.IV.Length + ciphertext.Length];
        BackupMagic.CopyTo(blob, 0);
        blob[BackupMagic.Length] = BackupVersion;
        aes.IV.CopyTo(blob, BackupMagic.Length + 1);
        ciphertext.CopyTo(blob, BackupMagic.Length + 1 + aes.IV.Length);

        return Convert.ToBase64String(blob).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal static byte[] DecryptBackup(string encoded)
    {
        var padded = encoded.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;
        if (remainder == 2) padded += "==";
        else if (remainder == 3) padded += "=";

        var blob = Convert.FromBase64String(padded);
        var headerLength = BackupMagic.Length + 1 + 16;

        if (blob.Length < headerLength)
            throw new InvalidOperationException("Invalid backup blob: too short.");

        for (var i = 0; i < BackupMagic.Length; i++)
            if (blob[i] != BackupMagic[i])
                throw new InvalidOperationException("Invalid backup blob: magic header mismatch.");

        if (blob[BackupMagic.Length] != BackupVersion)
            throw new InvalidOperationException($"Unsupported backup version: {blob[BackupMagic.Length]}.");

        var iv = new byte[16];
        Array.Copy(blob, BackupMagic.Length + 1, iv, 0, 16);
        var ciphertext = new byte[blob.Length - headerLength];
        Array.Copy(blob, headerLength, ciphertext, 0, ciphertext.Length);

        using var aes = Aes.Create();
        aes.Key = BackupEncryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }
}
