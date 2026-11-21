using System.Buffers;
using System.Security.Cryptography;

namespace YMes.Api.Wip;

// Server-owned seekable spool, not a caller path or a full in-memory byte[].
// CreateNew/0600/DeleteOnClose prevent adoption of an existing file and retain
// the bytes only for this request. No original filename is persisted.
internal sealed class Wp109IncomingAttachment : IAsyncDisposable
{
    internal FileStream Content { get; }
    internal long Length { get; }
    internal string Sha256 { get; }
    private Wp109IncomingAttachment(FileStream content, long length, string sha256)
    { Content = content; Length = length; Sha256 = sha256; }

    internal static async Task<Wp109IncomingAttachment> ReadAsync(Stream input,
        long maximumBytes, CancellationToken token)
    {
        if (maximumBytes < 0) throw new Wp109WriteRuleException(503, "附件大小設定無效。");
        var path = Path.Combine(Path.GetTempPath(), "ymes-wp109-" + Guid.NewGuid().ToString("N") + ".tmp");
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew, Access = FileAccess.ReadWrite, Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose
        };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        var output = new FileStream(path, options);
        byte[]? buffer = null;
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            buffer = ArrayPool<byte>.Shared.Rent(64 * 1024); var prefix = new byte[5];
            var prefixLength = 0; long length = 0;
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, 64 * 1024), token)) != 0)
            {
                length = checked(length + read);
                if (length > maximumBytes) throw new Wp109WriteRuleException(413, "附件超過系統設定的大小上限。");
                hash.AppendData(buffer, 0, read);
                if (prefixLength < prefix.Length)
                {
                    var take = Math.Min(prefix.Length - prefixLength, read);
                    buffer.AsSpan(0, take).CopyTo(prefix.AsSpan(prefixLength)); prefixLength += take;
                }
                await output.WriteAsync(buffer.AsMemory(0, read), token);
            }
            if (prefixLength != 5 || !prefix.AsSpan().SequenceEqual("%PDF-"u8))
                throw new Wp109WriteRuleException(422, "請選擇非空的PDF附件。");
            // Signature check plus complete stream ingestion; not a claim of
            // semantic PDF document parsing. Every byte, including the last,
            // is retained. The writer rechecks current limit in its tx.
            await output.FlushAsync(token); output.Position = 0;
            return new(output, length, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
        }
        catch { await output.DisposeAsync(); throw; }
        finally { if (buffer is not null) ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
    }

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
