using System.Data;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresSmdAreaBasisRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRows = 5_000;
    private const string AreaColumns = "areano,areaname,areatype::int,areaclass::int,baseareano,factoryno,departmentno,description,creator,createdate,issuestate::int,areacompose::int,reviser,revisedate,datastamp::bigint";

    public IReadOnlyList<SmdAreaBasisDto> ListAreas(string? rawAreaNo, string? rawFactoryNo, int? issueState, int? areaClass)
    {
        if (issueState is not null and not (-1 or 0 or 1 or 2))
            throw new MasterDataRuleException(400, "IssueState 僅接受 -1、0、1 或 2。");
        if (areaClass is not null and not (0 or 1))
            throw new MasterDataRuleException(400, "AreaClass 僅接受 0 或 1。");

        var areaNo = OptionalFilter(rawAreaNo, "AreaNo", 20);
        var factoryNo = OptionalFilter(rawFactoryNo, "FactoryNo", 20);
        using var command = dataSource.CreateCommand($"""
            SELECT {AreaColumns}
            FROM "YMES".tblsmdareabasis
            WHERE (@areaNo IS NULL OR position(lower(@areaNo) in lower(areano)) > 0)
              AND (@factoryNo IS NULL OR factoryno=@factoryNo)
              AND (@issueState IS NULL OR issuestate=@issueState)
              AND (@areaClass IS NULL OR areaclass=@areaClass)
            ORDER BY areano COLLATE "C"
            LIMIT @take;
            """);
        AddVarchar(command, "areaNo", areaNo);
        AddVarchar(command, "factoryNo", factoryNo);
        AddNumeric(command, "issueState", issueState);
        AddNumeric(command, "areaClass", areaClass);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxRows + 1;

        var rows = new List<SmdAreaBasisDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (rows.Count == MaxRows)
                throw new MasterDataRuleException(413, $"區域資料超過 {MaxRows} 筆，請縮小查詢條件。");
            rows.Add(MapArea(reader));
        }
        return rows;
    }

    public SmdAreaBasisOptionsDto Options()
    {
        var factories = ReadBounded("""
            SELECT factoryno,description,factoryname
            FROM "YMES".tblsmdfactorybasis
            WHERE issuestate=2
            ORDER BY factoryno COLLATE "C"
            LIMIT @take;
            """, reader => new SmdFactoryOptionDto(reader.GetString(0), NullableString(reader, 1), NullableString(reader, 2)), "已簽核工廠");

        var baseAreas = ReadBounded("""
            SELECT areano,areaname,description,issuestate::int,areaclass::int,factoryno
            FROM "YMES".tblsmdareabasis
            WHERE issuestate=2 AND areaclass=1
            ORDER BY areano COLLATE "C"
            LIMIT @take;
            """, reader => new SmdAreaOptionDto(
                reader.GetString(0), reader.GetString(1), NullableString(reader, 2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetString(5)), "已簽核基底區域");

        // LoadDepartmentBasis has no verified state filter in the office handoff.
        var departments = ReadBounded("""
            SELECT departmentno,departmentname
            FROM "YMES".tblusrdepartmentbasis
            ORDER BY departmentno COLLATE "C"
            LIMIT @take;
            """, reader => new SmdDepartmentOptionDto(reader.GetString(0), reader.GetString(1)), "部門");

        return new SmdAreaBasisOptionsDto(factories, baseAreas, departments);
    }

    public SmdAreaBasisDto Create(SmdAreaBasisWriteRequest request, string actor)
    {
        var values = ValidateWrite(request, actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var references = ValidateReferences(connection, transaction, values);
            using (var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblsmdareabasis
                    (areano,areaname,areatype,areaclass,baseareano,factoryno,departmentno,description,
                     creator,createdate,issuestate,areacompose,datastamp)
                VALUES
                    (@areaNo,@areaName,@areaType,@areaClass,@baseAreaNo,@factoryNo,@departmentNo,@description,
                     @actor,now(),0,@areaCompose,0);
                """, connection, transaction))
            {
                AddVarchar(insert, "areaNo", values.AreaNo);
                AddVarchar(insert, "areaName", values.AreaName);
                AddNumeric(insert, "areaType", values.AreaType);
                AddNumeric(insert, "areaClass", values.AreaClass);
                AddVarchar(insert, "baseAreaNo", references.BaseAreaNo);
                AddVarchar(insert, "factoryNo", references.FactoryNo);
                AddVarchar(insert, "departmentNo", values.DepartmentNo);
                AddVarchar(insert, "description", values.Description);
                AddVarchar(insert, "actor", actor.Trim());
                AddNumeric(insert, "areaCompose", values.AreaCompose);
                insert.ExecuteNonQuery();
            }

            var created = GetArea(connection, transaction, values.AreaNo, forUpdate: false)
                ?? throw new MasterDataRuleException(500, "區域資料建立後無法讀回。");
            transaction.Commit();
            return created;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new MasterDataRuleException(409, $"區域編號 {values.AreaNo} 已存在。");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new MasterDataRuleException(409, "區域或參照資料已同時變更，請重新載入後再操作。");
        }
    }

    public SmdAreaBasisDto Update(string rawAreaNo, long dataStamp, SmdAreaBasisWriteRequest request, string actor)
    {
        var areaNo = Required(rawAreaNo, "AreaNo", 20).ToUpperInvariant();
        if (!string.Equals(areaNo, request.AreaNo?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new MasterDataRuleException(400, "網址與資料內容的 AreaNo 必須相同。");
        var values = ValidateWrite(request, actor) with { AreaNo = areaNo };
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current = GetArea(connection, transaction, areaNo, forUpdate: true)
                ?? throw AreaNotFound(areaNo);
            if (current.IssueState != 0)
                throw new MasterDataRuleException(409, "只有未簽核區域可以修改。");
            if ((current.DataStamp ?? 0) != dataStamp)
                throw new MasterDataRuleException(409, "區域資料已被其他人修改，請重新載入後再儲存。");

            var references = ValidateReferences(connection, transaction, values);
            using (var update = new NpgsqlCommand("""
                UPDATE "YMES".tblsmdareabasis
                SET areaname=@areaName,areatype=@areaType,areaclass=@areaClass,baseareano=@baseAreaNo,
                    factoryno=@factoryNo,departmentno=@departmentNo,description=@description,
                    reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE upper(areano)=upper(@areaNo) AND issuestate=0 AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction))
            {
                AddVarchar(update, "areaNo", areaNo);
                AddVarchar(update, "areaName", values.AreaName);
                AddNumeric(update, "areaType", values.AreaType);
                AddNumeric(update, "areaClass", values.AreaClass);
                AddVarchar(update, "baseAreaNo", references.BaseAreaNo);
                AddVarchar(update, "factoryNo", references.FactoryNo);
                AddVarchar(update, "departmentNo", values.DepartmentNo);
                AddVarchar(update, "description", values.Description);
                AddVarchar(update, "actor", actor.Trim());
                AddNumeric(update, "dataStamp", dataStamp);
                if (update.ExecuteNonQuery() != 1)
                    throw new MasterDataRuleException(409, "區域資料已被其他人修改，請重新載入後再儲存。");
            }

            var updated = GetArea(connection, transaction, areaNo, forUpdate: false)
                ?? throw AreaNotFound(areaNo);
            transaction.Commit();
            return updated;
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new MasterDataRuleException(409, "區域或參照資料已同時變更，請重新載入後再操作。");
        }
    }

    public void Delete(string rawAreaNo, int issueState, long dataStamp, string actor)
    {
        var areaNo = Required(rawAreaNo, "AreaNo", 20).ToUpperInvariant();
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current = GetArea(connection, transaction, areaNo, forUpdate: true)
                ?? throw AreaNotFound(areaNo);
            if (current.IssueState != issueState || (current.DataStamp ?? 0) != dataStamp)
                throw new MasterDataRuleException(409, "區域狀態或版本已變更，請重新載入後再操作。");
            if (current.IssueState == 1)
                throw new MasterDataRuleException(409, "簽核中的區域不可刪除。");
            if (current.IssueState is not (0 or 2 or -1))
                throw new MasterDataRuleException(409, "此區域狀態不可刪除。");

            if (current.IssueState == 2)
            {
                using var retire = new NpgsqlCommand("""
                    UPDATE "YMES".tblsmdareabasis
                    SET issuestate=-1,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                    WHERE upper(areano)=upper(@areaNo) AND issuestate=2 AND COALESCE(datastamp,0)=@dataStamp;
                    """, connection, transaction);
                AddVarchar(retire, "areaNo", areaNo);
                AddVarchar(retire, "actor", actor.Trim());
                AddNumeric(retire, "dataStamp", dataStamp);
                if (retire.ExecuteNonQuery() != 1)
                    throw new MasterDataRuleException(409, "區域資料已被其他人修改，請重新載入後再操作。");
            }
            else
            {
                using (var relationCheck = new NpgsqlCommand("""
                    SELECT EXISTS(
                        SELECT 1 FROM "YMES".tblsmdarearelation
                        WHERE baseareano=@areaNo OR containareano=@areaNo
                           OR (objecttype=0 AND objectno=@areaNo)
                    );
                    """, connection, transaction))
                {
                    AddVarchar(relationCheck, "areaNo", areaNo);
                    if (relationCheck.ExecuteScalar() is true)
                        throw new MasterDataRuleException(409, "此區域仍有配置關聯，請先移除配置後再刪除。");
                }

                using var delete = new NpgsqlCommand("""
                    DELETE FROM "YMES".tblsmdareabasis
                    WHERE upper(areano)=upper(@areaNo) AND issuestate=@issueState
                      AND COALESCE(datastamp,0)=@dataStamp;
                    """, connection, transaction);
                AddVarchar(delete, "areaNo", areaNo);
                AddNumeric(delete, "issueState", issueState);
                AddNumeric(delete, "dataStamp", dataStamp);
                if (delete.ExecuteNonQuery() != 1)
                    throw new MasterDataRuleException(409, "區域資料已被其他人修改，請重新載入後再操作。");
            }

            transaction.Commit();
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new MasterDataRuleException(409, "區域資料或配置已同時變更，請重新載入後再操作。");
        }
    }

    public SmdAreaBasisDto Approve(string rawAreaNo, long dataStamp)
    {
        var areaNo = Required(rawAreaNo, "AreaNo", 20).ToUpperInvariant();
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = GetArea(connection, transaction, areaNo, forUpdate: true)
            ?? throw AreaNotFound(areaNo);
        if (current.IssueState != 0 || (current.DataStamp ?? 0) != dataStamp)
            throw new MasterDataRuleException(409, "只有版本一致的未簽核區域可以送出核准。");

        // AREA BASIS requires a verified Issue/Job provider before production state can change.
        throw new MasterDataRuleException(503, "AREA BASIS 簽核服務尚未接妥；沒有修改正式資料。");
    }

    private static SmdAreaBasisDto? GetArea(NpgsqlConnection connection, NpgsqlTransaction transaction, string areaNo, bool forUpdate)
    {
        var lockClause = forUpdate ? " FOR UPDATE" : "";
        using var command = new NpgsqlCommand($"""
            SELECT {AreaColumns}
            FROM "YMES".tblsmdareabasis
            WHERE upper(areano)=upper(@areaNo)
            {lockClause};
            """, connection, transaction);
        AddVarchar(command, "areaNo", areaNo);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapArea(reader) : null;
    }

    private static SmdAreaBasisDto MapArea(NpgsqlDataReader reader) => new()
    {
        AreaNo = reader.GetString(0),
        AreaName = reader.GetString(1),
        AreaType = reader.GetInt32(2),
        AreaClass = reader.GetInt32(3),
        BaseAreaNo = NullableString(reader, 4),
        FactoryNo = reader.GetString(5),
        DepartmentNo = NullableString(reader, 6),
        Description = NullableString(reader, 7),
        Creator = reader.GetString(8),
        CreateDate = reader.GetDateTime(9),
        IssueState = reader.GetInt32(10),
        AreaCompose = reader.GetInt32(11),
        Reviser = NullableString(reader, 12),
        ReviseDate = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
        DataStamp = reader.IsDBNull(14) ? null : reader.GetInt64(14),
    };

    private static (string AreaNo, string AreaName, int AreaType, int AreaClass, string? BaseAreaNo, string FactoryNo, string DepartmentNo, string? Description, int AreaCompose) ValidateWrite(
        SmdAreaBasisWriteRequest request,
        string actor)
    {
        var areaNo = Required(request.AreaNo, "AreaNo", 20).ToUpperInvariant();
        var areaName = Required(request.AreaName, "AreaName", 50);
        var factoryNo = Required(request.FactoryNo, "FactoryNo", 20);
        var departmentNo = Required(request.DepartmentNo, "DepartmentNo", 20);
        var description = OptionalText(request.Description, "Description", 255);
        ValidateActor(actor);

        if (request.AreaType is not (0 or 1 or 2) || request.AreaClass is not (0 or 1) || request.AreaCompose is not (0 or 1))
            throw new MasterDataRuleException(400, "AreaType、AreaClass 或 AreaCompose 超出來源範圍。");

        var areaClass = request.AreaClass.Value;
        var areaType = request.AreaType.Value;
        var areaCompose = request.AreaCompose.Value;
        var validCombination = (areaClass, areaType, areaCompose) is (1, 2, 0) or (0, 0, 0) or (0, 1, 1) or (0, 2, 0);
        if (!validCombination)
            throw new MasterDataRuleException(400, "區域類型、分類及配置組合不符合 MES 規則。");

        var baseAreaNo = OptionalText(request.BaseAreaNo, "BaseAreaNo", 20);
        if (areaClass == 1 && baseAreaNo is not null)
            throw new MasterDataRuleException(400, "基底區域不可再指定所屬 BaseAreaNo。");
        if (areaClass == 0 && baseAreaNo is null)
            throw new MasterDataRuleException(400, "一般區域必須選擇已簽核的基底區域。");
        if (baseAreaNo is not null && string.Equals(baseAreaNo, areaNo, StringComparison.OrdinalIgnoreCase))
            throw new MasterDataRuleException(400, "區域不可將自己指定為基底區域。");

        return (areaNo, areaName, areaType, areaClass, baseAreaNo, factoryNo, departmentNo, description, areaCompose);
    }

    private static (string? BaseAreaNo, string FactoryNo) ValidateReferences(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        (string AreaNo, string AreaName, int AreaType, int AreaClass, string? BaseAreaNo, string FactoryNo, string DepartmentNo, string? Description, int AreaCompose) values)
    {
        string? baseAreaNo = null;
        var factoryNo = values.FactoryNo;
        if (values.AreaClass == 1)
        {
            using var factory = new NpgsqlCommand("""
                SELECT 1 FROM "YMES".tblsmdfactorybasis
                WHERE upper(factoryno)=upper(@factoryNo) AND issuestate=2;
                """, connection, transaction);
            AddVarchar(factory, "factoryNo", factoryNo);
            if (factory.ExecuteScalar() is null)
                throw new MasterDataRuleException(400, "基底區域必須選擇已簽核的工廠。");
        }
        else
        {
            baseAreaNo = values.BaseAreaNo;
            using var parent = new NpgsqlCommand("""
                SELECT factoryno FROM "YMES".tblsmdareabasis
                WHERE upper(areano)=upper(@baseAreaNo) AND areaclass=1 AND issuestate=2;
                """, connection, transaction);
            AddVarchar(parent, "baseAreaNo", baseAreaNo);
            var inheritedFactory = parent.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(inheritedFactory))
                throw new MasterDataRuleException(400, "一般區域必須屬於已簽核的基底區域。");
            if (!string.Equals(factoryNo, inheritedFactory, StringComparison.OrdinalIgnoreCase))
                throw new MasterDataRuleException(409, "FactoryNo 與所選基底區域不一致，請重新選取。");
            factoryNo = inheritedFactory;
        }

        using var department = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tblusrdepartmentbasis
            WHERE upper(departmentno)=upper(@departmentNo);
            """, connection, transaction);
        AddVarchar(department, "departmentNo", values.DepartmentNo);
        if (department.ExecuteScalar() is null)
            throw new MasterDataRuleException(400, "找不到所選部門。");

        return (baseAreaNo, factoryNo);
    }

    private IReadOnlyList<T> ReadBounded<T>(string sql, Func<NpgsqlDataReader, T> map, string label)
    {
        using var command = dataSource.CreateCommand(sql);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxRows + 1;
        using var reader = command.ExecuteReader();
        var rows = new List<T>();
        while (reader.Read())
        {
            if (rows.Count == MaxRows)
                throw new MasterDataRuleException(413, $"{label}選項超過 {MaxRows} 筆。");
            rows.Add(map(reader));
        }
        return rows;
    }

    private static string Required(string? value, string label, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new MasterDataRuleException(400, $"{label} 為必填。");
        if (text.Length > maxLength)
            throw new MasterDataRuleException(400, $"{label} 不可超過 {maxLength} 個字元。");
        return text;
    }

    private static string? OptionalText(string? value, string label, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > maxLength)
            throw new MasterDataRuleException(400, $"{label} 不可超過 {maxLength} 個字元。");
        return text;
    }

    private static string? OptionalFilter(string? value, string label, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, label, maxLength);

    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10)
            throw new MasterDataRuleException(400, "登入者帳號不存在或超出 Creator 欄位長度。");
    }

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void AddVarchar(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, int value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static void AddNumeric(NpgsqlCommand command, string name, int? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, long value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static bool IsConcurrencyFailure(PostgresException exception) =>
        exception.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected;

    private static MasterDataRuleException AreaNotFound(string areaNo) =>
        new(404, $"找不到區域 {areaNo}。");
}
