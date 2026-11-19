using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Processes;

public sealed class Ps101Repository(NpgsqlDataSource dataSource, string? enterpriseNo = null)
{
    private const string Columns = "psno,psname,psorder,description,issuestate,havecomponent,havelevel,serialtypeno_lot,serialtypeno_comp,ispartial,isnetflow,datastamp,creator,createdate,reviser,revisedate";
    private readonly bool forceNoFlags = string.Equals(enterpriseNo?.Trim(), "WAHHONG", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<Ps101SegmentDto> List(string? psNo)
    {
        var query = SearchText(psNo);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblprsproductionsegment");
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblprsproductionsegment WHERE @query='' OR position(lower(@query) in lower(psno))>0 ORDER BY psno;", connection);
        Text(command, "query", query, 50);
        var result = new List<Ps101SegmentDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public IReadOnlyList<Ps101SerialRuleDto> SerialRules(string? serialType)
    {
        var type = RequiredText(serialType, "SerialType", 50).ToUpperInvariant();
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblsysserialrulebasis");
        // WinForms supplies SerialType only. Include both enabled and disabled rules.
        using var command = new NpgsqlCommand("SELECT serialtypeno,description,enable FROM \"YMES\".tblsysserialrulebasis WHERE upper(serialtype)=@type ORDER BY serialtypeno;", connection);
        Text(command, "type", type, 50);
        var result = new List<Ps101SerialRuleDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(new(reader.GetString(0), NullableText(reader, 1), Convert.ToInt32(reader.GetValue(2))));
        return result;
    }

    public Ps101SegmentDto Create(Ps101SegmentWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var value = Normalize(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblprsproductionsegment");
            EnsureUnique(connection, transaction, value.No);
            EnsureLotSerial(connection, transaction, value.LotSerial);
            // Hidden IsPartial, IsNetFlow and DataStamp use the source DB defaults.
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblprsproductionsegment
                    (psno,psname,psorder,description,havecomponent,havelevel,serialtypeno_lot,serialtypeno_comp,issuestate,creator,createdate)
                VALUES (@no,@name,@order,@description,@component,@level,@lotserial,@compserial,0,@actor,now())
                RETURNING {Columns};
                """, connection, transaction);
            Bind(command, value);
            Text(command, "actor", actor.Trim(), 10);
            var created = ReadReturned(command);
            transaction.Commit();
            return created;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction);
            throw Rule(409, "PSNo 已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps101SegmentDto Update(string psNo, Ps101SegmentUpdateRequest request, string actor)
    {
        ValidateActor(actor);
        var no = Key(psNo);
        var value = Normalize(request);
        if (!string.Equals(no, value.No, StringComparison.OrdinalIgnoreCase)) throw Rule(400, "PSNo 不可修改。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblprsproductionsegment");
            var current = ReadForUpdate(connection, transaction, no);
            EnsureExpected(current, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有未核准（IssueState=0）的生產區段可修改。");
            // The standard editor hides Component serial. Preserve the stored
            // value even when an older client omits it or sends an empty string.
            value = value with { No = current.PsNo, CompSerial = current.SerialTypeNoComp };
            EnsureLotSerial(connection, transaction, value.LotSerial);
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblprsproductionsegment
                SET psname=@name,psorder=@order,description=@description,havecomponent=@component,havelevel=@level,
                    serialtypeno_lot=@lotserial,serialtypeno_comp=@compserial,
                    reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE psno=@no AND issuestate=0 AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            Bind(command, value);
            Text(command, "actor", actor.Trim(), 10);
            Number(command, "stamp", request.DataStamp);
            var updated = ReadReturned(command);
            transaction.Commit();
            return updated;
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps101SegmentDto Delete(string psNo, Ps101SegmentStateRequest request, string actor)
    {
        ValidateActor(actor);
        var no = Key(psNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblprsproductionsegment");
            var current = ReadForUpdate(connection, transaction, no);
            EnsureExpected(current, request.DataStamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的生產區段不可刪除。");
            Ps101SegmentDto result;
            if (current.IssueState == 2) result = ChangeState(connection, transaction, current, -1, actor);
            else if (current.IssueState is 0 or -1)
            {
                // Source deletes only the master. Do not invent cascading deletes.
                using var command = new NpgsqlCommand("""
                    DELETE FROM "YMES".tblprsproductionsegment
                    WHERE psno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;
                    """, connection, transaction);
                BindExpected(command, current);
                if (command.ExecuteNonQuery() != 1) throw Conflict();
                result = current;
            }
            else throw Rule(409, "目前資料狀態不允許刪除，請確認生產區段狀態。");
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps101SegmentDto Approve(string psNo, Ps101SegmentStateRequest request, string actor)
    {
        ValidateActor(actor);
        var no = Key(psNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblprsproductionsegment");
            var current = ReadForUpdate(connection, transaction, no);
            EnsureExpected(current, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有未核准（IssueState=0）的生產區段可送出核准。");
            foreach (var table in new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" })
            {
                if (TableExists(connection, transaction, table))
                    throw Rule(503, "YMES 已存在 Issue/Job 表；PS101 尚未接入共用簽核服務，資料未變更。");
            }
            var result = ChangeState(connection, transaction, current, 2, actor);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private static Ps101SegmentDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Ps101SegmentDto current, int next, string actor)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblprsproductionsegment
            SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE psno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        BindExpected(command, current);
        Number(command, "next", next);
        Text(command, "actor", actor.Trim(), 10);
        return ReadReturned(command);
    }

    private static Ps101SegmentDto ReadForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblprsproductionsegment WHERE lower(psno)=lower(@no) FOR UPDATE;", connection, transaction);
        Text(command, "no", no, 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到生產區段 {no}。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "PSNo 存在大小寫重複資料，請先修正資料。");
        return result;
    }

    private static Ps101SegmentDto ReadReturned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        return Read(reader);
    }

    private static Ps101SegmentDto Read(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), Convert.ToInt32(reader.GetValue(2)), NullableText(reader, 3),
        Convert.ToInt32(reader.GetValue(4)), Convert.ToInt32(reader.GetValue(5)), Convert.ToInt32(reader.GetValue(6)),
        NullableText(reader, 7), NullableText(reader, 8), Convert.ToInt32(reader.GetValue(9)),
        reader.IsDBNull(10) ? null : Convert.ToInt32(reader.GetValue(10)),
        reader.IsDBNull(11) ? null : reader.GetDecimal(11), NullableText(reader, 12),
        reader.IsDBNull(13) ? null : reader.GetDateTime(13), NullableText(reader, 14),
        reader.IsDBNull(15) ? null : reader.GetDateTime(15));

    private sealed record SegmentValue(string No, string Name, decimal Order, string? Description,
        int Component, int Level, string? LotSerial, string? CompSerial);

    private SegmentValue Normalize(Ps101SegmentWriteRequest request)
    {
        var no = Key(request.PsNo);
        var name = RequiredText(request.PsName, "PSName", 50);
        if (request.PsOrder != decimal.Truncate(request.PsOrder) || request.PsOrder < 1 || request.PsOrder > 99)
            throw Rule(400, "PSOrder 必須是 1 到 99 的整數。");
        if (request.HaveComponent is not (0 or 1) || request.HaveLevel is not (0 or 1))
            throw Rule(400, "HaveComponent 與 HaveLevel 僅允許 0 或 1。");
        var component = forceNoFlags ? 0 : request.HaveComponent;
        var level = forceNoFlags ? 0 : request.HaveLevel;
        if (component == 1 && level == 1) throw Rule(400, "HaveComponent 與 HaveLevel 不可同時為 Yes。");
        return new(no, name, request.PsOrder, OptionalText(request.Description, "Description", 255, multiline: true), component, level,
            OptionalText(request.SerialTypeNoLot, "SerialTypeNoLot", 50), OptionalText(request.SerialTypeNoComp, "SerialTypeNoComp", 50));
    }

    private static void EnsureUnique(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblprsproductionsegment WHERE lower(psno)=lower(@no);", connection, transaction);
        Text(command, "no", no, 50);
        if (command.ExecuteScalar() is not null) throw Rule(409, "PSNo 已存在。");
    }

    private static void EnsureLotSerial(NpgsqlConnection connection, NpgsqlTransaction transaction, string? serialNo)
    {
        if (serialNo is null) return;
        EnsureTable(connection, transaction, "tblsysserialrulebasis");
        using var command = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tblsysserialrulebasis
            WHERE upper(serialtype)='LOTNO' AND lower(serialtypeno)=lower(@serial)
            FOR KEY SHARE;
            """, connection, transaction);
        Text(command, "serial", serialNo, 50);
        if (command.ExecuteScalar() is null) throw Rule(422, "找不到所選的 LOTNO 序號規則，請重新選取。");
    }

    private static void EnsureExpected(Ps101SegmentDto current, decimal? expectedStamp)
    {
        if (current.DataStamp != expectedStamp) throw Conflict();
    }

    private static void Bind(NpgsqlCommand command, SegmentValue value)
    {
        Text(command, "no", value.No, 50);
        Text(command, "name", value.Name, 50);
        Number(command, "order", value.Order);
        Text(command, "description", value.Description, 255);
        Number(command, "component", value.Component);
        Number(command, "level", value.Level);
        Text(command, "lotserial", value.LotSerial, 50);
        Text(command, "compserial", value.CompSerial, 50);
    }

    private static void BindExpected(NpgsqlCommand command, Ps101SegmentDto current)
    {
        Text(command, "no", current.PsNo, 50);
        Number(command, "state", current.IssueState);
        Number(command, "stamp", current.DataStamp);
    }

    private static string Key(string? value) => RequiredText(value, "PSNo", 50).ToUpperInvariant();
    private static string RequiredText(string? value, string field, int max)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0 || text.Length > max || text.Any(char.IsControl))
            throw Rule(400, $"{field} 為必填，最長 {max} 個字元且不可含控制字元。");
        return text;
    }

