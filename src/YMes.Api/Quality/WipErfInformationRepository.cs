using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class WipErfInformationRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRowsPerDetail = 500;
    private const int MaxAttachments = 500;
    private const long MaxAttachmentBytes = 20 * 1024 * 1024;

    public WipErfInformationResponse? GetInformation(string erfNo, int? requestedSource)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = BeginReadOnly(connection);
        int source;
        string lotNo;
        using (var command = new NpgsqlCommand("""
            SELECT erfsource::int,lotno
            FROM "YMES".tblwiperfbasis
            WHERE erfno=@erfNo AND (@source IS NULL OR erfsource=@source)
            LIMIT 1;
            """, connection, transaction))
        {
            command.Parameters.Add("erfNo", NpgsqlDbType.Varchar).Value = erfNo;
            command.Parameters.Add("source", NpgsqlDbType.Integer).Value = (object?)requestedSource ?? DBNull.Value;
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            source = reader.GetInt32(0);
            lotNo = reader.GetString(1);
        }

        var createHoldCodes = ReadCreateHoldCodes(connection, transaction, erfNo);
        var holdCodeDispositions = ReadHoldCodeDispositions(connection, transaction, erfNo);
        var dispositions = ReadErfDispositions(connection, transaction, erfNo);
        dispositions.AddRange(ReadOcapDispositions(connection, transaction, erfNo));
        var lotDispositions = ReadLotDispositions(connection, transaction, erfNo);
        var attachments = ReadAttachments(connection, transaction, erfNo, lotNo);
        transaction.Commit();
        return new(source, createHoldCodes, holdCodeDispositions, dispositions, lotDispositions, attachments);
    }

    public WipErfAttachmentContent? GetAttachment(string erfNo, string lotNo, int erfSource, string source,
        string name, string? ocapNo)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = BeginReadOnly(connection);
        var sql = source == "OCAP"
            ? "SELECT octet_length(a.attachbody)::bigint,CASE WHEN octet_length(a.attachbody)<=@maxBytes THEN a.attachbody ELSE NULL END FROM \"YMES\".tblwiperfocap_attachfile a WHERE a.lotno=@lotNo AND a.erfno=@erfNo AND a.attachname=@name AND a.ocapno=@ocapNo AND a.attachbody IS NOT NULL AND EXISTS (SELECT 1 FROM \"YMES\".tblwiperfbasis b WHERE b.erfno=@erfNo AND b.lotno=@lotNo AND b.erfsource=@erfSource) LIMIT 1;"
            : "SELECT octet_length(a.attachbody)::bigint,CASE WHEN octet_length(a.attachbody)<=@maxBytes THEN a.attachbody ELSE NULL END FROM \"YMES\".tblwiperf_attachfile a WHERE a.lotno=@lotNo AND a.erfno=@erfNo AND a.attachname=@name AND a.attachbody IS NOT NULL AND EXISTS (SELECT 1 FROM \"YMES\".tblwiperfbasis b WHERE b.erfno=@erfNo AND b.lotno=@lotNo AND b.erfsource=@erfSource) LIMIT 1;";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("lotNo", NpgsqlDbType.Varchar).Value = lotNo;
        command.Parameters.Add("erfNo", NpgsqlDbType.Varchar).Value = erfNo;
        command.Parameters.Add("erfSource", NpgsqlDbType.Integer).Value = erfSource;
        command.Parameters.Add("maxBytes", NpgsqlDbType.Bigint).Value = MaxAttachmentBytes;
        command.Parameters.Add("name", NpgsqlDbType.Varchar).Value = name;
        if (source == "OCAP") command.Parameters.Add("ocapNo", NpgsqlDbType.Varchar).Value = ocapNo!;
        byte[] bytes;
        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read()) return null;
            var length = reader.GetInt64(0);
            if (length > MaxAttachmentBytes)
                throw new WipErfInformationException(StatusCodes.Status413PayloadTooLarge,
                    $"附件超過 {MaxAttachmentBytes / (1024 * 1024)} MB，無法透過此頁面下載。");
            if (reader.IsDBNull(1)) return null;
            bytes = reader.GetFieldValue<byte[]>(1);
        }
        transaction.Commit();
        return new(bytes, SafeFileName(name));
    }

    private static NpgsqlTransaction BeginReadOnly(NpgsqlConnection connection)
    {
        var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        command.ExecuteNonQuery();
        return transaction;
    }

    private static List<WipErfHoldCodeRow> ReadCreateHoldCodes(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string erfNo) => ReadRows(
        connection, transaction, """
            SELECT componentno,areano,holditemno,COALESCE(qty,0)::numeric,description
            FROM "YMES".tblwiperfholdreason
            WHERE erfno=@erfNo
            ORDER BY componentno,areano,holditemno
            """, erfNo, MaxRowsPerDetail,
        reader => new WipErfHoldCodeRow(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            null, reader.GetDecimal(3), NullableText(reader, 4), null));

    private static List<WipErfHoldCodeDispositionRow> ReadHoldCodeDispositions(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string erfNo) => ReadRows(
        connection, transaction, """
            SELECT componentno,areano,holditemno,COALESCE(qty,0)::numeric,description,holddisposition
            FROM "YMES".tblwiperfholdreasondisp
            WHERE erfno=@erfNo
            ORDER BY componentno,areano,holditemno
            """, erfNo, MaxRowsPerDetail,
        reader => new WipErfHoldCodeDispositionRow(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            null, reader.GetDecimal(3), NullableText(reader, 4), NullableText(reader, 5)));

    private static List<WipErfDispositionRow> ReadErfDispositions(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string erfNo) => ReadRows(
        connection, transaction, """
            SELECT disporder::int,groupno,dispdescription,contactorname,nextdisptype::text,
                   nextgroupno,creator,createdate
            FROM "YMES".tblwiperfdisposition
            WHERE erfno=@erfNo
            ORDER BY disporder
            """, erfNo, MaxRowsPerDetail,
        reader => new WipErfDispositionRow(reader.GetInt32(0), NullableText(reader, 1), NullableText(reader, 2),
            NullableText(reader, 3), NullableText(reader, 4), NullableText(reader, 5), NullableText(reader, 6),
            NullableDate(reader, 7), null, null, null, null, null));

    private static List<WipErfDispositionRow> ReadOcapDispositions(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string erfNo) => ReadRows(
        connection, transaction, """
            SELECT disporder::int,ocapgroupno,description,ocapnextdisptype,ocapnextgroupno,
                   dispuserno,dispdate,ocapno,holditemno,ocapnodeno
            FROM "YMES".tblwiperfocapdisposition
            WHERE erfno=@erfNo
            ORDER BY holditemno,ocapno,ocapversion,disporder
            """, erfNo, MaxRowsPerDetail,
        reader => new WipErfDispositionRow(reader.GetInt32(0), NullableText(reader, 1), NullableText(reader, 2),
            null, NullableText(reader, 3), NullableText(reader, 4), NullableText(reader, 5), NullableDate(reader, 6),
            NullableText(reader, 7), NullableText(reader, 8), NullableText(reader, 9), NullableText(reader, 5),
            NullableDate(reader, 6)));

    private static List<WipErfLotDispositionRow> ReadLotDispositions(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string erfNo) => ReadRows(
        connection, transaction, """
            SELECT d.lotdisporder::int,d.lotdisptype::int,d.description,d.creator,d.createdate,
                   n.opno,n.processno,n.processversion
            FROM "YMES".tblwiperflotdisposition d
            LEFT JOIN "YMES".tblprsnodebasis n ON n.nodeid=d.nodeid
            WHERE d.erfno=@erfNo
            ORDER BY d.lotdisporder
            """, erfNo, MaxRowsPerDetail,
        reader => new WipErfLotDispositionRow(reader.GetInt32(0), reader.GetInt32(1), NullableText(reader, 2),
            NullableText(reader, 3), NullableDate(reader, 4), NullableText(reader, 5), NullableText(reader, 6),
            NullableText(reader, 7)));

    private static List<WipErfAttachmentRow> ReadAttachments(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string erfNo, string lotNo)
    {
        var rows = ReadRows(connection, transaction, """
            SELECT attachname,attachtype,creator,createdate
            FROM "YMES".tblwiperf_attachfile
            WHERE erfno=@erfNo AND lotno=@lotNo
            ORDER BY createdate,attachname COLLATE "C"
            """, erfNo, MaxAttachments, reader => new WipErfAttachmentRow(
                "ERF", lotNo, reader.GetString(0), NullableText(reader, 1) ?? string.Empty,
                NullableText(reader, 2), NullableDate(reader, 3), null, null, null),
            extraParameters: command => command.Parameters.Add("lotNo", NpgsqlDbType.Varchar).Value = lotNo);
        rows.AddRange(ReadRows(connection, transaction, """
            SELECT attachname,attachtype::text,creator,createdate,ocapno,ocapversion,ocapnodeno
            FROM "YMES".tblwiperfocap_attachfile
            WHERE erfno=@erfNo AND lotno=@lotNo
            ORDER BY createdate,attachname COLLATE "C",ocapno COLLATE "C"
            """, erfNo, MaxAttachments - rows.Count, reader => new WipErfAttachmentRow(
                "OCAP", lotNo, reader.GetString(0), NullableText(reader, 1) ?? string.Empty,
                NullableText(reader, 2), NullableDate(reader, 3), NullableText(reader, 4),
                NullableText(reader, 5), NullableText(reader, 6)),
            extraParameters: command => command.Parameters.Add("lotNo", NpgsqlDbType.Varchar).Value = lotNo));
        return rows;
    }

    private static List<T> ReadRows<T>(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string selectSql, string erfNo, int maxRows, Func<NpgsqlDataReader, T> map,
        Action<NpgsqlCommand>? extraParameters = null)
    {
        var result = new List<T>();
        using var command = new NpgsqlCommand($"{selectSql} LIMIT @limit;", connection, transaction);
        command.Parameters.Add("erfNo", NpgsqlDbType.Varchar).Value = erfNo;
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = maxRows + 1;
        extraParameters?.Invoke(command);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (result.Count == maxRows)
                throw new WipErfInformationException(StatusCodes.Status413PayloadTooLarge,
                    $"異常單詳細資料超過單頁 {maxRows} 筆，為避免不完整畫面已停止載入。");
            result.Add(map(reader));
        }
        return result;
    }

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);

    private static string SafeFileName(string name)
    {
        var safe = new string(name.Where(c => !char.IsControl(c) && c is not '/' and not '\\').ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "attachment.bin" : safe;
    }
}
