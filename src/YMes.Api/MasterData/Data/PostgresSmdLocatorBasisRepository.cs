using System.Data;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresSmdLocatorBasisRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRows = 5_000;
    private const long MaxDataStamp = 99_999_999_999;
    private const decimal MaxCapacity = 99_999_999.9999m;
    private const string Columns = "inventoryno,locatorno,locatorname,capacity,description,creator,createdate,issuestate::int,reviser,revisedate,datastamp::bigint";

    public IReadOnlyList<SmdLocatorBasisDto> List(string? rawInventoryNo)
    {
        var inventoryNo = OptionalKey(rawInventoryNo, "InventoryNo");
        using var command = dataSource.CreateCommand($"""
            SELECT {Columns}
            FROM "YMES".tblinvlocatorbasis
            WHERE @inventoryNo IS NULL OR inventoryno=@inventoryNo
            ORDER BY inventoryno COLLATE "C", locatorno COLLATE "C"
            LIMIT @take;
            """);
        Varchar(command, "inventoryNo", inventoryNo, 20);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxRows + 1;

        var items = new List<SmdLocatorBasisDto>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (items.Count == MaxRows)
                    throw new EquipmentRuleException(413, $"儲位資料超過 {MaxRows} 筆，請輸入庫存編號縮小查詢範圍。");
                items.Add(Map(reader));
            }
        }
        return items;
    }

    public SmdLocatorBasisOptionsDto Options()
    {
        using var command = dataSource.CreateCommand("""
            SELECT DISTINCT inventoryno COLLATE "C" AS inventoryno
            FROM "YMES".tblinvinventorybasis
            WHERE issuestate=2 AND char_length(inventoryno)<=20
            ORDER BY inventoryno
            LIMIT @take;
            """);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxRows + 1;
        var items = new List<SmdLocatorInventoryOptionDto>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (items.Count == MaxRows)
                    throw new EquipmentRuleException(413, $"已核准庫存選項超過 {MaxRows} 筆，請縮小範圍。");
                items.Add(new SmdLocatorInventoryOptionDto(reader.GetString(0)));
            }
        }
        return new SmdLocatorBasisOptionsDto(items);
    }

    public SmdLocatorBasisDto Create(SmdLocatorBasisWriteRequest request, string actor)
    {
        var inventoryNo = Key(request.InventoryNo, "InventoryNo");
        var locatorNo = Key(request.LocatorNo, "LocatorNo");
        var locatorName = OptionalText(request.LocatorName, "LocatorName", 50, preserveWhitespace: true);
        var description = OptionalText(request.Description, "Description", 255, preserveWhitespace: true);
        var capacity = Capacity(request.Capacity);
        actor = Actor(actor);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            ValidateApprovedInventory(connection, transaction, inventoryNo);
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblinvlocatorbasis
                    (inventoryno,locatorno,locatorname,capacity,description,creator,createdate,issuestate,datastamp)
                VALUES (@inventoryNo,@locatorNo,@locatorName,COALESCE(@capacity,0),@description,@actor,now() AT TIME ZONE 'UTC',0,0)
                RETURNING {Columns};
                """, connection, transaction);
            Varchar(command, "inventoryNo", inventoryNo, 20);
            Varchar(command, "locatorNo", locatorNo, 20);
            Varchar(command, "locatorName", locatorName, 50);
            Numeric(command, "capacity", capacity);
            Varchar(command, "description", description, 255);
            Varchar(command, "actor", actor, 10);
            var created = Returned(command);
            transaction.Commit();
            return created;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, $"庫存 {inventoryNo} 的儲位 {locatorNo} 已存在。");
        }
    }

    public SmdLocatorBasisDto Update(
        string rawInventoryNo,
        string rawLocatorNo,
        int issueState,
        long dataStamp,
        SmdLocatorBasisWriteRequest request,
        string actor)
    {
        var inventoryNo = Key(rawInventoryNo, "InventoryNo");
        var locatorNo = Key(rawLocatorNo, "LocatorNo");
        ExpectedStamp(dataStamp);
        if (issueState != 0) throw new EquipmentRuleException(400, "修改只接受 IssueState=0。");
        if (!string.Equals(Key(request.InventoryNo, "InventoryNo"), inventoryNo, StringComparison.Ordinal)
            || !string.Equals(Key(request.LocatorNo, "LocatorNo"), locatorNo, StringComparison.Ordinal))
            throw new EquipmentRuleException(400, "修改時不可變更 InventoryNo 或 LocatorNo。");
        var locatorName = OptionalText(request.LocatorName, "LocatorName", 50, preserveWhitespace: true);
        var description = OptionalText(request.Description, "Description", 255, preserveWhitespace: true);
        var capacity = Capacity(request.Capacity);
        actor = Actor(actor);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Read(connection, transaction, inventoryNo, locatorNo, forUpdate: true);
        Expected(current, issueState, dataStamp);
        if (current.IssueState != 0) throw new EquipmentRuleException(409, "只有未簽核儲位資料可以修改。");
        CanIncrement(current.DataStamp);

        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblinvlocatorbasis
            SET locatorname=COALESCE(@locatorName,locatorname),
                capacity=COALESCE(@capacity,capacity),
                description=COALESCE(@description,description),
                reviser=@actor,
                revisedate=now() AT TIME ZONE 'UTC',
                datastamp=COALESCE(datastamp,0)+1
            WHERE inventoryno=@inventoryNo AND locatorno=@locatorNo
              AND issuestate=@issueState AND COALESCE(datastamp,0)=@dataStamp
            RETURNING {Columns};
            """, connection, transaction);
        Varchar(command, "locatorName", locatorName, 50);
        Numeric(command, "capacity", capacity);
        Varchar(command, "description", description, 255);
        Varchar(command, "actor", actor, 10);
        Varchar(command, "inventoryNo", inventoryNo, 20);
        Varchar(command, "locatorNo", locatorNo, 20);
        Numeric(command, "issueState", issueState);
        Numeric(command, "dataStamp", dataStamp);
        var updated = Returned(command);
        transaction.Commit();
        return updated;
    }

    public void Delete(string rawInventoryNo, string rawLocatorNo, int issueState, long dataStamp, string actor)
    {
        var inventoryNo = Key(rawInventoryNo, "InventoryNo");
        var locatorNo = Key(rawLocatorNo, "LocatorNo");
        ExpectedStamp(dataStamp);
        actor = Actor(actor);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Read(connection, transaction, inventoryNo, locatorNo, forUpdate: true);
        Expected(current, issueState, dataStamp);
        CanIncrement(current.DataStamp);

        if (current.IssueState is 0 or -1)
        {
            using var delete = new NpgsqlCommand("""
                DELETE FROM "YMES".tblinvlocatorbasis
                WHERE inventoryno=@inventoryNo AND locatorno=@locatorNo
                  AND issuestate=@issueState AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction);
            Varchar(delete, "inventoryNo", inventoryNo, 20);
            Varchar(delete, "locatorNo", locatorNo, 20);
            Numeric(delete, "issueState", issueState);
            Numeric(delete, "dataStamp", dataStamp);
            if (delete.ExecuteNonQuery() != 1) throw Conflict();
        }
        else if (current.IssueState == 2)
        {
            using var disable = new NpgsqlCommand("""
                UPDATE "YMES".tblinvlocatorbasis
                SET issuestate=-1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',
                    datastamp=COALESCE(datastamp,0)+1
                WHERE inventoryno=@inventoryNo AND locatorno=@locatorNo
                  AND issuestate=@issueState AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction);
            Varchar(disable, "actor", actor, 10);
            Varchar(disable, "inventoryNo", inventoryNo, 20);
            Varchar(disable, "locatorNo", locatorNo, 20);
            Numeric(disable, "issueState", issueState);
            Numeric(disable, "dataStamp", dataStamp);
            if (disable.ExecuteNonQuery() != 1) throw Conflict();
        }
        else if (current.IssueState == 1)
        {
            throw new EquipmentRuleException(409, "簽核中的儲位資料不可刪除。");
        }
        else
        {
            throw new EquipmentRuleException(409, $"不支援刪除 IssueState={current.IssueState} 的儲位資料。");
        }
        transaction.Commit();
    }

    public SmdLocatorBasisDto Approve(string rawInventoryNo, string rawLocatorNo, int issueState, long dataStamp)
    {
        var inventoryNo = Key(rawInventoryNo, "InventoryNo");
        var locatorNo = Key(rawLocatorNo, "LocatorNo");
        ExpectedStamp(dataStamp);
        if (issueState != 0) throw new EquipmentRuleException(400, "核准只接受 IssueState=0。");

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Read(connection, transaction, inventoryNo, locatorNo, forUpdate: true);
        Expected(current, issueState, dataStamp);
        if (current.IssueState != 0) throw new EquipmentRuleException(409, "只有未簽核儲位資料可以核准。");

        // The source function requires Issue workflow, but this replica has no
        // reviewed Issue/Job provider or mapping. Never approve by changing state.
        throw new EquipmentRuleException(503, "INV LOCATOR 已設定簽核，但 Issue/Job 簽核服務尚未接妥；本次未變更資料。");
    }

    private static void ValidateApprovedInventory(NpgsqlConnection connection, NpgsqlTransaction transaction, string inventoryNo)
    {
        using var command = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tblinvinventorybasis
            WHERE inventoryno=@inventoryNo AND issuestate=2;
            """, connection, transaction);
        Varchar(command, "inventoryNo", inventoryNo, 20);
        if (command.ExecuteScalar() is null)
            throw new EquipmentRuleException(400, "InventoryNo 必須存在於已核准庫存選項中。");
    }

    private static SmdLocatorBasisDto Read(NpgsqlConnection connection, NpgsqlTransaction transaction, string inventoryNo, string locatorNo, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblinvlocatorbasis WHERE inventoryno=@inventoryNo AND locatorno=@locatorNo{(forUpdate ? " FOR UPDATE" : "")};", connection, transaction);
        Varchar(command, "inventoryNo", inventoryNo, 20);
        Varchar(command, "locatorNo", locatorNo, 20);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new EquipmentRuleException(404, $"找不到儲位 {inventoryNo} / {locatorNo}。");
        return Map(reader);
    }

    private static SmdLocatorBasisDto Map(NpgsqlDataReader reader) => new()
    {
        InventoryNo = reader.GetString(0),
        LocatorNo = reader.GetString(1),
        LocatorName = NullableText(reader, 2),
        Capacity = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
        Description = NullableText(reader, 4),
        Creator = reader.GetString(5),
        CreateDate = reader.GetDateTime(6),
        IssueState = reader.GetInt32(7),
        ErpNo = null,
        Reviser = NullableText(reader, 8),
        ReviseDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
        DataStamp = reader.IsDBNull(10) ? null : reader.GetInt64(10),
    };

    private static SmdLocatorBasisDto Returned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        return Map(reader);
    }

    private static void Expected(SmdLocatorBasisDto current, int issueState, long dataStamp)
    {
        if (current.IssueState != issueState || (current.DataStamp ?? 0) != dataStamp) throw Conflict();
    }

    private static string Key(string? raw, string label)
    {
        var value = raw?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 20 || value.Any(char.IsControl))
            throw new EquipmentRuleException(400, $"{label} 必填，最多 20 個字元且不可含控制字元。");
        return value;
    }

    private static string? OptionalKey(string? raw, string label)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return Key(raw, label);
    }

    private static string? OptionalText(string? raw, string label, int maxLength, bool preserveWhitespace)
    {
        if (raw is null) return null;
        var value = preserveWhitespace ? raw : raw.Trim();
        if (value.Length == 0) return null;
        if (value.Length > maxLength || (value.Any(char.IsControl) && label != "Description"))
            throw new EquipmentRuleException(400, $"{label} 超出允許長度或含有不允許的控制字元。");
        return value;
    }

    private static decimal? Capacity(decimal? value)
    {
        if (value is null) return null;
        var scale = (decimal.GetBits(value.Value)[3] >> 16) & 0x7f;
        if (value < 0 || value > MaxCapacity || scale > 4)
            throw new EquipmentRuleException(400, "容量必須介於 0 至 99999999.9999，且最多 4 位小數。");
        return value;
    }

    private static string Actor(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 10 || value.Any(char.IsControl))
            throw new EquipmentRuleException(400, "登入者帳號不存在或超出 Creator 欄位長度。");
        return value;
    }

    private static void ExpectedStamp(long value)
    {
        if (value < 0 || value > MaxDataStamp)
            throw new EquipmentRuleException(400, "DataStamp 超出允許範圍。");
    }

    private static void CanIncrement(long? value)
    {
        if ((value ?? 0) >= MaxDataStamp)
            throw new EquipmentRuleException(409, "DataStamp 已達來源欄位上限，資料暫不能修改。");
    }

    private static void Varchar(NpgsqlCommand command, string name, string? value, int size)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Varchar);
        parameter.Size = size;
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static void Numeric(NpgsqlCommand command, string name, decimal value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static void Numeric(NpgsqlCommand command, string name, decimal? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Numeric);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static EquipmentRuleException Conflict() => new(409, "資料已被其他人修改或狀態已改變，請重新載入後再操作。");
}
