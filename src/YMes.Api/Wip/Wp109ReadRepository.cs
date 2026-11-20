using System.Data;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using YMes.Api.Enterprise;

namespace YMes.Api.Wip;

public sealed class Wp109ReadRepository(NpgsqlDataSource dataSource)
{
    private const string Basis = "\"YMES\".tblwipswrbasis";
    private const string Detail = "\"YMES\".tblwipswrdetail";
    private const string Projection = """
        a.swrno,a.swrtype,a.status,a.customerno,a.opno,a.deviceno,a.starttime,a.endtime,
        a.customerlotno,a.mono,a.swrdesc,a.creator,a.createdate,
        octet_length(a.attachfile)::bigint,
        CASE WHEN a.attachfilepath IS NOT NULL AND a.attachfilepath<>'' THEN 'Y' ELSE 'N' END
        """;

    public Wp109QueryResult List(Wp109Query query)
    {
        Validate(query);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        ReadOnly(connection, transaction); EnsureBasis(connection, transaction); EnsureDetail(connection, transaction);
        var conditions = new List<string> { "a.status=@status" };
        using var command = new NpgsqlCommand { Connection = connection, Transaction = transaction };
        Numeric(command, "status", query.Status);
        if (query.SwrTypeEnabled) { conditions.Add("a.swrtype=@swrType"); Numeric(command, "swrType", query.SwrType!.Value); }
        if (query.CustomerNoEnabled) { conditions.Add("a.customerno=@customerNo"); Text(command, "customerNo", query.CustomerNo!); }
        if (query.LotNoEnabled) { conditions.Add("b.lotno=ANY(@lotNos)"); TextArray(command, "lotNos", query.LotNos!); }
        if (query.DeviceNoEnabled) { conditions.Add("a.deviceno=@deviceNo"); Text(command, "deviceNo", query.DeviceNo!); }
        if (query.OpNoEnabled) { conditions.Add("a.opno=ANY(@opNos)"); TextArray(command, "opNos", query.OpNos!); }
        if (query.CustomerLotNoEnabled) { conditions.Add("a.customerlotno=@customerLotNo"); Text(command, "customerLotNo", query.CustomerLotNo!); }
        if (query.MoNoEnabled) { conditions.Add("a.mono=upper(@moNo)"); Text(command, "moNo", query.MoNo!); }
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = query.Limit + 1;
        command.CommandText = $"SELECT {Projection},b.lotno FROM {Basis} a LEFT JOIN {Detail} b ON b.swrno=a.swrno WHERE {string.Join(" AND ", conditions)} ORDER BY a.swrno COLLATE \"C\",b.lotno COLLATE \"C\" NULLS FIRST LIMIT @take;";
        var rows = new List<Wp109QueryRowDto>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(new(Map(reader), Optional(reader, 15)));
        var more = rows.Count > query.Limit;
        if (more) rows.RemoveAt(rows.Count - 1);
        transaction.Commit(); return new(rows, query.Limit, more);
    }

