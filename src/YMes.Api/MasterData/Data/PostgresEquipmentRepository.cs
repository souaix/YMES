using System.Globalization;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresEquipmentRepository(NpgsqlDataSource dataSource) : IEquipmentRepository
{
    private const string SelectColumns = """
        equipmentno,
        equipmenttype,
        COALESCE(equipmentname, ''),
        COALESCE(engineergroupno, ''),
        vendorno,
        COALESCE(modelno, ''),
        COALESCE(assetno, ''),
        COALESCE(equipmentclass, ''),
        COALESCE(equipmentclassno::text, ''),
        COALESCE(description, ''),
        COALESCE(capacity, -1)::numeric,
        COALESCE(loadport, -1)::numeric,
        COALESCE(autoflag, 0)::numeric,
        COALESCE(eacontroller, 'N/A'),
        COALESCE(eqprecipe, 0)::numeric,
        COALESCE(qclistno, ''),
        maxtime,
        fixeqptime,
        vareqptime,
        COALESCE(countopunitqty, 1)::numeric,
        COALESCE(counteqpunitqty, 1)::numeric,
        COALESCE(queueserver, ''),
        COALESCE(requestmsgqueue, ''),
        COALESCE(responsemsgqueue, ''),
        COALESCE(requestmsgqueue_mes, ''),
        COALESCE(responsemsgqueue_eap, ''),
        COALESCE(chamberflag, 0) = 1,
        belongequipmentno,
        issuestate::numeric,
        COALESCE(datastamp, 0)::numeric,
        createdate,
        revisedate
        """;

    public IReadOnlyList<EquipmentDto> List(string? keyword, int? issueState, string? equipmentType)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {SelectColumns}
            FROM "YMES".tbleqpequipmentbasis
            WHERE COALESCE(chamberflag, 0) <> 1
              AND (@issueState IS NULL OR issuestate = @issueState)
              AND (@equipmentType IS NULL OR equipmenttype ILIKE @equipmentType)
              AND (@keyword IS NULL OR equipmentno ILIKE @keyword OR equipmentname ILIKE @keyword)
            ORDER BY equipmentno;
            """);
        AddNumeric(command, "issueState", issueState is null ? null : (decimal)issueState.Value);
        AddVarchar(command, "equipmentType", string.IsNullOrWhiteSpace(equipmentType) ? null : equipmentType.Trim());
        AddVarchar(command, "keyword", string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%");

        using var reader = command.ExecuteReader();
        var items = new List<EquipmentDto>();
        while (reader.Read()) items.Add(Map(reader));
        return items;
    }

    public EquipmentDto Get(string equipmentNo)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {SelectColumns}
            FROM "YMES".tbleqpequipmentbasis
            WHERE lower(equipmentno) = lower(@equipmentNo)
            LIMIT 1;
            """);
        AddVarchar(command, "equipmentNo", equipmentNo.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : throw NotFound(equipmentNo);
    }

    public IReadOnlyList<EquipmentDto> Chambers(string equipmentNo)
    {
        var parent = Get(equipmentNo);
        EnsureChamberParent(parent);
        using var command = dataSource.CreateCommand($"""
            SELECT {SelectColumns}
            FROM "YMES".tbleqpequipmentbasis
            WHERE COALESCE(chamberflag, 0) = 1
              AND lower(belongequipmentno) = lower(@equipmentNo)
            ORDER BY equipmentno;
            """);
        AddVarchar(command, "equipmentNo", equipmentNo.Trim());
        using var reader = command.ExecuteReader();
        var items = new List<EquipmentDto>();
        while (reader.Read()) items.Add(Map(reader));
        return items;
    }

    public EquipmentDto CreateChamber(string parentEquipmentNo, ChamberWriteRequest request, string actorUserNo)
    {
        var equipmentNo = Required(request.EquipmentNo, "Chamber 編號不可空白。", 50);
        var parentNo = Required(request.BelongEquipmentNo, "父設備編號不可空白。", 50);
        if (!Same(parentEquipmentNo, parentNo))
            throw new EquipmentRuleException(400, "Chamber 的父設備必須與網址中的設備一致。");
        var equipmentName = Required(request.EquipmentName, "Chamber 名稱不可空白。", 255);
        ValidateActor(actorUserNo);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var parent = Get(connection, transaction, parentEquipmentNo, forUpdate: true);
        EnsureChamberParent(parent);

        using (var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tbleqpequipmentbasis
                (equipmentno, equipmenttype, capacity, vendorno, description, creator, createdate,
                 issuestate, engineergroupno, loadport, equipmentname, chamberflag,
                 belongequipmentno, datastamp)
            VALUES
                (@equipmentNo, 'CHAMBER', 1, @vendorNo, @description, @actor, now(),
                 0, @engineerGroupNo, 0, @equipmentName, 1,
                 @parentEquipmentNo, 1);
            """, connection, transaction))
        {
            AddVarchar(command, "equipmentNo", equipmentNo);
            AddVarchar(command, "vendorNo", parent.VendorNo);
            AddVarchar(command, "description", Optional(request.Description));
            AddVarchar(command, "actor", actorUserNo.Trim());
            AddVarchar(command, "engineerGroupNo", Optional(parent.EngineerGroupNo));
            AddVarchar(command, "equipmentName", equipmentName);
            AddVarchar(command, "parentEquipmentNo", parent.EquipmentNo);
            ExecuteInsert(command, equipmentNo);
        }

        transaction.Commit();
        return Get(equipmentNo);
    }

    public EquipmentDto UpdateChamber(string parentEquipmentNo, string chamberNo, ChamberWriteRequest request, string actorUserNo)
    {
        var parent = Get(parentEquipmentNo);
        EnsureChamberParent(parent);
        var current = Get(chamberNo);
        EnsureChamber(current, parentEquipmentNo);
        EnsureEditable(current, request.DataStamp);
        if (!Same(current.EquipmentNo, request.EquipmentNo.Trim()))
            throw new EquipmentRuleException(400, "更新時不可變更 Chamber 編號。");
        if (!Same(parentEquipmentNo, request.BelongEquipmentNo.Trim()))
            throw new EquipmentRuleException(400, "更新時不可變更 Chamber 的父設備。");
        ValidateActor(actorUserNo);

        using var command = dataSource.CreateCommand("""
            UPDATE "YMES".tbleqpequipmentbasis
            SET equipmentname = @equipmentName,
                description = @description,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE lower(equipmentno) = lower(@equipmentNo)
              AND COALESCE(datastamp, 0) = @dataStamp
              AND issuestate = 0
              AND COALESCE(chamberflag, 0) = 1
              AND lower(belongequipmentno) = lower(@parentEquipmentNo);
            """);
        AddVarchar(command, "equipmentName", Required(request.EquipmentName, "Chamber 名稱不可空白。", 255));
        AddVarchar(command, "description", Optional(request.Description));
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "equipmentNo", chamberNo.Trim());
        AddNumeric(command, "dataStamp", request.DataStamp);
        AddVarchar(command, "parentEquipmentNo", parentEquipmentNo.Trim());
        ExecuteOptimisticUpdate(command, "Chamber 資料已變更，請重新載入後再操作。");
        return Get(chamberNo);
    }

    public EquipmentDto ApproveChamber(string parentEquipmentNo, string chamberNo, long dataStamp, string actorUserNo)
    {
        var current = Get(chamberNo);
        EnsureChamber(current, parentEquipmentNo);
        EnsureEditable(current, dataStamp);
        return UpdateState(chamberNo, dataStamp, 2, actorUserNo, onlyUnfrozen: true);
    }

    public EquipmentDto DeleteChamber(string parentEquipmentNo, string chamberNo, int issueState, long dataStamp, string actorUserNo)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = Get(connection, transaction, chamberNo, forUpdate: true);
        EnsureChamber(current, parentEquipmentNo);
        EnsureStamp(current, dataStamp);
        EnsureIssueState(current, issueState);
        if (current.IssueState == 1)
            throw new EquipmentRuleException(409, "PENDING Chamber 不可刪除。");

        EquipmentDto result;
        if (current.IssueState == 2)
        {
            result = UpdateState(connection, transaction, current, dataStamp, -1, actorUserNo);
        }
        else
        {
            using var command = new NpgsqlCommand("""
                DELETE FROM "YMES".tbleqpequipmentbasis
                WHERE lower(equipmentno) = lower(@equipmentNo)
                  AND COALESCE(datastamp, 0) = @dataStamp
                  AND issuestate = @issueState
                  AND COALESCE(chamberflag, 0) = 1
                  AND lower(belongequipmentno) = lower(@parentEquipmentNo);
                """, connection, transaction);
            AddVarchar(command, "equipmentNo", chamberNo.Trim());
            AddNumeric(command, "dataStamp", dataStamp);
            AddNumeric(command, "issueState", issueState);
            AddVarchar(command, "parentEquipmentNo", parentEquipmentNo.Trim());
            ExecuteOptimisticUpdate(command, "Chamber 資料已變更，請重新載入後再操作。");
            result = current;
        }

        transaction.Commit();
        return current.IssueState == 2 ? Get(chamberNo) : result;
    }

    public EquipmentDto Create(EquipmentWriteRequest request, string actorUserNo)
    {
        ValidateRequest(request);
        ValidateActor(actorUserNo);
        using var command = dataSource.CreateCommand("""
            INSERT INTO "YMES".tbleqpequipmentbasis
                (equipmentno, equipmenttype, capacity, vendorno, modelno, description, creator, createdate,
                 issuestate, engineergroupno, assetno, equipmentclass, loadport, autoflag, eacontroller,
                 eqprecipe, qclistno, maxtime, fixeqptime, vareqptime, countopunitqty, counteqpunitqty,
                 equipmentname, queueserver, requestmsgqueue, responsemsgqueue, chamberflag,
                 equipmentclassno, datastamp, requestmsgqueue_mes, responsemsgqueue_eap)
            VALUES
                (@equipmentNo, @equipmentType, @capacity, @vendorNo, @modelNo, @description, @actor, now(),
                 0, @engineerGroupNo, @assetNo, @equipmentClass, @loadPort, @autoFlag, @eaController,
                 @eqpRecipe, @qcListNo, @maxTime, @fixEqpTime, @varEqpTime, @countOpUnitQty, @countEqpUnitQty,
                 @equipmentName, @queueServer, @requestMsgQueue, @responseMsgQueue, 0,
                 @equipmentClassNo, 1, @requestMsgQueueMes, @responseMsgQueueEap);
            """);
        AddRequestParameters(command, request, actorUserNo);
        ExecuteInsert(command, request.EquipmentNo.Trim());
        return Get(request.EquipmentNo);
    }

    public EquipmentDto Update(string equipmentNo, EquipmentWriteRequest request, string actorUserNo)
    {
        ValidateRequest(request);
        ValidateActor(actorUserNo);
        var current = Get(equipmentNo);
        EnsureEditable(current, request.DataStamp);
        if (!Same(current.EquipmentNo, request.EquipmentNo.Trim()))
            throw new EquipmentRuleException(400, "更新時不可變更設備編號。");

        using var command = dataSource.CreateCommand("""
            UPDATE "YMES".tbleqpequipmentbasis
            SET equipmenttype = @equipmentType,
                capacity = @capacity,
                vendorno = @vendorNo,
                modelno = @modelNo,
                description = @description,
                engineergroupno = @engineerGroupNo,
                assetno = @assetNo,
                equipmentclass = @equipmentClass,
                loadport = @loadPort,
                autoflag = @autoFlag,
                eacontroller = @eaController,
                eqprecipe = @eqpRecipe,
                qclistno = @qcListNo,
                maxtime = @maxTime,
                fixeqptime = @fixEqpTime,
                vareqptime = @varEqpTime,
                countopunitqty = @countOpUnitQty,
                counteqpunitqty = @countEqpUnitQty,
                equipmentname = @equipmentName,
                queueserver = @queueServer,
                requestmsgqueue = @requestMsgQueue,
                responsemsgqueue = @responseMsgQueue,
                equipmentclassno = @equipmentClassNo,
                requestmsgqueue_mes = @requestMsgQueueMes,
                responsemsgqueue_eap = @responseMsgQueueEap,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE lower(equipmentno) = lower(@equipmentNo)
              AND COALESCE(datastamp, 0) = @dataStamp
              AND issuestate = 0
              AND COALESCE(chamberflag, 0) <> 1;
            """);
        AddRequestParameters(command, request, actorUserNo);
        AddVarchar(command, "equipmentNo", equipmentNo.Trim());
        AddNumeric(command, "dataStamp", request.DataStamp);
        ExecuteOptimisticUpdate(command, "設備資料已變更，請重新載入後再儲存。");
        return Get(equipmentNo);
    }

    public EquipmentDto Approve(string equipmentNo, long dataStamp, string actorUserNo)
    {
        var current = Get(equipmentNo);
        EnsureEditable(current, dataStamp);
        if (current.ChamberFlag) throw new EquipmentRuleException(400, "Chamber 請使用 Chamber 核准功能。");
        return UpdateState(equipmentNo, dataStamp, 2, actorUserNo, onlyUnfrozen: true);
    }

    public EquipmentDto DeleteOrRetire(string equipmentNo, int issueState, long dataStamp, string actorUserNo)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = Get(connection, transaction, equipmentNo, forUpdate: true);
        EnsureStamp(current, dataStamp);
        EnsureIssueState(current, issueState);
        if (current.ChamberFlag) throw new EquipmentRuleException(400, "Chamber 請使用 Chamber 刪除功能。");
        if (current.IssueState == 1) throw new EquipmentRuleException(409, "PENDING 設備不可刪除。");

        EquipmentDto result;
        if (current.IssueState is 0 or -1)
        {
            if (HasChambers(connection, transaction, current.EquipmentNo, activeOnly: false))
                throw new EquipmentRuleException(409, "設備仍有 Chamber，請先移除 Chamber。");
            using var command = new NpgsqlCommand("""
                DELETE FROM "YMES".tbleqpequipmentbasis
                WHERE lower(equipmentno) = lower(@equipmentNo)
                  AND COALESCE(datastamp, 0) = @dataStamp
                  AND issuestate = @issueState
                  AND COALESCE(chamberflag, 0) <> 1;
                """, connection, transaction);
            AddVarchar(command, "equipmentNo", equipmentNo.Trim());
            AddNumeric(command, "dataStamp", dataStamp);
            AddNumeric(command, "issueState", issueState);
            ExecuteOptimisticUpdate(command, "設備資料已變更，請重新載入後再操作。");
            result = current;
        }
        else
        {
            if (HasChambers(connection, transaction, current.EquipmentNo, activeOnly: true))
                throw new EquipmentRuleException(409, "設備仍有使用中的 Chamber，請先停用或移除 Chamber。");
            result = UpdateState(connection, transaction, current, dataStamp, -1, actorUserNo);
        }

        transaction.Commit();
        return current.IssueState == 2 ? Get(equipmentNo) : result;
    }

    private EquipmentDto UpdateState(string equipmentNo, long dataStamp, int nextState, string actorUserNo, bool onlyUnfrozen)
    {
        ValidateActor(actorUserNo);
        using var command = dataSource.CreateCommand("""
            UPDATE "YMES".tbleqpequipmentbasis
            SET issuestate = @nextState,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE lower(equipmentno) = lower(@equipmentNo)
              AND COALESCE(datastamp, 0) = @dataStamp
              AND (@onlyUnfrozen = false OR issuestate = 0);
            """);
        AddNumeric(command, "nextState", nextState);
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "equipmentNo", equipmentNo.Trim());
        AddNumeric(command, "dataStamp", dataStamp);
        command.Parameters.AddWithValue("onlyUnfrozen", onlyUnfrozen);
        ExecuteOptimisticUpdate(command, "設備狀態已變更，請重新載入後再操作。");
        return Get(equipmentNo);
    }

    private EquipmentDto UpdateState(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        EquipmentDto current,
        long dataStamp,
        int nextState,
        string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tbleqpequipmentbasis
            SET issuestate = @nextState,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE lower(equipmentno) = lower(@equipmentNo)
              AND COALESCE(datastamp, 0) = @dataStamp;
            """, connection, transaction);
        AddNumeric(command, "nextState", nextState);
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "equipmentNo", current.EquipmentNo);
        AddNumeric(command, "dataStamp", dataStamp);
        ExecuteOptimisticUpdate(command, "設備狀態已變更，請重新載入後再操作。");
        return current with { IssueState = nextState, DataStamp = dataStamp + 1, RevisedAt = DateTime.UtcNow };
    }

    private bool HasChambers(NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentNo, bool activeOnly)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM "YMES".tbleqpequipmentbasis
                WHERE COALESCE(chamberflag, 0) = 1
                  AND lower(belongequipmentno) = lower(@equipmentNo)
                  AND (@activeOnly = false OR issuestate <> -1));
            """, connection, transaction);
        AddVarchar(command, "equipmentNo", equipmentNo);
        command.Parameters.AddWithValue("activeOnly", activeOnly);
        return (bool)(command.ExecuteScalar() ?? false);
    }

    private static EquipmentDto Get(NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentNo, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"""
            SELECT {SelectColumns}
            FROM "YMES".tbleqpequipmentbasis
            WHERE lower(equipmentno) = lower(@equipmentNo)
            LIMIT 1{(forUpdate ? " FOR UPDATE" : "")};
            """, connection, transaction);
        AddVarchar(command, "equipmentNo", equipmentNo.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : throw NotFound(equipmentNo);
    }

    private static EquipmentDto Map(NpgsqlDataReader reader) => new()
    {
        EquipmentNo = reader.GetString(0),
        EquipmentType = reader.GetString(1),
        EquipmentName = reader.GetString(2),
        EngineerGroupNo = reader.GetString(3),
        VendorNo = reader.GetString(4),
        ModelNo = reader.GetString(5),
        AssetNo = reader.GetString(6),
        EquipmentClass = reader.GetString(7),
        EquipmentClassNo = reader.GetString(8),
        Description = reader.GetString(9),
        Capacity = reader.GetDecimal(10),
        LoadPort = ToInt32(reader.GetValue(11)),
        AutoFlag = ToInt32(reader.GetValue(12)),
        EAController = reader.GetString(13),
        EqpRecipe = ToInt32(reader.GetValue(14)),
        QCListNo = reader.GetString(15),
        MaxTime = ReadNullableDecimal(reader, 16),
        FixEqpTime = ReadNullableDecimal(reader, 17),
        VarEqpTime = ReadNullableDecimal(reader, 18),
        CountEqpUnitQty = ToInt32(reader.GetValue(19)),
        CountEquipmentUnitQty = ToInt32(reader.GetValue(20)),
        QueueServer = reader.GetString(21),
        RequestMsgQueue = reader.GetString(22),
        ResponseMsgQueue = reader.GetString(23),
        RequestMsgQueueMes = reader.GetString(24),
        ResponseMsgQueueEap = reader.GetString(25),
        ChamberFlag = reader.GetBoolean(26),
        BelongEquipmentNo = reader.IsDBNull(27) ? null : reader.GetString(27),
        IssueState = ToInt32(reader.GetValue(28)),
        DataStamp = Convert.ToInt64(reader.GetDecimal(29), CultureInfo.InvariantCulture),
        CreatedAt = reader.IsDBNull(30) ? null : reader.GetDateTime(30),
        RevisedAt = reader.IsDBNull(31) ? null : reader.GetDateTime(31)
    };

    private static decimal? ReadNullableDecimal(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private static int ToInt32(object value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static void AddRequestParameters(NpgsqlCommand command, EquipmentWriteRequest request, string actorUserNo)
    {
        AddVarchar(command, "equipmentNo", Required(request.EquipmentNo, "設備編號不可空白。", 50));
        AddVarchar(command, "equipmentType", Required(request.EquipmentType, "設備類別不可空白。", 50));
        AddNumeric(command, "capacity", request.Capacity);
        AddVarchar(command, "vendorNo", Required(request.VendorNo, "供應商編號不可空白。", 50));
        AddVarchar(command, "modelNo", Optional(request.ModelNo));
        AddVarchar(command, "description", Optional(request.Description));
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "engineerGroupNo", Optional(request.EngineerGroupNo));
        AddVarchar(command, "assetNo", Optional(request.AssetNo));
        AddVarchar(command, "equipmentClass", Optional(request.EquipmentClass));
        AddNumeric(command, "loadPort", request.LoadPort);
        AddNumeric(command, "autoFlag", request.AutoFlag);
        AddVarchar(command, "eaController", Optional(request.EAController));
        AddNumeric(command, "eqpRecipe", request.EqpRecipe);
        AddVarchar(command, "qcListNo", Optional(request.QCListNo));
        AddNumeric(command, "maxTime", request.MaxTime);
        AddNumeric(command, "fixEqpTime", request.FixEqpTime);
        AddNumeric(command, "varEqpTime", request.VarEqpTime);
        AddNumeric(command, "countOpUnitQty", request.CountEqpUnitQty);
        AddNumeric(command, "countEqpUnitQty", request.CountEquipmentUnitQty);
        AddVarchar(command, "equipmentName", Required(request.EquipmentName, "設備名稱不可空白。", 255));
        AddVarchar(command, "queueServer", Optional(request.QueueServer));
        AddVarchar(command, "requestMsgQueue", Optional(request.RequestMsgQueue));
        AddVarchar(command, "responseMsgQueue", Optional(request.ResponseMsgQueue));
        AddNumeric(command, "equipmentClassNo", ParseEquipmentClassNo(request.EquipmentClassNo));
        AddVarchar(command, "requestMsgQueueMes", Optional(request.RequestMsgQueueMes));
        AddVarchar(command, "responseMsgQueueEap", Optional(request.ResponseMsgQueueEap));
    }

    private static void ValidateRequest(EquipmentWriteRequest request)
    {
        if (decimal.Truncate(request.Capacity) != request.Capacity)
            throw new EquipmentRuleException(400, "批量限制必須是整數。");
        _ = ParseEquipmentClassNo(request.EquipmentClassNo);
    }

    private static decimal? ParseEquipmentClassNo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!decimal.TryParse(value.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed) ||
            parsed != decimal.Truncate(parsed) || parsed is < -999 or > 999)
            throw new EquipmentRuleException(400, "財產分類編號必須是 -999 到 999 的整數。");
        return parsed;
    }

    private static string Required(string? value, string message, int maxLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0) throw new EquipmentRuleException(400, message);
        if (text.Length > maxLength) throw new EquipmentRuleException(400, $"欄位長度不可超過 {maxLength} 個字元。");
        return text;
    }

    private static string? Optional(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static void ValidateActor(string actorUserNo)
    {
        if (string.IsNullOrWhiteSpace(actorUserNo) || actorUserNo.Trim().Length > 10)
            throw new EquipmentRuleException(400, "登入使用者編號長度不符合設備資料欄位。");
    }

    private static void EnsureEditable(EquipmentDto item, long dataStamp)
    {
        EnsureStamp(item, dataStamp);
        if (item.IssueState != 0) throw new EquipmentRuleException(409, "只有 UNFROZEN 設備可以編輯。");
    }

    private static void EnsureStamp(EquipmentDto item, long dataStamp)
    {
        if (item.DataStamp != dataStamp)
            throw new EquipmentRuleException(409, "資料已由其他人更新，請重新載入後再儲存。");
    }

    private static void EnsureIssueState(EquipmentDto item, int issueState)
    {
        if (item.IssueState != issueState)
            throw new EquipmentRuleException(409, "設備狀態已變更，請重新載入後再操作。");
    }

    private static void EnsureChamberParent(EquipmentDto parent)
    {
        if (parent.ChamberFlag) throw new EquipmentRuleException(400, "Chamber 不可再作為 Chamber 的父設備。");
        if (parent.IssueState is 1 or -1) throw new EquipmentRuleException(409, "PENDING 或 UNUSED 設備不可變更 Chamber。");
    }

    private static void EnsureChamber(EquipmentDto chamber, string parentEquipmentNo)
    {
        if (!chamber.ChamberFlag || !Same(chamber.BelongEquipmentNo, parentEquipmentNo))
            throw new EquipmentRuleException(404, $"設備 {chamber.EquipmentNo} 不屬於父設備 {parentEquipmentNo} 的 Chamber。");
    }

    private static void ExecuteInsert(NpgsqlCommand command, string equipmentNo)
    {
        try { command.ExecuteNonQuery(); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, $"設備編號 {equipmentNo} 已存在。");
        }
    }

    private static void ExecuteOptimisticUpdate(NpgsqlCommand command, string message)
    {
        if (command.ExecuteNonQuery() == 0) throw new EquipmentRuleException(409, message);
    }

    private static void AddVarchar(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static EquipmentRuleException NotFound(string equipmentNo) => new(404, $"找不到設備 {equipmentNo}。");
}
