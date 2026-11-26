using System.Data;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresSmdAreaInventoryRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRows = 5_000;

    public IReadOnlyList<SmdAreaInventoryDto> ListAreaInventories(string? rawBaseAreaNo)
    {
        var baseAreaNo = Required(rawBaseAreaNo, "BaseAreaNo", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = BeginReadOnly(connection);
        ValidateApprovedBaseArea(connection, transaction, baseAreaNo);

        using var command = new NpgsqlCommand("""
            SELECT areano,inventoryno,baseareano,defaultinv::int,
                   creator,createdate,reviser,revisedate,datastamp::bigint
            FROM "YMES".tblsmdareainventory
            WHERE baseareano=@baseAreaNo
            ORDER BY areano COLLATE "C", defaultinv DESC, inventoryno COLLATE "C"
            LIMIT @take;
            """, connection, transaction);
        AddVarchar(command, "baseAreaNo", baseAreaNo);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxRows + 1;

        var items = new List<SmdAreaInventoryDto>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (items.Count == MaxRows)
                    throw new EquipmentRuleException(413, $"區域庫房結果超過 {MaxRows} 筆，請縮小基底區域範圍。");
                items.Add(MapAreaInventory(reader));
            }
        }
        transaction.Commit();
        return items;
    }

    public SmdAreaLayoutRelationsDto ListAreaRelations(string? rawBaseAreaNo)
    {
        var baseAreaNo = Required(rawBaseAreaNo, "BaseAreaNo", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = BeginReadOnly(connection);
        ValidateApprovedBaseArea(connection, transaction, baseAreaNo);

        using var command = new NpgsqlCommand("""
            SELECT objectno,objecttext,objecttype::int,containareano,objectareatype::int
            FROM "YMES".tblsmdarearelation
            WHERE baseareano=@baseAreaNo
            ORDER BY objectsequence NULLS LAST, objectno COLLATE "C"
            LIMIT @take;
            """, connection, transaction);
        AddVarchar(command, "baseAreaNo", baseAreaNo);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxRows + 1;

        var relations = new List<SmdAreaRelationOptionDto>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (relations.Count == MaxRows)
                    throw new EquipmentRuleException(413, $"區域配置節點超過 {MaxRows} 個，請縮小查詢範圍。");
                relations.Add(new SmdAreaRelationOptionDto(
                    reader.GetString(0), reader.GetString(1), reader.GetInt32(2),
                    NullableString(reader, 3), reader.IsDBNull(4) ? null : reader.GetInt32(4)));
            }
        }
        transaction.Commit();
        return new SmdAreaLayoutRelationsDto(relations);
    }

    public SmdAreaInventoryDto CreateAreaInventory(SmdAreaInventoryWriteRequest request, string actor)
    {
        var values = ValidateWrite(request.AreaNo, request.InventoryNo, request.BaseAreaNo, request.DefaultInv, actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            ValidateApprovedBaseArea(connection, transaction, values.BaseAreaNo);
            ValidateRelation(connection, transaction, values.BaseAreaNo, values.AreaNo, objectType: 0, "AreaNo");
            ValidateRelation(connection, transaction, values.BaseAreaNo, values.InventoryNo, objectType: 1, "InventoryNo");
            if (values.DefaultInv == 1)
                ValidateNoDefault(connection, transaction, values.BaseAreaNo, values.AreaNo, excludeInventoryNo: null);

            using (var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblsmdareainventory
                    (areano,inventoryno,baseareano,defaultinv,creator,createdate,datastamp)
                VALUES (@areaNo,@inventoryNo,@baseAreaNo,@defaultInv,@actor,now(),0);
                """, connection, transaction))
            {
                AddVarchar(command, "areaNo", values.AreaNo);
                AddVarchar(command, "inventoryNo", values.InventoryNo);
                AddVarchar(command, "baseAreaNo", values.BaseAreaNo);
                AddNumeric(command, "defaultInv", values.DefaultInv);
                AddVarchar(command, "actor", actor.Trim());
                command.ExecuteNonQuery();
            }

            var created = GetAreaInventory(connection, transaction, values.AreaNo, values.InventoryNo, values.BaseAreaNo, forUpdate: false)
                ?? throw new EquipmentRuleException(500, "區域庫房資料建立後無法讀回。");
            transaction.Commit();
            return created;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, "此區域與庫房的對應已存在。");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "區域配置或預設庫房已同時變更，請重新載入後再操作。");
        }
    }

    public SmdAreaInventoryDto UpdateAreaInventory(
        string rawAreaNo,
        string rawInventoryNo,
        string? rawBaseAreaNo,
        long dataStamp,
        SmdAreaInventoryUpdateRequest request,
        string actor)
    {
        var areaNo = Required(rawAreaNo, "AreaNo", 50);
        var inventoryNo = Required(rawInventoryNo, "InventoryNo", 50);
        var baseAreaNo = Required(rawBaseAreaNo, "BaseAreaNo", 50);
        var defaultInv = ValidateDefaultInv(request.DefaultInv);
        ValidateActor(actor);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current = GetAreaInventory(connection, transaction, areaNo, inventoryNo, baseAreaNo, forUpdate: true);
            if (current is null) throw NotFound(areaNo, inventoryNo, baseAreaNo);
            if ((current.DataStamp ?? 0) != dataStamp)
                throw new EquipmentRuleException(409, "資料已被其他人修改，請重新載入後再操作。");

            if (defaultInv == 1)
                ValidateNoDefault(connection, transaction, baseAreaNo, areaNo, excludeInventoryNo: inventoryNo);

            using (var command = new NpgsqlCommand("""
                UPDATE "YMES".tblsmdareainventory
                SET defaultinv=@defaultInv,
                    reviser=@actor,
                    revisedate=now(),
                    datastamp=COALESCE(datastamp,0)+1
                WHERE areano=@areaNo AND inventoryno=@inventoryNo AND baseareano=@baseAreaNo
                  AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction))
            {
                AddNumeric(command, "defaultInv", defaultInv);
                AddVarchar(command, "actor", actor.Trim());
                AddVarchar(command, "areaNo", areaNo);
                AddVarchar(command, "inventoryNo", inventoryNo);
                AddVarchar(command, "baseAreaNo", baseAreaNo);
                AddNumeric(command, "dataStamp", dataStamp);
                if (command.ExecuteNonQuery() != 1)
                    throw new EquipmentRuleException(409, "資料已被其他人修改，請重新載入後再操作。");
            }

            var updated = GetAreaInventory(connection, transaction, areaNo, inventoryNo, baseAreaNo, forUpdate: false)
                ?? throw NotFound(areaNo, inventoryNo, baseAreaNo);
            transaction.Commit();
            return updated;
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "區域配置或預設庫房已同時變更，請重新載入後再操作。");
        }
    }

    public void DeleteAreaInventory(string rawAreaNo, string rawInventoryNo, string? rawBaseAreaNo, long dataStamp)
    {
        var areaNo = Required(rawAreaNo, "AreaNo", 50);
        var inventoryNo = Required(rawInventoryNo, "InventoryNo", 50);
        var baseAreaNo = Required(rawBaseAreaNo, "BaseAreaNo", 50);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current = GetAreaInventory(connection, transaction, areaNo, inventoryNo, baseAreaNo, forUpdate: true);
            if (current is null) throw NotFound(areaNo, inventoryNo, baseAreaNo);
            if ((current.DataStamp ?? 0) != dataStamp)
                throw new EquipmentRuleException(409, "資料已被其他人修改，請重新載入後再操作。");

            using var command = new NpgsqlCommand("""
                DELETE FROM "YMES".tblsmdareainventory
                WHERE areano=@areaNo AND inventoryno=@inventoryNo AND baseareano=@baseAreaNo
                  AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction);
            AddVarchar(command, "areaNo", areaNo);
            AddVarchar(command, "inventoryNo", inventoryNo);
            AddVarchar(command, "baseAreaNo", baseAreaNo);
            AddNumeric(command, "dataStamp", dataStamp);
            if (command.ExecuteNonQuery() != 1)
                throw new EquipmentRuleException(409, "資料已被其他人修改，請重新載入後再操作。");
            transaction.Commit();
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "區域庫房資料已同時變更，請重新載入後再操作。");
        }
    }

    private static NpgsqlTransaction BeginReadOnly(NpgsqlConnection connection)
    {
        var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        command.ExecuteNonQuery();
        return transaction;
    }

    private static void ValidateApprovedBaseArea(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string baseAreaNo)
    {
        using var command = new NpgsqlCommand($"""
            SELECT 1 FROM "YMES".tblsmdareabasis
            WHERE areano=@baseAreaNo AND areaclass=1 AND issuestate=2;
            """, connection, transaction);
        AddVarchar(command, "baseAreaNo", baseAreaNo);
        if (command.ExecuteScalar() is null)
            throw new EquipmentRuleException(404, $"找不到已簽核的基底區域 {baseAreaNo}。");
    }

    private static void ValidateRelation(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string baseAreaNo,
        string objectNo,
        int objectType,
        string label)
    {
        using var command = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tblsmdarearelation
            WHERE baseareano=@baseAreaNo AND objectno=@objectNo AND objecttype=@objectType;
            """, connection, transaction);
        AddVarchar(command, "baseAreaNo", baseAreaNo);
        AddVarchar(command, "objectNo", objectNo);
        AddNumeric(command, "objectType", objectType);
        if (command.ExecuteScalar() is null)
            throw new EquipmentRuleException(400, $"{label} 必須存在於所選基底區域的配置中。");
    }

    private static void ValidateNoDefault(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string baseAreaNo,
        string areaNo,
        string? excludeInventoryNo)
    {
        using var command = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tblsmdareainventory
            WHERE baseareano=@baseAreaNo AND areano=@areaNo AND defaultinv=1
              AND (@excludeInventoryNo IS NULL OR inventoryno<>@excludeInventoryNo)
            LIMIT 1;
            """, connection, transaction);
        AddVarchar(command, "baseAreaNo", baseAreaNo);
        AddVarchar(command, "areaNo", areaNo);
        AddVarchar(command, "excludeInventoryNo", excludeInventoryNo);
        if (command.ExecuteScalar() is not null)
            throw new EquipmentRuleException(409, "同一基底區域下的 Area 已有預設庫房。");
    }

    private static SmdAreaInventoryDto? GetAreaInventory(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string areaNo,
        string inventoryNo,
        string baseAreaNo,
        bool forUpdate)
    {
        var lockClause = forUpdate ? " FOR UPDATE" : "";
        using var command = new NpgsqlCommand($"""
            SELECT areano,inventoryno,baseareano,defaultinv::int,
                   creator,createdate,reviser,revisedate,datastamp::bigint
            FROM "YMES".tblsmdareainventory
            WHERE areano=@areaNo AND inventoryno=@inventoryNo AND baseareano=@baseAreaNo
            {lockClause};
            """, connection, transaction);
        AddVarchar(command, "areaNo", areaNo);
        AddVarchar(command, "inventoryNo", inventoryNo);
        AddVarchar(command, "baseAreaNo", baseAreaNo);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapAreaInventory(reader) : null;
    }

    private static SmdAreaInventoryDto MapAreaInventory(NpgsqlDataReader reader) => new()
    {
        AreaNo = reader.GetString(0),
        InventoryNo = reader.GetString(1),
        BaseAreaNo = reader.GetString(2),
        DefaultInv = reader.GetInt32(3),
        Creator = NullableString(reader, 4),
        CreateDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
        Reviser = NullableString(reader, 6),
        ReviseDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
        DataStamp = reader.IsDBNull(8) ? null : reader.GetInt64(8),
    };

    private static (string AreaNo, string InventoryNo, string BaseAreaNo, int DefaultInv) ValidateWrite(
        string? areaNo,
        string? inventoryNo,
        string? baseAreaNo,
        int? defaultInv,
        string actor)
    {
        var area = Required(areaNo, "AreaNo", 50);
        var inventory = Required(inventoryNo, "InventoryNo", 50);
        var baseArea = Required(baseAreaNo, "BaseAreaNo", 50);
        ValidateActor(actor);
        return (area, inventory, baseArea, ValidateDefaultInv(defaultInv));
    }

    private static int ValidateDefaultInv(int? value) =>
        value is 0 or 1 ? value.Value : throw new EquipmentRuleException(400, "DefaultINV 僅接受 0 或 1。");

    private static string Required(string? value, string label, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new EquipmentRuleException(400, $"{label} 為必填。");
        if (text.Length > maxLength)
            throw new EquipmentRuleException(400, $"{label} 不可超過 {maxLength} 個字元。");
        return text;
    }

    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10)
            throw new EquipmentRuleException(400, "登入者帳號不存在或超出 Creator 欄位長度。");
    }

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void AddVarchar(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, int value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static void AddNumeric(NpgsqlCommand command, string name, long value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static bool IsConcurrencyFailure(PostgresException exception) =>
        exception.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected;

    private static EquipmentRuleException NotFound(string areaNo, string inventoryNo, string baseAreaNo) =>
        new(404, $"找不到基底區域 {baseAreaNo} 的區域庫房 {areaNo}／{inventoryNo}。");
}
