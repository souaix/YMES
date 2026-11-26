using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp803CloseRepository(NpgsqlDataSource dataSource)
{
    private const string Release = "\"YMES\".tblwip8d_release";
    private const string Basis = "\"YMES\".tblwip8dbasis_factory";
    private const string TeamMember = "\"YMES\".tblwip8d_teammember";
    private const string Department = "\"YMES\".tblusrdepartmentbasis";

    private const string Joined = $"FROM {Release} r INNER JOIN {Basis} b ON b.qrsno=r.qrsno";
    private const string Filter = """
        r.issuestate=@issueState
        AND (@qrsNo IS NULL OR position(upper(@qrsNo) in upper(r.qrsno))>0)
        AND (@factoryNo IS NULL OR upper(b.factoryno)=upper(@factoryNo))
        AND (@projectDepartment IS NULL OR upper(b.projectdepartment)=upper(@projectDepartment))
        AND (@createFrom IS NULL OR r.createdate>=@createFrom)
        AND (@createTo IS NULL OR r.createdate<@createTo)
        AND (@releaseFrom IS NULL OR r.releasedate>=@releaseFrom)
        AND (@releaseTo IS NULL OR r.releasedate<@releaseTo)
        """;

    public Wp803CloseListResult List(Wp803CloseQuery query)
    {
        var filter = Normalize(query);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        ReadOnly(connection, transaction);

        using var countCommand = new NpgsqlCommand($"SELECT count(*) {Joined} WHERE {Filter};", connection, transaction);
        Bind(countCommand, filter);
        var totalCount = (long)countCommand.ExecuteScalar()!;
        var pageCount = totalCount == 0 ? 0 : (long)Math.Ceiling(totalCount / (double)filter.PageSize);
        var offset = checked((filter.Page - 1) * filter.PageSize);

        using var command = new NpgsqlCommand($"""
            SELECT r.qrsno,r.releasememo,r.releasedepartment,r.releaser,r.releasername,
                   r.releasedate,r.description,r.issuestate::int,r.creator,r.createdate
            {Joined}
            WHERE {Filter}
            ORDER BY r.qrsno COLLATE "C"
            LIMIT @pageSize OFFSET @offset;
            """, connection, transaction);
        Bind(command, filter);
        command.Parameters.Add("pageSize", NpgsqlDbType.Integer).Value = filter.PageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<Wp803CloseRow>(filter.PageSize);
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(ReadRow(reader));

        transaction.Commit();
        return new(totalCount, filter.Page, filter.PageSize, pageCount, rows);
    }

    public Wp803CloseDetail Detail(string qrsNo)
    {
        var key = Key(qrsNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        ReadOnly(connection, transaction);
        using var command = new NpgsqlCommand($"""
            SELECT r.qrsno,r.releasememo,r.releasedepartment,r.releaser,r.releasername,
                   r.releasedate,r.description,r.issuestate::int,r.creator,r.createdate,
                   b.qrsclass::int,b.qrssummary,b.factoryno,b.productno,b.productversion,
                   b.projectdepartment,b.projectmanager,b.projectmanagername,b.planclosedate,b.curstage,b.closedate
            {Joined}
            WHERE r.qrsno=@qrsNo;
            """, connection, transaction);
        AddText(command, "qrsNo", key);

        Wp803CloseDetail? result = null;
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                result = new(
                    ReadRow(reader),
                    new(reader.GetInt32(10), reader.GetString(11), reader.GetString(12),
                        Optional(reader, 13), Optional(reader, 14), Optional(reader, 15),
                        Optional(reader, 16), Optional(reader, 17), Time(reader, 18),
                        reader.GetString(19), Time(reader, 20)));
            }
        }

        if (result is null) throw new Wp803CloseRuleException(404, "找不到指定的 8D 結案申請或主單。");
        transaction.Commit();
        return result;
    }

    public Wp803CloseTeamResult TeamMembers(string qrsNo, string? departmentNo)
    {
        var key = Key(qrsNo);
        var departmentKey = NormalizeText(departmentNo, 20, "DepartmentNo");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        ReadOnly(connection, transaction);

        using (var exists = new NpgsqlCommand($"SELECT EXISTS (SELECT 1 {Joined} WHERE r.qrsno=@qrsNo);", connection, transaction))
        {
            AddText(exists, "qrsNo", key);
            if (!(bool)exists.ExecuteScalar()!)
                throw new Wp803CloseRuleException(404, "找不到指定的 8D 結案申請或主單。");
        }

        using var departmentCommand = new NpgsqlCommand($"""
            SELECT DISTINCT t.departmentno,d.departmentname
            FROM {TeamMember} t
            LEFT JOIN {Department} d ON d.departmentno=t.departmentno
            WHERE t.qrsno=@qrsNo AND t.membertype=1 AND t.departmentno IS NOT NULL
            ORDER BY t.departmentno COLLATE "C";
            """, connection, transaction);
        AddText(departmentCommand, "qrsNo", key);
        var departments = new List<Wp803CloseDepartment>();
        using (var reader = departmentCommand.ExecuteReader())
            while (reader.Read()) departments.Add(new(reader.GetString(0), Optional(reader, 1)));

        var members = new List<Wp803CloseMember>();
        if (departmentKey is not null)
        {
            if (!departments.Any(item => string.Equals(item.DepartmentNo, departmentKey, StringComparison.Ordinal)))
                throw new Wp803CloseRuleException(404, "指定部門不在此 8D 的 D1 團隊中。");

            using var memberCommand = new NpgsqlCommand($"""
                SELECT t.departmentno,d.departmentname,t.memberno,t.membername
                FROM {TeamMember} t
                LEFT JOIN {Department} d ON d.departmentno=t.departmentno
                WHERE t.qrsno=@qrsNo AND t.membertype=1 AND t.departmentno=@departmentNo
                ORDER BY t.memberno COLLATE "C";
                """, connection, transaction);
            AddText(memberCommand, "qrsNo", key);
            AddText(memberCommand, "departmentNo", departmentKey);
            using var reader = memberCommand.ExecuteReader();
            while (reader.Read())
                members.Add(new(reader.GetString(0), Optional(reader, 1), reader.GetString(2), reader.GetString(3)));
        }

        transaction.Commit();
        return new(key, departments, members);
    }

    public Wp803CloseRow Update(string qrsNo, Wp803CloseUpdateRequest? request)
    {
        var key = Key(qrsNo);
        if (request is null || request.Expected is null || request.Value is null)
            throw Bad("Expected 與 Value 必須同時提供。");
        var expected = request.Expected;
        var value = request.Value;
        if (!string.Equals(Key(expected.QrsNo), key, StringComparison.Ordinal))
            throw Bad("Expected.QrsNo 必須與路徑 QRSNo 相同。");
        if (expected.IssueState != 0)
            throw new Wp803CloseRuleException(409, "只有未簽核的結案申請可以修改。");
        ValidateExpected(expected);

        var releaseMemo = RequiredText(value.ReleaseMemo, 500, "CloseDescription");
        var releaseDepartment = RequiredText(value.ReleaseDepartment, 20, "CloseDepartment");
        var releaser = RequiredText(value.Releaser, 10, "CloseUserNo");
        var releaserName = RequiredText(value.ReleaserName, 50, "CloseUserName");
        if (!value.ReleaseDate.HasValue) throw Bad("CloseDate 不可空白。");
        var releaseDate = value.ReleaseDate.Value.ToDateTime(TimeOnly.MinValue);
        var description = OptionalText(value.Description, 255, "Comment");

        try
        {
            using var connection = dataSource.OpenConnection();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

            using (var parent = new NpgsqlCommand($"SELECT curstage,closedate FROM {Basis} WHERE qrsno=@qrsNo;", connection, transaction))
            {
                AddText(parent, "qrsNo", key);
                using var reader = parent.ExecuteReader();
                if (!reader.Read()) throw new Wp803CloseRuleException(404, "找不到指定的 8D 主單。");
                var curStage = reader.GetString(0);
                var closeDate = Time(reader, 1);
                if (string.Equals(curStage, "D9", StringComparison.Ordinal) || closeDate.HasValue)
                    throw new Wp803CloseRuleException(409, "8D 主單已結案，不可修改結案申請。");
            }

            using (var teamMember = new NpgsqlCommand($"""
                SELECT EXISTS (
                    SELECT 1 FROM {TeamMember}
                    WHERE qrsno=@qrsNo AND membertype=1
                      AND departmentno=@departmentNo
                      AND memberno=@memberNo AND membername=@memberName
                );
                """, connection, transaction))
            {
                AddText(teamMember, "qrsNo", key);
                AddText(teamMember, "departmentNo", releaseDepartment);
                AddText(teamMember, "memberNo", releaser);
                AddText(teamMember, "memberName", releaserName);
                if (!(bool)teamMember.ExecuteScalar()!)
                    throw Bad("結案部門與人員必須同屬此 8D 的 D1 團隊。");
            }

            using var command = new NpgsqlCommand($"""
                UPDATE {Release} r
                SET releasememo=@releaseMemo,
                    releasedepartment=@releaseDepartment,
                    releaser=@releaser,
                    releasername=@releaserName,
                    releasedate=@releaseDate,
                    description=@description
                WHERE r.qrsno=@qrsNo
                  AND r.issuestate=0
                  AND r.releasememo IS NOT DISTINCT FROM @expectedReleaseMemo
                  AND r.releasedepartment IS NOT DISTINCT FROM @expectedReleaseDepartment
                  AND r.releaser IS NOT DISTINCT FROM @expectedReleaser
                  AND r.releasername IS NOT DISTINCT FROM @expectedReleaserName
                  AND r.releasedate IS NOT DISTINCT FROM @expectedReleaseDate
                  AND r.description IS NOT DISTINCT FROM @expectedDescription
                  AND r.creator IS NOT DISTINCT FROM @expectedCreator
                  AND r.createdate IS NOT DISTINCT FROM @expectedCreateDate
                  AND EXISTS (
                      SELECT 1 FROM {Basis} b
                      WHERE b.qrsno=r.qrsno AND b.closedate IS NULL AND b.curstage<>'D9'
                  )
                RETURNING r.qrsno,r.releasememo,r.releasedepartment,r.releaser,r.releasername,
                          r.releasedate,r.description,r.issuestate::int,r.creator,r.createdate;
                """, connection, transaction);
            AddText(command, "qrsNo", key);
            AddText(command, "releaseMemo", releaseMemo);
            AddText(command, "releaseDepartment", releaseDepartment);
            AddText(command, "releaser", releaser);
            AddText(command, "releaserName", releaserName);
            AddTimestamp(command, "releaseDate", releaseDate);
            AddText(command, "description", description);
            AddText(command, "expectedReleaseMemo", expected.ReleaseMemo);
            AddText(command, "expectedReleaseDepartment", expected.ReleaseDepartment);
            AddText(command, "expectedReleaser", expected.Releaser);
            AddText(command, "expectedReleaserName", expected.ReleaserName);
            AddTimestamp(command, "expectedReleaseDate", expected.ReleaseDate);
            AddText(command, "expectedDescription", expected.Description);
            AddText(command, "expectedCreator", expected.Creator);
            AddTimestamp(command, "expectedCreateDate", expected.CreateDate);

            Wp803CloseRow? updated;
            using (var reader = command.ExecuteReader())
                updated = reader.Read() ? ReadRow(reader) : null;
            if (updated is null)
            {
                using var exists = new NpgsqlCommand($"SELECT EXISTS (SELECT 1 FROM {Release} WHERE qrsno=@qrsNo);", connection, transaction);
                AddText(exists, "qrsNo", key);
                if (!(bool)exists.ExecuteScalar()!)
                    throw new Wp803CloseRuleException(404, "找不到指定的結案申請。");
                throw new Wp803CloseRuleException(409,
                    "結案申請已變更、已簽核或主單已結案；請重新查詢後再修改。");
            }

            transaction.Commit();
            return updated;
        }
        catch (PostgresException ex) when (ex.SqlState == "40001")
        {
            throw new Wp803CloseRuleException(409, "結案申請同時被其他作業更新；請重新查詢後再修改。");
        }
    }

    private static NormalizedQuery Normalize(Wp803CloseQuery query)
    {
        var issueState = query.IssueState ?? 0;
        if (issueState is not (0 or 1 or 2 or -1))
            throw Bad("IssueState 僅接受 0、1、2 或 -1。");

        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        if (page is < 1 or > 100_000) throw Bad("頁碼必須介於 1 到 100,000。");
        if (pageSize is < 1 or > 100) throw Bad("每頁筆數必須介於 1 到 100。");

        var create = Range(query.CreateFrom, query.CreateTo, "CreateDate");
        var release = Range(query.ReleaseFrom, query.ReleaseTo, "ReleaseDate");
        return new(issueState,
            NormalizeText(query.QrsNo, 30, "QRSNo"),
            NormalizeText(query.FactoryNo, 20, "FactoryNo"),
            NormalizeText(query.ProjectDepartment, 20, "ProjectDepartment"),
            create?.From, create?.ToExclusive, release?.From, release?.ToExclusive, page, pageSize);
    }

    private static DateRange? Range(DateOnly? from, DateOnly? to, string name)
    {
        if (from.HasValue != to.HasValue)
            throw Bad($"{name} 必須同時提供起始與結束日期。");
        if (!from.HasValue) return null;
        if (to!.Value < from.Value) throw Bad($"{name} 起始日期不可晚於結束日期。");
        if (to.Value.DayNumber - from.Value.DayNumber > 365)
            throw Bad($"{name} 查詢範圍最多 366 天。");
        if (to.Value == DateOnly.MaxValue)
            throw Bad($"{name} 結束日期超出可查詢範圍。");
        return new(from.Value.ToDateTime(TimeOnly.MinValue), to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
    }

    private static string Key(string qrsNo)
    {
        var value = NormalizeText(qrsNo, 30, "QRSNo");
        return value ?? throw Bad("QRSNo 不可空白。");
    }

    private static string? NormalizeText(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad($"{name} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad($"{name} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static void ValidateExpected(Wp803CloseRow expected)
    {
        ExpectedText(expected.ReleaseMemo, 500, "Expected.ReleaseMemo");
        ExpectedText(expected.ReleaseDepartment, 20, "Expected.ReleaseDepartment");
        ExpectedText(expected.Releaser, 10, "Expected.Releaser");
        ExpectedText(expected.ReleaserName, 50, "Expected.ReleaserName");
        ExpectedText(expected.Description, 255, "Expected.Description");
        ExpectedText(expected.Creator, 10, "Expected.Creator");
        if (expected.ReleaseDate?.Kind is not (null or DateTimeKind.Unspecified) ||
            expected.CreateDate?.Kind is not (null or DateTimeKind.Unspecified))
            throw Bad("Expected 日期必須使用不含時區的資料庫時間格式。");
    }

    private static void ExpectedText(string? value, int maxLength, string name)
    {
        if (value is null) return;
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); }
        catch (EncoderFallbackException) { throw Bad($"{name} 含無效 Unicode。"); }
        if (value.Contains('\0') || value.Any(char.IsControl) || value.EnumerateRunes().Count() > maxLength)
            throw Bad($"{name} 長度或字元不正確。");
    }

    private static string RequiredText(string? value, int maxLength, string name)
    {
        var normalized = OptionalText(value, maxLength, name);
        return normalized ?? throw Bad($"{name} 不可空白。");
    }

    private static string? OptionalText(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad($"{name} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad($"{name} 超過來源欄位長度或含無效字元。");
        return normalized;
    }

    private static void Bind(NpgsqlCommand command, NormalizedQuery filter)
    {
        command.Parameters.Add("issueState", NpgsqlDbType.Integer).Value = filter.IssueState;
        AddText(command, "qrsNo", filter.QrsNo);
        AddText(command, "factoryNo", filter.FactoryNo);
        AddText(command, "projectDepartment", filter.ProjectDepartment);
        AddTimestamp(command, "createFrom", filter.CreateFrom);
        AddTimestamp(command, "createTo", filter.CreateToExclusive);
        AddTimestamp(command, "releaseFrom", filter.ReleaseFrom);
        AddTimestamp(command, "releaseTo", filter.ReleaseToExclusive);
    }

    private static Wp803CloseRow ReadRow(NpgsqlDataReader reader) => new(
        reader.GetString(0), Optional(reader, 1), Optional(reader, 2), Optional(reader, 3),
        Optional(reader, 4), Time(reader, 5), Optional(reader, 6), reader.GetInt32(7),
        Optional(reader, 8), Time(reader, 9));

    private static void ReadOnly(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        command.ExecuteNonQuery();
    }

    private static void AddText(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddTimestamp(NpgsqlCommand command, string name, DateTime? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;

    private static string? Optional(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? Time(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Unspecified);

    private static Wp803CloseRuleException Bad(string message) => new(400, message);

    private sealed record DateRange(DateTime From, DateTime ToExclusive);

    private sealed record NormalizedQuery(int IssueState, string? QrsNo, string? FactoryNo,
        string? ProjectDepartment, DateTime? CreateFrom, DateTime? CreateToExclusive,
        DateTime? ReleaseFrom, DateTime? ReleaseToExclusive, int Page, int PageSize);
}

public sealed class Wp803CloseRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class Wp803CloseQuery
{
    public int? IssueState { get; set; }
    public string? QrsNo { get; set; }
    public string? FactoryNo { get; set; }
    public string? ProjectDepartment { get; set; }
    public DateOnly? CreateFrom { get; set; }
    public DateOnly? CreateTo { get; set; }
    public DateOnly? ReleaseFrom { get; set; }
    public DateOnly? ReleaseTo { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp803CloseRow(string QrsNo, string? ReleaseMemo, string? ReleaseDepartment,
    string? Releaser, string? ReleaserName, DateTime? ReleaseDate, string? Description,
    int IssueState, string? Creator, DateTime? CreateDate);

public sealed record Wp803CloseListResult(long TotalCount, int Page, int PageSize, long PageCount,
    IReadOnlyList<Wp803CloseRow> Items);

public sealed record Wp803CloseBasis(int QrsClass, string QrsSummary, string FactoryNo,
    string? ProductNo, string? ProductVersion, string? ProjectDepartment, string? ProjectManager,
    string? ProjectManagerName, DateTime? PlanCloseDate, string CurStage, DateTime? CloseDate);

public sealed record Wp803CloseDetail(Wp803CloseRow Release, Wp803CloseBasis Basis);

public sealed record Wp803CloseDepartment(string DepartmentNo, string? DepartmentName);

public sealed record Wp803CloseMember(string DepartmentNo, string? DepartmentName,
    string MemberNo, string MemberName);

public sealed record Wp803CloseTeamResult(string QrsNo,
    IReadOnlyList<Wp803CloseDepartment> Departments,
    IReadOnlyList<Wp803CloseMember> Members);

public sealed record Wp803CloseUpdateRequest(Wp803CloseRow? Expected, Wp803CloseUpdate? Value);

public sealed record Wp803CloseUpdate(string? ReleaseMemo, string? ReleaseDepartment,
    string? Releaser, string? ReleaserName, DateOnly? ReleaseDate, string? Description);
