using System.Globalization;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresOperationRepository(NpgsqlDataSource dataSource) : IOperationRepository
{
    private const string SelectColumns = """
        opno,
        opname,
        optype,
        opclass::numeric,
        COALESCE(opshortname, ''),
        COALESCE(oporder, 0)::numeric,
        COALESCE(description, ''),
        COALESCE(psno, ''),
        COALESCE(materialoption, 0)::numeric,
        COALESCE(multioperatormode, 0)::numeric,
        COALESCE(printoutonruncard, 0) = 1,
        COALESCE(spcinspabnormalholdlot, 0) = 1,
        0::numeric,
        stdunitruntime,
        countopunitqty::numeric,
        stdqueuetime,
        issuestate::numeric,
        COALESCE(datastamp, 0)::numeric,
        createdate
        """;

    public IReadOnlyList<OperationDto> List(string? keyword, int? issueState)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {SelectColumns}
            FROM "YMES".tblopbasis
            WHERE (@issueState IS NULL OR issuestate = @issueState)
              AND (@keyword IS NULL OR opno ILIKE @keyword OR opname ILIKE @keyword)
            ORDER BY opno;
            """);
        AddNumeric(command, "issueState", issueState);
        AddVarchar(command, "keyword", string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%");

        using var reader = command.ExecuteReader();
        var items = new List<OperationDto>();
        while (reader.Read()) items.Add(Map(reader));
        return items;
    }

    public OperationDto Get(string operationNo)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {SelectColumns}
            FROM "YMES".tblopbasis
            WHERE lower(opno) = lower(@operationNo)
            LIMIT 1;
            """);
        AddVarchar(command, "operationNo", operationNo.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : throw NotFound(operationNo);
    }

    public OperationDto Create(OperationWriteRequest request, string actorUserNo)
    {
        ValidateRequest(request);
        ValidateActor(actorUserNo);
        var no=RequiredCode(request.OperationNo,"作業編號",20);
        if(no.Contains(',') || no=="LOTCREATE") throw new MasterDataRuleException(400,"作業編號不可含逗號或使用LOTCREATE。");
        using var connection=dataSource.OpenConnection();
        using var transaction=connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        YMes.Api.Operations.Op103IdentifierGuard.EnsureNewOperation(connection,transaction,no);
        var operationType=YMes.Api.Operations.Op103IdentifierGuard.LockExistingType(connection,transaction,request.OperationType);
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblopbasis
                (opno, opname, optype, opclass, opshortname, oporder, description,
                 creator, createdate, issuestate, psno, printoutonruncard,
                 stdunitruntime, countopunitqty, stdqueuetime, materialoption,
                 multioperatormode, spcinspabnormalholdlot, osoption, datastamp)
            VALUES
                (@operationNo, @operationName, @operationType, @operationClass, @shortName, @operationOrder, @description,
                 @actor, now(), 0, @productionSegmentNo, @printOutOnRunCard,
                 @standardUnitRunTime, @countOperationUnitQty, @standardQueueTime, @materialOption,
                 @multiOperatorMode, @spcInspAbnormalHoldLot, 0, 1);
            """,connection,transaction);
        AddWriteParameters(command, request, actorUserNo,operationType);
        ExecuteInsert(command,no);
        var result=GetLockedForWrite(connection,transaction,no);
        transaction.Commit();return result;
    }

    public OperationDto Update(string operationNo, OperationWriteRequest request, string actorUserNo)
    {
        ValidateRequest(request,validateType:false);
        ValidateActor(actorUserNo);
        using var connection=dataSource.OpenConnection();
        using var transaction=connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        var current = GetLockedForWrite(connection,transaction,operationNo);
        EnsureEditable(current, request.DataStamp);
        if (!Same(current.OperationNo, request.OperationNo))
            throw new MasterDataRuleException(400, "更新時不可變更作業編號。");
        // The raw type is read independently, never inferred from display DTO
        // defaults. Unchanged legacy NULL/blank/case/inactive/missing stays raw.
        string? rawType;
        using(var raw=new NpgsqlCommand("SELECT optype FROM \"YMES\".tblopbasis WHERE opno=@no;",connection,transaction))
        {
            AddVarchar(raw,"no",current.OperationNo);
            var value=raw.ExecuteScalar();
            rawType=value is null || value is DBNull ? null : (string)value;
        }
        var operationType=YMes.Api.Operations.Op103IdentifierGuard.ResolveForUpdate(connection,transaction,request.OperationType,rawType);

        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tblopbasis
            SET opname = @operationName,
                optype = @operationType,
                opclass = @operationClass,
                opshortname = @shortName,
                oporder = @operationOrder,
                description = @description,
                psno = @productionSegmentNo,
                printoutonruncard = @printOutOnRunCard,
                stdunitruntime = @standardUnitRunTime,
                countopunitqty = @countOperationUnitQty,
                stdqueuetime = @standardQueueTime,
                materialoption = @materialOption,
                multioperatormode = @multiOperatorMode,
                spcinspabnormalholdlot = @spcInspAbnormalHoldLot,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE opno = @operationNo
              AND COALESCE(datastamp, 0) = @dataStamp
              AND issuestate = 0;
            """,connection,transaction);
        AddWriteParameters(command, request, actorUserNo,operationType);
        command.Parameters["operationNo"].Value=current.OperationNo;
        AddNumeric(command, "dataStamp", request.DataStamp);
        ExecuteOptimisticUpdate(command, "作業資料已變更，請重新載入後再儲存。");
        var result=GetLockedForWrite(connection,transaction,current.OperationNo);
        transaction.Commit();return result;
    }

    public OperationDto Approve(string operationNo, long dataStamp, string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var command = dataSource.CreateCommand("""
            UPDATE "YMES".tblopbasis
            SET issuestate = 2,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE lower(opno) = lower(@operationNo)
              AND COALESCE(datastamp, 0) = @dataStamp
              AND issuestate = 0;
            """);
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "operationNo", operationNo.Trim());
        AddNumeric(command, "dataStamp", dataStamp);
        ExecuteOptimisticUpdate(command, "只有未簽核且資料未變更的作業可以核准。");
        return Get(operationNo);
    }

    public void DeleteOrRetire(string operationNo, int issueState, long dataStamp, string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = Get(connection, transaction, operationNo, forUpdate: true);
        EnsureStamp(current, dataStamp);
        EnsureIssueState(current, issueState);
        if (current.IssueState == 1)
            throw new MasterDataRuleException(409, "簽核中的作業不可刪除。");

        if (current.IssueState == 2)
        {
            using var retire = new NpgsqlCommand("""
                UPDATE "YMES".tblopbasis
                SET issuestate = -1,
                    datastamp = COALESCE(datastamp, 0) + 1,
                    reviser = @actor,
                    revisedate = now()
                WHERE lower(opno) = lower(@operationNo)
                  AND COALESCE(datastamp, 0) = @dataStamp
                  AND issuestate = 2;
                """, connection, transaction);
            AddVarchar(retire, "actor", actorUserNo.Trim());
            AddVarchar(retire, "operationNo", current.OperationNo);
            AddNumeric(retire, "dataStamp", dataStamp);
            ExecuteOptimisticUpdate(retire, "作業狀態已變更，請重新載入後再操作。");
        }
        else
        {
            if (HasDependents(connection, transaction, current.OperationNo))
                throw new MasterDataRuleException(409, "作業仍被途程、配方、設備或作業群組引用，不能物理刪除。");
            try
            {
                using var delete = new NpgsqlCommand("""
                    DELETE FROM "YMES".tblopbasis
                    WHERE lower(opno) = lower(@operationNo)
                      AND COALESCE(datastamp, 0) = @dataStamp
                      AND issuestate = @issueState;
                    """, connection, transaction);
                AddVarchar(delete, "operationNo", current.OperationNo);
                AddNumeric(delete, "dataStamp", dataStamp);
                AddNumeric(delete, "issueState", issueState);
                ExecuteOptimisticUpdate(delete, "作業資料已變更，請重新載入後再操作。");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                throw new MasterDataRuleException(409, "作業仍被其他主檔引用，不能刪除。");
            }
        }

        transaction.Commit();
    }

    private static OperationDto Get(NpgsqlConnection connection, NpgsqlTransaction transaction, string operationNo, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"""
            SELECT {SelectColumns}
            FROM "YMES".tblopbasis
            WHERE lower(opno) = lower(@operationNo)
            LIMIT 1{(forUpdate ? " FOR UPDATE" : "")};
            """, connection, transaction);
        AddVarchar(command, "operationNo", operationNo.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : throw NotFound(operationNo);
    }

    private static OperationDto GetLockedForWrite(NpgsqlConnection connection,NpgsqlTransaction transaction,string operationNo)
    {
        using var command=new NpgsqlCommand($"SELECT {SelectColumns} FROM \"YMES\".tblopbasis WHERE lower(btrim(opno))=lower(@no) ORDER BY opno FOR UPDATE;",connection,transaction);
        AddVarchar(command,"no",RequiredCode(operationNo,"作業編號",20));
        using var reader=command.ExecuteReader();
        if(!reader.Read())throw NotFound(operationNo);
        var result=Map(reader);
        if(reader.Read())throw new MasterDataRuleException(409,"作業編號存在大小寫／空白歧義，請先整理主檔。");
        return result;
    }

    private static bool HasDependents(NpgsqlConnection connection, NpgsqlTransaction transaction, string operationNo)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM "YMES".tblsysflownodebasis WHERE lower(opno) = lower(@operationNo))
                OR EXISTS (SELECT 1 FROM "YMES".tbleqplocatordetail WHERE lower(opno) = lower(@operationNo))
                OR EXISTS (SELECT 1 FROM "YMES".tblprdrecipedetail WHERE lower(opno) = lower(@operationNo))
                OR EXISTS (SELECT 1 FROM "YMES".tblprdesopbasis WHERE lower(opno) = lower(@operationNo))
                OR EXISTS (SELECT 1 FROM "YMES".tblopgroupdetail WHERE lower(btrim(opno)) = lower(btrim(@operationNo)))
                OR EXISTS (SELECT 1 FROM "YMES".tblprsopapsdetail WHERE lower(opno) = lower(@operationNo))
                OR EXISTS (SELECT 1 FROM "YMES".tblprdprsopapsbasedetail WHERE lower(opno) = lower(@operationNo))
                OR EXISTS (SELECT 1 FROM "YMES".tblprdprsopapssetupdetail WHERE lower(opno) = lower(@operationNo));
            """, connection, transaction);
        AddVarchar(command, "operationNo", operationNo);
        return (bool)(command.ExecuteScalar() ?? false) || HasEcnGroupDependents(connection, transaction, operationNo);
    }

    private static bool HasEcnGroupDependents(NpgsqlConnection connection, NpgsqlTransaction transaction, string operationNo)
    {
        // Caller already holds OP UPDATE. Keep this dependency check MVCC-only;
        // taking a group/global-OP lock here would reverse the ECN writer order.
        try
        {
            using var catalog = new NpgsqlCommand("""
                SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c
                  JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                  WHERE n.nspname='YMES' AND c.relname='tblopecngroupdetail');
                """, connection, transaction);
            if (catalog.ExecuteScalar() is not true) return false;
            using var reference = new NpgsqlCommand("""
                SELECT EXISTS(SELECT 1 FROM "YMES".tblopecngroupdetail
                  WHERE lower(btrim(opno))=lower(btrim(@operationNo)));
                """, connection, transaction);
            AddVarchar(reference, "operationNo", operationNo);
            return reference.ExecuteScalar() is true;
        }
        catch (NpgsqlException)
        {
            // Missing permission/schema on an existing source is not no-reference.
            // Disposing the outer transaction rolls back; do not leak SQL details.
            throw new MasterDataRuleException(503, "無法確認 ECN 作業站群組引用，本次未刪除；請檢查來源結構與必要讀取權限。");
        }
    }

    private static OperationDto Map(NpgsqlDataReader reader) => new()
    {
        OperationNo = reader.GetString(0),
        OperationName = reader.GetString(1),
        OperationType = reader.IsDBNull(2) ? null : reader.GetString(2),
        OperationClass = ToInt32(reader.GetValue(3)),
        ShortName = reader.GetString(4),
        OperationOrder = ToInt32(reader.GetValue(5)),
        Description = reader.GetString(6),
        ProductionSegmentNo = reader.GetString(7),
        MaterialOption = ToInt32(reader.GetValue(8)),
        MultiOperatorMode = ToInt32(reader.GetValue(9)),
        PrintOutOnRunCard = reader.GetBoolean(10),
        SpcInspAbnormalHoldLot = reader.GetBoolean(11),
        ErrorQtyToSpc = 0,
        StandardUnitRunTime = reader.GetDecimal(13),
        CountOperationUnitQty = ToInt32(reader.GetValue(14)),
        StandardQueueTime = reader.GetDecimal(15),
        IssueState = ToInt32(reader.GetValue(16)),
        DataStamp = Convert.ToInt64(reader.GetDecimal(17), CultureInfo.InvariantCulture),
        CreatedAt = reader.IsDBNull(18) ? null : reader.GetDateTime(18)
    };

    private static void AddWriteParameters(NpgsqlCommand command, OperationWriteRequest request, string actorUserNo,string? validatedType)
    {
        AddVarchar(command, "operationNo", RequiredCode(request.OperationNo, "作業編號", 20));
        AddVarchar(command, "operationName", Required(request.OperationName, "作業名稱", 200));
        AddVarchar(command, "operationType", validatedType);
        AddNumeric(command, "operationClass", request.OperationClass);
        AddVarchar(command, "shortName", Optional(request.ShortName, 50));
        AddNumeric(command, "operationOrder", request.OperationOrder);
        AddVarchar(command, "description", Optional(request.Description, 255));
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "productionSegmentNo", RequiredCode(request.ProductionSegmentNo, "製程段", 50));
        AddNumeric(command, "printOutOnRunCard", request.PrintOutOnRunCard ? 1 : 0);
        AddNumeric(command, "standardUnitRunTime", request.StandardUnitRunTime);
        AddNumeric(command, "countOperationUnitQty", request.CountOperationUnitQty);
        AddNumeric(command, "standardQueueTime", request.StandardQueueTime);
        AddNumeric(command, "materialOption", request.MaterialOption);
        AddNumeric(command, "multiOperatorMode", request.MultiOperatorMode);
        AddNumeric(command, "spcInspAbnormalHoldLot", request.SpcInspAbnormalHoldLot ? 1 : 0);
    }

    private static void ValidateRequest(OperationWriteRequest request,bool validateType=true)
    {
        _ = RequiredCode(request.OperationNo, "作業編號", 20);
        _ = Required(request.OperationName, "作業名稱", 200);
        if(validateType) _ = RequiredCode(request.OperationType, "作業類別", 20);
        _ = RequiredCode(request.ProductionSegmentNo, "製程段", 50);
        _ = Optional(request.ShortName, 50);
        _ = Optional(request.Description, 255);
        if (request.OperationClass is < 0 or > 9 || request.MaterialOption is < 0 or > 9 || request.MultiOperatorMode is < 0 or > 9)
            throw new MasterDataRuleException(400, "作業分類、物料選項與多人作業模式必須介於 0 到 9。");
        if (request.OperationOrder is < 0 or > 999999)
            throw new MasterDataRuleException(400, "工序順序必須介於 0 到 999999。");
        if (!FitsNumeric6Scale2(request.StandardUnitRunTime) || !FitsNumeric6Scale2(request.StandardQueueTime))
            throw new MasterDataRuleException(400, "標準工時與等待時間須介於 0 到 9999.99，最多兩位小數。");
        if (request.CountOperationUnitQty is < 1 or > 999999)
            throw new MasterDataRuleException(400, "單位批量必須介於 1 到 999999。");
        if (request.ErrorQtyToSpc != 0)
            throw new MasterDataRuleException(400, "tblopbasis 沒有 SPC 數量門檻欄位，請勿輸入此值。");
    }

    private static bool FitsNumeric6Scale2(decimal value) =>
        value is >= 0 and <= 9999.99m && decimal.Truncate(value * 100) == value * 100;

    private static string RequiredCode(string? value, string field, int maxLength) =>
        NormalizeCode(Required(value, field, maxLength));

    private static string Required(string? value, string field, int maxLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0) throw new MasterDataRuleException(400, $"{field}不可空白。");
        if (text.Length > maxLength) throw new MasterDataRuleException(400, $"{field}不可超過 {maxLength} 個字元。");
        return text;
    }

    private static string? Optional(string? value, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > maxLength) throw new MasterDataRuleException(400, $"欄位不可超過 {maxLength} 個字元。");
        return text;
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static bool Same(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void ValidateActor(string actorUserNo)
    {
        if (string.IsNullOrWhiteSpace(actorUserNo) || actorUserNo.Trim().Length > 10)
            throw new MasterDataRuleException(400, "登入使用者編號長度不符合作業資料欄位。");
    }

    private static void EnsureEditable(OperationDto item, long dataStamp)
    {
        EnsureStamp(item, dataStamp);
        if (item.IssueState != 0) throw new MasterDataRuleException(409, "只有未簽核作業可以編輯。");
    }

    private static void EnsureStamp(OperationDto item, long dataStamp)
    {
        if (item.DataStamp != dataStamp) throw new MasterDataRuleException(409, "資料已由其他人更新，請重新載入後再操作。");
    }

    private static void EnsureIssueState(OperationDto item, int issueState)
    {
        if (item.IssueState != issueState) throw new MasterDataRuleException(409, "作業狀態已變更，請重新載入後再操作。");
    }

    private static int ToInt32(object value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static void ExecuteInsert(NpgsqlCommand command, string operationNo)
    {
        try { command.ExecuteNonQuery(); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new MasterDataRuleException(409, $"作業編號 {operationNo} 已存在。");
        }
    }

    private static void ExecuteOptimisticUpdate(NpgsqlCommand command, string message)
    {
        if (command.ExecuteNonQuery() == 0) throw new MasterDataRuleException(409, message);
    }

    private static void AddVarchar(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, int? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static MasterDataRuleException NotFound(string operationNo) =>
        new(404, $"找不到作業 {operationNo}。");
}
