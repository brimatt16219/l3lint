using System.Security.Cryptography;

namespace L3Lint.Core.Packs;

/// <summary>
/// SHA-256 of pack files for the manifest check (ADR-0024). Text files are hashed with CRLF turned into LF so a
/// checkout on any operating system gives the same hash.
/// </summary>
internal static class PackHashing
{
    public static string Sha256(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Sha256(buffer.ToArray());
    }

    public static string Sha256(byte[] bytes)
    {
        var normalized = new List<byte>(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == (byte)'\r' && i + 1 < bytes.Length && bytes[i + 1] == (byte)'\n')
            {
                continue;
            }

            normalized.Add(bytes[i]);
        }

        return Convert.ToHexStringLower(SHA256.HashData(normalized.ToArray()));
    }
}