    private static string? OptionalText(string? value, string field, int max, bool multiline = false)
    {
        var text = (value ?? "").Trim();
        if (text.Length > max || text.Any(ch => char.IsControl(ch) && (!multiline || ch is not ('\r' or '\n' or '\t'))))
            throw Rule(400, $"{field} 最長 {max} 個字元且不可含無效控制字元。");
        return text.Length == 0 ? null : text;
    }

    private static string SearchText(string? value)
    {
        var text = (value ?? "").Trim();
        if (text.Length > 50 || text.Any(char.IsControl)) throw Rule(400, "PSNo 查詢條件最長 50 個字元且不可含控制字元。");
        return text;
    }

    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10 || actor.Any(char.IsControl))
            throw Rule(401, "登入帳號必須有效，且不可超過 10 個字元。");
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p','v','m','f'));", connection, transaction);
        Text(command, "table", table, 63);
        return command.ExecuteScalar() is true;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        if (!TableExists(connection, transaction, table))
            throw Rule(503, $"YMES 尚未建立 {table.ToUpperInvariant()}，請先完成對應 migration。");
    }

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* retain original failure */ } }
    private static Ps101RuleException Rule(int status, string message) => new(status, message);
    private static Ps101RuleException Conflict() => Rule(409, "資料已被其他作業修改，請重新查詢後再試。");
}
