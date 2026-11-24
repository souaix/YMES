using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/spc/qc-item-binding-history")]
public sealed class Qc216QcItemBindingHistoryController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc216QcItemBindingHistoryRepository _repository = new(dataSource);

    [HttpGet("items")]
    public IActionResult GetItems([FromQuery] Qc216QcItemOptionsQuery query)
    {
        try { return Ok(_repository.GetItems(query)); }
        catch (Qc216RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "SC216 QC Item 查詢失敗", detail: ex.Message); }
        catch (PostgresException) { return Problem(statusCode: 503, title: "SC216 QC Item 查詢失敗", detail: "QC Item 主檔尚未部署或目前資料庫角色缺少 SELECT，請確認 migration 0068 與資料庫權限。"); }
    }

    [HttpGet]
    public IActionResult GetHistory([FromQuery] Qc216BindingHistoryQuery query)
    {
        try { return Ok(_repository.GetHistory(query)); }
        catch (Qc216RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "SC216 綁定紀錄查詢失敗", detail: ex.Message); }
        catch (PostgresException) { return Problem(statusCode: 503, title: "SC216 綁定紀錄查詢失敗", detail: "TBLQCITEMBINDLOG 或 QC Item 主檔尚未部署或目前資料庫角色缺少 SELECT，請確認 migration 0068 與資料庫權限。"); }
    }
}

