using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc217Repository(NpgsqlDataSource dataSource)
{
    private static readonly CatalogSpec[] Catalogs =
    [
        new(0, "tblqcitemforattributes", "qcitemno", "qcitemname"),
        new(1, "tblqcitemforvariables", "qcitemno", "qcitemname"),
        new(2, "tblqcitemgroupbasis", "groupno", "groupname"),
        new(3, "tblqcitemattgrouporderbasis", "groupno", "groupname")
    ];

    public IReadOnlyList<Qc217InspectionDto> List(int issueState, string? opNo, string? equipmentNo, string? inspType)
    {
        if (issueState is not (-1 or 0 or 1 or 2))
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "IssueState 僅允許 -1、0、1、2。");

        int? inspectionType = null;
        if (!string.IsNullOrWhiteSpace(inspType))
        {
            if (!int.TryParse(inspType, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed is < 0 or > 8)
                throw new Qc217RuleException(StatusCodes.Status400BadRequest, "檢查類型必須為 0–8。");
            inspectionType = parsed;
        }

        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT opno,equipmentno,eqpdatatype,insptype,inspweek,inspstarttime,periodmin,equipmentstate,
                   lotcount,issuestate,mailgroupno,ccmailgroupno,creator,createdate,reviser,revisedate,COALESCE(datastamp,0)
            FROM "YMES".tblqcinspectionbasis
            WHERE issuestate=@state
              AND (@opNo='' OR position(lower(@opNo) in lower(opno))>0)
              AND (@equipmentNo='' OR position(lower(@equipmentNo) in lower(equipmentno))>0)
              AND (@inspType IS NULL OR insptype=@inspType)
            ORDER BY opno,equipmentno,insptype,inspweek,inspstarttime;
            """, connection);
        AddNumeric(command, "state", issueState);
        AddText(command, "opNo", NormalizeSearch(opNo), 20);
        AddText(command, "equipmentNo", NormalizeSearch(equipmentNo), 50);
        AddNumeric(command, "inspType", inspectionType);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc217InspectionDto>();
        while (reader.Read()) rows.Add(MapInspection(reader));
        return rows;
    }

    public IReadOnlyList<Qc217InspectionDetailDto> GetDetails(string opNo, string equipmentNo, int inspType)
    {
        var key = NormalizeSharedKey(opNo, equipmentNo, inspType);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT qcitemno,qcitemtype::int FROM "YMES".tblqcinspectiondetail
            WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
            ORDER BY qcitemtype,qcitemno;
            """, connection);
        AddSharedKey(command, key);
        var saved = new List<(string QcItemNo, int QcItemType)>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) saved.Add((reader.GetString(0), reader.GetInt32(1)));

        if (saved.Count == 0) return [];
        var catalog = ReadCatalog(connection, approvedOnly: false, validOnly: false)
            .ToDictionary(row => CatalogKey(row.QcItemType, row.QcItemNo), StringComparer.OrdinalIgnoreCase);
        return saved.Select(item => catalog.TryGetValue(CatalogKey(item.QcItemType, item.QcItemNo), out var match)
                ? match
                : new Qc217InspectionDetailDto(item.QcItemNo, "", item.QcItemType, 0))
            .ToArray();
    }

    public Qc217OptionsDto GetOptions()
    {
        using var connection = dataSource.OpenConnection();
        var operations = new List<string>();
        using (var command = new NpgsqlCommand("SELECT opno FROM \"YMES\".tblopbasis WHERE issuestate=2 ORDER BY opno;", connection))
        using (var reader = command.ExecuteReader())
            while (reader.Read()) operations.Add(reader.GetString(0));

        var equipment = new List<string>();
        var equipmentTypes = new List<string>();
        using (var command = new NpgsqlCommand("""
            SELECT equipmentno,COALESCE(equipmenttype,'')
            FROM "YMES".tbleqpequipmentbasis WHERE issuestate=2 ORDER BY equipmentno;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            var typeSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                equipment.Add(reader.GetString(0));
                var type = reader.GetString(1).Trim();
                if (type.Length > 0) typeSet.Add(type);
            }
            equipmentTypes.AddRange(typeSet);
        }

        var equipmentStates = new List<Qc217ValueOption>();
        using (var command = new NpgsqlCommand("SELECT equipmentstate::int,statename FROM \"YMES\".tbleqpstatebasis ORDER BY equipmentstate;", connection))
        using (var reader = command.ExecuteReader())
            while (reader.Read()) equipmentStates.Add(new Qc217ValueOption(reader.GetInt32(0), reader.GetString(1)));

        var items = ReadCatalog(connection, approvedOnly: true, validOnly: true);
        var missing = new List<string>();
        if (!TableExists(connection, null, "tblsysvaluelist"))
            missing.Add("MES ValueList（檢查類型與週別顯示名稱；週別目前回傳代碼）");
        if (!TableExists(connection, null, Catalogs[3].TableName))
            missing.Add("QCItemType=3 計數順序群組主檔 tblqcitemattgrouporderbasis");
        if (!TableExists(connection, null, "tblsysmailgroupbasis"))
            missing.Add("通知群組主檔；YMES 尚未複製 Mail Group 目錄");

        // The MES ValueList source is not yet replicated. Return the known week
        // codes without inventing display translations; clients can still use
        // the code and the missing-reference list makes the limitation visible.
        var weeks = Enumerable.Range(0, 7).Select(value => new Qc217ValueOption(value, value.ToString(CultureInfo.InvariantCulture))).ToArray();
        return new Qc217OptionsDto(operations, equipment, equipmentTypes, equipmentStates, weeks, [], items, missing);
    }

    public Qc217InspectionDto Create(Qc217InspectionWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var value = NormalizeAndValidate(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            ValidateReferences(connection, transaction, value, requireLockedScope: true);
            ValidateDetails(connection, transaction, value.Details);
            var siblingKeys = LockSiblingParents(connection, transaction, value.Key);
            var currentDetails = ReadSharedDetails(connection, transaction, value.Key);

            using (var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcinspectionbasis
                    (opno,equipmentno,eqpdatatype,insptype,inspweek,inspstarttime,periodmin,equipmentstate,
                     lotcount,issuestate,mailgroupno,ccmailgroupno,creator,createdate,datastamp)
                VALUES
                    (@opNo,@equipmentNo,@eqpDataType,@inspType,@inspWeek,@inspStartTime,@periodMin,@equipmentState,
                     @lotCount,0,@mailGroupNo,@ccMailGroupNo,@actor,now(),0);
                """, connection, transaction))
            {
                AddMainKey(command, value.Key);
                AddText(command, "eqpDataType", value.EqpDataType.ToString(CultureInfo.InvariantCulture), 255);
                AddSchedule(command, value);
                AddText(command, "mailGroupNo", value.MailGroupNo, 255);
                AddText(command, "ccMailGroupNo", value.CcMailGroupNo, 255);
                AddText(command, "actor", actor, 10);
                command.ExecuteNonQuery();
            }

            if (siblingKeys.Count == 0 || currentDetails.Count == 0)
                InsertDetails(connection, transaction, value.Key, value.Details);
            else if (!DetailsEqual(currentDetails, value.Details))
                throw new Qc217RuleException(StatusCodes.Status409Conflict,
                    "此 OP／設備／檢查類型的品管明細由不同週期共用；新增排程的明細與既有共用明細不一致，請先修改既有設定。");

            transaction.Commit();
            return Get(value.Key);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw new Qc217RuleException(StatusCodes.Status409Conflict, "相同 OP、設備範圍、檢查類型、週別及時間的設定已存在。");
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc217InspectionDto Update(Qc217MainKey pathKey, Qc217InspectionWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var value = NormalizeAndValidate(request);
        if (value.Key != NormalizeKey(pathKey))
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "OP、設備範圍、檢查類型、週別與時間不可在修改時變更。");

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var current = GetForUpdate(connection, transaction, value.Key);
            EnsureStamp(current, request.DataStamp);
            if (current.IssueState != 0)
                throw new Qc217RuleException(StatusCodes.Status409Conflict, "只有未凍結（IssueState=0）的設備品檢設定可以修改。");
            if (current.EqpDataType != value.EqpDataType)
                throw new Qc217RuleException(StatusCodes.Status400BadRequest, "設備編號／設備類型範圍不可在修改時變更。");

            ValidateReferences(connection, transaction, value, requireLockedScope: false);
            ValidateDetails(connection, transaction, value.Details);
            using (var command = new NpgsqlCommand("""
                UPDATE "YMES".tblqcinspectionbasis
                SET periodmin=@periodMin,equipmentstate=@equipmentState,lotcount=@lotCount,
                    mailgroupno=@mailGroupNo,ccmailgroupno=@ccMailGroupNo,
                    reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
                  AND inspweek=@inspWeek AND inspstarttime=@inspStartTime
                  AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
                """, connection, transaction))
            {
                AddMainKey(command, value.Key);
                AddSchedule(command, value);
                AddText(command, "mailGroupNo", value.MailGroupNo, 255);
                AddText(command, "ccMailGroupNo", value.CcMailGroupNo, 255);
                AddText(command, "actor", actor, 10);
                AddDecimal(command, "stamp", request.DataStamp);
                if (command.ExecuteNonQuery() != 1) throw Conflict();
            }

            using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcinspectiondetail WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType;", connection, transaction))
            {
                AddSharedKey(delete, value.Key);
                delete.ExecuteNonQuery();
            }
            InsertDetails(connection, transaction, value.Key, value.Details);
            transaction.Commit();
            return Get(value.Key);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw new Qc217RuleException(StatusCodes.Status409Conflict, "品管明細已存在重複項目。");
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc217DeleteResult Delete(Qc217MainKey pathKey, decimal dataStamp, string actor)
    {
        ValidateActor(actor);
        var key = NormalizeKey(pathKey);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var current = GetForUpdate(connection, transaction, key);
            EnsureStamp(current, dataStamp);
            if (current.IssueState == 1)
                throw new Qc217RuleException(StatusCodes.Status409Conflict, "簽核中的設備品檢設定不可刪除。");

            if (current.IssueState == 2)
            {
                using var disable = new NpgsqlCommand("""
                    UPDATE "YMES".tblqcinspectionbasis
                    SET issuestate=-1,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                    WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
                      AND inspweek=@inspWeek AND inspstarttime=@inspStartTime
                      AND issuestate=2 AND COALESCE(datastamp,0)=@stamp;
                    """, connection, transaction);
                AddMainKey(disable, key);
                AddText(disable, "actor", actor, 10);
                AddDecimal(disable, "stamp", dataStamp);
                if (disable.ExecuteNonQuery() != 1) throw Conflict();
                transaction.Commit();
                return new Qc217DeleteResult("disabled", -1, 0, false);
            }

            int cleared;
            using (var details = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcinspectiondetail WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType;", connection, transaction))
            {
                AddSharedKey(details, key);
                cleared = details.ExecuteNonQuery();
            }
            using (var delete = new NpgsqlCommand("""
                DELETE FROM "YMES".tblqcinspectionbasis
                WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
                  AND inspweek=@inspWeek AND inspstarttime=@inspStartTime
                  AND issuestate IN (-1,0) AND COALESCE(datastamp,0)=@stamp;
                """, connection, transaction))
            {
                AddMainKey(delete, key);
                AddDecimal(delete, "stamp", dataStamp);
                if (delete.ExecuteNonQuery() != 1) throw Conflict();
            }
            transaction.Commit();
            return new Qc217DeleteResult("deleted", null, cleared, true);
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc217InspectionDto Approve(Qc217MainKey pathKey, decimal dataStamp, string actor)
    {
        ValidateActor(actor);
        var key = NormalizeKey(pathKey);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var current = GetForUpdate(connection, transaction, key);
            EnsureStamp(current, dataStamp);
            if (current.IssueState != 0)
                throw new Qc217RuleException(StatusCodes.Status409Conflict, "只有未凍結（IssueState=0）的設備品檢設定可以送出核准。");
            if (CountSharedDetails(connection, transaction, key) == 0)
                throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity, "至少需要一筆品管項目明細才能核准。");

            // YMES has no replicated Issue Job/Issue Detail tables or shared
            // workflow API yet. As confirmed by the live function catalog,
            // QC INSPECTION supports Issue Job; do not bypass a future job by
            // directly setting state 2. This endpoint reports the dependency.
            if (WorkflowTablesExist(connection))
                throw new Qc217RuleException(StatusCodes.Status503ServiceUnavailable,
                    "QC INSPECTION 已啟用簽核；YMES 共用 Issue Job/Issue Detail 流程尚未接入，未變更資料狀態。");

            using (var update = new NpgsqlCommand("""
                UPDATE "YMES".tblqcinspectionbasis
                SET issuestate=2,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
                  AND inspweek=@inspWeek AND inspstarttime=@inspStartTime
                  AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
                """, connection, transaction))
            {
                AddMainKey(update, key);
                AddText(update, "actor", actor, 10);
                AddDecimal(update, "stamp", dataStamp);
                if (update.ExecuteNonQuery() != 1) throw Conflict();
            }
            transaction.Commit();
            return Get(key);
        }
        catch { TryRollback(transaction); throw; }
    }

    private Qc217InspectionDto Get(Qc217MainKey key)
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT opno,equipmentno,eqpdatatype,insptype,inspweek,inspstarttime,periodmin,equipmentstate,
                   lotcount,issuestate,mailgroupno,ccmailgroupno,creator,createdate,reviser,revisedate,COALESCE(datastamp,0)
            FROM "YMES".tblqcinspectionbasis
            WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
              AND inspweek=@inspWeek AND inspstarttime=@inspStartTime;
            """, connection);
        AddMainKey(command, key);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapInspection(reader) : throw NotFound();
    }

    private static Qc217InspectionDto MapInspection(NpgsqlDataReader reader)
    {
        var eqpDataTypeText = reader.IsDBNull(2) ? "0" : reader.GetString(2);
        var eqpDataType = int.TryParse(eqpDataTypeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedType) && parsedType == 1 ? 1 : 0;
        return new Qc217InspectionDto(
            reader.GetString(0), reader.GetString(1), eqpDataType,
            Convert.ToInt32(reader.GetDecimal(3), CultureInfo.InvariantCulture),
            Convert.ToInt32(reader.GetDecimal(4), CultureInfo.InvariantCulture),
            reader.GetString(5), NullableInt(reader, 6), NullableInt(reader, 7), NullableInt(reader, 8),
            Convert.ToInt32(reader.GetDecimal(9), CultureInfo.InvariantCulture),
            reader.IsDBNull(10) ? "" : reader.GetString(10), reader.IsDBNull(11) ? "" : reader.GetString(11),
            reader.IsDBNull(12) ? "" : reader.GetString(12), reader.IsDBNull(13) ? null : reader.GetDateTime(13),
            reader.IsDBNull(14) ? "" : reader.GetString(14), reader.IsDBNull(15) ? null : reader.GetDateTime(15),
            reader.GetDecimal(16));
    }

    private static int? NullableInt(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetDecimal(ordinal), CultureInfo.InvariantCulture);

    private Qc217InspectionDto GetForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc217MainKey key)
    {
        using var command = new NpgsqlCommand("""
            SELECT opno,equipmentno,eqpdatatype,insptype,inspweek,inspstarttime,periodmin,equipmentstate,
                   lotcount,issuestate,mailgroupno,ccmailgroupno,creator,createdate,reviser,revisedate,COALESCE(datastamp,0)
            FROM "YMES".tblqcinspectionbasis
            WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
              AND inspweek=@inspWeek AND inspstarttime=@inspStartTime
            FOR UPDATE;
            """, connection, transaction);
        AddMainKey(command, key);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapInspection(reader) : throw NotFound();
    }

    private IReadOnlyList<Qc217InspectionDetailDto> ReadCatalog(NpgsqlConnection connection, bool approvedOnly, bool validOnly)
    {
        var items = new List<Qc217InspectionDetailDto>();
        foreach (var spec in Catalogs)
        {
            if (!TableExists(connection, null, spec.TableName)) continue;
            var stateFilter = approvedOnly ? " AND COALESCE(issuestate,0)=2" : "";
            var lotFilter = validOnly ? " AND COALESCE(lotisvalid,0)<>0" : "";
            var sql = $"SELECT {spec.NumberColumn},COALESCE({spec.NameColumn},''),COALESCE(lotisvalid,0)::int FROM \"YMES\".{spec.TableName} WHERE true{stateFilter}{lotFilter} ORDER BY {spec.NumberColumn};";
            using var command = new NpgsqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                items.Add(new Qc217InspectionDetailDto(reader.GetString(0), reader.GetString(1), spec.QcItemType, reader.GetInt32(2)));
        }
        return items;
    }

    private void ValidateReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, NormalizedWrite value, bool requireLockedScope)
    {
        if (value.Key.OpNo != "*")
        {
            using var op = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblopbasis WHERE lower(opno)=lower(@value) AND issuestate=2 FOR SHARE;", connection, transaction);
            AddText(op, "value", value.Key.OpNo, 20);
            if (op.ExecuteScalar() is null)
                throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity, "作業站不存在或尚未核准。");
        }

        if (requireLockedScope)
        {
            var column = value.EqpDataType == 0 ? "equipmentno" : "equipmenttype";
            using var equipment = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".tbleqpequipmentbasis WHERE lower({column})=lower(@value) AND issuestate=2 LIMIT 1 FOR SHARE;", connection, transaction);
            AddText(equipment, "value", value.Key.EquipmentNo, 50);
            if (equipment.ExecuteScalar() is null)
                throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity,
                    value.EqpDataType == 0 ? "設備不存在或尚未核准。" : "沒有已核准設備使用此設備類型。");
        }

        if (value.EquipmentState is int state)
        {
            using var stateCommand = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tbleqpstatebasis WHERE equipmentstate=@state FOR SHARE;", connection, transaction);
            AddNumeric(stateCommand, "state", state);
            if (stateCommand.ExecuteScalar() is null)
                throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity, "設備狀態不存在。");
        }

        if (value.MailGroupNo.Length > 0 || value.CcMailGroupNo.Length > 0)
        {
            if (!TableExists(connection, transaction, "tblsysmailgroupbasis"))
                throw new Qc217RuleException(StatusCodes.Status503ServiceUnavailable, "YMES 尚未複製通知群組主檔，暫時不能設定 Mail Group。");
            throw new Qc217RuleException(StatusCodes.Status503ServiceUnavailable, "通知群組目錄欄位尚未完成 YMES 對照，暫時不能設定 Mail Group。");
        }
        if (value.CcMailGroupNo.Length > 0 && value.MailGroupNo.Length == 0)
            throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity, "副本通知群組有值時，主通知群組必填。");
    }

    private void ValidateDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, IReadOnlyList<Qc217InspectionDetailRequest> details)
    {
        if (details.Count == 0)
            throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity, "至少需選擇一個品管項目。");
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var detail in details)
        {
            if (detail.QcItemType is < 0 or > 3)
                throw new Qc217RuleException(StatusCodes.Status400BadRequest, "QCItemType 僅允許 0–3。");
            var itemNo = ValidateCode(detail.QcItemNo, "QCItemNo", 25);
            if (!normalized.Add(CatalogKey(detail.QcItemType, itemNo)))
                throw new Qc217RuleException(StatusCodes.Status400BadRequest, "品管項目明細不可重複。");
            var spec = Catalogs[detail.QcItemType];
            if (!TableExists(connection, transaction, spec.TableName))
                throw new Qc217RuleException(StatusCodes.Status503ServiceUnavailable, $"QCItemType={detail.QcItemType} 的來源主檔 {spec.TableName} 尚未建立。");

            using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{spec.TableName} WHERE lower({spec.NumberColumn})=lower(@itemNo) AND COALESCE(issuestate,0)=2 AND COALESCE(lotisvalid,0)<>0 FOR SHARE;", connection, transaction);
            AddText(command, "itemNo", itemNo, 25);
            if (command.ExecuteScalar() is null)
                throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity, $"品管項目 {itemNo} 不存在、未核准或 LotIsValid=0。");
        }
    }

    private static NormalizedWrite NormalizeAndValidate(Qc217InspectionWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.EqpDataType is not (0 or 1))
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "EQPDataType 僅允許 0（設備）或 1（設備類型）。");
        var opNo = request.OpNo.Trim().ToUpperInvariant();
        if (opNo.Length is < 1 or > 20)
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "OPNo 必填，最多 20 個字元。");
        var equipmentNo = ValidateCode(request.EquipmentNo, "設備編號／類型", 50);
        if (request.InspType is < 0 or > 8)
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "檢查類型必須為 0–8。");

        int inspWeek;
        string inspStartTime;
        int? periodMin;
        int? equipmentState;
        int? lotCount;
        switch (request.InspType)
        {
            case 4:
                inspWeek = -1;
                inspStartTime = ValidateTime(request.InspStartTime);
                periodMin = null; equipmentState = null; lotCount = null;
                break;
            case 5:
                if (request.PeriodMin is < 1 or > 99999)
                    throw new Qc217RuleException(StatusCodes.Status400BadRequest, "定期檢查分鐘需為 1–99999 的整數。");
                inspWeek = -1; inspStartTime = "N/A"; periodMin = request.PeriodMin; equipmentState = null; lotCount = null;
                break;
            case 6:
                if (request.EquipmentState is null or < 0 or > 99)
                    throw new Qc217RuleException(StatusCodes.Status400BadRequest, "設備狀態檢查必須選擇有效設備狀態。");
                inspWeek = -1; inspStartTime = "N/A"; periodMin = null; equipmentState = request.EquipmentState; lotCount = null;
                break;
            case 7:
                if (request.LotCount is null or < 0 or > 999)
                    throw new Qc217RuleException(StatusCodes.Status400BadRequest, "連續批量需為 0–999 的整數。");
                inspWeek = -1; inspStartTime = "N/A"; periodMin = null; equipmentState = null; lotCount = request.LotCount;
                break;
            case 8:
                if (request.InspWeek is < 0 or > 6)
                    throw new Qc217RuleException(StatusCodes.Status400BadRequest, "每週檢查週別需為 0–6。");
                inspWeek = request.InspWeek; inspStartTime = ValidateTime(request.InspStartTime);
                periodMin = null; equipmentState = null; lotCount = null;
                break;
            default:
                inspWeek = -1; inspStartTime = "N/A"; periodMin = null; equipmentState = null; lotCount = null;
                break;
        }

        var mailGroupNo = (request.MailGroupNo ?? "").Trim().ToUpperInvariant();
        var ccMailGroupNo = (request.CcMailGroupNo ?? "").Trim().ToUpperInvariant();
        if (mailGroupNo.Length > 255 || ccMailGroupNo.Length > 255)
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "通知群組欄位最多 255 個字元。");
        if (ccMailGroupNo.Length > 0 && mailGroupNo.Length == 0)
            throw new Qc217RuleException(StatusCodes.Status422UnprocessableEntity, "副本通知群組有值時，主通知群組必填。");

        var details = (request.Details ?? []).Select(detail => new Qc217InspectionDetailRequest
        {
            QcItemNo = ValidateCode(detail.QcItemNo, "QCItemNo", 25),
            QcItemType = detail.QcItemType
        }).ToArray();
        var key = new Qc217MainKey(opNo, equipmentNo, request.InspType, inspWeek, inspStartTime);
        return new NormalizedWrite(key, request.EqpDataType, periodMin, equipmentState, lotCount, mailGroupNo, ccMailGroupNo, details);
    }

    private static string ValidateTime(string value)
    {
        if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) || time.Minute % 30 != 0)
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "每日／每週開始時間必須為 00:00–23:30、每 30 分鐘一格。");
        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static string ValidateCode(string? value, string label, int maxLength)
    {
        var normalized = (value ?? "").Trim().ToUpperInvariant();
        if (normalized.Length is < 1 || normalized.Length > maxLength)
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, $"{label}必填，最多 {maxLength} 個字元。");
        return normalized;
    }

    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10)
            throw new Qc217RuleException(StatusCodes.Status401Unauthorized, "登入帳號不存在或超過資料庫稽核欄位長度。");
    }

    private static string NormalizeSearch(string? value) => value?.Trim() ?? "";

    private static Qc217MainKey NormalizeSharedKey(string opNo, string equipmentNo, int inspType)
    {
        if (inspType is < 0 or > 8) throw new Qc217RuleException(StatusCodes.Status400BadRequest, "檢查類型必須為 0–8。");
        return new Qc217MainKey(ValidateCode(opNo, "OPNo", 20), ValidateCode(equipmentNo, "設備編號／類型", 50), inspType, -1, "N/A");
    }

    private static Qc217MainKey NormalizeKey(Qc217MainKey key)
    {
        if (key.InspType is < 0 or > 8 || key.InspWeek is < -1 or > 6)
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "複合主鍵格式錯誤。");
        var opNo = ValidateCode(key.OpNo, "OPNo", 20);
        var equipmentNo = ValidateCode(key.EquipmentNo, "設備編號／類型", 50);
        var start = key.InspStartTime.Trim();
        if (start != "N/A") start = ValidateTime(start);
        return new Qc217MainKey(opNo, equipmentNo, key.InspType, key.InspWeek, start);
    }

    private static void EnsureStamp(Qc217InspectionDto current, decimal stamp)
    {
        if (current.DataStamp != stamp)
            throw new Qc217RuleException(StatusCodes.Status409Conflict, "資料已被其他人修改，請重新載入後再操作。");
    }

    private List<Qc217MainKey> LockSiblingParents(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc217MainKey key)
    {
        using var command = new NpgsqlCommand("""
            SELECT opno,equipmentno,insptype,inspweek,inspstarttime FROM "YMES".tblqcinspectionbasis
            WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
            ORDER BY inspweek,inspstarttime FOR UPDATE;
            """, connection, transaction);
        AddSharedKey(command, key);
        var siblings = new List<Qc217MainKey>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) siblings.Add(new Qc217MainKey(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetString(4)));
        return siblings;
    }

    private static List<(string QcItemNo, int QcItemType)> ReadSharedDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc217MainKey key)
    {
        using var command = new NpgsqlCommand("""
            SELECT qcitemno,qcitemtype::int FROM "YMES".tblqcinspectiondetail
            WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType
            ORDER BY qcitemtype,qcitemno FOR UPDATE;
            """, connection, transaction);
        AddSharedKey(command, key);
        var rows = new List<(string QcItemNo, int QcItemType)>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) rows.Add((reader.GetString(0), reader.GetInt32(1)));
        return rows;
    }

    private static bool DetailsEqual(IReadOnlyList<(string QcItemNo, int QcItemType)> existing, IReadOnlyList<Qc217InspectionDetailRequest> requested)
    {
        var a = existing.Select(item => CatalogKey(item.QcItemType, item.QcItemNo)).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var b = requested.Select(item => CatalogKey(item.QcItemType, item.QcItemNo)).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        return a.SequenceEqual(b, StringComparer.OrdinalIgnoreCase);
    }

    private static void InsertDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc217MainKey key, IReadOnlyList<Qc217InspectionDetailRequest> details)
    {
        foreach (var detail in details)
        {
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcinspectiondetail (opno,equipmentno,insptype,qcitemno,qcitemtype)
                VALUES (@opNo,@equipmentNo,@inspType,@itemNo,@itemType);
                """, connection, transaction);
            AddSharedKey(command, key);
            AddText(command, "itemNo", detail.QcItemNo, 25);
            AddNumeric(command, "itemType", detail.QcItemType);
            command.ExecuteNonQuery();
        }
    }

    private static int CountSharedDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc217MainKey key)
    {
        using var command = new NpgsqlCommand("SELECT count(*) FROM \"YMES\".tblqcinspectiondetail WHERE opno=@opNo AND equipmentno=@equipmentNo AND insptype=@inspType;", connection, transaction);
        AddSharedKey(command, key);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string tableName)
    {
        using var command = new NpgsqlCommand("SELECT to_regclass(format('%I.%I','YMES',@tableName)) IS NOT NULL;", connection, transaction);
        AddText(command, "tableName", tableName, 63);
        return (bool)(command.ExecuteScalar() ?? false);
    }

    private static bool WorkflowTablesExist(NpgsqlConnection connection)
    {
        using var command = new NpgsqlCommand("""
            SELECT to_regclass(format('%I.%I','YMES','tblisujobbasis')) IS NOT NULL
                OR to_regclass(format('%I.%I','YMES','tblisujobdetail')) IS NOT NULL
                OR to_regclass(format('%I.%I','YMES','tblissuejobbasis')) IS NOT NULL
                OR to_regclass(format('%I.%I','YMES','tblissuejobdetail')) IS NOT NULL;
            """, connection);
        return (bool)(command.ExecuteScalar() ?? false);
    }

    private static void AddMainKey(NpgsqlCommand command, Qc217MainKey key)
    {
        AddText(command, "opNo", key.OpNo, 20);
        AddText(command, "equipmentNo", key.EquipmentNo, 50);
        AddNumeric(command, "inspType", key.InspType);
        AddNumeric(command, "inspWeek", key.InspWeek);
        AddText(command, "inspStartTime", key.InspStartTime, 20);
    }

    private static void AddSharedKey(NpgsqlCommand command, Qc217MainKey key)
    {
        AddText(command, "opNo", key.OpNo, 20);
        AddText(command, "equipmentNo", key.EquipmentNo, 50);
        AddNumeric(command, "inspType", key.InspType);
    }

    private static void AddSchedule(NpgsqlCommand command, NormalizedWrite value)
    {
        AddNumeric(command, "periodMin", value.PeriodMin);
        AddNumeric(command, "equipmentState", value.EquipmentState);
        AddNumeric(command, "lotCount", value.LotCount);
    }

    private static void AddText(NpgsqlCommand command, string name, string? value, int size)
    {
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    }

    private static void AddNumeric(NpgsqlCommand command, string name, int? value)
    {
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value is null ? DBNull.Value : (decimal)value.Value;
    }

    private static void AddDecimal(NpgsqlCommand command, string name, decimal value)
    {
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;
    }

    private static Qc217RuleException NotFound() => new(StatusCodes.Status404NotFound, "找不到設備品檢設定。");
    private static Qc217RuleException Conflict() => new(StatusCodes.Status409Conflict, "資料已變更，請重新載入後再操作。");

    private static void TryRollback(NpgsqlTransaction transaction)
    {
        try { transaction.Rollback(); } catch { }
    }

    private static string CatalogKey(int qcItemType, string qcItemNo) => $"{qcItemType}\u001f{qcItemNo}";

    private sealed record CatalogSpec(int QcItemType, string TableName, string NumberColumn, string NameColumn);
    private sealed record NormalizedWrite(
        Qc217MainKey Key,
        int EqpDataType,
        int? PeriodMin,
        int? EquipmentState,
        int? LotCount,
        string MailGroupNo,
        string CcMailGroupNo,
        IReadOnlyList<Qc217InspectionDetailRequest> Details);
}

public sealed record Qc217MainKey(string OpNo, string EquipmentNo, int InspType, int InspWeek, string InspStartTime);
public sealed record Qc217DeleteResult(string Result, int? IssueState, int ClearedDetailRows, bool DetailsWereShared);
