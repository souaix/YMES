using System.Data;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresSmdRepository(NpgsqlDataSource dataSource)
{
    private const string PdLineColumns = "pdlineno,pdlinename,pdlinestatus::int,issuestate::int,description,creator,createdate,reviser,revisedate,datastamp::bigint";

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

    public IReadOnlyList<SmdPdLineBasisDto> ListPdLineBasis(string? pdLineNo)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {PdLineColumns}
            FROM "YMES".tblsmdpdlinebasis
            WHERE (@pdLineNo IS NULL OR pdlineno ILIKE '%' || @pdLineNo || '%')
            ORDER BY pdlineno;
            """);
        AddVarchar(command, "pdLineNo", OptionalFilter(pdLineNo));
        using var reader = command.ExecuteReader();
        var rows = new List<SmdPdLineBasisDto>();
        while (reader.Read()) rows.Add(MapPdLine(reader));
        return rows;
    }

    public SmdPdLineBasisOptionsDto PdLineBasisOptions()
    {
        // The official TBLSYSVALUELIST labels have not yet been handed off by
        // the Oracle source PC. Do not return guessed labels as Production data.
        throw new EquipmentRuleException(503, "生產線狀態正式選項尚未由 MES 來源端交付，請完成 ValueList 核對後再使用此選項 API。");
    }

    public SmdPdLineBasisDto CreatePdLineBasis(SmdPdLineBasisWriteRequest request, string actor)
    {
        var values = ValidatePdLineBasis(request, actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblsmdpdlinebasis
                    (pdlineno,pdlinename,pdlinestatus,issuestate,description,creator,createdate,datastamp)
                VALUES(@pdLineNo,@pdLineName,@pdLineStatus,0,@description,@actor,now(),0);
                """, connection, transaction);
            AddVarchar(insert, "pdLineNo", values.PdLineNo);
            AddVarchar(insert, "pdLineName", values.PdLineName);
            AddNumeric(insert, "pdLineStatus", (int?)values.PdLineStatus);
            AddVarchar(insert, "description", values.Description);
            AddVarchar(insert, "actor", actor.Trim());
            insert.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, $"生產線別編號 {values.PdLineNo} 已存在。");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "生產線別資料已同時變更，請重新載入後再操作。");
        }
        return GetPdLineBasis(values.PdLineNo);
    }

    public SmdPdLineBasisDto UpdatePdLineBasis(string pdLineNo, int issueState, long dataStamp, SmdPdLineBasisWriteRequest request, string actor)
    {
        var no = Required(pdLineNo, "PDLineNo", 50).ToUpperInvariant();
        if (!string.Equals(no, request.PdLineNo?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new EquipmentRuleException(400, "網址與資料內容的 PDLineNo 必須相同。");
        if (issueState != 0) throw new EquipmentRuleException(409, "只有未簽核的生產線別可以修改。");
        var values = ValidatePdLineBasis(request, actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current = GetPdLineBasis(connection, transaction, no, forUpdate: true);
            if (current is null) throw PdLineNotFound(no);
            EnsurePdLineVersion(current, issueState, dataStamp);
            using var update = new NpgsqlCommand("""
                UPDATE "YMES".tblsmdpdlinebasis
                SET pdlinename=@pdLineName,pdlinestatus=@pdLineStatus,
                    description=COALESCE(@description,description),
                    reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE upper(pdlineno)=upper(@pdLineNo) AND issuestate=0 AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction);
            AddVarchar(update, "pdLineNo", no);
            AddVarchar(update, "pdLineName", values.PdLineName);
            AddNumeric(update, "pdLineStatus", (int?)values.PdLineStatus);
            AddVarchar(update, "description", values.Description);
            AddVarchar(update, "actor", actor.Trim());
            AddNumeric(update, "dataStamp", dataStamp);
            if (update.ExecuteNonQuery() == 0)
                throw new EquipmentRuleException(409, "生產線別資料已被其他人修改，請重新載入後再儲存。");
            transaction.Commit();
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new EquipmentRuleException(409, "生產線別資料已同時變更，請重新載入後再操作。");
        }
        return GetPdLineBasis(no);
    }

    public void DeletePdLineBasis(string pdLineNo, int issueState, long dataStamp, string actor)
    {
        var no = Required(pdLineNo, "PDLineNo", 50).ToUpperInvariant();
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = GetPdLineBasis(connection, transaction, no, forUpdate: true);
        if (current is null) throw PdLineNotFound(no);
        EnsurePdLineVersion(current, issueState, dataStamp);
        if (current.IssueState == 2)
        {
            using var retire = new NpgsqlCommand("""
                UPDATE "YMES".tblsmdpdlinebasis
                SET issuestate=-1,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE upper(pdlineno)=upper(@pdLineNo) AND issuestate=2 AND COALESCE(datastamp,0)=@dataStamp;
                """, connection, transaction);
            AddVarchar(retire, "pdLineNo", no);
            AddVarchar(retire, "actor", actor.Trim());
            AddNumeric(retire, "dataStamp", dataStamp);
            if (retire.ExecuteNonQuery() == 0)
                throw new EquipmentRuleException(409, "生產線別資料已被其他人修改，請重新載入後再操作。");
        }
        else if (current.IssueState is 0 or 1 or -1)
        {
            using var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblsmdpdlinebasis WHERE upper(pdlineno)=upper(@pdLineNo) AND issuestate=@issueState AND COALESCE(datastamp,0)=@dataStamp;", connection, transaction);
            AddVarchar(delete, "pdLineNo", no);
            AddNumeric(delete, "issueState", (int?)current.IssueState);
            AddNumeric(delete, "dataStamp", dataStamp);
            if (delete.ExecuteNonQuery() == 0)
                throw new EquipmentRuleException(409, "生產線別資料已被其他人修改，請重新載入後再操作。");
        }
        else throw new EquipmentRuleException(409, "目前簽核狀態不允許刪除。");
        transaction.Commit();
    }

    public SmdPdLineBasisDto ApprovePdLineBasis(string pdLineNo, int issueState, long dataStamp, string actor)
    {
        _ = Required(pdLineNo, "PDLineNo", 50);
        _ = actor;
        if (issueState != 0) throw new EquipmentRuleException(409, "只有未簽核的生產線別可以核准。");
        _ = dataStamp;
        throw new EquipmentRuleException(503, "YMES 共用 Issue/Job 簽核表與流程尚未建立；目前停用 Production 核准，避免直接變更簽核狀態。");
    }

    private SmdPdLineBasisDto GetPdLineBasis(string pdLineNo)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {PdLineColumns}
            FROM "YMES".tblsmdpdlinebasis
            WHERE upper(pdlineno)=upper(@pdLineNo)
            LIMIT 1;
            """);
        AddVarchar(command, "pdLineNo", pdLineNo.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapPdLine(reader) : throw PdLineNotFound(pdLineNo);
    }

    private static SmdPdLineBasisDto? GetPdLineBasis(NpgsqlConnection connection, NpgsqlTransaction transaction, string pdLineNo, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"""
            SELECT {PdLineColumns}
            FROM "YMES".tblsmdpdlinebasis
            WHERE upper(pdlineno)=upper(@pdLineNo)
            LIMIT 1{(forUpdate ? " FOR UPDATE" : "")};
            """, connection, transaction);
        AddVarchar(command, "pdLineNo", pdLineNo.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapPdLine(reader) : null;
    }

    private static SmdPdLineBasisDto MapPdLine(NpgsqlDataReader reader) => new()
    {
        PdLineNo = reader.GetString(0),
        PdLineName = reader.GetString(1),
        PdLineStatus = reader.GetInt32(2),
        IssueState = reader.GetInt32(3),
        Description = NullableString(reader, 4),
        Creator = NullableString(reader, 5),
        CreateDate = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        Reviser = NullableString(reader, 7),
        ReviseDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
        DataStamp = reader.IsDBNull(9) ? null : reader.GetInt64(9),
    };

    private static (string PdLineNo, string PdLineName, int PdLineStatus, string? Description) ValidatePdLineBasis(SmdPdLineBasisWriteRequest request, string actor)
    {
        var no = Required(request.PdLineNo, "PDLineNo", 50).ToUpperInvariant();
        var name = Required(request.PdLineName, "PDLineName", 50);
        ValidateActor(actor);
        if (request.PdLineStatus is not (0 or 1 or 2))
            throw new EquipmentRuleException(400, "PDLineStatus 僅接受 0、1、2。");
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : Required(request.Description, "Description", 255);
        return (no, name, request.PdLineStatus.Value, description);
    }

    private static void EnsurePdLineVersion(SmdPdLineBasisDto current, int issueState, long dataStamp)
    {
        if (current.IssueState != issueState || (current.DataStamp ?? 0) != dataStamp)
            throw new EquipmentRuleException(409, "生產線別狀態或資料版本已變更，請重新載入後再操作。");
    }

    private static EquipmentRuleException PdLineNotFound(string pdLineNo) => new(404, $"找不到生產線別 {pdLineNo}。");

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
