using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace YMes.Api.Enterprise;

// Technical CAS version for the complete native 13-column multiset. No row ID,
// text normalization, timestamp conversion or duplicate elimination is involved.
internal static class Et106DeclarationVersion
{
    private const string Prefix = "et106-decl-v1:";
    private const decimal MaximumStamp = 99999999999m;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly byte[] Domain = Utf8.GetBytes("YMES\0ET106\0DeclarationGroup\0v1\0");

    internal static string Compute(string customerNo, Et106DeclarationKey key,
        IEnumerable<Et106DeclarationDto> rows)
    {
        ArgumentNullException.ThrowIfNull(customerNo);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(rows);

        var encoded = new List<byte[]>();
        foreach (var row in rows)
        {
            ArgumentNullException.ThrowIfNull(row);
            encoded.Add(EncodeRow(row));
        }
        encoded.Sort(static (left, right) => left.AsSpan().SequenceCompareTo(right.AsSpan()));

        using var header = new MemoryStream();
        header.Write(Domain);
        header.WriteByte(7);
        Text(header, customerNo);
        Text(header, key.ShipTo);
        Text(header, key.Flow);
        Text(header, key.OverseaFlag);
        Text(header, key.DeclarationFlag);
        Text(header, key.BondFlag);
        Text(header, key.HandCarryFlag);
        Span<byte> count = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(count, encoded.Count);
        header.Write(count);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(header.GetBuffer().AsSpan(0, checked((int)header.Length)));
        Span<byte> size = stackalloc byte[sizeof(int)];
        foreach (var row in encoded)
        {
            BinaryPrimitives.WriteInt32BigEndian(size, row.Length);
            hash.AppendData(size);
            hash.AppendData(row);
        }
        return Prefix + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static byte[] EncodeRow(Et106DeclarationDto row)
    {
        using var data = new MemoryStream();
        data.WriteByte(13);
        Text(data, row.CustomerNo);
        Text(data, row.ShipTo);
        Text(data, row.Flow);
        Text(data, row.OverseaFlag);
        Text(data, row.DeclarationFlag);
        Text(data, row.BondFlag);
        Text(data, row.HandCarryFlag);
        Text(data, row.DeclarationWay);
        Text(data, row.Creator);
        Timestamp(data, row.CreateDate);
        Stamp(data, row.DataStamp);
        Text(data, row.Reviser);
        Timestamp(data, row.ReviseDate);
        return data.ToArray();
    }

    private static void Text(Stream target, string? value)
    {
        target.WriteByte(1);
        target.WriteByte(value is null ? (byte)0 : (byte)1);
        if (value is not null) Bytes(target, Utf8.GetBytes(value));
    }

    private static void Stamp(Stream target, decimal? value)
    {
        target.WriteByte(2);
        target.WriteByte(value.HasValue ? (byte)1 : (byte)0);
        if (!value.HasValue) return;
        var number = value.Value;
        if (number < -MaximumStamp || number > MaximumStamp || decimal.Truncate(number) != number)
            throw new ArgumentOutOfRangeException(nameof(value), "Expected an integral native numeric(11,0) stamp.");
        var canonical = number == decimal.Zero ? "0" : number.ToString("G29", CultureInfo.InvariantCulture);
        Bytes(target, Utf8.GetBytes(canonical));
    }

    private static void Timestamp(Stream target, DateTime? value)
    {
        target.WriteByte(3);
        target.WriteByte(value.HasValue ? (byte)1 : (byte)0);
        if (!value.HasValue) return;
        // timestamp without time zone is a stored wall-clock value. Keep every
        // tick; DateTime.Kind and the machine's local time zone do not transform it.
        Span<byte> ticks = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(ticks, value.Value.Ticks);
        target.Write(ticks);
    }

    private static void Bytes(Stream target, byte[] value)
    {
        Span<byte> size = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(size, value.Length);
        target.Write(size);
        target.Write(value);
    }
}