    public Wp109ItemResult Get(string swrNo)
    {
        Key(swrNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        ReadOnly(connection, transaction); EnsureBasis(connection, transaction); EnsureDetail(connection, transaction);
        // One statement/snapshot: never combine a header from before an atomic
        // writer commit with details read after that commit.
        using var command = new NpgsqlCommand($"SELECT {Projection},b.lotno FROM {Basis} a LEFT JOIN {Detail} b ON b.swrno=a.swrno WHERE a.swrno=@no ORDER BY b.lotno COLLATE \"C\" NULLS FIRST;", connection, transaction);
        Text(command, "no", swrNo);
        Wp109SwrDto? item = null; var details = new List<Wp109DetailDto>();
        using (var reader = command.ExecuteReader())
            while (reader.Read())
            {
                item ??= Map(reader);
                if (!reader.IsDBNull(15)) details.Add(new(item.SwrNo, reader.GetString(15)));
            }
        if (item is null) throw new Wp109ReadRuleException(404, "找不到指定的原始SWRNo。");
        transaction.Commit(); return new(item, details);
    }

    internal async Task StreamDownloadAsync(string swrNo,
        Func<Wp109Download, Stream, CancellationToken, Task> copyToResponse,
        CancellationToken cancellationToken)
    {
        Key(swrNo);
        ArgumentNullException.ThrowIfNull(copyToResponse);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        // Reuse the immutable basis schema guard. Its bounded synchronous
        // catalog probes retain the existing command timeout; no background
        // task is abandoned if cancellation arrives during that guard.
        EnsureBasis(connection, transaction);
        cancellationToken.ThrowIfCancellationRequested();
        await using (var command = new NpgsqlCommand($"SELECT octet_length(attachfile)::bigint,substring(attachfile FROM 1 FOR 5),attachfile FROM {Basis} WHERE swrno=@no;", connection, transaction))
        {
            Text(command, "no", swrNo);
            // Cancellation during a client-side write must also cancel the
            // source command, rather than merely drain a large unread result.
            using var cancelSource = cancellationToken.Register(() => TryCancel(command));
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new Wp109ReadRuleException(404, "找不到指定的原始SWRNo。");
            if (await reader.IsDBNullAsync(0, cancellationToken))
                throw new Wp109ReadRuleException(404, "沒有可下載的資料庫附件；舊路徑提示不能替代檔案。");
            var length = reader.GetInt64(0);
            if (length <= 0) throw new Wp109ReadRuleException(404, "資料庫附件為空，沒有可下載的內容。");
            // This column is server-bounded to five bytes. The BLOB itself is
            // never materialized or hashed before sending the response.
            var prefix = await reader.GetFieldValueAsync<byte[]>(1, cancellationToken);
            var pdf = prefix.AsSpan().StartsWith("%PDF-"u8);
            var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(swrNo)))[..20];
            var metadata = new Wp109Download(length, pdf ? "application/pdf" : "application/octet-stream",
                $"SWR-{keyHash}.{(pdf ? "pdf" : "bin")}");
            await using var content = await reader.GetStreamAsync(2, cancellationToken);
            try
            {
                // The callback is awaited INSIDE stream/reader/command/tx/
                // connection ownership. Header selection and HTTP copy happen
                // only after exact same-row NULL/length checks above.
                await copyToResponse(metadata, content, cancellationToken);
            }
            catch
            {
                TryCancel(command);
                throw;
            }
        }
        // Reader and stream are already asynchronously disposed before COMMIT.
        // All await-using scopes generate finally cleanup on success, database
        // failure, client cancellation, or response-copy failure. Read-only
        // transaction disposal rolls back if completion/commit did not occur.
        await transaction.CommitAsync(cancellationToken);
    }

    private static void TryCancel(NpgsqlCommand command)
    {
        try { command.Cancel(); }
        catch (NpgsqlException) { }
        catch (ObjectDisposedException) { }
    }

    private static void EnsureBasis(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        if (!SwrReferenceGuard.EnsureOptional(connection, transaction, SwrReference.Customer, Unavailable)) throw Unavailable();
        using var probe = new NpgsqlCommand($"SELECT {Projection} FROM {Basis} a WHERE false;", connection, transaction);
        probe.ExecuteNonQuery();
    }

    private static void EnsureDetail(NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        Wp109DetailReadGuard.Ensure(connection, transaction, Unavailable);

    private static void ReadOnly(NpgsqlConnection connection, NpgsqlTransaction transaction)
    { using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction); command.ExecuteNonQuery(); }

    private static Wp109SwrDto Map(NpgsqlDataReader reader)
    {
        long? bytes = reader.IsDBNull(13) ? null : reader.GetInt64(13);
        return new(reader.GetString(0), decimal.ToInt32(reader.GetDecimal(1)), decimal.ToInt32(reader.GetDecimal(2)),
            reader.GetString(3), reader.GetString(4), Optional(reader, 5), Time(reader, 6), Time(reader, 7),
            Optional(reader, 8), Optional(reader, 9), reader.GetString(10), Optional(reader, 11), Time(reader, 12),
            new(reader.GetString(14), bytes, bytes > 0));
    }

    private static void Validate(Wp109Query query)
    {
        if (query.Status is not (0 or 1 or 2 or 99)) throw Bad("Status只接受0／1／2／99。");
        if (query.Limit is < 1 or > 500) throw Bad("Limit須為1–500個JOIN結果列。");
        if (query.SwrTypeEnabled && query.SwrType is not (0 or 1)) throw Bad("勾選SWRType後須指定0或1。");
        if (query.CustomerNoEnabled) Value(query.CustomerNo, 50, "CustomerNo");
        if (query.LotNoEnabled) Values(query.LotNos, 50, "LotNos");
        if (query.DeviceNoEnabled) Value(query.DeviceNo, 50, "DeviceNo");
        if (query.OpNoEnabled) Values(query.OpNos, 20, "OpNos");
        if (query.CustomerLotNoEnabled) Value(query.CustomerLotNo, 50, "CustomerLotNo");
        if (query.MoNoEnabled) Value(query.MoNo, 50, "MoNo");
    }

    private static void Key(string value) => Value(value, 20, "SWRNo");
    private static void Values(string[]? values, int max, string name)
    {
        if (values is null || values.Length is < 1 or > 100) throw Bad($"勾選{name}後須提供1–100個獨立值。");
        foreach (var value in values) Value(value, max, name);
    }
    private static void Value(string? value, int max, string name)
    {
        if (value is null || value.Trim(' ').Length == 0 || value.Contains('\0')) throw Bad($"{name}不可空白或含NUL。");
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); }
        catch (EncoderFallbackException) { throw Bad($"{name}含無效Unicode。"); }
        if (value.EnumerateRunes().Count() > max) throw Bad($"{name}超過來源欄位長度{max}。");
        // Validate without rewriting: %, *, /, |, quotes, case and padding stay
        // literal query values. Only native MONo filtering calls database upper.
    }
    private static string? Optional(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? Time(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Unspecified);
    private static void Text(NpgsqlCommand command, string name, string value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = value;
    private static void TextArray(NpgsqlCommand command, string name, string[] values) => command.Parameters.Add(name, NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = values;
    private static void Numeric(NpgsqlCommand command, string name, int value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (decimal)value;
    private static Wp109ReadRuleException Bad(string message) => new(400, message);
    private static Wp109ReadRuleException Unavailable() => new(503, "必要SWR結構或讀取權限尚未符合來源；未回傳替代資料。");
}