public sealed class Qc216QcItemBindingHistoryRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 25;
    private const int MaxParents = 100_000;
    private const int MaxChildEvents = 20_000;
    private static readonly JsonSerializerOptions IdJsonOptions = new(JsonSerializerDefaults.Web);

    public Qc216ItemOptionsResponse GetItems(Qc216QcItemOptionsQuery query)
    {
        var (table, keyColumn, nameColumn) = ItemSource(query.QcItemType);
        var search = NormalizeSearch(query.Search);
        var (page, pageSize) = Page(query.Page, query.PageSize, 100);
        using var connection = dataSource.OpenConnection();
        using var transaction = ReadSnapshot(connection);

        using var count = new NpgsqlCommand($"SELECT count(*) FROM \"YMES\".{table} WHERE issuestate=2 AND (@search IS NULL OR position(lower(@search) in lower({keyColumn}))>0 OR position(lower(@search) in lower(COALESCE({nameColumn},'')))>0);", connection, transaction);
        count.Parameters.Add("search", NpgsqlDbType.Varchar).Value = (object?)search ?? DBNull.Value;
        var total = (long)count.ExecuteScalar()!;
        var offset = checked((page - 1) * pageSize);
        using var command = new NpgsqlCommand($"SELECT {keyColumn},COALESCE({nameColumn},'') FROM \"YMES\".{table} WHERE issuestate=2 AND (@search IS NULL OR position(lower(@search) in lower({keyColumn}))>0 OR position(lower(@search) in lower(COALESCE({nameColumn},'')))>0) ORDER BY lower({keyColumn}) COLLATE \"C\",{keyColumn} COLLATE \"C\" LIMIT @limit OFFSET @offset;", connection, transaction);
        command.Parameters.Add("search", NpgsqlDbType.Varchar).Value = (object?)search ?? DBNull.Value;
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;
        var rows = new List<Qc216ItemOption>(pageSize);
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) rows.Add(new Qc216ItemOption(reader.GetString(0), reader.GetString(1)));
        }
        transaction.Commit();
        return new Qc216ItemOptionsResponse(query.QcItemType, total, page, pageSize,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize), rows);
    }

    public Qc216BindingHistoryResponse GetHistory(Qc216BindingHistoryQuery query)
    {
        Validate(query);
        var (page, pageSize) = Page(query.Page, query.PageSize, DefaultPageSize);
        using var connection = dataSource.OpenConnection();
        using var transaction = ReadSnapshot(connection);

        using var count = new NpgsqlCommand($"SELECT count(*) FROM (SELECT 1 FROM \"YMES\".tblqcitembindlog b WHERE {Filter} GROUP BY b.qcitemtype,b.qcitemno,b.bindtype,b.productno,b.productversion,b.opno,b.mtltype,b.mtlno) parents;", connection, transaction);
        AddFilters(count, query);
        var total = (long)count.ExecuteScalar()!;
        if (total > MaxParents)
            throw new Qc216RuleException(413, $"符合條件的綁定對象共 {total:N0} 組，超過 {MaxParents:N0} 組上限；請縮小日期或增加條件。");

        var offset = checked((page - 1) * pageSize);
        using var parentCommand = new NpgsqlCommand($"""
            SELECT b.qcitemtype::int,b.qcitemno,b.bindtype::int,b.productno,b.productversion,b.opno,b.mtltype,b.mtlno,
                   COALESCE(CASE b.qcitemtype
                     WHEN 0 THEN (SELECT q.qcitemname FROM "YMES".tblqcitemforattributes q WHERE q.qcitemno=b.qcitemno LIMIT 1)
                     WHEN 1 THEN (SELECT q.qcitemname FROM "YMES".tblqcitemforvariables q WHERE q.qcitemno=b.qcitemno LIMIT 1)
                     WHEN 2 THEN (SELECT q.groupname FROM "YMES".tblqcitemgroupbasis q WHERE q.groupno=b.qcitemno LIMIT 1)
                     WHEN 3 THEN (SELECT q.groupname FROM "YMES".tblqcitemattgrouporderbasis q WHERE q.groupno=b.qcitemno LIMIT 1)
                     WHEN 4 THEN (SELECT q.groupname FROM "YMES".tblqcitemgroupbasis_pmi q WHERE q.groupno=b.qcitemno LIMIT 1)
                   END,'')
            FROM "YMES".tblqcitembindlog b
            WHERE {Filter}
            GROUP BY b.qcitemtype,b.qcitemno,b.bindtype,b.productno,b.productversion,b.opno,b.mtltype,b.mtlno
            ORDER BY b.qcitemtype,b.qcitemno COLLATE "C",b.bindtype,
                     b.productno COLLATE "C" NULLS FIRST,b.productversion COLLATE "C" NULLS FIRST,
                     b.opno COLLATE "C" NULLS FIRST,b.mtltype COLLATE "C" NULLS FIRST,b.mtlno COLLATE "C" NULLS FIRST
            LIMIT @limit OFFSET @offset;
            """, connection, transaction);
        AddFilters(parentCommand, query);
        parentCommand.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        parentCommand.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

        var parents = new List<Qc216BindingParent>(pageSize);
        var parentMap = new Dictionary<Qc216BindingKey, Qc216BindingParent>();
        using (var reader = parentCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                var key = new Qc216BindingKey(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2),
                    NullableText(reader, 3), NullableText(reader, 4), NullableText(reader, 5), NullableText(reader, 6), NullableText(reader, 7));
                var parent = new Qc216BindingParent(StableId(key), key.QcItemType, key.QcItemNo, reader.GetString(8), key.BindType,
                    key.ProductNo ?? "", key.ProductVersion ?? "", key.OpNo ?? "", key.MaterialType ?? "", key.MaterialNo ?? "", []);
                parents.Add(parent);
                parentMap.Add(key, parent);
            }
        }

        if (parents.Count == 0)
        {
            transaction.Commit();
            return new Qc216BindingHistoryResponse(query.BindType, total, page, pageSize,
                total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize), []);
        }

        var keyPayload = JsonSerializer.Serialize(parentMap.Keys.Select(key => new
        {
            qcitemtype = key.QcItemType,
            qcitemno = key.QcItemNo,
            bindtype = key.BindType,
            productno = key.ProductNo,
            productversion = key.ProductVersion,
            opno = key.OpNo,
            mtltype = key.MaterialType,
            mtlno = key.MaterialNo
        }));
        using var eventCommand = new NpgsqlCommand($"""
            WITH selected AS (
                SELECT * FROM jsonb_to_recordset(@parentKeys::jsonb) AS p(
                    qcitemtype integer,qcitemno varchar(25),bindtype integer,productno varchar(50),
                    productversion varchar(5),opno varchar(20),mtltype varchar(50),mtlno varchar(50))
            )
            SELECT b.qcitemtype::int,b.qcitemno,b.bindtype::int,b.productno,b.productversion,b.opno,b.mtltype,b.mtlno,
                   b.qcitemlogserial,b.executestatus::int,b.executedate, b.executor,
                   b.inline::int,b.executeorder::int,b.insptype_fai::int,b.insptype_ipqc::int,b.insptype_pqc::int
            FROM "YMES".tblqcitembindlog b
            JOIN selected p ON b.qcitemtype=p.qcitemtype AND b.qcitemno=p.qcitemno AND b.bindtype=p.bindtype
                AND b.productno IS NOT DISTINCT FROM p.productno
                AND b.productversion IS NOT DISTINCT FROM p.productversion
                AND b.opno IS NOT DISTINCT FROM p.opno
                AND b.mtltype IS NOT DISTINCT FROM p.mtltype
                AND b.mtlno IS NOT DISTINCT FROM p.mtlno
            WHERE {Filter}
            ORDER BY b.qcitemtype,b.qcitemno COLLATE "C",b.bindtype,
                     b.productno COLLATE "C" NULLS FIRST,b.productversion COLLATE "C" NULLS FIRST,
                     b.opno COLLATE "C" NULLS FIRST,b.mtltype COLLATE "C" NULLS FIRST,b.mtlno COLLATE "C" NULLS FIRST,
                     b.executedate,b.qcitemlogserial COLLATE "C"
            LIMIT @maxRows;
            """, connection, transaction);
        eventCommand.Parameters.Add("parentKeys", NpgsqlDbType.Jsonb).Value = keyPayload;
        AddFilters(eventCommand, query);
        eventCommand.Parameters.Add("maxRows", NpgsqlDbType.Integer).Value = MaxChildEvents + 1;
        var eventCount = 0;
        using (var reader = eventCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                if (eventCount++ >= MaxChildEvents)
                    throw new Qc216RuleException(413, $"本頁綁定歷程超過 {MaxChildEvents:N0} 筆；請縮小日期或減少每頁綁定對象數。");
                var key = new Qc216BindingKey(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2),
                    NullableText(reader, 3), NullableText(reader, 4), NullableText(reader, 5), NullableText(reader, 6), NullableText(reader, 7));
                if (!parentMap.TryGetValue(key, out var parent)) continue;
                parent.Events.Add(new Qc216BindingEvent(reader.GetString(8), reader.GetInt32(9),
                    DateText(reader.GetDateTime(10)), reader.GetString(11), NullableInt(reader, 12), NullableInt(reader, 13),
                    NullableInt(reader, 14), NullableInt(reader, 15), NullableInt(reader, 16)));
            }
        }

        transaction.Commit();
        return new Qc216BindingHistoryResponse(query.BindType, total, page, pageSize,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize), parents);
    }

    private const string Filter = "b.bindtype=@bindType AND b.executedate>=@fromDate AND b.executedate<=@endDate AND (@qcItemType IS NULL OR b.qcitemtype=@qcItemType) AND (@qcItemNo IS NULL OR b.qcitemno=@qcItemNo) AND (@executeStatus IS NULL OR b.executestatus=@executeStatus)";

    private static (string Table, string KeyColumn, string NameColumn) ItemSource(int type) => type switch
    {
        0 => ("tblqcitemforattributes", "qcitemno", "qcitemname"),
        1 => ("tblqcitemforvariables", "qcitemno", "qcitemname"),
        2 => ("tblqcitemgroupbasis", "groupno", "groupname"),
        3 => ("tblqcitemattgrouporderbasis", "groupno", "groupname"),
        4 => ("tblqcitemgroupbasis_pmi", "groupno", "groupname"),
        _ => throw new Qc216RuleException(400, "QC Item Type 僅接受 0 至 4。")
    };

    private static NpgsqlTransaction ReadSnapshot(NpgsqlConnection connection)
    {
        var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        command.ExecuteNonQuery();
        using var timeout = new NpgsqlCommand("SET LOCAL statement_timeout = '30s';", connection, transaction);
        timeout.ExecuteNonQuery();
        return transaction;
    }

    private static void Validate(Qc216BindingHistoryQuery query)
    {
        if (query.BindType is < 1 or > 4) throw new Qc216RuleException(400, "BindType 僅接受 1 至 4。請重新從綁定類型頁籤查詢。");
        if (query.QcItemType is < 0 or > 4) throw new Qc216RuleException(400, "QC Item Type 僅接受 0 至 4。");
        if (query.ExecuteStatus is < 0 or > 2) throw new Qc216RuleException(400, "ExecuteStatus 僅接受 0、1 或 2。");
        ValidateCode(query.QcItemNo, "QC Item", 25);
        if (query.FromDate is null || query.EndDate is null)
            throw new Qc216RuleException(400, "From Date 與 To Date 為必填條件，並包含起訖時間。");
        if (query.FromDate >= query.EndDate) throw new Qc216RuleException(400, "From Date 必須早於或等於 To Date。");
        if (query.EndDate.Value - query.FromDate.Value > TimeSpan.FromDays(366))
            throw new Qc216RuleException(400, "日期區間不可超過 366 天。");
    }

    private static void ValidateCode(string? value, string field, int maxLength)
    {
        if (value is { } code && (code.Trim().Length > maxLength || code.Any(char.IsControl)))
            throw new Qc216RuleException(400, $"{field} 條件不可超過 {maxLength} 個字元或包含控制字元。");
    }

    private static string? NormalizeSearch(string? search)
    {
        if (search is null) return null;
        var value = search.Trim();
        if (value.Length > 100 || value.Any(char.IsControl)) throw new Qc216RuleException(400, "搜尋文字不可超過 100 個字元或包含控制字元。");
        return value.Length == 0 ? null : value;
    }

    private static (int Page, int PageSize) Page(int? requestedPage, int? requestedSize, int defaultSize)
    {
        var page = requestedPage ?? 1;
        var size = requestedSize ?? defaultSize;
        if (page is < 1 or > 100_000 || size is < 1 or > MaxPageSize)
            throw new Qc216RuleException(400, $"頁碼需介於 1 至 100000，單頁筆數需介於 1 至 {MaxPageSize}。");
        return (page, size);
    }

    private static void AddFilters(NpgsqlCommand command, Qc216BindingHistoryQuery query)
    {
        command.Parameters.Add("bindType", NpgsqlDbType.Integer).Value = query.BindType;
        command.Parameters.Add("fromDate", NpgsqlDbType.Timestamp).Value = DateTime.SpecifyKind(query.FromDate!.Value, DateTimeKind.Unspecified);
        command.Parameters.Add("endDate", NpgsqlDbType.Timestamp).Value = DateTime.SpecifyKind(query.EndDate!.Value, DateTimeKind.Unspecified);
        command.Parameters.Add("qcItemType", NpgsqlDbType.Integer).Value = (object?)query.QcItemType ?? DBNull.Value;
        command.Parameters.Add("qcItemNo", NpgsqlDbType.Varchar, 25).Value = (object?)(string.IsNullOrWhiteSpace(query.QcItemNo) ? null : query.QcItemNo.Trim()) ?? DBNull.Value;
        command.Parameters.Add("executeStatus", NpgsqlDbType.Integer).Value = (object?)query.ExecuteStatus ?? DBNull.Value;
    }

    private static string StableId(Qc216BindingKey key)
    {
        var json = JsonSerializer.Serialize(key, IdJsonOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string DateText(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified)
        .ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static int? NullableInt(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
}

public sealed class Qc216QcItemOptionsQuery
{
    public int QcItemType { get; set; }
    public string? Search { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class Qc216BindingHistoryQuery
{
    public int BindType { get; set; }
    public int? QcItemType { get; set; }
    public string? QcItemNo { get; set; }
    public int? ExecuteStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Qc216ItemOption(string No, string Name);
public sealed record Qc216ItemOptionsResponse(int QcItemType, long TotalCount, int Page, int PageSize, int PageCount, IReadOnlyList<Qc216ItemOption> Items);
public sealed record Qc216BindingHistoryResponse(int BindType, long TotalCount, int Page, int PageSize, int PageCount, IReadOnlyList<Qc216BindingParent> Parents);
public sealed record Qc216BindingParent(string Id, int QcItemType, string QcItemNo, string QcItemName, int BindType,
    string ProductNo, string ProductVersion, string OpNo, string MaterialType, string MaterialNo, List<Qc216BindingEvent> Events);
public sealed record Qc216BindingEvent(string Id, int ExecuteStatus, string ExecuteDate, string Executor, int? InLine,
    int? ExecuteOrder, int? InspTypeFai, int? InspTypeIpqc, int? InspTypePqc);
public sealed record Qc216BindingKey(int QcItemType, string QcItemNo, int BindType, string? ProductNo,
    string? ProductVersion, string? OpNo, string? MaterialType, string? MaterialNo);

public sealed class Qc216RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
