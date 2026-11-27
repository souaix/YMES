using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp804QueryRepository(NpgsqlDataSource dataSource)
{
    private const string Basis = "\"YMES\".tblwip8dbasis_factory";
    private const string Filter = """
        (@qrsClass IS NULL OR b.qrsclass::int=ANY(@qrsClass))
        AND (@qrsNo IS NULL OR position(upper(@qrsNo) in upper(b.qrsno))>0)
        AND (@qrsSummary IS NULL OR position(upper(@qrsSummary) in upper(b.qrssummary))>0)
        AND (@factoryNo IS NULL OR upper(b.factoryno)=upper(@factoryNo))
        AND (@projectDepartment IS NULL OR upper(b.projectdepartment)=upper(@projectDepartment))
        AND (@projectManager IS NULL OR upper(b.projectmanager)=upper(@projectManager))
        AND (@createFrom IS NULL OR b.createdate>=@createFrom)
        AND (@createTo IS NULL OR b.createdate<@createTo)
        AND (@planFrom IS NULL OR b.planclosedate>=@planFrom)
        AND (@planTo IS NULL OR b.planclosedate<@planTo)
        AND (@closeFrom IS NULL OR b.closedate>=@closeFrom)
        AND (@closeTo IS NULL OR b.closedate<@closeTo)
        AND (@closeState IS NULL OR @closeState='all'
             OR (@closeState='closed' AND b.closedate IS NOT NULL)
             OR (@closeState='open' AND b.closedate IS NULL))
        AND (@erfNo IS NULL OR upper(b.erfno)=upper(@erfNo))
        """;

    private const string Columns = """
        b.qrsno,b.qrsclass::int,b.qrssummary,b.lotno,b.lotqty,b.sampleqty,b.errorqty,
        b.factoryno,b.areano,b.mono,b.customerno,b.productno,b.productversion,b.rono,b.opno,
        b.equipmentno,b.erfno,b.holddescription,b.description,b.projectdepartment,b.projectmanager,
        b.projectmanagername,b.curstage,b.planclosedate,b.lastupdatedate,b.summarydescription,
        b.disphour,b.dispcost,b.summarydepartment,b.summaryuserno,b.summaryusername,b.closetype,
        b.closedepartment,b.closeuserno,b.closeusername,b.closedate,b.issuestate::int,b.creator,b.createdate
        """;

    public Wp804QueryResult List(Wp804Query query)
    {
        var filter = Normalize(query);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            readOnly.ExecuteNonQuery();

        using var countCommand = new NpgsqlCommand($"SELECT count(*) FROM {Basis} b WHERE {Filter};", connection, transaction);
        Bind(countCommand, filter);
        var totalCount = (long)countCommand.ExecuteScalar()!;
        var pageCount = totalCount == 0 ? 0 : (long)Math.Ceiling(totalCount / (double)filter.PageSize);
        var offset = checked((filter.Page - 1) * filter.PageSize);

        using var command = new NpgsqlCommand($"""
            SELECT {Columns}
            FROM {Basis} b
            WHERE {Filter}
            ORDER BY b.qrsno COLLATE "C"
            LIMIT @pageSize OFFSET @offset;
            """, connection, transaction);
        Bind(command, filter);
        command.Parameters.Add("pageSize", NpgsqlDbType.Integer).Value = filter.PageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<Wp804QueryRow>(filter.PageSize);
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(ReadRow(reader));

        transaction.Commit();
        return new(totalCount, filter.Page, filter.PageSize, pageCount, rows);
    }

    private static NormalizedQuery Normalize(Wp804Query query)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        if (page is < 1 or > 100_000) throw Bad("頁碼必須介於 1 到 100,000。");
        if (pageSize is < 1 or > 100) throw Bad("每頁筆數必須介於 1 到 100。");

        var classes = query.QrsClass?.Distinct().ToArray();
        if (classes is { Length: > 4 } || classes?.Any(value => value is < 1 or > 4) == true)
            throw Bad("QRSClass 僅接受 1、2、3、4。");
        if (classes?.Length == 0) classes = null;

        var qrsNo = Text(query.QrsNo, 30, "QRSNo");
        var qrsSummary = Text(query.QrsSummary, 500, "QRSSummary");
        var factoryNo = Text(query.FactoryNo, 20, "FactoryNo");
        var projectDepartment = Text(query.ProjectDepartment, 20, "ProjectDepartment");
        var projectManager = Text(query.ProjectManager, 10, "ProjectManager");
        var erfNo = Text(query.ErfNo, 25, "ERFNo");

        var create = Range(query.CreateFrom, query.CreateTo, "CreateDate");
        var plan = Range(query.PlanFrom, query.PlanTo, "PlanCloseDate");
        var close = Range(query.CloseFrom, query.CloseTo, "CloseDate");
        var closeState = query.CloseState?.Trim().ToLowerInvariant();
        if (closeState is not (null or "" or "all" or "closed" or "open"))
            throw Bad("CloseState 僅接受 closed、open 或 all。");
        if (closeState is "" or "all") closeState = null;

        if (classes is null && qrsNo is null && qrsSummary is null && factoryNo is null &&
            projectDepartment is null && projectManager is null && create is null && plan is null &&
            close is null && closeState is null && erfNo is null)
            throw Bad("請至少提供一個有效查詢條件。");

        return new(classes, qrsNo, qrsSummary, factoryNo, projectDepartment, projectManager,
            create?.From, create?.ToExclusive, plan?.From, plan?.ToExclusive,
            close?.From, close?.ToExclusive, closeState, erfNo, page, pageSize);
    }

    private static DateRange? Range(DateOnly? from, DateOnly? to, string name)
    {
        if (from.HasValue != to.HasValue) throw Bad($"{name} 必須同時提供起始與結束日期。");
        if (!from.HasValue) return null;
        if (to!.Value < from.Value) throw Bad($"{name} 起始日期不可晚於結束日期。");
        if (to.Value.DayNumber - from.Value.DayNumber > 365)
            throw Bad($"{name} 查詢範圍最多 366 天。");
        if (to.Value == DateOnly.MaxValue) throw Bad($"{name} 結束日期超出可查詢範圍。");
        return new(from.Value.ToDateTime(TimeOnly.MinValue), to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
    }

    private static string? Text(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad($"{name} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad($"{name} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static void Bind(NpgsqlCommand command, NormalizedQuery filter)
    {
        command.Parameters.Add("qrsClass", NpgsqlDbType.Array | NpgsqlDbType.Integer).Value =
            (object?)filter.QrsClass ?? DBNull.Value;
        AddText(command, "qrsNo", filter.QrsNo);
        AddText(command, "qrsSummary", filter.QrsSummary);
        AddText(command, "factoryNo", filter.FactoryNo);
        AddText(command, "projectDepartment", filter.ProjectDepartment);
        AddText(command, "projectManager", filter.ProjectManager);
        AddTimestamp(command, "createFrom", filter.CreateFrom);
        AddTimestamp(command, "createTo", filter.CreateTo);
        AddTimestamp(command, "planFrom", filter.PlanFrom);
        AddTimestamp(command, "planTo", filter.PlanTo);
        AddTimestamp(command, "closeFrom", filter.CloseFrom);
        AddTimestamp(command, "closeTo", filter.CloseTo);
        AddText(command, "closeState", filter.CloseState);
        AddText(command, "erfNo", filter.ErfNo);
    }

    private static Wp804QueryRow ReadRow(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetInt32(1), reader.GetString(2), Optional(reader, 3), Decimal(reader, 4),
        Decimal(reader, 5), Decimal(reader, 6), reader.GetString(7), reader.GetString(8), Optional(reader, 9),
        Optional(reader, 10), Optional(reader, 11), Optional(reader, 12), Optional(reader, 13), Optional(reader, 14),
        Optional(reader, 15), Optional(reader, 16), Optional(reader, 17), Optional(reader, 18), Optional(reader, 19),
        Optional(reader, 20), Optional(reader, 21), reader.GetString(22), Time(reader, 23), Time(reader, 24),
        Optional(reader, 25), Decimal(reader, 26), Decimal(reader, 27), Optional(reader, 28), Optional(reader, 29),
        Optional(reader, 30), Decimal(reader, 31), Optional(reader, 32), Optional(reader, 33), Optional(reader, 34),
        Time(reader, 35), reader.GetInt32(36), Optional(reader, 37), Time(reader, 38));

    private static string? Optional(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static decimal? Decimal(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static DateTime? Time(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal)
        ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Unspecified);
    private static void AddText(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void AddTimestamp(NpgsqlCommand command, string name, DateTime? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;
    private static Wp804QueryRuleException Bad(string message) => new(400, message);

    private sealed record DateRange(DateTime From, DateTime ToExclusive);
    private sealed record NormalizedQuery(int[]? QrsClass, string? QrsNo, string? QrsSummary, string? FactoryNo,
        string? ProjectDepartment, string? ProjectManager, DateTime? CreateFrom, DateTime? CreateTo,
        DateTime? PlanFrom, DateTime? PlanTo, DateTime? CloseFrom, DateTime? CloseTo, string? CloseState,
        string? ErfNo, int Page, int PageSize);
}

public sealed class Wp804QueryRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class Wp804Query
{
    public int[]? QrsClass { get; set; }
    public string? QrsNo { get; set; }
    public string? QrsSummary { get; set; }
    public string? FactoryNo { get; set; }
    public string? ProjectDepartment { get; set; }
    public string? ProjectManager { get; set; }
    public DateOnly? CreateFrom { get; set; }
    public DateOnly? CreateTo { get; set; }
    public DateOnly? PlanFrom { get; set; }
    public DateOnly? PlanTo { get; set; }
    public DateOnly? CloseFrom { get; set; }
    public DateOnly? CloseTo { get; set; }
    public string? CloseState { get; set; }
    public string? ErfNo { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp804QueryRow(string QrsNo, int QrsClass, string QrsSummary, string? LotNo,
    decimal? LotQty, decimal? SampleQty, decimal? ErrorQty, string FactoryNo, string AreaNo, string? MoNo,
    string? CustomerNo, string? ProductNo, string? ProductVersion, string? RoNo, string? OpNo, string? EquipmentNo,
    string? ErfNo, string? HoldDescription, string? Description, string? ProjectDepartment, string? ProjectManager,
    string? ProjectManagerName, string CurStage, DateTime? PlanCloseDate, DateTime? LastUpdateDate,
    string? SummaryDescription, decimal? DispHour, decimal? DispCost, string? SummaryDepartment,
    string? SummaryUserNo, string? SummaryUserName, decimal? CloseType, string? CloseDepartment,
    string? CloseUserNo, string? CloseUserName, DateTime? CloseDate, int IssueState, string? Creator,
    DateTime? CreateDate);

public sealed record Wp804QueryResult(long TotalCount, int Page, int PageSize, long PageCount,
    IReadOnlyList<Wp804QueryRow> Items);
