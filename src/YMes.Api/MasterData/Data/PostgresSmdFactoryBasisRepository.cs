using System.Data;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresSmdFactoryBasisRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRows = 5_000;
    private const long MaxDataStamp = 99_999_999_999;
    private const string Columns = "factoryno,description,creator,createdate,issuestate::int,fabcode,factoryname,reviser,revisedate,datastamp::bigint";

    public IReadOnlyList<SmdFactoryBasisDto> List(string? rawFactoryNo)
    {
        var query = OptionalText(rawFactoryNo, "FactoryNo 查詢", 50, preserveWhitespace: false) ?? "";
        using var command = dataSource.CreateCommand($"""
            SELECT {Columns}
            FROM "YMES".tblsmdfactorybasis
            WHERE @query='' OR position(lower(@query) in lower(factoryno))>0
            ORDER BY factoryno COLLATE "C"
            LIMIT @take;
            """);
        Text(command, "query", query);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxRows + 1;

        var items = new List<SmdFactoryBasisDto>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (items.Count == MaxRows)
                    throw new EquipmentRuleException(413, $"工廠資料超過 {MaxRows} 筆，請輸入 FactoryNo 縮小查詢範圍。");
                items.Add(Map(reader));
            }
        }
        return items;
    }

    public SmdFactoryBasisDto Create(SmdFactoryBasisWriteRequest request, string actor)
    {
        var factoryNo = NewKey(request.FactoryNo);
        actor = Actor(actor);
        var factoryName = OptionalText(request.FactoryName, "FactoryName", 50, preserveWhitespace: true);
        var fabCode = OptionalUpper(request.FabCode, "FabCode", 10);
        var description = OptionalText(request.Description, "Description", 255, preserveWhitespace: true);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblsmdfactorybasis
                    (factoryno,description,creator,createdate,issuestate,fabcode,factoryname,datastamp)
                VALUES (@factoryNo,@description,@actor,now() AT TIME ZONE 'UTC',0,@fabCode,@factoryName,0)
                RETURNING {Columns};
                """, connection, transaction);
            Text(command, "factoryNo", factoryNo);
            Text(command, "description", description);
            Text(command, "actor", actor);
            Text(command, "fabCode", fabCode);
            Text(command, "factoryName", factoryName);
            var created = Returned(command);
            transaction.Commit();
            return created;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, $"FactoryNo {factoryNo} 已存在。");
        }
    }

    public SmdFactoryBasisDto Update(
        string rawFactoryNo,
        int issueState,
        long dataStamp,
        SmdFactoryBasisWriteRequest request,
        string actor)
    {
        var factoryNo = Key(rawFactoryNo);
        actor = Actor(actor);
        Stamp(dataStamp);
        if (issueState != 0) throw new EquipmentRuleException(400, "修改只接受 IssueState=0。");
        if (!string.Equals(Key(request.FactoryNo), factoryNo, StringComparison.OrdinalIgnoreCase))
            throw new EquipmentRuleException(400, "修改時不可變更 FactoryNo。");
        var factoryName = OptionalText(request.FactoryName, "FactoryName", 50, preserveWhitespace: true);
        var fabCode = OptionalUpper(request.FabCode, "FabCode", 10);
        var description = OptionalText(request.Description, "Description", 255, preserveWhitespace: true);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Read(connection, transaction, factoryNo, forUpdate: true);
        Expected(current, issueState, dataStamp);
        if (current.IssueState != 0) throw new EquipmentRuleException(409, "只有未簽核工廠資料可以修改。");
        CanIncrement(current.DataStamp);

        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblsmdfactorybasis
            SET description=@description,
                fabcode=@fabCode,
                factoryname=CASE WHEN @factoryName IS NULL THEN factoryname ELSE @factoryName END,
                reviser=@actor,
                revisedate=now() AT TIME ZONE 'UTC',
                datastamp=COALESCE(datastamp,0)+1
            WHERE factoryno=@factoryNo AND issuestate=@issueState AND COALESCE(datastamp,0)=@dataStamp
            RETURNING {Columns};
            """, connection, transaction);
        Text(command, "description", description);
        Text(command, "fabCode", fabCode);
        Text(command, "factoryName", factoryName);
        Text(command, "actor", actor);
        Text(command, "factoryNo", factoryNo);
        Number(command, "issueState", issueState);
        Number(command, "dataStamp", dataStamp);
        var updated = Returned(command);
        transaction.Commit();
        return updated;
    }

    public void Delete(string rawFactoryNo, int issueState, long dataStamp)
    {
        var factoryNo = Key(rawFactoryNo);
        Stamp(dataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Read(connection, transaction, factoryNo, forUpdate: true);
        Expected(current, issueState, dataStamp);
        if (current.IssueState == 1) throw new EquipmentRuleException(409, "簽核中的工廠資料不可刪除。");
        if (current.IssueState is not (0 or 2 or -1))
            throw new EquipmentRuleException(409, $"不支援刪除 IssueState={current.IssueState} 的工廠資料。");

        using var command = new NpgsqlCommand("""
            DELETE FROM "YMES".tblsmdfactorybasis
            WHERE factoryno=@factoryNo AND issuestate=@issueState AND COALESCE(datastamp,0)=@dataStamp;
            """, connection, transaction);
        Text(command, "factoryNo", factoryNo);
        Number(command, "issueState", issueState);
        Number(command, "dataStamp", dataStamp);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
        transaction.Commit();
    }

    public SmdFactoryBasisDto Approve(string rawFactoryNo, SmdFactoryBasisExpectedRequest request, string actor)
    {
        var factoryNo = Key(rawFactoryNo);
        actor = Actor(actor);
        var issueState = request.IssueState ?? throw new EquipmentRuleException(400, "IssueState 為必填。");
        var dataStamp = request.DataStamp ?? throw new EquipmentRuleException(400, "DataStamp 為必填。");
        Stamp(dataStamp);
        if (issueState != 0) throw new EquipmentRuleException(400, "核准只接受 IssueState=0。");

        // FACTORY BASIS is marked for Issue workflow in the source function catalog.
        // The YMES replica currently has no reviewed Issue/Job provider or trusted
        // mapping schema, so never change IssueState directly or pretend approval succeeded.
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Read(connection, transaction, factoryNo, forUpdate: true);
        Expected(current, issueState, dataStamp);
        if (current.IssueState != 0) throw new EquipmentRuleException(409, "只有未簽核工廠資料可以核准。");
        _ = actor;
        throw new EquipmentRuleException(503, "FACTORY BASIS 已設定簽核，但 Issue/Job 簽核服務尚未接妥；本次未變更資料。");
    }

    private static SmdFactoryBasisDto Read(NpgsqlConnection connection, NpgsqlTransaction transaction, string factoryNo, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblsmdfactorybasis WHERE factoryno=@factoryNo{(forUpdate ? " FOR UPDATE" : "")};", connection, transaction);
        Text(command, "factoryNo", factoryNo);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new EquipmentRuleException(404, $"找不到工廠 {factoryNo}。");
        return Map(reader);
    }

    private static SmdFactoryBasisDto Map(NpgsqlDataReader reader) => new()
    {
        FactoryNo = reader.GetString(0),
        Description = NullableText(reader, 1),
        Creator = reader.GetString(2),
        CreateDate = reader.GetDateTime(3),
        IssueState = reader.GetInt32(4),
        FabCode = NullableText(reader, 5),
        FactoryName = NullableText(reader, 6),
        Reviser = NullableText(reader, 7),
        ReviseDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
        DataStamp = reader.IsDBNull(9) ? null : reader.GetInt64(9),
    };

    private static SmdFactoryBasisDto Returned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        return Map(reader);
    }

    private static void Expected(SmdFactoryBasisDto current, int issueState, long dataStamp)
    {
        if (current.IssueState != issueState || (current.DataStamp ?? 0) != dataStamp) throw Conflict();
    }

    private static string NewKey(string? raw) => Key(raw).ToUpperInvariant();

    private static string Key(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 20 || value.Any(char.IsControl))
            throw new EquipmentRuleException(400, "FactoryNo 必填，最多 20 個字元且不可含控制字元。");
        return value;
    }

    private static string? OptionalUpper(string? raw, string label, int maxLength)
    {
        var value = OptionalText(raw, label, maxLength, preserveWhitespace: false);
        return value?.ToUpperInvariant();
    }

    private static string? OptionalText(string? raw, string label, int maxLength, bool preserveWhitespace)
    {
        if (raw is null) return null;
        var value = preserveWhitespace ? raw : raw.Trim();
        if (value.Length == 0) return null;
        if (value.Length > maxLength || value.Any(char.IsControl) && label != "Description")
            throw new EquipmentRuleException(400, $"{label} 超出允許長度或含有不允許的控制字元。");
        return value;
    }

    private static string Actor(string actor)
    {
        var value = actor?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 10 || value.Any(char.IsControl))
            throw new EquipmentRuleException(400, "登入者帳號不存在或超出 Creator 欄位長度。");
        return value;
    }

    private static void Stamp(long value)
    {
        if (value < 0 || value > MaxDataStamp) throw new EquipmentRuleException(400, "DataStamp 超出允許範圍。");
    }

    private static void CanIncrement(long? value)
    {
        if ((value ?? 0) >= MaxDataStamp) throw new EquipmentRuleException(409, "DataStamp 已達來源欄位上限，資料暫不能修改。");
    }

    private static EquipmentRuleException Conflict() => new(409, "工廠資料已被其他人修改，請重新載入後再操作。");

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void Text(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void Number(NpgsqlCommand command, string name, long value) =>
        command.Parameters.Add(name, NpgsqlDbType.Bigint).Value = value;

    private static void Number(NpgsqlCommand command, string name, int value) =>
        command.Parameters.Add(name, NpgsqlDbType.Integer).Value = value;
}
