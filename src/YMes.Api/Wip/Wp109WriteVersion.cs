using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace YMes.Api.Wip;

// YMES technical CAS, not a claim about an original MES algorithm. The caller
// must obtain header/detail/BLOB digest from one statement snapshot while it
// owns the reviewed group locks. No database/native DataStamp is invented.
internal static class Wp109WriteVersion
{
    internal const string Prefix = "wp109-v1:";
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static string Compute(Wp109RawHeader header,
        IReadOnlyList<Wp109DetailDto> details)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(details);
        using var body = new MemoryStream();
        Text(body, "YMES/WP109/raw15+detail2/blob-sha256/v1");
        Text(body, header.SwrNo); Number(body, header.SwrType); Number(body, header.Status);
        Text(body, header.CustomerNo); Text(body, header.OpNo); Text(body, header.DeviceNo);
        Time(body, header.StartTime); Time(body, header.EndTime);
        Text(body, header.CustomerLotNo); Text(body, header.MoNo);
        Blob(body, header.Blob); Text(body, header.SwrDesc); Text(body, header.Creator);
        Time(body, header.CreateDate); Text(body, header.LegacyAttachmentPath);

        var rows = new List<byte[]>(details.Count);
        foreach (var detail in details)
        {
            ArgumentNullException.ThrowIfNull(detail);
            if (!StringComparer.Ordinal.Equals(detail.SwrNo, header.SwrNo))
                throw new ArgumentException("Detail raw owner differs from the header.", nameof(details));
            using var row = new MemoryStream();
            Text(row, detail.SwrNo); Text(row, detail.LotNo); rows.Add(row.ToArray());
        }
        rows.Sort(static (left, right) => left.AsSpan().SequenceCompareTo(right));
        Count(body, rows.Count);
        foreach (var row in rows) { Count(body, row.Length); body.Write(row); }
        return Prefix + Convert.ToHexString(SHA256.HashData(body.GetBuffer().AsSpan(0,
            checked((int)body.Length)))).ToLowerInvariant();
    }

    internal static bool Valid(string? token) => token is not null
        && token.Length == Prefix.Length + 64 && token.StartsWith(Prefix, StringComparison.Ordinal)
        && token.AsSpan(Prefix.Length).IndexOfAnyExcept("0123456789abcdef") < 0;

    internal static bool Matches(string expected, string actual)
    {
        if (!Valid(expected) || !Valid(actual)) return false;
        return CryptographicOperations.FixedTimeEquals(Utf8.GetBytes(expected), Utf8.GetBytes(actual));
    }

    // No full BLOB byte[] allocation. Caller retains stream/reader/tx ownership
    // until this awaited operation completes or cancellation propagates.
    internal static async Task<Wp109BlobDigest> HashBlobAsync(Stream stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        long length = 0;
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, 64 * 1024), cancellationToken)) != 0)
            {
                length = checked(length + read); hash.AppendData(buffer, 0, read);
            }
            return new(length, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
        }
        finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
    }

    private static void Text(Stream output, string? value)
    {
        output.WriteByte((byte)'s');
        if (value is null) { output.WriteByte(0); return; }
        output.WriteByte(1); var bytes = Utf8.GetBytes(value); Count(output, bytes.Length); output.Write(bytes);
    }

    private static void Number(Stream output, long value)
    {
        output.WriteByte((byte)'n'); Span<byte> data = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(data, value); output.Write(data);
    }

    private static void Time(Stream output, DateTime? value)
    {
        output.WriteByte((byte)'t');
        if (value is null) { output.WriteByte(0); return; }
        if (value.Value.Kind != DateTimeKind.Unspecified)
            throw new ArgumentException("Native timestamp must retain raw wall-clock ticks.", nameof(value));
        output.WriteByte(1); Number(output, value.Value.Ticks);
    }

    private static void Blob(Stream output, Wp109BlobDigest? value)
    {
        output.WriteByte((byte)'b');
        if (value is null) { output.WriteByte(0); return; }
        if (value.Length < 0 || value.Sha256.Length != 64
            || value.Sha256.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0)
            throw new ArgumentException("Actual complete BLOB length/SHA required.", nameof(value));
        if (value.Length == 0 && value.Sha256 != "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")
            throw new ArgumentException("Empty bytea requires SHA256(empty).", nameof(value));
        output.WriteByte(1); Number(output, value.Length); output.Write(Convert.FromHexString(value.Sha256));
    }

    private static void Count(Stream output, int value)
    {
        Span<byte> data = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(data, value); output.Write(data);
    }
}
