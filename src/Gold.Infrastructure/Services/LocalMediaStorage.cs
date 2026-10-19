using System.Security.Cryptography;

namespace Gold.Infrastructure.Services;

public sealed record StoredMedia(string FileName, string ContentType, long Bytes);

/// <summary>
/// Stores uploads OUTSIDE wwwroot with random names after checking magic bytes (not the extension).
/// SVG is rejected on purpose (can carry scripts). Production: swap for S3-compatible storage (ArvanCloud).
/// </summary>
public sealed class LocalMediaStorage(string root)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    /// <summary>Short MP4 clips (e.g. the owner talking to customers) — plays even where Instagram is filtered.</summary>
    public const long MaxVideoBytes = 25 * 1024 * 1024;

    private static readonly (string Ext, string Type, Func<byte[], bool> Match)[] Kinds =
    [
        ("jpg", "image/jpeg", b => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF),
        ("png", "image/png", b => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47),
        ("webp", "image/webp", b => b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P'),
        ("avif", "image/avif", b => b.Length > 12 && b[4] == 'f' && b[5] == 't' && b[6] == 'y' && b[7] == 'p' && b[8] == 'a' && b[9] == 'v' && b[10] == 'i'),
        ("mp4", "video/mp4", b => b.Length > 12 && b[4] == 'f' && b[5] == 't' && b[6] == 'y' && b[7] == 'p' && !(b[8] == 'h' && b[9] == 'e' && b[10] == 'i')),
    ];

    public async Task<StoredMedia?> SaveAsync(Stream input, long length, CancellationToken ct)
    {
        if (length is <= 0 or > MaxVideoBytes) return null;
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var kind = Kinds.FirstOrDefault(k => k.Match(bytes));
        if (kind.Ext is null) return null;
        if (bytes.Length > (kind.Ext == "mp4" ? MaxVideoBytes : MaxBytes)) return null;

        Directory.CreateDirectory(root);
        var name = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}.{kind.Ext}";
        await File.WriteAllBytesAsync(Path.Combine(root, name), bytes, ct);
        return new StoredMedia(name, kind.Type, bytes.Length);
    }

    public (string Path, string Type)? Open(string fileName)
    {
        // Only our own generated names: 32 hex chars + known extension → no path traversal possible.
        if (!System.Text.RegularExpressions.Regex.IsMatch(fileName, "^[a-f0-9]{32}\\.(jpg|png|webp|avif|mp4)$")) return null;
        var path = Path.Combine(root, fileName);
        if (!File.Exists(path)) return null;
        var ext = Path.GetExtension(fileName)[1..];
        return (path, Kinds.First(k => k.Ext == ext).Type);
    }
}
