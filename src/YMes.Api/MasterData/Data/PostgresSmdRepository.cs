using System.Data;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresSmdRepository(NpgsqlDataSource dataSource)
{
    public IReadOnlyList<SmdComputerAreaDto> ListComputerAreas(string? computerNo, string? factoryNo)
    {
        using var command = dataSource.CreateCommand("""
            SELECT b.computerno,b.factoryno,b.creator,b.createdate,b.reviser,b.revisedate,b.datastamp::bigint,
                   COALESCE(array_agg(d.areano ORDER BY d.areano) FILTER (WHERE d.areano IS NOT NULL), ARRAY[]::varchar[])
            FROM "YMES".tblsmdcomputerareabasis b
            LEFT JOIN "YMES".tblsmdcomputerareadetail d
              ON d.computerno=b.computerno AND d.factoryno=b.factoryno
            WHERE (@computerNo IS NULL OR b.computerno ILIKE '%' || @computerNo || '%')
              AND (@factoryNo IS NULL OR b.factoryno ILIKE '%' || @factoryNo || '%')
            GROUP BY b.computerno,b.factoryno,b.creator,b.createdate,b.reviser,b.revisedate,b.datastamp
            ORDER BY b.computerno,b.factoryno;
            """);
        AddVarchar(command, "computerNo", OptionalFilter(computerNo));
        AddVarchar(command, "factoryNo", OptionalFilter(factoryNo));
        using var reader = command.ExecuteReader();
        var rows = new List<SmdComputerAreaDto>();
        while (reader.Read()) rows.Add(MapComputerArea(reader));
        return rows;
    }

    public SmdAreaBasisOptionsDto AreaBasisOptions()
    {
        using var command = dataSource.CreateCommand("""
            SELECT factoryno,description,factoryname
            FROM "YMES".tblsmdfactorybasis
            WHERE issuestate=2
            ORDER BY factoryno;
            """);
        using var reader = command.ExecuteReader();
        var factories = new List<SmdFactoryOptionDto>();
        while (reader.Read())
            factories.Add(new SmdFactoryOptionDto(reader.GetString(0), NullableString(reader, 1), NullableString(reader, 2)));
        return new SmdAreaBasisOptionsDto(factories);
    }

    public IReadOnlyList<SmdAreaOptionDto> ListAreas(string? factoryNo, int? issueState, int? areaClass)
    {
        using var command = dataSource.CreateCommand("""
            SELECT areano,areaname,description,issuestate::int,areaclass::int,factoryno
            FROM "YMES".tblsmdareabasis
            WHERE (@factoryNo IS NULL OR factoryno=@factoryNo)
              AND (@issueState IS NULL OR issuestate=@issueState)
              AND (@areaClass IS NULL OR areaclass=@areaClass)
            ORDER BY areano;
            """);
        AddVarchar(command, "factoryNo", OptionalFilter(factoryNo));
        AddNumeric(command, "issueState", issueState);
        AddNumeric(command, "areaClass", areaClass);
        using var reader = command.ExecuteReader();
        var rows = new List<SmdAreaOptionDto>();
        while (reader.Read())
            rows.Add(new SmdAreaOptionDto(reader.GetString(0), reader.GetString(1), NullableString(reader, 2), reader.GetInt32(3), reader.GetInt32(4), reader.GetString(5)));
        return rows;
    }

    public SmdComputerAreaDto CreateComputerArea(SmdComputerAreaWriteRequest request, string actor)
    {
        var values = ValidateWrite(request.ComputerNo, request.FactoryNo, request.Areas, actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            ValidateApprovedFactory(connection, transaction, values.FactoryNo);
            ValidateApprovedAreas(connection, transaction, values.FactoryNo, values.Areas);
            InsertComputerArea(connection, transaction, values.ComputerNo, values.FactoryNo, values.Areas, actor);
            transaction.Commit();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, $"過帳電腦 {values.ComputerNo} 與工廠 {values.FactoryNo} 已存在。");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "工廠或區域資料已同時變更，請重新載入後再操作。");
        }
        return GetComputerArea(values.ComputerNo, values.FactoryNo);
    }

    public SmdComputerAreaDto UpdateComputerArea(string computerNo, string factoryNo, long dataStamp, SmdComputerAreaUpdateRequest request, string actor)
    {
        var no = Required(computerNo, "ComputerNo", 20).ToUpperInvariant();
        var factory = Required(factoryNo, "FactoryNo", 20);
        var areas = NormalizeAreas(request.Areas);
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current = GetComputerArea(connection, transaction, no, factory, forUpdate: true);
            if (current is null) throw NotFound(no, factory);
            if ((current.DataStamp ?? 0) != dataStamp)
                throw new EquipmentRuleException(409, "資料已被其他人修改，請重新載入後再儲存。");

            ValidateApprovedFactory(connection, transaction, factory);
            ValidateApprovedAreas(connection, transaction, factory, areas);
            using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblsmdcomputerareadetail WHERE computerno=@computerNo AND factoryno=@factoryNo;", connection, transaction))
            {
                AddVarchar(delete, "computerNo", no);
                AddVarchar(delete, "factoryNo", factory);
                delete.ExecuteNonQuery();
            }
            InsertAreas(connection, transaction, no, factory, areas);
            using (var update = new NpgsqlCommand("""
                UPDATE "YMES".tblsmdcomputerareabasis
                SET reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE computerno=@computerNo AND factoryno=@factoryNo AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction))
            {
                AddVarchar(update, "actor", actor.Trim());
                AddVarchar(update, "computerNo", no);
                AddVarchar(update, "factoryNo", factory);
                AddNumeric(update, "dataStamp", dataStamp);
                if (update.ExecuteNonQuery() == 0)
                    throw new EquipmentRuleException(409, "資料已被其他人修改，請重新載入後再儲存。");
            }
            transaction.Commit();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, "區域明細重複，請重新檢查選取項目。");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "工廠或區域資料已同時變更，請重新載入後再操作。");
        }
        return GetComputerArea(no, factory);
    }

    public void DeleteComputerArea(string computerNo, string factoryNo)
    {
        var no = Required(computerNo, "ComputerNo", 20).ToUpperInvariant();
        var factory = Required(factoryNo, "FactoryNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = GetComputerArea(connection, transaction, no, factory, forUpdate: true);
        if (current is null) throw NotFound(no, factory);
        using (var deleteDetails = new NpgsqlCommand("DELETE FROM \"YMES\".tblsmdcomputerareadetail WHERE computerno=@computerNo AND factoryno=@factoryNo;", connection, transaction))
        {
            AddVarchar(deleteDetails, "computerNo", no);
            AddVarchar(deleteDetails, "factoryNo", factory);
            deleteDetails.ExecuteNonQuery();
        }
        using (var deleteMaster = new NpgsqlCommand("DELETE FROM \"YMES\".tblsmdcomputerareabasis WHERE computerno=@computerNo AND factoryno=@factoryNo;", connection, transaction))
        {
            AddVarchar(deleteMaster, "computerNo", no);
            AddVarchar(deleteMaster, "factoryNo", factory);
            if (deleteMaster.ExecuteNonQuery() == 0) throw NotFound(no, factory);
        }
        transaction.Commit();
    }

    public SmdComputerAreaDto CopyComputerArea(SmdComputerAreaCopyRequest request, string actor)
    {
        var sourceNo = Required(request.FromComputerNo, "來源 ComputerNo", 20).ToUpperInvariant();
        var sourceFactory = Required(request.FromFactoryNo, "來源 FactoryNo", 20);
        var targetNo = Required(request.ToComputerNo, "目標 ComputerNo", 20).ToUpperInvariant();
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var source = GetComputerArea(connection, transaction, sourceNo, sourceFactory, forUpdate: true);
            if (source is null) throw NotFound(sourceNo, sourceFactory);
            ValidateApprovedFactory(connection, transaction, sourceFactory);
            ValidateApprovedAreas(connection, transaction, sourceFactory, source.Areas);
            InsertComputerArea(connection, transaction, targetNo, sourceFactory, source.Areas, actor);
            transaction.Commit();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, $"過帳電腦 {targetNo} 與工廠 {sourceFactory} 已存在。");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "來源或區域資料已同時變更，請重新載入後再操作。");
        }
        return GetComputerArea(targetNo, sourceFactory);
    }

    private SmdComputerAreaDto GetComputerArea(string computerNo, string factoryNo)
    {
        using var command = dataSource.CreateCommand("""
            SELECT b.computerno,b.factoryno,b.creator,b.createdate,b.reviser,b.revisedate,b.datastamp::bigint,
                   COALESCE(array_agg(d.areano ORDER BY d.areano) FILTER (WHERE d.areano IS NOT NULL), ARRAY[]::varchar[])
            FROM "YMES".tblsmdcomputerareabasis b
            LEFT JOIN "YMES".tblsmdcomputerareadetail d
              ON d.computerno=b.computerno AND d.factoryno=b.factoryno
            WHERE b.computerno=@computerNo AND b.factoryno=@factoryNo
            GROUP BY b.computerno,b.factoryno,b.creator,b.createdate,b.reviser,b.revisedate,b.datastamp;
            """);
        AddVarchar(command, "computerNo", computerNo);
        AddVarchar(command, "factoryNo", factoryNo);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapComputerArea(reader) : throw NotFound(computerNo, factoryNo);
    }

    private static SmdComputerAreaDto? GetComputerArea(NpgsqlConnection connection, NpgsqlTransaction transaction, string computerNo, string factoryNo, bool forUpdate)
    {
        var suffix = forUpdate ? " FOR UPDATE OF b" : "";
        using var command = new NpgsqlCommand($"""
            SELECT b.computerno,b.factoryno,b.creator,b.createdate,b.reviser,b.revisedate,b.datastamp::bigint
            FROM "YMES".tblsmdcomputerareabasis b
            WHERE b.computerno=@computerNo AND b.factoryno=@factoryNo
            {suffix};
            """, connection, transaction);
        AddVarchar(command, "computerNo", computerNo);
        AddVarchar(command, "factoryNo", factoryNo);
        string? creator;
        DateTime? createDate;
        string? reviser;
        DateTime? reviseDate;
        long? dataStamp;
        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read()) return null;
            creator = NullableString(reader, 2);
            createDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
            reviser = NullableString(reader, 4);
            reviseDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
            dataStamp = reader.IsDBNull(6) ? null : reader.GetInt64(6);
        }

        using var areasCommand = new NpgsqlCommand("SELECT areano FROM \"YMES\".tblsmdcomputerareadetail WHERE computerno=@computerNo AND factoryno=@factoryNo ORDER BY areano;", connection, transaction);
        AddVarchar(areasCommand, "computerNo", computerNo);
        AddVarchar(areasCommand, "factoryNo", factoryNo);
        using var areasReader = areasCommand.ExecuteReader();
        var areas = new List<string>();
        while (areasReader.Read()) areas.Add(areasReader.GetString(0));
        return new SmdComputerAreaDto
        {
            ComputerNo = computerNo,
            FactoryNo = factoryNo,
            Creator = creator,
            CreateDate = createDate,
            Reviser = reviser,
            ReviseDate = reviseDate,
            DataStamp = dataStamp,
            Areas = areas,
        };
    }

    private static SmdComputerAreaDto MapComputerArea(NpgsqlDataReader reader) => new()
    {
        ComputerNo = reader.GetString(0),
        FactoryNo = reader.GetString(1),
        Creator = NullableString(reader, 2),
        CreateDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
        Reviser = NullableString(reader, 4),
        ReviseDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
        DataStamp = reader.IsDBNull(6) ? null : reader.GetInt64(6),
        Areas = reader.GetFieldValue<string[]>(7),
    };

    private static void ValidateApprovedFactory(NpgsqlConnection connection, NpgsqlTransaction transaction, string factoryNo)
    {
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblsmdfactorybasis WHERE factoryno=@factoryNo AND issuestate=2 FOR SHARE;", connection, transaction);
        AddVarchar(command, "factoryNo", factoryNo);
        if (command.ExecuteScalar() is null)
            throw new EquipmentRuleException(400, "FactoryNo 必須是已簽核工廠。");
    }

    private static void ValidateApprovedAreas(NpgsqlConnection connection, NpgsqlTransaction transaction, string factoryNo, IReadOnlyList<string> areas)
    {
        if (areas.Count == 0) throw new EquipmentRuleException(400, "至少要選取一個 AreaNo。");
        using var command = new NpgsqlCommand("""
            SELECT areano FROM "YMES".tblsmdareabasis
            WHERE factoryno=@factoryNo AND issuestate=2 AND areaclass=0 AND areano=ANY(@areas)
            FOR SHARE;
            """, connection, transaction);
        AddVarchar(command, "factoryNo", factoryNo);
        command.Parameters.Add("areas", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = areas.ToArray();
        using var reader = command.ExecuteReader();
        var found = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read()) found.Add(reader.GetString(0));
        var missing = areas.Where(area => !found.Contains(area)).ToArray();
        if (missing.Length > 0)
            throw new EquipmentRuleException(400, $"區域 {string.Join(", ", missing)} 不屬於此工廠下已簽核的非 Base Area。");
    }

    private static void InsertComputerArea(NpgsqlConnection connection, NpgsqlTransaction transaction, string computerNo, string factoryNo, IReadOnlyList<string> areas, string actor)
    {
        using (var insert = new NpgsqlCommand("""
            INSERT INTO "YMES".tblsmdcomputerareabasis(computerno,factoryno,creator,createdate,datastamp)
            VALUES(@computerNo,@factoryNo,@actor,now(),0);
            """, connection, transaction))
        {
            AddVarchar(insert, "computerNo", computerNo);
            AddVarchar(insert, "factoryNo", factoryNo);
            AddVarchar(insert, "actor", actor.Trim());
            insert.ExecuteNonQuery();
        }
        InsertAreas(connection, transaction, computerNo, factoryNo, areas);
    }

    private static void InsertAreas(NpgsqlConnection connection, NpgsqlTransaction transaction, string computerNo, string factoryNo, IReadOnlyList<string> areas)
    {
        foreach (var area in areas)
        {
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblsmdcomputerareadetail(computerno,factoryno,areano) VALUES(@computerNo,@factoryNo,@areaNo);", connection, transaction);
            AddVarchar(insert, "computerNo", computerNo);
            AddVarchar(insert, "factoryNo", factoryNo);
            AddVarchar(insert, "areaNo", area);
            insert.ExecuteNonQuery();
        }
    }

    private static (string ComputerNo, string FactoryNo, IReadOnlyList<string> Areas) ValidateWrite(string computerNo, string factoryNo, IReadOnlyList<string>? areas, string actor)
    {
        var no = Required(computerNo, "ComputerNo", 20).ToUpperInvariant();
        var factory = Required(factoryNo, "FactoryNo", 20);
        ValidateActor(actor);
        return (no, factory, NormalizeAreas(areas));
    }

    private static IReadOnlyList<string> NormalizeAreas(IReadOnlyList<string>? areas)
    {
        if (areas is null || areas.Count == 0) throw new EquipmentRuleException(400, "至少要選取一個 AreaNo。");
        var normalized = areas.Select(area => Required(area, "AreaNo", 20)).ToArray();
        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
            throw new EquipmentRuleException(400, "AreaNo 不可重複。");
        return normalized;
    }

    private static string Required(string? value, string label, int max)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text)) throw new EquipmentRuleException(400, $"{label} 為必填。");
        if (text.Length > max) throw new EquipmentRuleException(400, $"{label} 不可超過 {max} 個字元。");
        return text;
    }

    private static string? OptionalFilter(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NullableString(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static void AddVarchar(NpgsqlCommand command, string name, string? value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void AddNumeric(NpgsqlCommand command, string name, long value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;
    private static void AddNumeric(NpgsqlCommand command, string name, int? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10)
            throw new EquipmentRuleException(400, "登入者帳號不存在或超出 Creator 欄位長度。");
    }
    private static bool IsConcurrencyFailure(PostgresException ex) => ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected;
    private static EquipmentRuleException NotFound(string computerNo, string factoryNo) => new(404, $"找不到過帳電腦 {computerNo}／工廠 {factoryNo}。");
}
