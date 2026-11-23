using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Ems;

public sealed class EquipmentCapabilityBasisRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const long MaxDataStamp = 99_999_999_999;
    private static readonly int[] AllowedIssueStates = [0, 1, 2, -1];
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public EquipmentCapabilityBasisPage Search(EquipmentCapabilityBasisFilter filter)
    {
        if (!AllowedIssueStates.Contains(filter.IssueState))
            throw Rule(400, "簽核狀態無效。");
        if (filter.Page is < 1 or > MaxPage)
            throw Rule(400, $"頁碼需介於 1 到 {MaxPage}。");
        if (filter.PageSize is < 1 or > MaxPageSize)
            throw Rule(400, $"每頁筆數需介於 1 到 {MaxPageSize}。");

        var keyword = NormalizeSearch(filter.CapItem);
        var offset = checked((filter.Page - 1) * filter.PageSize);
        using var connection = dataSource.OpenConnection();

        var counts = new Dictionary<int, int> { [0] = 0, [1] = 0, [2] = 0, [-1] = 0 };
        using (var count = new NpgsqlCommand("""
            SELECT issuestate::int, count(*)::int
            FROM "YMES".tblemseqpcapbasis
            GROUP BY issuestate;
            """, connection))
        using (var reader = count.ExecuteReader())
            while (reader.Read()) counts[reader.GetInt32(0)] = reader.GetInt32(1);

        using var command = new NpgsqlCommand("""
            SELECT capitem, description, issuestate::int, creator, createdate,
                   reviser, revisedate, COALESCE(datastamp, 0)::bigint
            FROM "YMES".tblemseqpcapbasis
            WHERE issuestate = @issueState
              AND (@keyword IS NULL OR strpos(upper(capitem), upper(@keyword)) > 0)
            ORDER BY capitem COLLATE "C"
            LIMIT @take OFFSET @skip;
            """, connection);
        AddNumeric(command, "issueState", filter.IssueState);
        AddVarchar(command, "keyword", keyword);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = filter.PageSize + 1;
        command.Parameters.Add("skip", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<EquipmentCapabilityBasisRow>(filter.PageSize + 1);
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(Map(reader));

        var hasMore = rows.Count > filter.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new EquipmentCapabilityBasisPage(filter.Page, filter.PageSize, hasMore, counts, rows);
    }

    public EquipmentCapabilityBasisRow Create(EquipmentCapabilityBasisCreateRequest request, string actor)
    {
        var capItem = NormalizeCapItem(request.CapItem);
        var description = NormalizeDescription(request.Description);
        var user = ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblemseqpcapbasis
                    (capitem, description, issuestate, creator, createdate, datastamp)
                VALUES (@capItem, @description, 0, @actor, timezone('Asia/Taipei', now()), 0);
                """, connection, transaction);
            AddVarchar(command, "capItem", capItem);
            AddVarchar(command, "description", description);
            AddVarchar(command, "actor", user);
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw Rule(409, $"設備能力項目 {capItem} 已存在。");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw Rule(409, "設備能力項目資料同時變更，請重新查詢後再操作。");
        }
        return Get(capItem);
    }

    public EquipmentCapabilityBasisRow Update(string rawCapItem, EquipmentCapabilityBasisUpdateRequest request, string actor)
    {
        var capItem = NormalizeCapItem(rawCapItem);
        var description = NormalizeDescription(request.Description);
        var user = ValidateActor(actor);
        var expectedStamp = ValidateDataStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current = ReadForUpdate(connection, transaction, capItem)
                ?? throw Rule(404, $"找不到設備能力項目 {capItem}。");
            EnsureCurrentVersion(current, 0, expectedStamp);

            using var command = new NpgsqlCommand("""
                UPDATE "YMES".tblemseqpcapbasis
                SET description = @description, reviser = @actor,
                    revisedate = timezone('Asia/Taipei', now()),
                    datastamp = COALESCE(datastamp, 0) + 1
                WHERE capitem = @capItem AND issuestate = 0
                  AND COALESCE(datastamp, 0) = @dataStamp;
                """, connection, transaction);
            AddVarchar(command, "description", description);
            AddVarchar(command, "actor", user);
            AddVarchar(command, "capItem", capItem);
            AddNumeric(command, "dataStamp", expectedStamp);
            if (command.ExecuteNonQuery() != 1)
                throw Rule(409, "設備能力項目資料已被其他人修改，請重新載入後再儲存。");
            transaction.Commit();
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw Rule(409, "設備能力項目資料同時變更，請重新查詢後再操作。");
        }
        return Get(capItem);
    }

    public EquipmentCapabilityBasisDeleteResult Delete(EquipmentCapabilityBasisDeleteRequest request, string actor)
    {
        if (request.Items is null || request.Items.Count is < 1 or > MaxPageSize)
            throw Rule(400, $"請選擇 1 到 {MaxPageSize} 筆設備能力項目。");
        var user = ValidateActor(actor);
        var items = request.Items.Select(item =>
        {
            if (item.IssueState is null || item.DataStamp is null)
                throw Rule(400, "刪除資料缺少簽核狀態或版本。");
            if (!AllowedIssueStates.Contains(item.IssueState.Value))
                throw Rule(400, "簽核狀態無效。");
            return new EquipmentCapabilityBasisDeleteItem(
                NormalizeCapItem(item.CapItem), item.IssueState.Value, ValidateDataStamp(item.DataStamp.Value));
        }).OrderBy(item => item.CapItem, StringComparer.Ordinal).ToArray();
        if (items.Select(item => item.CapItem).Distinct(StringComparer.Ordinal).Count() != items.Length)
            throw Rule(400, "刪除清單含重複設備能力項目。");

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var deleted = 0;
            var disabled = 0;
            foreach (var item in items)
            {
                var current = ReadForUpdate(connection, transaction, item.CapItem)
                    ?? throw Rule(404, $"找不到設備能力項目 {item.CapItem}；本批未變更。");
                EnsureCurrentVersion(current, item.IssueState, item.DataStamp);

                if (current.IssueState == 1)
                    throw Rule(409, $"{item.CapItem} 為簽核中資料，不允許刪除；本批未變更。");

                if (current.IssueState is 0 or -1)
                {
                    if (HasProductReferences(connection, transaction, item.CapItem))
                        throw Rule(409, $"{item.CapItem} 已被產品設備能力引用，不允許實體刪除；本批未變更。");
                    using var remove = new NpgsqlCommand("""
                        DELETE FROM "YMES".tblemseqpcapbasis
                        WHERE capitem = @capItem AND issuestate = @issueState
                          AND COALESCE(datastamp, 0) = @dataStamp;
                        """, connection, transaction);
                    AddVarchar(remove, "capItem", item.CapItem);
                    AddNumeric(remove, "issueState", item.IssueState);
                    AddNumeric(remove, "dataStamp", item.DataStamp);
                    if (remove.ExecuteNonQuery() != 1)
                        throw Rule(409, "設備能力項目資料已被其他人修改；本批未變更。");
                    deleted++;
                }
                else if (current.IssueState == 2)
                {
                    using var retire = new NpgsqlCommand("""
                        UPDATE "YMES".tblemseqpcapbasis
                        SET issuestate = -1, reviser = @actor,
                            revisedate = timezone('Asia/Taipei', now()),
                            datastamp = COALESCE(datastamp, 0) + 1
                        WHERE capitem = @capItem AND issuestate = 2
                          AND COALESCE(datastamp, 0) = @dataStamp;
                        """, connection, transaction);
                    AddVarchar(retire, "actor", user);
                    AddVarchar(retire, "capItem", item.CapItem);
                    AddNumeric(retire, "dataStamp", item.DataStamp);
                    if (retire.ExecuteNonQuery() != 1)
                        throw Rule(409, "設備能力項目資料已被其他人修改；本批未變更。");
                    disabled++;
                }
                else
                {
                    throw Rule(409, "目前簽核狀態不允許刪除；本批未變更。");
                }
            }

            transaction.Commit();
            return new EquipmentCapabilityBasisDeleteResult(items.Length, deleted, disabled);
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw Rule(409, "設備能力項目資料同時變更；本批未變更，請重新查詢。");
        }
    }

    public EquipmentCapabilityBasisRow Get(string capItem)
    {
        using var command = dataSource.CreateCommand("""
            SELECT capitem, description, issuestate::int, creator, createdate,
                   reviser, revisedate, COALESCE(datastamp, 0)::bigint
            FROM "YMES".tblemseqpcapbasis
            WHERE capitem = @capItem;
            """);
        AddVarchar(command, "capItem", capItem);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : throw Rule(404, $"找不到設備能力項目 {capItem}。");
    }

    private static EquipmentCapabilityBasisRow? ReadForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, string capItem)
    {
        using var command = new NpgsqlCommand("""
            SELECT capitem, description, issuestate::int, creator, createdate,
                   reviser, revisedate, COALESCE(datastamp, 0)::bigint
            FROM "YMES".tblemseqpcapbasis
            WHERE capitem = @capItem
            FOR UPDATE;
            """, connection, transaction);
        AddVarchar(command, "capItem", capItem);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    private static bool HasProductReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string capItem)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM "YMES".tblprdopcapitem WHERE capitem = @capItem
            );
            """, connection, transaction);
        AddVarchar(command, "capItem", capItem);
        return command.ExecuteScalar() is true;
    }

    private static EquipmentCapabilityBasisRow Map(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.GetInt32(2),
        reader.GetString(3), reader.GetDateTime(4), reader.IsDBNull(5) ? null : reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetDateTime(6), reader.GetInt64(7));

    private static void EnsureCurrentVersion(EquipmentCapabilityBasisRow current, int expectedState, long expectedStamp)
    {
        if (current.IssueState != expectedState || current.DataStamp != expectedStamp)
            throw Rule(409, "設備能力項目狀態或資料版本已變更，請重新載入後再操作。");
    }

    private static string NormalizeCapItem(string? raw)
    {
        var value = (raw ?? string.Empty).Trim().ToUpperInvariant();
        if (value.Length == 0) throw Rule(400, "CapItem 為必填。");
        if (ContainsControl(value) || ByteCount(value, "CapItem") > 50)
            throw Rule(400, "CapItem 不可超過 50 個 UTF-8 位元組，且不可包含控制字元。");
        return value;
    }

    private static string? NormalizeDescription(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (ContainsControl(raw) || ByteCount(raw, "Description") > 255)
            throw Rule(400, "Description 不可超過 255 個 UTF-8 位元組，且不可包含控制字元。");
        return raw;
    }

    private static string? NormalizeSearch(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (ContainsControl(value) || ByteCount(value, "CapItem 查詢") > 50)
            throw Rule(400, "CapItem 查詢不可超過 50 個 UTF-8 位元組，且不可包含控制字元。");
        return value;
    }

    private static string ValidateActor(string? actor)
    {
        var value = actor?.Trim() ?? string.Empty;
        if (value.Length == 0 || ContainsControl(value) || ByteCount(value, "登入使用者") > 10)
            throw Rule(401, "登入使用者編號無效，無法執行主檔異動。");
        return value;
    }

    private static long ValidateDataStamp(long raw)
    {
        if (raw is < 0 or > MaxDataStamp) throw Rule(400, "DataStamp 超出來源資料範圍。");
        return raw;
    }

    private static int ByteCount(string value, string label)
    {
        try { return StrictUtf8.GetByteCount(value); }
        catch (EncoderFallbackException) { throw Rule(400, $"{label}包含無效文字。"); }
    }

    private static bool ContainsControl(string value) => value.Any(char.IsControl);

    private static void AddVarchar(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, int value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (decimal)value;

    private static void AddNumeric(NpgsqlCommand command, string name, long value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (decimal)value;

    private static bool IsConcurrencyFailure(PostgresException ex) =>
        ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected;

    private static EquipmentCapabilityBasisRuleException Rule(int statusCode, string message) => new(statusCode, message);
}

public sealed record EquipmentCapabilityBasisFilter(int IssueState, string? CapItem, int Page, int PageSize);
public sealed record EquipmentCapabilityBasisRow(
    string CapItem, string? Description, int IssueState, string Creator,
    DateTime CreateDate, string? Reviser, DateTime? ReviseDate, long DataStamp);
public sealed record EquipmentCapabilityBasisPage(
    int Page, int PageSize, bool HasMore, IReadOnlyDictionary<int, int> StateCounts,
    IReadOnlyList<EquipmentCapabilityBasisRow> Items);
public sealed record EquipmentCapabilityBasisCreateRequest(string? CapItem, string? Description);
public sealed record EquipmentCapabilityBasisUpdateRequest(string? Description, long DataStamp);
public sealed record EquipmentCapabilityBasisDeleteRequest(IReadOnlyList<EquipmentCapabilityBasisDeleteItemRequest>? Items);
public sealed record EquipmentCapabilityBasisDeleteItemRequest(string? CapItem, int? IssueState, long? DataStamp);
public sealed record EquipmentCapabilityBasisDeleteItem(string CapItem, int IssueState, long DataStamp);
public sealed record EquipmentCapabilityBasisDeleteResult(int ProcessedCount, int DeletedCount, int DisabledCount);

public sealed class EquipmentCapabilityBasisRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
