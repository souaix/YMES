using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/spc/chart-line-edit-log")]
public sealed class Qc215ChartLineEditLogController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc215ChartLineEditLogRepository _repository = new(dataSource);

    [HttpGet]
    public IActionResult Get([FromQuery] Qc215ChartLineEditLogQuery query)
    {
        try
        {
            return Ok(_repository.Search(query));
        }
        catch (Qc302RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "SC215 管制線修改紀錄查詢失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "SC215 資料操作失敗", detail: "YMES 管制圖主檔或歷程表尚未部署，請確認 migration 0024 與資料庫權限。");
        }
    }
}

public sealed class Qc215ChartLineEditLogRepository(NpgsqlDataSource dataSource)
{
    private const int MaxFactorSerials = 250;
    private const int MaxTimelineRows = 30000;
    private const int MaxResultRows = 20000;
    private const int MaxPageSize = 100;

    public Qc215ChartLineEditLogResponse Search(Qc215ChartLineEditLogQuery query)
    {
        Validate(query);
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        var filters = BuildFilter(query);

        using var connection = dataSource.OpenConnection();
        var chartCount = CountCharts(connection, filters, query);
        if (chartCount > MaxFactorSerials)
            throw new Qc302RuleException(StatusCodes.Status413PayloadTooLarge,
                $"查詢條件符合 {chartCount:N0} 組管制圖；請增加 QC Item、產品或其他條件，單次上限為 {MaxFactorSerials} 組。");
        if (chartCount == 0)
            return new Qc215ChartLineEditLogResponse(0, page, pageSize, 0, [], []);

        var charts = ReadCharts(connection, filters, query);
        var serials = charts.Keys.ToArray();
        var factorFields = ReadAllowedFactors(connection, charts);
        var snapshots = ReadTimeline(connection, serials, charts, factorFields);
        var history = Reconstruct(snapshots);

        // Keep the legacy strict boundaries, and apply them only after the complete history is reconstructed.
        if (query.FromDate is { } fromDate)
        {
            var boundary = fromDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            history = history.Where(row => StringComparer.Ordinal.Compare(row.RecordDate, boundary) > 0).ToList();
        }
        if (query.EndDate is { } endDate)
        {
            var boundary = endDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            history = history.Where(row => StringComparer.Ordinal.Compare(row.RecordDate, boundary) < 0).ToList();
        }
        if (history.Count > MaxResultRows)
            throw new Qc302RuleException(StatusCodes.Status413PayloadTooLarge,
                $"符合條件的歷程超過 {MaxResultRows:N0} 筆；請縮小查詢日期或其他條件。");

        history.Sort(HistoryComparer.Instance);
        var total = history.Count;
        var offset = checked((page - 1) * pageSize);
        var rows = history.Skip(offset).Take(pageSize).ToArray();
        return new Qc215ChartLineEditLogResponse(total, page, pageSize,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize), factorFields, rows);
    }

    private static void Validate(Qc215ChartLineEditLogQuery query)
    {
        ValidateCode(query.QcItemNo, "QC Item", 25);
        ValidateCode(query.ProductNo, "Product", 61);
        ValidateCode(query.CustomerNo, "Customer", 61);
        ValidateCode(query.MaterialNo, "Material", 61);
        ValidateCode(query.VendorNo, "Vendor", 61);
        var page = query.Page ?? 1;
        var size = query.PageSize ?? 50;
        if (page is < 1 or > 100000 || size is < 1 or > MaxPageSize)
            throw new Qc302RuleException(400, $"頁碼需介於 1 至 100000，單頁筆數需介於 1 至 {MaxPageSize}。");
        if (query.FromDate is { } from && query.EndDate is { } end)
        {
            if (from >= end) throw new Qc302RuleException(400, "From Date 必須早於 To Date。");
            if (end - from > TimeSpan.FromDays(366))
                throw new Qc302RuleException(400, "日期區間不可超過 366 天。");
        }
    }

    private static void ValidateCode(string? value, string field, int maxLength)
    {
        if (value is null) return;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength || trimmed.Any(char.IsControl))
            throw new Qc302RuleException(400, $"{field} 條件不可超過 {maxLength} 個字元或包含控制字元。");
    }

    private static string BuildFilter(Qc215ChartLineEditLogQuery query) => """
        (@qcItemNo IS NULL OR lower(b.qcitemno)=lower(@qcItemNo))
        AND (@productNo IS NULL OR lower(COALESCE(b.product,''))=lower(@productNo))
        AND (@customerNo IS NULL OR lower(COALESCE(b.customer,''))=lower(@customerNo))
        AND (@materialNo IS NULL OR lower(COALESCE(b.material,''))=lower(@materialNo))
        AND (@vendorNo IS NULL OR lower(COALESCE(b.mtlvendor,''))=lower(@vendorNo))
        """;

    private static void AddFilters(NpgsqlCommand command, Qc215ChartLineEditLogQuery query)
    {
        AddVarchar(command, "qcItemNo", query.QcItemNo, 25);
        AddVarchar(command, "productNo", query.ProductNo, 61);
        AddVarchar(command, "customerNo", query.CustomerNo, 61);
        AddVarchar(command, "materialNo", query.MaterialNo, 61);
        AddVarchar(command, "vendorNo", query.VendorNo, 61);
    }

    private static long CountCharts(NpgsqlConnection connection, string filters, Qc215ChartLineEditLogQuery query)
    {
        using var command = new NpgsqlCommand($"SELECT count(*) FROM \"YMES\".tblspcqcchartbasis b WHERE {filters};", connection);
        AddFilters(command, query);
        return (long)command.ExecuteScalar()!;
    }

    private Dictionary<string, ChartBasis> ReadCharts(NpgsqlConnection connection, string filters, Qc215ChartLineEditLogQuery query)
    {
        // Query conditions remain parameters; table/column names are fixed in source.
        using var command = new NpgsqlCommand($"""
            SELECT b.factorserial,b.qcitemno,COALESCE(q.qcitemname,''),
                   COALESCE(b.product,''),COALESCE((SELECT p.productname FROM "YMES".tblprdproductbasis p
                     WHERE lower(p.productno)=lower(NULLIF(b.product,'*')) ORDER BY p.productversion DESC LIMIT 1),''),
                   COALESCE(b.customer,''),COALESCE((SELECT c.customername FROM "YMES".tblentcustomerbasis c
                     WHERE lower(c.customerno)=lower(NULLIF(b.customer,'*')) LIMIT 1),''),
                   COALESCE(b.material,''),COALESCE((SELECT m.materialname FROM "YMES".tblmtlmaterialbasis m
                     WHERE lower(m.materialno)=lower(NULLIF(b.material,'*')) LIMIT 1),''),
                   COALESCE(b.mtlvendor,''),COALESCE((SELECT v.vendorname FROM "YMES".tbleqpvendor v
                     WHERE lower(v.vendorno)=lower(NULLIF(b.mtlvendor,'*')) LIMIT 1),''),
                   b.factorvalues::text
            FROM "YMES".tblspcqcchartbasis b
            LEFT JOIN "YMES".tblqcitemforvariables q ON lower(q.qcitemno)=lower(b.qcitemno)
            WHERE {filters}
            ORDER BY b.factorserial;
            """, connection);
        AddFilters(command, query);
        using var reader = command.ExecuteReader();
        var charts = new Dictionary<string, ChartBasis>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var chart = new ChartBasis(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.GetString(7), reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetString(11));
            charts.Add(chart.FactorSerial, chart);
        }
        return charts;
    }

    private static IReadOnlyList<string> ReadAllowedFactors(NpgsqlConnection connection, IReadOnlyDictionary<string, ChartBasis> charts)
    {
        var serials = charts.Keys.ToArray();
        using var command = new NpgsqlCommand("""
            SELECT DISTINCT f.factorno
            FROM "YMES".tblqcitemfactor f
            JOIN "YMES".tblspcqcchartbasis b ON lower(b.qcitemno)=lower(f.qcitemno)
            WHERE b.factorserial = ANY(@serials)
              AND upper(f.factoronline)='Y'
              AND upper(f.factorno)<>'MEMO'
            ORDER BY f.factorno;
            """, connection);
        command.Parameters.Add("serials", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = serials;
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read()) result.Add(reader.GetString(0));
        return result;
    }

    private static List<ChartSnapshot> ReadTimeline(NpgsqlConnection connection, string[] serials,
        IReadOnlyDictionary<string, ChartBasis> charts, IReadOnlyList<string> allowedFields)
    {
        using var command = new NpgsqlCommand("""
            SELECT factorserial,spcid::int,COALESCE(creator,''),starttime,us,cs,ls,ucl,cl,lcl,COALESCE(memo,'')
            FROM "YMES".tblspcqcchartdetaillog
            WHERE factorserial = ANY(@serials)
            ORDER BY factorserial,spcid,starttime
            LIMIT @maxRows;
            """, connection);
        command.Parameters.Add("serials", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = serials;
        command.Parameters.Add("maxRows", NpgsqlDbType.Integer).Value = MaxTimelineRows + 1;
        using var reader = command.ExecuteReader();
        var result = new List<ChartSnapshot>();
        while (reader.Read())
        {
            if (result.Count >= MaxTimelineRows)
                throw new Qc302RuleException(StatusCodes.Status413PayloadTooLarge,
                    $"完整管制線歷程超過 {MaxTimelineRows:N0} 筆；請增加查詢條件，避免截斷比較前一筆設定。");
            var serial = reader.GetString(0);
            var chart = charts[serial];
            var date = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Unspecified);
            result.Add(new ChartSnapshot(serial, reader.GetInt32(1), reader.GetString(2), date, chart,
                ReadFactorValues(chart.FactorValuesJson, allowedFields), NullableText(reader, 4), NullableText(reader, 5),
                NullableText(reader, 6), NullableText(reader, 7), NullableText(reader, 8), NullableText(reader, 9), reader.GetString(10)));
        }
        return result;
    }

    private static Dictionary<string, string> ReadFactorValues(string json, IReadOnlyList<string> allowedFields)
    {
        var allowed = allowedFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return values;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!allowed.Contains(property.Name) || property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) continue;
            values[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? ""
                : property.Value.GetRawText();
        }
        return values;
    }

    private static List<Qc215HistoryRow> Reconstruct(IReadOnlyList<ChartSnapshot> snapshots)
    {
        var result = new List<Qc215HistoryRow>();
        var previous = new Dictionary<(string FactorSerial, int SpcId), ChartSnapshot>();
        foreach (var current in snapshots)
        {
            if (current.Limits.All(value => value is null)) continue;
            var key = (current.FactorSerial, current.SpcId);
            if (!previous.TryGetValue(key, out var prior))
            {
                result.Add(ToRow(current, "新增", current.AllValues()));
                previous[key] = current;
                continue;
            }

            var spec = Changed(current, prior, ["US", "CS", "LS"]);
            if (spec.Count > 0) result.Add(ToRow(current, "規格修改", spec));
            var lines = Changed(current, prior, ["UCL", "CL", "LCL"]);
            if (lines.Count > 0) result.Add(ToRow(current, "管制線修改", lines));
            previous[key] = current;
        }
        return result;
    }

    private static Dictionary<string, string> Changed(ChartSnapshot current, ChartSnapshot prior, string[] fields)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            var before = prior.Value(field) ?? "";
            var after = current.Value(field) ?? "";
            if (!string.Equals(before, after, StringComparison.Ordinal)) values[field] = after.Length == 0 ? "（清除）" : after;
        }
        if (values.Count > 0) values["MEMO"] = current.Memo;
        return values;
    }

    private static Qc215HistoryRow ToRow(ChartSnapshot snapshot, string type, Dictionary<string, string> values)
    {
        var chart = snapshot.Chart;
        return new Qc215HistoryRow($"{snapshot.FactorSerial}-{snapshot.SpcId}-{snapshot.StartTime:yyyyMMddHHmmss.fffffff}-{type}",
            snapshot.FactorSerial, snapshot.SpcId, type, snapshot.Creator,
            snapshot.StartTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            chart.ProductName, chart.CustomerName, chart.MaterialName, chart.VendorName,
            chart.ProductNo, chart.CustomerNo, chart.MaterialNo, chart.VendorNo,
            snapshot.FactorValues, chart.QcItemNo, chart.QcItemName, ChartName(snapshot.SpcId), values);
    }

    private static string ChartName(int spcId) => spcId switch
    {
        0 => "Trend Chart", 1 => "Xbar Chart", 2 => "R Chart", 3 => "S Chart", 4 => "Rm Chart",
        _ => $"SPCID {spcId}"
    };

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static void AddVarchar(NpgsqlCommand command, string name, string? value, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is { Length: > 0 } && normalized.Length > maxLength)
            throw new Qc302RuleException(400, $"{name} 超過欄位長度。");
        command.Parameters.Add(name, NpgsqlDbType.Varchar, maxLength).Value = (object?)normalized ?? DBNull.Value;
    }

    private sealed record ChartBasis(string FactorSerial, string QcItemNo, string QcItemName,
        string ProductNo, string ProductName, string CustomerNo, string CustomerName,
        string MaterialNo, string MaterialName, string VendorNo, string VendorName, string FactorValuesJson);

    private sealed record ChartSnapshot(string FactorSerial, int SpcId, string Creator, DateTime StartTime,
        ChartBasis Chart, Dictionary<string, string> FactorValues,
        string? Us, string? Cs, string? Ls, string? Ucl, string? Cl, string? Lcl, string Memo)
    {
        public string?[] Limits => [Us, Cs, Ls, Ucl, Cl, Lcl];
        public string? Value(string field) => field switch
        {
            "US" => Us, "CS" => Cs, "LS" => Ls, "UCL" => Ucl, "CL" => Cl, "LCL" => Lcl, _ => null
        };
        public Dictionary<string, string> AllValues() => new(StringComparer.Ordinal)
        {
            ["US"] = Us ?? "", ["CS"] = Cs ?? "", ["LS"] = Ls ?? "",
            ["UCL"] = Ucl ?? "", ["CL"] = Cl ?? "", ["LCL"] = Lcl ?? "", ["MEMO"] = Memo
        };
    }

    private sealed class HistoryComparer : IComparer<Qc215HistoryRow>
    {
        public static HistoryComparer Instance { get; } = new();
        public int Compare(Qc215HistoryRow? x, Qc215HistoryRow? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            var value = StringComparer.Ordinal.Compare(x.FactorSerial, y.FactorSerial);
            if (value != 0) return value;
            value = StringComparer.Ordinal.Compare(x.RecordDate, y.RecordDate);
            if (value != 0) return value;
            value = x.SpcId.CompareTo(y.SpcId);
            if (value != 0) return value;
            value = (x.ChangeType == "規格修改" ? 0 : x.ChangeType == "管制線修改" ? 1 : 2)
                .CompareTo(y.ChangeType == "規格修改" ? 0 : y.ChangeType == "管制線修改" ? 1 : 2);
            return value != 0 ? value : StringComparer.Ordinal.Compare(x.Id, y.Id);
        }
    }
}

public sealed class Qc215ChartLineEditLogQuery
{
    public string? QcItemNo { get; set; }
    public string? ProductNo { get; set; }
    public string? CustomerNo { get; set; }
    public string? MaterialNo { get; set; }
    public string? VendorNo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Qc215ChartLineEditLogResponse(int TotalCount, int Page, int PageSize, int PageCount,
    IReadOnlyList<string> FactorFields, IReadOnlyList<Qc215HistoryRow> Rows);

public sealed record Qc215HistoryRow(string Id, string FactorSerial, int SpcId, string ChangeType, string UserId, string RecordDate,
    string ProductName, string CustomerName, string MaterialName, string VendorName,
    string ProductNo, string CustomerNo, string MaterialNo, string VendorNo,
    IReadOnlyDictionary<string, string> FactorValues, string QcItemNo, string QcItemName, string ControlChart,
    IReadOnlyDictionary<string, string> Values);
