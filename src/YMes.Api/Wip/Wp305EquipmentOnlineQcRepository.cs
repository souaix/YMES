using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp305EquipmentOnlineQcRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxCandidateCount = 200;
    private const int MaxQcListDetails = 500;
    private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
    private static readonly JsonSerializerOptions HashOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<Wp305EquipmentTypeItem>> GetEquipmentTypesAsync(
        Wp305EquipmentTypeQuery query, CancellationToken token)
    {
        var keyword = OptionalText(query.Keyword, 100, "Keyword");
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        var items = new List<Wp305EquipmentTypeItem>();
        await using (var command = new NpgsqlCommand("""
            SELECT DISTINCT equipmenttype COLLATE "C" AS equipmenttype
            FROM "YMES".tbleqpequipmentbasis
            WHERE issuestate=2
              AND (@keyword IS NULL OR strpos(upper(equipmenttype),upper(@keyword))>0)
            ORDER BY equipmenttype COLLATE "C"
            LIMIT @take;
            """, connection, transaction))
        {
            Text(command, "keyword", keyword);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxCandidateCount + 1;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) items.Add(new(reader.GetString(0)));
        }
        if (items.Count > MaxCandidateCount)
            throw Bad(413, $"設備類別超過 {MaxCandidateCount} 項，請縮小搜尋條件。" );
        await transaction.CommitAsync(token);
        return items;
    }

    public async Task<Wp305Page<Wp305EquipmentItem>> GetEquipmentAsync(
        Wp305EquipmentLookupQuery query, CancellationToken token)
    {
        var equipmentType = OptionalText(query.EquipmentType, 50, "EquipmentType");
        var keyword = OptionalText(query.Keyword, 100, "Keyword");
        var page = NormalizePage(query.Page, query.PageSize);
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        var rows = new List<Wp305EquipmentItem>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT equipmentno,equipmenttype,equipmentname
            FROM "YMES".tbleqpequipmentbasis
            WHERE issuestate=2
              AND (@equipmentType IS NULL OR equipmenttype=@equipmentType)
              AND (@keyword IS NULL OR strpos(upper(equipmentno),upper(@keyword))>0
                   OR strpos(upper(equipmenttype),upper(@keyword))>0
                   OR strpos(upper(COALESCE(equipmentname,'')),upper(@keyword))>0)
            ORDER BY equipmenttype COLLATE "C",equipmentno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            Text(command, "equipmentType", equipmentType);
            Text(command, "keyword", keyword);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                rows.Add(new(reader.GetString(0), reader.GetString(1), NullableText(reader, 2)));
        }
        var result = FinishPage(page, rows);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<IReadOnlyList<Wp305Candidate>> GetCandidatesAsync(
        Wp305CandidateQuery query, CancellationToken token)
    {
        var equipmentNo = RequiredText(query.EquipmentNo, 50, "EquipmentNo").ToUpperInvariant();
        var mode = Mode(query.Mode);
        var lotSerial = OptionalText(query.LotSerial, 55, "LotSerial");
        var logGroupSerial = OptionalText(query.LogGroupSerial, 55, "LogGroupSerial");
        if (query.CheckIn && (mode != "wait" || lotSerial is null || logGroupSerial is null))
            throw Bad(400, "報工 Check-in 候檢需要 Wait 模式、LotSerial 與 LogGroupSerial。" );
        if (!query.CheckIn && (lotSerial is not null || logGroupSerial is not null))
            throw Bad(400, "獨立點檢不可指定 LotSerial 或 LogGroupSerial。" );

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        await RequireEquipmentAsync(connection, transaction, equipmentNo, token);
        if (query.CheckIn)
            await RequireCheckInContextAsync(connection, transaction, equipmentNo, lotSerial!, logGroupSerial!, token);

        var candidates = await ReadCandidatesAsync(connection, transaction, equipmentNo, mode, query.CheckIn, token);
        await transaction.CommitAsync(token);
        return candidates;
    }

    public async Task<Wp305QcList> GetQcListAsync(
        string qcListNo, Wp305QcListQuery query, CancellationToken token)
    {
        var equipmentNo = RequiredText(query.EquipmentNo, 50, "EquipmentNo").ToUpperInvariant();
        var normalizedQcListNo = RequiredText(qcListNo, 100, "QCListNo");
        var mode = Mode(query.Mode);
        var taskNo = OptionalText(query.QcTaskNo, 30, "QCTaskNo");
        var lotSerial = OptionalText(query.LotSerial, 55, "LotSerial");
        var logGroupSerial = OptionalText(query.LogGroupSerial, 55, "LogGroupSerial");
        if (query.CheckIn && (mode != "wait" || taskNo is null || lotSerial is null || logGroupSerial is null))
            throw Bad(400, "報工 Check-in 點檢表需要 Wait 模式、候檢任務、LotSerial 與 LogGroupSerial。" );
        if (!query.CheckIn && (lotSerial is not null || logGroupSerial is not null))
            throw Bad(400, "獨立點檢不可指定 LotSerial 或 LogGroupSerial。" );
        if (mode == "recheck" && taskNo is null)
            throw Bad(400, "補驗點檢表必須指定候選 QCTaskNo。" );

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        await RequireEquipmentAsync(connection, transaction, equipmentNo, token);
        if (query.CheckIn)
            await RequireCheckInContextAsync(connection, transaction, equipmentNo, lotSerial!, logGroupSerial!, token);

        if (taskNo is not null)
            await RequireEligibleTaskAsync(connection, transaction, equipmentNo, normalizedQcListNo, taskNo, mode, query.CheckIn, token);
        else
        {
            if (query.CheckIn || mode != "wait")
                throw Bad(400, "只有獨立 Wait QC 可載入非任務型點檢表。" );
            await RequireManualQcListAsync(connection, transaction, equipmentNo, normalizedQcListNo, null, null, token);
        }

        var result = await ReadQcListAsync(connection, transaction, normalizedQcListNo, token);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp305InspectionSaveResult> CreateAsync(
        Wp305InspectionSaveRequest request, string actor, CancellationToken token)
    {
        var requestId = ParseRequestId(request.RequestId);
        var equipmentNo = RequiredText(request.EquipmentNo, 50, "EquipmentNo").ToUpperInvariant();
        var qcListNo = RequiredText(request.QcListNo, 100, "QCListNo");
        var taskNo = OptionalText(request.QcTaskNo, 30, "QCTaskNo");
        var mode = Mode(request.Mode);
        actor = RequiredText(actor, 10, "登入者");
        if (request.CheckIn || OptionalText(request.LotSerial, 55, "LotSerial") is not null
            || OptionalText(request.LogGroupSerial, 55, "LogGroupSerial") is not null)
            throw Bad(409, "報工 Check-in 點檢存檔尚未接通完整的下機交易；本次未寫入任何資料。" );
        if (taskNo is null && mode != "wait") throw Bad(400, "只有 Wait QC 可建立非任務型點檢。" );
        if (taskNo is not null && request.InspType is not null)
            throw Bad(400, "任務型點檢的 InspType 必須由伺服器讀取，不接受用戶端指定。" );
        if (taskNo is null && request.InspType is null)
            throw Bad(400, "非任務型點檢需傳入候選清單的 InspType。" );
        var inspStartTime = taskNo is null
            ? RequiredText(request.InspStartTime, 20, "InspStartTime")
            : null;
        var description = OptionalText(request.Description, 255, "Description");
        var inputs = ValidateRequestDetails(request.Details);
        var payloadHash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, HashOptions))));
        var idempotencySerial = GenerateQcListSerial();

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await using (var timeout = new NpgsqlCommand("SET LOCAL statement_timeout='30s';", connection, transaction))
            await timeout.ExecuteNonQueryAsync(token);
        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@requestId, 0));", connection, transaction))
        {
            Text(lockCommand, "requestId", requestId.ToString("D"));
            await lockCommand.ExecuteNonQueryAsync(token);
        }

        var replay = await FindIdempotencyAsync(connection, transaction, requestId, token);
        if (replay is not null)
        {
            if (!StringComparer.Ordinal.Equals(replay.PayloadHash, payloadHash)
                || !StringComparer.Ordinal.Equals(replay.Creator, actor))
                throw Bad(409, "此 RequestId 已用於不同內容或不同登入者；未重複寫入。" );
            var replayResult = await ReadSavedResultAsync(connection, transaction, replay.QcListSerial, true, token)
                ?? throw Bad(503, "找到點檢冪等記錄，但對應點檢單不存在；請聯絡管理者檢查資料一致性。" );
            await transaction.CommitAsync(token);
            return replayResult;
        }

        await RequireEquipmentAsync(connection, transaction, equipmentNo, token);
        TaskSnapshot? task = null;
        int inspType;
        string taskStartTime;
        if (taskNo is not null)
        {
            var expectedStatus = mode == "recheck" ? 2 : 0;
            task = await ReadTaskForUpdateAsync(connection, transaction, taskNo, token)
                ?? throw Bad(404, "找不到候檢任務。" );
            if (!StringComparer.Ordinal.Equals(task.EquipmentNo, equipmentNo)
                || task.TaskClass != 1
                || task.Status != expectedStatus
                || !StringComparer.Ordinal.Equals(task.QcListNo, qcListNo))
                throw Bad(409, "候檢任務設備、點檢表或狀態已變更；請重新載入候選清單。" );
            if (await HasActiveTaskEquipmentContextAsync(connection, transaction, task, equipmentNo, token))
                throw Bad(409, "此任務目前連結報工中的設備批次；Check-in 存檔尚未接通完整下機交易，本次未寫入任何資料。" );
            inspType = task.InspType;
            taskStartTime = NormalizeStartTime(task.InspStartTime);
        }
        else
        {
            if (mode != "wait") throw Bad(400, "補驗點檢必須指定 QCTaskNo。" );
            inspType = request.InspType!.Value;
            taskStartTime = inspStartTime!;
            await RequireManualQcListAsync(connection, transaction, equipmentNo, qcListNo,
                inspType, taskStartTime, token);
        }

        var qcList = await ReadQcListAsync(connection, transaction, qcListNo, token);
        if (qcList.Details.Count == 0) throw Bad(409, "點檢表沒有檢查項目，無法存檔。" );
        if (qcList.Details.Count > MaxQcListDetails) throw Bad(413, $"點檢表超過 {MaxQcListDetails} 個檢查項目。" );
        var calculated = Calculate(qcList.Details, inputs);
        var overall = calculated.Any(item => item.QcResult == -1) ? -1
            : calculated.Any(item => item.QcResult == 1) ? 1 : 0;
        if (overall == -1 && !request.AllowPartial)
            throw Bad(400, "點檢項目尚未完成；獨立模式需確認部分存檔後才可送出。" );
        if (overall == 1 && qcList.UnValidAction is not null and not -1)
            throw Bad(409, "此點檢表的 UnValidAction 會改變設備狀態；相關設備下機交易尚未接通，本次完全未寫入。" );

        var now = TaipeiNow();
        var qcListSerial = idempotencySerial;
        await InsertHeaderAsync(connection, transaction, qcListSerial, equipmentNo, qcList, task,
            inspType, taskStartTime, null, null, overall, description, actor, now, token);
        foreach (var item in calculated)
            await InsertDetailAsync(connection, transaction, qcListSerial, item, token);
        if (task is not null)
        {
            await using var updateTask = new NpgsqlCommand("""
                UPDATE "YMES".tblwipeqpqctask
                SET status=1,qcserialno=@serial
                WHERE qctaskno=@taskNo AND equipmentno=@equipmentNo
                  AND qctaskclass=1 AND status=@expectedStatus;
                """, connection, transaction);
            Text(updateTask, "serial", qcListSerial);
            Text(updateTask, "taskNo", task.TaskNo);
            Text(updateTask, "equipmentNo", equipmentNo);
            updateTask.Parameters.Add("expectedStatus", NpgsqlDbType.Integer).Value = task.Status;
            if (await updateTask.ExecuteNonQueryAsync(token) != 1)
                throw Bad(409, "候檢任務已由其他操作完成；點檢紀錄已回滾。" );
        }

        await using (var saveKey = new NpgsqlCommand("""
            INSERT INTO "YMES".ym_wp305_request(requestid,creator,payloadsha256,qclistserial,createdate)
            VALUES(@requestId,@actor,@hash,@serial,@createdate);
            """, connection, transaction))
        {
            saveKey.Parameters.Add("requestId", NpgsqlDbType.Uuid).Value = requestId;
            Text(saveKey, "actor", actor);
            Text(saveKey, "hash", payloadHash);
            Text(saveKey, "serial", qcListSerial);
            Timestamp(saveKey, "createdate", now);
            await saveKey.ExecuteNonQueryAsync(token);
        }

        await transaction.CommitAsync(token);
        return new(qcListSerial, equipmentNo, qcListNo, task?.TaskNo, overall, now, false);
    }

    private async Task<IReadOnlyList<Wp305Candidate>> ReadCandidatesAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentNo,
        string mode, bool checkIn, CancellationToken token)
    {
        var rows = new List<Wp305Candidate>(MaxCandidateCount + 1);
        var sql = checkIn ? CheckInCandidatesSql : mode == "recheck" ? RecheckCandidatesSql : WaitCandidatesSql;
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            Text(command, "equipmentNo", equipmentNo);
            if (checkIn)
            {
                var today = TaipeiNow().Date;
                Timestamp(command, "dayStart", today);
                Timestamp(command, "dayEnd", today.AddDays(1));
            }
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxCandidateCount + 1;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                rows.Add(new(
                    NullableText(reader, 0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3),
                    reader.GetString(4), NullableDateTime(reader, 5), NullableText(reader, 6),
                    NullableText(reader, 7), NullableInt(reader, 8)));
            }
        }
        if (rows.Count > MaxCandidateCount)
            throw Bad(413, $"候檢清單超過 {MaxCandidateCount} 筆，請先縮小設備範圍或清理逾期任務。" );
        return rows;
    }

    private const string WaitCandidatesSql = """
        WITH candidates AS (
            SELECT NULL::varchar(30) AS qctaskno,e.qclistno,q.qctype::int AS insptype,
                   'N/A'::varchar(20) AS inspstarttime,NULL::timestamp AS planqctime,
                   NULL::varchar(20) AS shiftno,NULL::varchar(50) AS lotno,NULL::int AS status
            FROM "YMES".tbleqpequipmentbasis e
            JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=e.qclistno
            WHERE e.equipmentno=@equipmentNo AND e.issuestate=2 AND NULLIF(btrim(e.qclistno),'') IS NOT NULL
            UNION
            SELECT NULL::varchar(30),s.qclistno,s.insptype::int,s.inspstarttime,
                   NULL::timestamp,NULL::varchar(20),NULL::varchar(50),NULL::int
            FROM "YMES".tbleqpqclistsetup s
            JOIN "YMES".tbleqpequipmentbasis e ON e.equipmentno=s.equipmentno AND e.issuestate=2
            JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=s.qclistno
            WHERE s.equipmentno=@equipmentNo
            UNION
            SELECT t.qctaskno,t.qcno,t.insptype::int,
                   COALESCE(NULLIF(btrim(t.inspstarttime),''),'N/A'),t.planqctime,t.shiftno,t.lotno,t.status::int
            FROM "YMES".tblwipeqpqctask t
            JOIN "YMES".tbleqpequipmentbasis e ON e.equipmentno=t.equipmentno AND e.issuestate=2
            JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=t.qcno
            WHERE t.equipmentno=@equipmentNo AND t.qctaskclass=1 AND t.status=0
        )
        SELECT c.qctaskno,c.qclistno,q.qclistname,c.insptype,c.inspstarttime,c.planqctime,
               c.shiftno,c.lotno,c.status
        FROM candidates c
        JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=c.qclistno
        ORDER BY c.qclistno COLLATE "C",c.insptype,c.inspstarttime COLLATE "C",
                 c.planqctime DESC NULLS LAST,c.qctaskno COLLATE "C" NULLS FIRST
        LIMIT @take;
        """;

    private const string RecheckCandidatesSql = """
        SELECT t.qctaskno,t.qcno,q.qclistname,t.insptype::int,
               COALESCE(NULLIF(btrim(t.inspstarttime),''),'N/A'),t.planqctime,
               t.shiftno,t.lotno,t.status::int
        FROM "YMES".tblwipeqpqctask t
        JOIN "YMES".tbleqpequipmentbasis e ON e.equipmentno=t.equipmentno AND e.issuestate=2
        JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=t.qcno
        WHERE t.equipmentno=@equipmentNo AND t.qctaskclass=1 AND t.status=2
        ORDER BY t.qcno COLLATE "C",t.insptype,t.inspstarttime COLLATE "C",
                 t.planqctime DESC NULLS LAST,t.qctaskno COLLATE "C"
        LIMIT @take;
        """;

    private const string CheckInCandidatesSql = """
        WITH scheduled AS (
            SELECT t.qctaskno,t.qcno,t.insptype::int AS insptype,
                   COALESCE(NULLIF(btrim(t.inspstarttime),''),'N/A') AS inspstarttime,
                   t.planqctime,t.shiftno,t.lotno,t.status::int AS status,
                   row_number() OVER (
                       PARTITION BY t.qcno,t.insptype,COALESCE(NULLIF(btrim(t.inspstarttime),''),'N/A')
                       ORDER BY t.planqctime DESC NULLS LAST,t.qctaskno COLLATE "C") AS schedule_rank
            FROM "YMES".tblwipeqpqctask t
            JOIN "YMES".tbleqpqclistsetup s
              ON s.equipmentno=t.equipmentno AND s.qclistno=t.qcno
             AND s.insptype=t.insptype
             AND s.inspstarttime=COALESCE(NULLIF(btrim(t.inspstarttime),''),'N/A')
            JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=t.qcno
            JOIN "YMES".tbleqpequipmentbasis e ON e.equipmentno=t.equipmentno AND e.issuestate=2
            WHERE t.equipmentno=@equipmentNo AND t.qctaskclass=1 AND t.status=0
              AND t.belongdate>=@dayStart AND t.belongdate<@dayEnd
        )
        SELECT s.qctaskno,s.qcno,q.qclistname,s.insptype,s.inspstarttime,s.planqctime,
               s.shiftno,s.lotno,s.status
        FROM scheduled s
        JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=s.qcno
        WHERE s.schedule_rank=1
        ORDER BY s.qcno COLLATE "C",s.insptype,s.inspstarttime COLLATE "C",s.planqctime DESC NULLS LAST
        LIMIT @take;
        """;

    private async Task<Wp305QcList> ReadQcListAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string qcListNo, CancellationToken token)
    {
        string qcListName;
        int qcType;
        int? unValidAction;
        string? description;
        int afterEventQc;
        await using (var command = new NpgsqlCommand("""
            SELECT qclistname,qctype::int,unvalidaction::int,description,aftereventqc::int
            FROM "YMES".tbleqpqclistbasis WHERE qclistno=@qcListNo;
            """, connection, transaction))
        {
            Text(command, "qcListNo", qcListNo);
            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token)) throw Bad(404, "找不到指定的設備點檢表。" );
            qcListName = reader.GetString(0);
            qcType = reader.GetInt32(1);
            unValidAction = NullableInt(reader, 2);
            description = NullableText(reader, 3);
            afterEventQc = reader.GetInt32(4);
        }

        var details = new List<Wp305QcListDetail>();
        await using (var command = new NpgsqlCommand("""
            SELECT qcorder,qcitem,qctype::int,inputdatacount::int,stdvalue,minivalue,maxivalue,
                   inspectionmethod,inspectionstandards
            FROM "YMES".tbleqpqclistdetail
            WHERE qclistno=@qcListNo
            ORDER BY qcorder,qcitem COLLATE "C"
            LIMIT @take;
            """, connection, transaction))
        {
            Text(command, "qcListNo", qcListNo);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxQcListDetails + 1;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                details.Add(new(reader.GetDecimal(0),reader.GetString(1),reader.GetInt32(2),reader.GetInt32(3),
                    NullableText(reader, 4),NullableText(reader, 5),NullableText(reader, 6),
                    NullableText(reader, 7),NullableText(reader, 8)));
            }
        }
        if (details.Count > MaxQcListDetails)
            throw Bad(413, $"點檢表超過 {MaxQcListDetails} 個檢查項目。" );
        return new(qcListNo,qcListName,qcType,unValidAction,description,afterEventQc,details);
    }

    private async Task RequireEquipmentAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentNo, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tbleqpequipmentbasis
            WHERE equipmentno=@equipmentNo AND issuestate=2;
            """, connection, transaction);
        Text(command, "equipmentNo", equipmentNo);
        if (await command.ExecuteScalarAsync(token) is null)
            throw Bad(404, "找不到 Issue=2 的有效設備，請重新查詢設備主檔。" );
    }

    private async Task RequireCheckInContextAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentNo,
        string lotSerial, string logGroupSerial, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM "YMES".tblwiptemp_equipment
                WHERE equipmentno=@equipmentNo AND lotserial=@lotSerial AND loggroupserial=@logGroupSerial
            );
            """, connection, transaction);
        Text(command, "equipmentNo", equipmentNo);
        Text(command, "lotSerial", lotSerial);
        Text(command, "logGroupSerial", logGroupSerial);
        var exists = (bool)(await command.ExecuteScalarAsync(token) ?? false);
        if (!exists)
            throw Bad(409, "目前設備、LotSerial 與 LogGroupSerial 不符合有效設備上機資料。" );
    }

    private async Task RequireEligibleTaskAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentNo, string qcListNo,
        string taskNo, string mode, bool checkIn, CancellationToken token)
    {
        var expectedStatus = mode == "recheck" ? 2 : 0;
        TaskSnapshot? task;
        await using (var command = new NpgsqlCommand("""
            SELECT qctaskno,equipmentno,qctaskclass::int,insptype::int,shiftno,lotno,
                   inspstarttime,qcno,status::int,belongdate,planqctime
            FROM "YMES".tblwipeqpqctask WHERE qctaskno=@taskNo;
            """, connection, transaction))
        {
            Text(command, "taskNo", taskNo);
            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token)) throw Bad(404, "找不到指定的候檢任務。" );
            task = MapTask(reader);
        }
        if (!StringComparer.Ordinal.Equals(task.EquipmentNo,equipmentNo)
            || task.TaskClass!=1 || task.Status!=expectedStatus
            || !StringComparer.Ordinal.Equals(task.QcListNo,qcListNo))
            throw Bad(409,"候檢任務設備、點檢表或狀態已變更，請重新載入候選清單。" );
        if (checkIn)
            await RequireScheduledCheckInTaskAsync(connection,transaction,task,token);
    }

    private async Task RequireScheduledCheckInTaskAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, TaskSnapshot task, CancellationToken token)
    {
        var today=TaipeiNow().Date;
        await using var verify = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1
                FROM "YMES".tblwipeqpqctask t
                JOIN "YMES".tbleqpqclistsetup s
                  ON s.equipmentno=t.equipmentno AND s.qclistno=t.qcno AND s.insptype=t.insptype
                 AND s.inspstarttime=COALESCE(NULLIF(btrim(t.inspstarttime),''),'N/A')
                WHERE t.qctaskno=@taskNo AND t.qctaskclass=1 AND t.status=0
                  AND t.belongdate>=@dayStart AND t.belongdate<@dayEnd
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "YMES".tblwipeqpqctask newer
                      JOIN "YMES".tbleqpqclistsetup newerSetup
                        ON newerSetup.equipmentno=newer.equipmentno AND newerSetup.qclistno=newer.qcno
                       AND newerSetup.insptype=newer.insptype
                       AND newerSetup.inspstarttime=COALESCE(NULLIF(btrim(newer.inspstarttime),''),'N/A')
                      WHERE newer.equipmentno=t.equipmentno AND newer.qcno=t.qcno
                        AND newer.insptype=t.insptype AND newer.status=0 AND newer.qctaskclass=1
                        AND newer.belongdate>=@dayStart AND newer.belongdate<@dayEnd
                        AND COALESCE(NULLIF(btrim(newer.inspstarttime),''),'N/A')
                            =COALESCE(NULLIF(btrim(t.inspstarttime),''),'N/A')
                        AND (newer.planqctime>t.planqctime
                             OR (newer.planqctime IS NOT DISTINCT FROM t.planqctime
                                 AND newer.qctaskno COLLATE "C"<t.qctaskno COLLATE "C")
                             OR (t.planqctime IS NULL AND newer.planqctime IS NOT NULL))
                  )
            );
            """, connection, transaction);
        Text(verify,"taskNo",task.TaskNo);
        Timestamp(verify,"dayStart",today);
        Timestamp(verify,"dayEnd",today.AddDays(1));
        var exists=(bool)(await verify.ExecuteScalarAsync(token) ?? false);
        if(!exists) throw Bad(409,"此任務不符合當日設備點檢設定或頻率，請重新整理候檢清單。" );
    }

    private async Task RequireManualQcListAsync(
        NpgsqlConnection connection,NpgsqlTransaction transaction,string equipmentNo,string qcListNo,
        int? inspType,string? startTime,CancellationToken token)
    {
        if (inspType is null || startTime is null)
        {
            await using var any = new NpgsqlCommand("""
                SELECT EXISTS (
                    SELECT 1 FROM "YMES".tbleqpqclistsetup
                    WHERE equipmentno=@equipmentNo AND qclistno=@qcListNo
                    UNION ALL
                    SELECT 1 FROM "YMES".tbleqpequipmentbasis e
                    JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=e.qclistno
                    WHERE e.equipmentno=@equipmentNo AND e.qclistno=@qcListNo
                );
                """,connection,transaction);
            Text(any,"equipmentNo",equipmentNo); Text(any,"qcListNo",qcListNo);
            if(!(bool)(await any.ExecuteScalarAsync(token) ?? false))
                throw Bad(404,"此設備沒有設定該點檢表。" );
            return;
        }
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM "YMES".tbleqpqclistsetup
                WHERE equipmentno=@equipmentNo AND qclistno=@qcListNo
                  AND insptype=@inspType AND inspstarttime=@startTime
                UNION ALL
                SELECT 1 FROM "YMES".tbleqpequipmentbasis e
                JOIN "YMES".tbleqpqclistbasis q ON q.qclistno=e.qclistno
                WHERE e.equipmentno=@equipmentNo AND e.qclistno=@qcListNo AND q.qctype=@inspType
                  AND @startTime='N/A'
            );
            """,connection,transaction);
        Text(command,"equipmentNo",equipmentNo); Text(command,"qcListNo",qcListNo);
        command.Parameters.Add("inspType",NpgsqlDbType.Integer).Value=inspType.Value;
        Text(command,"startTime",startTime);
        if(!(bool)(await command.ExecuteScalarAsync(token) ?? false))
            throw Bad(404,"設備、點檢表、檢查頻率或起始時間不符合有效設定。" );
    }

    private async Task<TaskSnapshot?> ReadTaskForUpdateAsync(
        NpgsqlConnection connection,NpgsqlTransaction transaction,string taskNo,CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT qctaskno,equipmentno,qctaskclass::int,insptype::int,shiftno,lotno,
                   inspstarttime,qcno,status::int,belongdate,planqctime
            FROM "YMES".tblwipeqpqctask WHERE qctaskno=@taskNo FOR UPDATE;
            """,connection,transaction);
        Text(command,"taskNo",taskNo);
        await using var reader=await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? MapTask(reader) : null;
    }

    private async Task<bool> HasActiveTaskEquipmentContextAsync(
        NpgsqlConnection connection,NpgsqlTransaction transaction,TaskSnapshot task,string equipmentNo,CancellationToken token)
    {
        if(string.IsNullOrWhiteSpace(task.LotNo)) return false;
        await using var command=new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM "YMES".tblwiptemp_equipment
                WHERE equipmentno=@equipmentNo AND lotno=@lotNo
            );
            """,connection,transaction);
        Text(command,"equipmentNo",equipmentNo); Text(command,"lotNo",task.LotNo);
        return (bool)(await command.ExecuteScalarAsync(token) ?? false);
    }

    private async Task InsertHeaderAsync(
        NpgsqlConnection connection,NpgsqlTransaction transaction,string serial,string equipmentNo,Wp305QcList qcList,
        TaskSnapshot? task,int inspType,string startTime,string? lotSerial,string? logGroupSerial,int overall,
        string? description,string actor,DateTime now,CancellationToken token)
    {
        await using var command=new NpgsqlCommand("""
            INSERT INTO "YMES".tblwipeqpqclistlog
                (qclistserial,equipmentno,qclistno,lotserial,loggroupserial,qcresult,description,creator,createdate,
                 qctaskno,insptype,shiftno,lotno,inspstarttime,qctaskstatus,belongdate,planqctime)
            VALUES(@serial,@equipmentNo,@qcListNo,@lotSerial,@logGroupSerial,@qcResult,@description,@actor,@createdate,
                   @taskNo,@inspType,@shiftNo,@lotNo,@startTime,@taskStatus,@belongDate,@planQcTime);
            """,connection,transaction);
        Text(command,"serial",serial); Text(command,"equipmentNo",equipmentNo); Text(command,"qcListNo",qcList.QcListNo);
        Text(command,"lotSerial",lotSerial); Text(command,"logGroupSerial",logGroupSerial);
        command.Parameters.Add("qcResult",NpgsqlDbType.Integer).Value=overall;
        Text(command,"description",description); Text(command,"actor",actor); Timestamp(command,"createdate",now);
        Text(command,"taskNo",task?.TaskNo);
        command.Parameters.Add("inspType",NpgsqlDbType.Integer).Value=inspType;
        Text(command,"shiftNo",task?.ShiftNo); Text(command,"lotNo",task?.LotNo); Text(command,"startTime",startTime);
        if(task is null)
        {
            command.Parameters.Add("taskStatus",NpgsqlDbType.Integer).Value=DBNull.Value;
            command.Parameters.Add("belongDate",NpgsqlDbType.Timestamp).Value=DBNull.Value;
            command.Parameters.Add("planQcTime",NpgsqlDbType.Timestamp).Value=DBNull.Value;
        }
        else
        {
            command.Parameters.Add("taskStatus",NpgsqlDbType.Integer).Value=task.Status;
            Timestamp(command,"belongDate",task.BelongDate);
            NullableTimestamp(command,"planQcTime",task.PlanQcTime);
        }
        if(await command.ExecuteNonQueryAsync(token)!=1) throw Bad(503,"設備點檢表頭未能寫入；交易已回滾。" );
    }

    private static async Task InsertDetailAsync(
        NpgsqlConnection connection,NpgsqlTransaction transaction,string serial,CalculatedDetail item,CancellationToken token)
    {
        await using var command=new NpgsqlCommand("""
            INSERT INTO "YMES".tblwipeqpqclistdetail
                (qclistserial,qcitem,qcresult,inputvalue,stdvalue,maxivalue,minivalue,inputdatacount,qcorder,qctype,
                 d01,d02,d03,d04,d05,d06,d07,d08,d09,d10)
            VALUES(@serial,@qcItem,@qcResult,@inputValue,@stdValue,@maxValue,@minValue,@inputCount,@qcOrder,@qcType,
                   @d01,@d02,@d03,@d04,@d05,@d06,@d07,@d08,@d09,@d10);
            """,connection,transaction);
        Text(command,"serial",serial); Text(command,"qcItem",item.QcItem);
        command.Parameters.Add("qcResult",NpgsqlDbType.Integer).Value=item.QcResult;
        Text(command,"inputValue",item.InputValue); Text(command,"stdValue",item.StdValue);
        Text(command,"maxValue",item.MaxiValue); Text(command,"minValue",item.MiniValue);
        command.Parameters.Add("inputCount",NpgsqlDbType.Integer).Value=item.InputDataCount;
        command.Parameters.Add("qcOrder",NpgsqlDbType.Numeric).Value=item.QcOrder;
        command.Parameters.Add("qcType",NpgsqlDbType.Integer).Value=item.QcType;
        for(var index=0;index<10;index++) Text(command,$"d{index+1:00}",index<item.Readings.Count?item.Readings[index]:null);
        if(await command.ExecuteNonQueryAsync(token)!=1) throw Bad(503,"設備點檢明細未能完整寫入；交易已回滾。" );
    }

    private async Task<IdempotencyEntry?> FindIdempotencyAsync(
        NpgsqlConnection connection,NpgsqlTransaction transaction,Guid requestId,CancellationToken token)
    {
        await using var command=new NpgsqlCommand("""
            SELECT creator,payloadsha256,qclistserial FROM "YMES".ym_wp305_request WHERE requestid=@requestId;
            """,connection,transaction);
        command.Parameters.Add("requestId",NpgsqlDbType.Uuid).Value=requestId;
        await using var reader=await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token)?new(reader.GetString(0),reader.GetString(1),reader.GetString(2)):null;
    }

    private async Task<Wp305InspectionSaveResult?> ReadSavedResultAsync(
        NpgsqlConnection connection,NpgsqlTransaction transaction,string serial,bool replayed,CancellationToken token)
    {
        await using var command=new NpgsqlCommand("""
            SELECT qclistserial,equipmentno,qclistno,qctaskno,qcresult::int,createdate
            FROM "YMES".tblwipeqpqclistlog WHERE qclistserial=@serial;
            """,connection,transaction);
        Text(command,"serial",serial);
        await using var reader=await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token)
            ?new(reader.GetString(0),reader.GetString(1),reader.GetString(2),NullableText(reader,3),reader.GetInt32(4),reader.GetDateTime(5),replayed)
            :null;
    }

    private static List<Wp305InspectionInput> ValidateRequestDetails(IReadOnlyList<Wp305InspectionInput>? details)
    {
        if(details is null||details.Count is <1 or >MaxQcListDetails) throw Bad(400,$"Details 必須有 1 至 {MaxQcListDetails} 筆。" );
        var rows=details.ToList();
        if(rows.Select(row=>row.QcOrder).Distinct().Count()!=rows.Count) throw Bad(400,"QCOrder 不可重複。" );
        foreach(var row in rows)
        {
            if(row.QcOrder is < -99 or > 99) throw Bad(400,"QCOrder 超出來源欄位範圍。" );
            if(row.Readings is {Count:>10}) throw Bad(400,"D01–D10 最多輸入 10 個量測值。" );
            row.InputValue=OptionalText(row.InputValue,50,"InputValue");
            if(row.Readings is not null)
                row.Readings=row.Readings.Select(reading=>OptionalText(reading,50,"Reading")).ToList();
            if(row.MessageChoice is not null&&!StringComparer.Ordinal.Equals(row.MessageChoice,"Y")
                &&!StringComparer.Ordinal.Equals(row.MessageChoice,"N")) throw Bad(400,"MessageChoice 僅接受 Y 或 N。" );
        }
        return rows;
    }

    private static List<CalculatedDetail> Calculate(
        IReadOnlyList<Wp305QcListDetail> source,IReadOnlyList<Wp305InspectionInput> inputs)
    {
        var byOrder=inputs.ToDictionary(row=>row.QcOrder);
        if(source.Count!=inputs.Count||source.Any(row=>!byOrder.ContainsKey(row.QcOrder)))
            throw Bad(400,"送出的檢查項目與伺服器點檢表不一致，請重新載入。" );
        if(source.Select(row=>row.QcItem).Distinct(StringComparer.Ordinal).Count()!=source.Count)
            throw Bad(409,"點檢表含重複 QCItem，與結果明細來源主鍵衝突；未寫入。" );

        var output=new List<CalculatedDetail>(source.Count);
        foreach(var spec in source)
        {
            if(spec.InputDataCount is <1 or >10) throw Bad(409,$"點檢項目 {spec.QcItem} 的 InputDataCount 不在 1 至 10 範圍。" );
            var input=byOrder[spec.QcOrder];
            var extraReadings=input.Readings?.Skip(spec.InputDataCount).Any(value=>!string.IsNullOrEmpty(value))==true;
            if(extraReadings || (spec.InputDataCount==1 && input.Readings?.Any(value=>!string.IsNullOrEmpty(value))==true)
                || (spec.InputDataCount>1 && !string.IsNullOrEmpty(input.InputValue)))
                throw Bad(400,$"點檢項目 {spec.QcItem} 的輸入欄位與 InputDataCount 不符；未捨棄多餘輸入。" );
            if(spec.QcType==2 && (!string.IsNullOrEmpty(input.InputValue)
                || input.Readings?.Any(value=>!string.IsNullOrEmpty(value))==true))
                throw Bad(400,$"點檢項目 {spec.QcItem} 為訊息確認項目，請只傳 MessageChoice。" );
            var values=spec.InputDataCount==1
                ?new List<string>{NormalizeInput(input.InputValue)}
                :Enumerable.Range(0,spec.InputDataCount).Select(index=>NormalizeInput(input.Readings is not null&&index<input.Readings.Count?input.Readings[index]:null)).ToList();
            var qcResult=-1;
            string? savedInput=null;
            var readings=new string?[10];

            switch(spec.QcType)
            {
                case 0:
                case 1:
                    if(values.Any(string.IsNullOrEmpty))
                    {
                        qcResult=-1;
                    }
                    else
                    {
                        var numbers=values.Select(value=>ParseDecimal(value,spec.QcItem)).ToArray();
                        if(spec.QcType==0)
                        {
                            if(!decimal.TryParse(spec.StdValue,NumberStyles.Float,CultureInfo.InvariantCulture,out var standard))
                                throw Bad(409,$"點檢項目 {spec.QcItem} 的 StdValue 不是有效數字。" );
                            qcResult=numbers.All(value=>value==standard)?0:1;
                        }
                        else
                        {
                            var lower=ReadBound(spec.MiniValue,spec.QcItem,"MiniValue");
                            var upper=ReadBound(spec.MaxiValue,spec.QcItem,"MaxiValue");
                            if(lower.HasValue&&upper.HasValue&&lower.Value>upper.Value)
                                throw Bad(409,$"點檢項目 {spec.QcItem} 的 MiniValue 大於 MaxiValue。" );
                            qcResult=numbers.All(value=>(!lower.HasValue||value>=lower.Value)&&(!upper.HasValue||value<=upper.Value))?0:1;
                        }
                    }
                    break;
                case 2:
                    if(input.MessageChoice is null) qcResult=-1;
                    else { qcResult=input.MessageChoice=="Y"?0:1; savedInput=input.MessageChoice; }
                    break;
                case 3:
                    if(values.Any(string.IsNullOrEmpty)) qcResult=-1;
                    else qcResult=0;
                    break;
                default:
                    throw Bad(409,$"點檢項目 {spec.QcItem} 使用不支援的 QCType={spec.QcType}。" );
            }

            if(spec.QcType!=2)
            {
                if(spec.InputDataCount==1) savedInput=values[0].Length==0?null:values[0];
                else for(var index=0;index<spec.InputDataCount;index++) readings[index]=values[index].Length==0?null:values[index];
            }
            output.Add(new(spec.QcOrder,spec.QcItem,spec.QcType,spec.InputDataCount,qcResult,savedInput,
                spec.StdValue,spec.MaxiValue,spec.MiniValue,readings));
        }
        return output;
    }

    private static decimal ParseDecimal(string value,string qcItem)
    {
        if(!decimal.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var result))
            throw Bad(400,$"點檢項目 {qcItem} 僅接受有效數值。" );
        return result;
    }

    private static decimal? ReadBound(string? value,string qcItem,string field)
    {
        if(StringComparer.Ordinal.Equals(value,"*")) return null;
        if(decimal.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var result)) return result;
        throw Bad(409,$"點檢項目 {qcItem} 的 {field} 必須是數字或 *。" );
    }

    private static async Task<NpgsqlTransaction> BeginReadOnlyAsync(NpgsqlConnection connection,CancellationToken token)
    {
        var transaction=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        await using var command=new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL statement_timeout='30s';",connection,transaction);
        await command.ExecuteNonQueryAsync(token);
        return transaction;
    }

    private static string GenerateQcListSerial()
    {
        var token=Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+','-').Replace('/','_');
        return token+RandomNumberGenerator.GetInt32(1,1000).ToString("D3",CultureInfo.InvariantCulture);
    }

    private static Guid ParseRequestId(string? value)
    {
        if(!Guid.TryParseExact(value,"D",out var requestId)||requestId==Guid.Empty)
            throw Bad(400,"RequestId 必須是非空 UUID，供安全重送同一點檢操作。" );
        return requestId;
    }

    private static string Mode(string? value)
    {
        if(StringComparer.OrdinalIgnoreCase.Equals(value,"wait")) return "wait";
        if(StringComparer.OrdinalIgnoreCase.Equals(value,"recheck")) return "recheck";
        throw Bad(400,"Mode 僅接受 wait 或 recheck。" );
    }

    private static string NormalizeStartTime(string? value) => string.IsNullOrWhiteSpace(value)?"N/A":value.Trim();
    private static string NormalizeInput(string? value)=>value?.Trim()??"";

    private static string RequiredText(string? value,int maxLength,string field)=>
        OptionalText(value,maxLength,field)??throw Bad(400,$"{field} 為必填欄位。" );

    private static string? OptionalText(string? value,int maxLength,string field)
    {
        if(string.IsNullOrWhiteSpace(value)) return null;
        var normalized=value.Trim();
        try { _=new UTF8Encoding(false,true).GetByteCount(normalized); }
        catch(EncoderFallbackException) { throw Bad(400,$"{field} 含無效 Unicode。" ); }
        if(normalized.Contains('\0')||normalized.Any(char.IsControl)||normalized.EnumerateRunes().Count()>maxLength)
            throw Bad(400,$"{field} 長度或字元不正確（最多 {maxLength} 字元）。" );
        return normalized;
    }

    private static NormalizedPage NormalizePage(int? page,int? pageSize)
    {
        var p=page??1; var size=pageSize??50;
        if(p is <1 or >MaxPage) throw Bad(400,$"頁碼必須介於 1 到 {MaxPage:N0}。" );
        if(size is <1 or >MaxPageSize) throw Bad(400,$"每頁筆數必須介於 1 到 {MaxPageSize}。" );
        return new(p,size,checked((p-1)*size));
    }

    private static Wp305Page<T> FinishPage<T>(NormalizedPage page,List<T> rows)
    {
        var more=rows.Count>page.PageSize;
        if(more) rows.RemoveAt(rows.Count-1);
        return new(page.Page,page.PageSize,more,rows);
    }

    private static void Text(NpgsqlCommand command,string name,string? value)
    {
        command.Parameters.Add(name,NpgsqlDbType.Varchar).Value=(object?)value??DBNull.Value;
    }

    private static void Text(NpgsqlCommand command,string name,string? value,int maxLength)
    {
        command.Parameters.Add(name,NpgsqlDbType.Varchar,maxLength).Value=(object?)value??DBNull.Value;
    }

    private static void Timestamp(NpgsqlCommand command,string name,DateTime value) =>
        command.Parameters.Add(name,NpgsqlDbType.Timestamp).Value=DateTime.SpecifyKind(value,DateTimeKind.Unspecified);

    private static void NullableTimestamp(NpgsqlCommand command,string name,DateTime? value) =>
        command.Parameters.Add(name,NpgsqlDbType.Timestamp).Value=value is null?(object)DBNull.Value:DateTime.SpecifyKind(value.Value,DateTimeKind.Unspecified);

    private static string? NullableText(NpgsqlDataReader reader,int index)=>reader.IsDBNull(index)?null:reader.GetString(index);
    private static int? NullableInt(NpgsqlDataReader reader,int index)=>reader.IsDBNull(index)?null:reader.GetInt32(index);
    private static DateTime? NullableDateTime(NpgsqlDataReader reader,int index)=>reader.IsDBNull(index)?null:DateTime.SpecifyKind(reader.GetDateTime(index),DateTimeKind.Unspecified);
    private static TaskSnapshot MapTask(NpgsqlDataReader reader)=>new(
        reader.GetString(0),reader.GetString(1),reader.GetInt32(2),reader.GetInt32(3),NullableText(reader,4),
        NullableText(reader,5),NullableText(reader,6),NullableText(reader,7),reader.GetInt32(8),
        DateTime.SpecifyKind(reader.GetDateTime(9),DateTimeKind.Unspecified),NullableDateTime(reader,10));
    private static DateTime TaipeiNow()=>TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TaipeiTimeZone).DateTime;
    private static Wp305EquipmentOnlineQcRuleException Bad(int status,string message)=>new(status,message);

    private sealed record NormalizedPage(int Page,int PageSize,int Offset);
    private sealed record TaskSnapshot(string TaskNo,string EquipmentNo,int TaskClass,int InspType,string? ShiftNo,
        string? LotNo,string? InspStartTime,string? QcListNo,int Status,DateTime BelongDate,DateTime? PlanQcTime);
    private sealed record IdempotencyEntry(string Creator,string PayloadHash,string QcListSerial);
    private sealed record CalculatedDetail(decimal QcOrder,string QcItem,int QcType,int InputDataCount,int QcResult,
        string? InputValue,string? StdValue,string? MaxiValue,string? MiniValue,IReadOnlyList<string?> Readings);
}
