using System.Text.RegularExpressions;

namespace Unfiltered.Api.Services;

/// <summary>
/// Stores proof-of-purchase and rejection screenshots outside wwwroot so they are never public.
/// Only moderators can read them, through the moderation API.
/// </summary>
public partial class FileStore
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["application/pdf"] = ".pdf",
    };

    private static readonly Dictionary<string, string> ContentTypes = Extensions.ToDictionary(kv => kv.Value, kv => kv.Key);

    [GeneratedRegex(@"^[a-f0-9]{32}\.(jpg|png|webp|pdf)$")]
    private static partial Regex StoredName();

    private readonly string _root;

    public FileStore(IWebHostEnvironment env, IConfiguration config)
    {
        _root = config["Uploads:Path"] is { Length: > 0 } configured
            ? configured
            : Path.Combine(env.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(_root);
    }

    /// <summary>Returns an error message, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile file)
    {
        if (file.Length == 0) return "The uploaded file is empty.";
        if (file.Length > MaxBytes) return "Each file must be 5 MB or smaller.";
        if (!Extensions.ContainsKey(file.ContentType)) return "Only JPG, PNG, WEBP or PDF files are allowed.";
        if (!HasMatchingSignature(file)) return "That file does not look like a real image or PDF.";
        return null;
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        var name = Guid.NewGuid().ToString("N") + Extensions[file.ContentType];
        await using var stream = File.Create(Path.Combine(_root, name));
        await file.CopyToAsync(stream, ct);
        return name;
    }

    public (string Path, string ContentType)? Open(string name)
    {
        if (!StoredName().IsMatch(name)) return null;
        var path = Path.Combine(_root, name);
        return File.Exists(path) ? (path, ContentTypes[Path.GetExtension(name)]) : null;
    }

    public void Delete(string? name)
    {
        if (name is not null && StoredName().IsMatch(name)) File.Delete(Path.Combine(_root, name));
    }

    // Checks the first bytes so a renamed executable or HTML file can't be uploaded as an "image".
    private static bool HasMatchingSignature(IFormFile file)
    {
        Span<byte> head = stackalloc byte[12];
        using var s = file.OpenReadStream();
        var read = s.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        head = head[..read];
        return file.ContentType switch
        {
            "image/jpeg" => head.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            "image/png" => head.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
            "image/webp" => read >= 12 && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8),
            "application/pdf" => head.StartsWith("%PDF"u8),
            _ => false,
        };
    }
}
