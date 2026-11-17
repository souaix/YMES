using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace YMes.Api.Quality;

public sealed class LocalYmesQc112FileStorage : IYmesQc112FileStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".bmp", ".pdf" };
    private static readonly Regex LogicalPathPattern = new(
        @"^QC112/\d{4}/\d{2}/[0-9a-f]{32}\.(jpg|jpeg|jpe|jfif|png|bmp|pdf)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly string _root;
    private readonly string _staging;
    private readonly string _pending;
    private readonly long _maxStorageBytes;
    private readonly SemaphoreSlim _storageLock = new(1, 1);

    public LocalYmesQc112FileStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _root = Path.GetFullPath(configuration["YMES_QC112_FILE_STORAGE_PATH"] ??
            Path.Combine(environment.ContentRootPath, "App_Data", "qc112-files"));
        _staging = Path.Combine(_root, ".staging");
        _pending = Path.Combine(_root, ".pending");
        _maxStorageBytes = long.TryParse(configuration["YMES_QC112_MAX_STORAGE_BYTES"], out var configuredLimit)
            ? Math.Clamp(configuredLimit, 50L * 1024 * 1024, 10L * 1024 * 1024 * 1024)
            : 1024L * 1024 * 1024;
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_staging);
        Directory.CreateDirectory(_pending);
    }

    public async Task<Qc112StoredFile> SaveAsync(IFormFile upload, string displayFileName, CancellationToken cancellationToken)
    {
        if (upload.Length is <= 0 or > 25L * 1024 * 1024)
            throw new Qc112RuleException(StatusCodes.Status413PayloadTooLarge, "附件大小需介於 1 byte 至 25 MB。 ");

        var extension = Path.GetExtension(displayFileName);
        if (!AllowedExtensions.Contains(extension) ||
            !string.Equals(extension, Path.GetExtension(upload.FileName), StringComparison.OrdinalIgnoreCase))
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "附件只允許 JPG、JPEG、JPE、JFIF、PNG、BMP、PDF，且檔名副檔名需與附件一致。");

        await using var input = upload.OpenReadStream();
        var prefix = new byte[8];
        var prefixLength = await input.ReadAsync(prefix.AsMemory(), cancellationToken);
        if (!HasValidSignature(extension, prefix.AsSpan(0, prefixLength)))
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "附件內容與副檔名不符或格式無效。");
        input.Position = 0;

        await _storageLock.WaitAsync(cancellationToken);
        try
        {
            var currentBytes = EnumerateStoredFiles().Sum(path => new FileInfo(path).Length);
            if (currentBytes + upload.Length > _maxStorageBytes)
                throw new Qc112RuleException(StatusCodes.Status507InsufficientStorage, "QC112 附件儲存空間已達設定上限。");

            var now = DateTime.UtcNow;
            var relative = $"QC112/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var destination = ResolvePath(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var temporary = Path.Combine(_staging, $"{Guid.NewGuid():N}.tmp");
            try
            {
                await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await input.CopyToAsync(output, cancellationToken);
                    await output.FlushAsync(cancellationToken);
                }
                File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }

            return new Qc112StoredFile(relative);
        }
        finally { _storageLock.Release(); }
    }

    public string? MoveToPendingDelete(string displayPath)
    {
        if (!LogicalPathPattern.IsMatch(displayPath)) return null;
        var source = ResolvePath(displayPath);
        if (!File.Exists(source)) return null;
        var token = Guid.NewGuid().ToString("N");
        File.Move(source, Path.Combine(_pending, token));
        return token;
    }

    public void RestorePendingDelete(string token, string displayPath)
    {
        ValidateToken(token);
        var pending = Path.Combine(_pending, token);
        if (!File.Exists(pending)) return;
        var destination = ResolvePath(displayPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (!File.Exists(destination)) File.Move(pending, destination);
    }

    public void FinalizePendingDelete(string token)
    {
        ValidateToken(token);
        var pending = Path.Combine(_pending, token);
        if (File.Exists(pending)) File.Delete(pending);
    }

    public void Delete(string displayPath)
    {
        var path = ResolvePath(displayPath);
        if (File.Exists(path)) File.Delete(path);
    }

    private string ResolvePath(string logicalPath)
    {
        if (!LogicalPathPattern.IsMatch(logicalPath))
            throw new InvalidOperationException("QC112 file path is not a valid logical storage path.");
        var relative = logicalPath.Replace('/', Path.DirectorySeparatorChar);
        var resolved = Path.GetFullPath(Path.Combine(_root, relative));
        if (!resolved.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("QC112 storage path escaped its root.");
        return resolved;
    }

    private IEnumerable<string> EnumerateStoredFiles() => Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories);

    private static void ValidateToken(string token)
    {
        if (token.Length != 32 || !token.All(Uri.IsHexDigit))
            throw new InvalidOperationException("Invalid QC112 pending-delete token.");
    }

    private static bool HasValidSignature(string extension, ReadOnlySpan<byte> bytes) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" or ".jpe" or ".jfif" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        ".png" => bytes.Length >= 8 && bytes.SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
        ".bmp" => bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4d,
        ".pdf" => bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8),
        _ => false,
    };
}
