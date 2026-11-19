using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc301Repository(NpgsqlDataSource dataSource)
{
    private static readonly int[] ValidActions = [0, 1, 2, 3];

    public IReadOnlyList<Qc301QcItemDto> QcItems()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");
        var items = new List<(string No, string Name, string Type, int? ChartType, int SpecLineType,
            int? ComponentCollect, string Unit, int LotIsValid, string PlanNo, int SampleSize)>();
        using (var command = new NpgsqlCommand("""
            SELECT qcitemno,qcitemname,COALESCE(qcdatatype,0)::int,charttype::int,
                   COALESCE(speclinetype,0)::int,componentcollect::int,
                   COALESCE(unit,''),COALESCE(lotisvalid,0)::int,COALESCE(planno,''),COALESCE(samplesize,0)::int
            FROM "YMES".tblqcitemforvariables
            ORDER BY qcitemno;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                items.Add((reader.GetString(0), reader.GetString(1), reader.GetInt32(2).ToString(CultureInfo.InvariantCulture),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    reader.GetString(6), reader.GetInt32(7), reader.GetString(8), reader.GetInt32(9)));
            }
        }

        var result = new List<Qc301QcItemDto>(items.Count);
        foreach (var item in items)
        {
            // The lookup and writers must reject the same ambiguous catalog;
            // separate factor projections previously bypassed that guard.
            var factors = GetFactors(connection, null, item.No);
            result.Add(new(item.No, item.Name, "Variable", item.ChartType, item.SpecLineType, item.ComponentCollect,
                item.Unit, item.LotIsValid, item.PlanNo, item.SampleSize, factors));
        }
        return result;
    }

    public IReadOnlyList<Qc301ChartBasisDto> List(string qcItemNo, int? issueState)
    {
        var no = ValidateCode(qcItemNo, "QCItemNo", 25, required: true);
        if (issueState is not null and not (-1 or 0 or 1 or 2))
            throw Rule(StatusCodes.Status400BadRequest, "IssueState 僅允許 -1、0、1、2。");
        using var connection = dataSource.OpenConnection();
        EnsureChartTables(connection, null, includeSamplingParameters: true);
        EnsureTable(connection, null, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");
        var sql = """
            SELECT b.factorserial,b.qcitemno,COALESCE(q.qcitemname,''),b.issuestate::int,
                   b.datastamp,COALESCE(b.samplesize,0)::int,COALESCE(b.keynode,'N'),
                   COALESCE(b.charttitle,''),COALESCE(b.chartunit,''),COALESCE(b.sponsor,''),COALESCE(b.planno,''),
                   COALESCE(b.memo,''),COALESCE(b.creator,''),b.createdate,COALESCE(b.reviser,''),b.revisedate,
                   b.factorvalues::text,COALESCE(q.charttype,-1)::int
            FROM "YMES".tblspcqcchartbasis b
            LEFT JOIN "YMES".tblqcitemforvariables q ON lower(q.qcitemno)=lower(b.qcitemno)
            WHERE lower(b.qcitemno)=lower(@no)
            """ + (issueState is null ? "" : " AND b.issuestate=@state") + " ORDER BY b.createdate DESC NULLS LAST,b.factorserial;";
        var rows = new List<Qc301ChartBasisDto>();
        using (var command = new NpgsqlCommand(sql, connection))
        {
            AddVarchar(command, "no", no, 25);
            if (issueState is { } state) AddNumeric(command, "state", state);
            using var reader = command.ExecuteReader();
            while (reader.Read()) rows.Add(MapBasis(reader));
        }
        foreach (var row in rows)
        {
            row.Details = ReadDetails(connection, row.FactorSerial, GetChartType(connection, row.QcItemNo));
            row.Rules = ReadRules(connection, row.FactorSerial, GetChartType(connection, row.QcItemNo));
            row.Parameters = ReadChartParameters(connection, row.FactorSerial);
            row.HasMeasurementData = MeasurementDataExists(connection, row.FactorSerial);
            row.IssueJobHasDetail = WorkflowTablesExist(connection);
        }
        return rows;
    }

    public IReadOnlyList<Qc301ChartDetailDto> Details(string factorSerial)
    {
        using var connection = dataSource.OpenConnection();
        var chartType = GetChartTypeBySerial(connection, factorSerial);
        return ReadDetails(connection, factorSerial, chartType);
    }

    public IReadOnlyList<Qc301ChartRuleDto> Rules(string factorSerial)
    {
        using var connection = dataSource.OpenConnection();
        var chartType = GetChartTypeBySerial(connection, factorSerial);
        return ReadRules(connection, factorSerial, chartType);
    }

    public Qc301ChartBasisDto Create(Qc301ChartWriteRequest request, string actor)
    {
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureChartTables(connection, transaction, includeSamplingParameters: true);
            EnsureTable(connection, transaction, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");
            EnsureTable(connection, transaction, "tblqcitemfactor", "TBLQCITEMFACTOR 要因設定");
            EnsureTable(connection, transaction, "tblqcitemrule", "TBLQCITEMRULE 品管項目預設規則");
            EnsureTable(connection, transaction, "tblqcrulebasis", "TBLQCRULEBASIS 計量管制法則");
            var item = ReadQcItem(connection, transaction, request.QcItemNo, lockRow: true);
            var factors = NormalizeFactorValues(connection, transaction, item.QcItemNo, request.FactorValues);
            ValidateBasis(request, item, factors, requireCompleteParameters: true, connection, transaction);
            ValidatePlan(connection, transaction, item, request.PlanNo, request.SamplingPlanParameters);
            var serial = NewSerial();
            EnsureNoDuplicate(connection, transaction, item.QcItemNo, factors, null);
            InsertBasis(connection, transaction, serial, item, request, factors, actor);
            ReplaceDetails(connection, transaction, serial, item.ChartType, item.SpecLineType, request.Details, actor, writeHistory: true);
            ReplaceChartParameters(connection, transaction, serial, request.SamplingPlanParameters);
            CopyDefaultRules(connection, transaction, serial, item.QcItemNo);
            var result = ReadAggregate(connection, transaction, serial);
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw Rule(StatusCodes.Status409Conflict, "相同 QC Item 與要因組合已存在管制圖。");
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc301ChartBasisDto Update(string factorSerial, Qc301ChartUpdateRequest request, string actor)
    {
        ValidateActor(actor);
        ValidateVersion(request.ExpectedIssueState, request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureChartTables(connection, transaction, includeSamplingParameters: true);
            var (item, current) = ReadItemThenChartForUpdate(connection, transaction, factorSerial);
            EnsureExpected(current, request.ExpectedIssueState, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有 UNFROZEN 管制圖可以修改。");
            var nextStamp = NextStamp(current.DataStamp);
            if (!string.Equals(current.QcItemNo, request.QcItemNo?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw Rule(400, "QCItemNo 不可在修改時變更。");
            var factors = NormalizeFactorValues(connection, transaction, current.QcItemNo, request.FactorValues);
            ValidateBasis(request, item, factors, requireCompleteParameters: true, connection, transaction);
            ValidatePlan(connection, transaction, item, request.PlanNo, request.SamplingPlanParameters);
            EnsureNoDuplicate(connection, transaction, current.QcItemNo, factors, current.FactorSerial);
            using (var update = new NpgsqlCommand("""
                UPDATE "YMES".tblspcqcchartbasis
                SET samplesize=@sample,keynode=@keynode,charttitle=NULLIF(@title,''),chartunit=NULLIF(@unit,''),
                    sponsor=NULLIF(@sponsor,''),planno=NULLIF(@plan,''),memo=NULLIF(@memo,''),factorvalues=@factors,
                    reviser=@actor,revisedate=now(),datastamp=@next
                WHERE factorserial=@serial AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction))
            {
                AddBasisParameters(update, current.FactorSerial, item, request, factors, actor);
                AddNumeric(update, "state", request.ExpectedIssueState); AddDecimal(update, "stamp", request.DataStamp);
                AddDecimal(update, "next", nextStamp);
                if (update.ExecuteNonQuery() != 1) throw Conflict();
            }
            ReplaceDetails(connection, transaction, current.FactorSerial, item.ChartType, item.SpecLineType, request.Details, actor, writeHistory: true);
            ReplaceChartParameters(connection, transaction, current.FactorSerial, request.SamplingPlanParameters);
            var result = ReadAggregate(connection, transaction, current.FactorSerial);
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw Rule(409, "相同 QC Item 與要因組合已存在管制圖。");
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc301DeleteResult Delete(string factorSerial, Qc301StateRequest request, string actor)
    {
        ValidateActor(actor);
        ValidateVersion(request.IssueState, request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureChartTables(connection, transaction, includeSamplingParameters: true);
            var current = ReadBasisForUpdate(connection, transaction, factorSerial);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState == 2)
            {
                ChangeState(connection, transaction, current, -1, actor);
                transaction.Commit();
                return new(current.FactorSerial, "UNUSED", -1);
            }
            if (current.IssueState is not (0 or -1)) throw Rule(409, "只有 UNFROZEN 或 UNUSED 管制圖可以實體刪除。");
            EnsureTable(connection, transaction, "tblspcvardetaillog", "TBLSPCVARDETAILLOG 量測使用檢查");
            using (var measurement = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblspcvardetaillog WHERE factorserial=@serial LIMIT 1;", connection, transaction))
            {
                AddVarchar(measurement, "serial", current.FactorSerial, 50);
                if (measurement.ExecuteScalar() is not null) throw Rule(409, "此管制圖已有量測資料，不允許實體刪除。");
            }
            foreach (var table in new[] { "tblspcqcchartsamplanparameter", "tblspcqcchartrule", "tblspcqcchartdetaillog", "tblspcqcchartdetail" })
            {
                using var delete = new NpgsqlCommand($"DELETE FROM \"YMES\".{table} WHERE factorserial=@serial;", connection, transaction);
                AddVarchar(delete, "serial", current.FactorSerial, 50);
                delete.ExecuteNonQuery();
            }
            using (var deleteBasis = new NpgsqlCommand("DELETE FROM \"YMES\".tblspcqcchartbasis WHERE factorserial=@serial AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
            {
                AddVarchar(deleteBasis, "serial", current.FactorSerial, 50); AddNumeric(deleteBasis, "state", request.IssueState);
                AddDecimal(deleteBasis, "stamp", request.DataStamp);
                if (deleteBasis.ExecuteNonQuery() != 1) throw Conflict();
            }
            transaction.Commit();
            return new(current.FactorSerial, "DELETED", null);
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc301ApprovalResult Approve(string factorSerial, Qc301StateRequest request, string actor)
    {
        ValidateActor(actor);
        ValidateVersion(request.IssueState, request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureChartTables(connection, transaction, includeSamplingParameters: true);
            var (item, current) = ReadItemThenChartForUpdate(connection, transaction, factorSerial);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有 UNFROZEN 管制圖可以核准。");
            var nextStamp = NextStamp(current.DataStamp);
            ValidatePlan(connection, transaction, item, current.PlanNo,
                ReadChartParameters(connection, current.FactorSerial, transaction));
            var factors = NormalizeFactorValues(connection, transaction, current.QcItemNo, current.FactorValues);
            foreach (var factor in GetFactors(connection, transaction, current.QcItemNo))
                if (factor.FactorSpec && factors[factor.FactorNo] == "*")
                    throw Rule(422, $"規格要因 {factor.FactorNo} 不允許使用 *。");
            EnsureNoDuplicate(connection, transaction, current.QcItemNo, factors, current.FactorSerial);
            if (WorkflowTablesExist(connection, transaction))
                throw Rule(503, "YMES Issue/Job 流程表已存在；QC301 尚未接入該共用簽核流程，因此未變更狀態。");
            using (var update = new NpgsqlCommand("""
                UPDATE "YMES".tblspcqcchartbasis SET issuestate=2,reviser=@actor,revisedate=now(),datastamp=@next
                WHERE factorserial=@serial AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction))
            {
                AddVarchar(update, "actor", actor.Trim(), 10); AddVarchar(update, "serial", current.FactorSerial, 50);
                AddDecimal(update, "next", nextStamp); AddDecimal(update, "stamp", request.DataStamp);
                AddNumeric(update, "state", request.IssueState);
                if (update.ExecuteNonQuery() != 1) throw Conflict();
            }
            InsertDetailHistory(connection, transaction, current.FactorSerial, actor);
            transaction.Commit();
            return new(current.FactorSerial, 2, nextStamp);
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc301ApprovalResult Disapprove(string factorSerial, Qc301StateRequest request, string actor)
    {
        ValidateActor(actor);
        ValidateVersion(request.IssueState, request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureChartTables(connection, transaction, includeSamplingParameters: true);
            var current = ReadBasisForUpdate(connection, transaction, factorSerial);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState is not (2 or -1)) throw Rule(409, "只有 ACTIVE 或 UNUSED 管制圖可以取消核准。");
            var nextStamp = NextStamp(current.DataStamp);
            if (WorkflowTablesExist(connection, transaction))
                throw Rule(503, "YMES Issue/Job 流程表已存在；QC301 尚未接入該共用簽核流程，因此未變更狀態。");
            using var command = new NpgsqlCommand("""
                UPDATE "YMES".tblspcqcchartbasis SET issuestate=0,reviser=@actor,revisedate=now(),datastamp=@next
                WHERE factorserial=@serial AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction);
            AddVarchar(command, "actor", actor.Trim(), 10); AddVarchar(command, "serial", current.FactorSerial, 50);
            AddNumeric(command, "state", request.IssueState); AddDecimal(command, "next", nextStamp); AddDecimal(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return new(current.FactorSerial, 0, nextStamp);
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc301RuleUpdateResult ReplaceRules(string factorSerial, Qc301RulesRequest request, string actor)
    {
        ValidateActor(actor);
        ValidateVersion(request.ExpectedIssueState, request.DataStamp);
        var rows = request.Rules ?? throw Rule(400, "Rules 必須明確提供陣列；清空請傳 []，不可傳 NULL。");
        if (rows.Any(row => row is null)) throw Rule(400, "Rules 不可包含 NULL 資料列。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblspcqcchartrule", "QC Chart Rule 表");
            EnsureTable(connection, transaction, "tblqcrulebasis", "TBLQCRULEBASIS 核准管制法則");
            var current = ReadBasisForUpdate(connection, transaction, factorSerial);
            EnsureExpected(current, request.ExpectedIssueState, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有 UNFROZEN 管制圖可以維護 Rule。");
            var nextStamp = NextStamp(current.DataStamp);
            var chartType = GetChartType(connection, transaction, current.QcItemNo);
            var allowedSpcIds = SpcIds(chartType);
            if (rows.Count > 300) throw Rule(400, "Rule 筆數超過上限。");
            if (rows.Any(x => string.IsNullOrWhiteSpace(x.RuleNo))) throw Rule(400, "RuleNo 不可空白。");
            if (rows.Any(x => !allowedSpcIds.Contains(x.SpcId) || x.Action is < 0 or > 3))
                throw Rule(400, "SPCID 必須符合 ChartType，Action 僅允許 0 至 3。");
            if (rows.GroupBy(x => (x.SpcId, Rule: x.RuleNo.Trim()), new SpcRuleComparer()).Any(group => group.Count() > 1))
                throw Rule(400, "同一 SPCID 的 RuleNo 不可重複。");
            var approved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var ruleLookup = new NpgsqlCommand("SELECT ruleno FROM \"YMES\".tblqcrulebasis WHERE issuestate=2 ORDER BY ruleserial FOR SHARE;", connection, transaction))
            using (var reader = ruleLookup.ExecuteReader()) while (reader.Read()) approved.Add(reader.GetString(0));
            foreach (var row in rows)
            {
                var ruleNo = ValidateCode(row.RuleNo, "RuleNo", 20, required: true);
                if (!approved.Contains(ruleNo)) throw Rule(422, $"計量管制法則 {ruleNo} 不存在或尚未核准。");
            }
            using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblspcqcchartrule WHERE factorserial=@serial;", connection, transaction))
            {
                AddVarchar(delete, "serial", current.FactorSerial, 50); delete.ExecuteNonQuery();
            }
            foreach (var row in rows)
            {
                using var insert = new NpgsqlCommand("""
                    INSERT INTO "YMES".tblspcqcchartrule(factorserial,spcid,ruleno,action)
                    VALUES(@serial,@spcid,@rule,@action);
                    """, connection, transaction);
                AddVarchar(insert, "serial", current.FactorSerial, 50); AddNumeric(insert, "spcid", row.SpcId);
                AddVarchar(insert, "rule", row.RuleNo.Trim(), 30); AddNumeric(insert, "action", row.Action);
                insert.ExecuteNonQuery();
            }
            DateTime reviseDate;
            using (var audit = new NpgsqlCommand("""
                UPDATE "YMES".tblspcqcchartbasis
                SET reviser=@actor,revisedate=now(),datastamp=@next
                WHERE factorserial=@serial AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING revisedate;
                """, connection, transaction))
            {
                AddVarchar(audit, "actor", actor.Trim(), 10); AddDecimal(audit, "next", nextStamp);
                AddVarchar(audit, "serial", current.FactorSerial, 50); AddDecimal(audit, "stamp", request.DataStamp);
                AddNumeric(audit, "state", request.ExpectedIssueState);
                var persistedDate = audit.ExecuteScalar();
                if (persistedDate is not DateTime value) throw Conflict();
                reviseDate = value;
            }
            var result = new Qc301RuleUpdateResult(current.FactorSerial, nextStamp, actor.Trim(), reviseDate,
                ReadRules(connection, current.FactorSerial, chartType, transaction));
            transaction.Commit();
            return result;
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc301CopyResult Copy(Qc301CopyRequest request, string actor)
    {
        ValidateActor(actor);
        var sourceNo = ValidateCode(request.FromQcItemNo, "FromQCItemNo", 25, true);
        var targetNo = ValidateCode(request.ToQcItemNo, "ToQCItemNo", 25, true);
        if (string.Equals(sourceNo, targetNo, StringComparison.OrdinalIgnoreCase)) throw Rule(400, "來源與目標 QC Item 不可相同。");
        if (request.CopyType is not (0 or 2 or 3)) throw Rule(400, "CopyType 僅允許 3（UNFROZEN+ACTIVE）、0（UNFROZEN）、2（ACTIVE）。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureChartTables(connection, transaction, includeSamplingParameters: true);
            LockQcItemsForCopy(connection, transaction, sourceNo, targetNo);
            var source = ReadQcItem(connection, transaction, sourceNo, lockRow: false);
            var target = ReadQcItem(connection, transaction, targetNo, lockRow: false);
            if (string.Equals(source.QcItemNo, target.QcItemNo, StringComparison.Ordinal))
                throw Rule(400, "來源與目標必須是不同的 QC Item。");
            sourceNo = source.QcItemNo;
            targetNo = target.QcItemNo;
            if (source.ChartType != target.ChartType)
                throw Rule(422, "來源與目標 QC Item 的 ChartType 不同，無法複製管制圖明細。");
            var fromSpec = GetFactors(connection, transaction, sourceNo).Where(f => f.FactorSpec).Select(f => f.FactorNo).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var toSpec = GetFactors(connection, transaction, targetNo).Where(f => f.FactorSpec).Select(f => f.FactorNo).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            if (!fromSpec.SequenceEqual(toSpec, StringComparer.OrdinalIgnoreCase))
                throw Rule(422, "來源與目標 QC Item 的 FactorSpec 清單不同，無法跨 QC Item 複製。");
            var predicate = request.CopyType switch { 0 => "issuestate=0", 2 => "issuestate=2", _ => "issuestate IN (0,2)" };
            var rows = new List<Qc301ChartBasisDto>();
            using (var list = new NpgsqlCommand($"SELECT factorserial,qcitemno,''::varchar,issuestate::int,datastamp,COALESCE(samplesize,0)::int,COALESCE(keynode,'N'),COALESCE(charttitle,''),COALESCE(chartunit,''),COALESCE(sponsor,''),COALESCE(planno,''),COALESCE(memo,''),COALESCE(creator,''),createdate,COALESCE(reviser,''),revisedate,factorvalues::text,COALESCE(@chartType,-1)::int FROM \"YMES\".tblspcqcchartbasis WHERE lower(qcitemno)=lower(@no) AND {predicate} ORDER BY createdate,factorserial FOR UPDATE;", connection, transaction))
            {
                AddVarchar(list, "no", sourceNo, 25); AddNumeric(list, "chartType", source.ChartType);
                using var reader = list.ExecuteReader(); while (reader.Read()) rows.Add(MapBasis(reader));
            }
            var copied = 0;
            foreach (var old in rows)
            {
                var sourceValues = old.FactorValues;
                var targetFactors = GetFactors(connection, transaction, targetNo);
                var targetValues = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var factor in targetFactors)
                {
                    if (sourceValues.TryGetValue(factor.FactorNo, out var value)) targetValues[factor.FactorNo] = value;
                    else if (factor.FactorSpec) throw Rule(422, $"規格要因 {factor.FactorNo} 缺少可複製值。");
                    else targetValues[factor.FactorNo] = "*";
                }
                if (!string.IsNullOrWhiteSpace(old.PlanNo) &&
                    QcSamplingPlanReference.Namespace(source.ComponentCollect, Rule) != QcSamplingPlanReference.Namespace(target.ComponentCollect, Rule))
                    throw Rule(422, "帶有 PlanNo 的 Copy2 不允許跨一般／Wafer namespace；相同 PlanNo 不代表相同計劃，請分別維護目標。");
                EnsureNoDuplicate(connection, transaction, targetNo, targetValues, null);
                ValidatePlan(connection, transaction, target, old.PlanNo,
                    ReadChartParameters(connection, old.FactorSerial, transaction));
                var serial = NewSerial();
                using (var insert = new NpgsqlCommand("""
                    INSERT INTO "YMES".tblspcqcchartbasis
                        (factorserial,qcitemno,creator,createdate,samplesize,issuestate,keynode,sponsor,charttitle,isshortrunchart,
                         planno,chartunit,memo,product,equipment,br,customer,op,preopno,datastamp,mtlvendor,eqptype,material,
                         version,posion,employee,thinkness,thickness,device,factorvalues)
                    SELECT @newserial,@target,@actor,now(),samplesize,0,keynode,sponsor,charttitle,isshortrunchart,
                           planno,chartunit,memo,product,equipment,br,customer,op,preopno,0,mtlvendor,eqptype,material,
                           version,posion,employee,thinkness,thickness,device,@factors
                    FROM "YMES".tblspcqcchartbasis WHERE factorserial=@oldserial;
                    """, connection, transaction))
                {
                    AddVarchar(insert, "newserial", serial, 50); AddVarchar(insert, "target", targetNo, 25);
                    AddVarchar(insert, "actor", actor.Trim(), 10); AddVarchar(insert, "oldserial", old.FactorSerial, 50);
                    insert.Parameters.Add("factors", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(targetValues);
                    if (insert.ExecuteNonQuery() != 1) throw Conflict();
                }
                CopyChildRows(connection, transaction, old.FactorSerial, serial, actor);
                copied++;
            }
            transaction.Commit();
            return new(copied);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw Rule(409, "目標 QC Item 已有相同要因組合；複製交易已回復。");
        }
        catch { TryRollback(transaction); throw; }
    }

    // Compatibility entry points share the same namespace-aware lookup as the
    // published controller; no second interpretation of ComponentCollect.
    public IReadOnlyList<Qc301SamplingPlanDto> SamplingPlans(int? issueState, int? componentCollect) =>
        new Qc103Repository(dataSource).List(issueState, componentCollect, null, null)
            .Select(row => new Qc301SamplingPlanDto(row.PlanNo, row.PlanName)).ToArray();

    public IReadOnlyList<Qc301SamplingPlanParameterDto> SamplingPlanParameters(string planNo, int? issueState,
        int? componentCollect = null) => new Qc103Repository(dataSource).Parameters(planNo, issueState, componentCollect)
            .Select(row => new Qc301SamplingPlanParameterDto(row.ParameterNo, row.ParameterValue ?? "")).ToArray();

    // Called while the mutation still owns the parent lock. All child reads
    // share that transaction, so the returned version describes this commit.
    private static Qc301ChartBasisDto ReadAggregate(NpgsqlConnection connection, NpgsqlTransaction transaction, string serial)
    {
        var row = ReadBasisForUpdate(connection, transaction, serial);
        var chartType = GetChartType(connection, transaction, row.QcItemNo);
        row.Details = ReadDetails(connection, row.FactorSerial, chartType, transaction);
        row.Rules = ReadRules(connection, row.FactorSerial, chartType, transaction);
        row.Parameters = ReadChartParameters(connection, row.FactorSerial, transaction);
        row.HasMeasurementData = MeasurementDataExists(connection, row.FactorSerial, transaction);
        row.IssueJobHasDetail = WorkflowTablesExist(connection, transaction);
        return row;
    }

    private static Qc301ChartBasisDto MapBasis(NpgsqlDataReader reader) => new()
    {
        FactorSerial = reader.GetString(0), QcItemNo = reader.GetString(1), QcItemName = reader.GetString(2),
        IssueState = reader.IsDBNull(3) ? null : reader.GetInt32(3), DataStamp = reader.IsDBNull(4) ? null : reader.GetDecimal(4), SampleSize = reader.GetInt32(5),
        KeyNode = reader.GetString(6), ChartTitle = reader.GetString(7), ChartUnit = reader.GetString(8),
        Sponsor = reader.GetString(9), PlanNo = reader.GetString(10), Memo = reader.GetString(11),
        Creator = reader.GetString(12), CreateDate = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
        Reviser = reader.GetString(14), ReviseDate = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
        FactorValues = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(16)) ?? new(),
    };

    private static IReadOnlyList<Qc301ChartDetailDto> ReadDetails(NpgsqlConnection connection, string serial, int chartType,
        NpgsqlTransaction? transaction = null)
    {
        EnsureTable(connection, transaction, "tblspcqcchartdetail", "QC Chart Detail 表 migration 0024");
        using var command = new NpgsqlCommand("""
            SELECT spcid::int,COALESCE(us,''),COALESCE(cs,''),COALESCE(ls,''),COALESCE(ucl,''),COALESCE(cl,''),
                   COALESCE(lcl,''),COALESCE(target,''),COALESCE(cpkgoal,''),COALESCE(yaxisfrom,''),COALESCE(yaxisto,'')
            FROM "YMES".tblspcqcchartdetail WHERE factorserial=@serial ORDER BY spcid;
            """, connection, transaction);
        AddVarchar(command, "serial", serial, 50);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc301ChartDetailDto>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            rows.Add(new(id, ChartName(chartType, id), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
                reader.GetString(9), reader.GetString(10)));
        }
        return rows;
    }

    private static IReadOnlyList<Qc301ChartRuleDto> ReadRules(NpgsqlConnection connection, string serial, int chartType,
        NpgsqlTransaction? transaction = null)
    {
        EnsureTable(connection, transaction, "tblspcqcchartrule", "QC Chart Rule 表 migration 0024");
        var hasBasis = TableExists(connection, transaction, "tblqcrulebasis");
        var sql = hasBasis
            ? """
              SELECT r.spcid::int,r.ruleno,COALESCE(b.rulename,''),r.action::int
              FROM "YMES".tblspcqcchartrule r LEFT JOIN "YMES".tblqcrulebasis b ON lower(b.ruleno)=lower(r.ruleno)
              WHERE r.factorserial=@serial ORDER BY r.spcid,r.ruleno;
              """
            : "SELECT spcid::int,ruleno,''::varchar,action::int FROM \"YMES\".tblspcqcchartrule WHERE factorserial=@serial ORDER BY spcid,ruleno;";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        AddVarchar(command, "serial", serial, 50);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc301ChartRuleDto>();
        while (reader.Read()) rows.Add(new(reader.GetInt32(0), ChartName(chartType, reader.GetInt32(0)), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
        return rows;
    }

    private static IReadOnlyList<Qc301SamplingPlanParameterDto> ReadChartParameters(NpgsqlConnection connection, string serial,
        NpgsqlTransaction? transaction = null)
    {
        EnsureTable(connection, transaction, "tblspcqcchartsamplanparameter", "TBLSPCQCCHARTSAMPLANPARAMETER migration 0025");
        using var command = new NpgsqlCommand("SELECT parameterno,COALESCE(parametervalue,'') FROM \"YMES\".tblspcqcchartsamplanparameter WHERE factorserial=@serial ORDER BY parameterno;", connection, transaction);
        AddVarchar(command, "serial", serial, 50);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc301SamplingPlanParameterDto>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1)));
        return rows;
    }

    private static Qc301ChartBasisDto ReadBasisForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, string factorSerial)
    {
        EnsureTable(connection, transaction, "tblspcqcchartbasis", "QC Chart Basis 表 migration 0024");
        using var command = new NpgsqlCommand("""
            SELECT b.factorserial,b.qcitemno,COALESCE(q.qcitemname,''),b.issuestate::int,
                   b.datastamp,COALESCE(b.samplesize,0)::int,COALESCE(b.keynode,'N'),
                   COALESCE(b.charttitle,''),COALESCE(b.chartunit,''),COALESCE(b.sponsor,''),COALESCE(b.planno,''),
                   COALESCE(b.memo,''),COALESCE(b.creator,''),b.createdate,COALESCE(b.reviser,''),b.revisedate,
                   b.factorvalues::text,COALESCE(q.charttype,-1)::int
            FROM "YMES".tblspcqcchartbasis b LEFT JOIN "YMES".tblqcitemforvariables q ON lower(q.qcitemno)=lower(b.qcitemno)
            WHERE lower(b.factorserial)=lower(@serial) FOR UPDATE OF b;
            """, connection, transaction);
        AddVarchar(command, "serial", ValidateCode(factorSerial, "FactorSerial", 50, true), 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到管制圖資料。");
        var row = MapBasis(reader);
        if (reader.Read()) throw Rule(409, "FactorSerial 或 QC Item 存在大小寫歧義，無法安全選取管制圖。");
        reader.Close();
        return row;
    }

    private static (Qc301QcItemDto Item, Qc301ChartBasisDto Chart) ReadItemThenChartForUpdate(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string factorSerial)
    {
        // Resolve without a chart row lock, then take the common Item -> Chart
        // order. Recheck ownership after both locks: a stale lookup must never
        // authorize a different parent or silently borrow its newer version.
        string owner;
        using (var lookup = new NpgsqlCommand("SELECT qcitemno FROM \"YMES\".tblspcqcchartbasis WHERE lower(factorserial)=lower(@serial) ORDER BY factorserial;", connection, transaction))
        {
            AddVarchar(lookup, "serial", ValidateCode(factorSerial, "FactorSerial", 50, true), 50);
            using var reader = lookup.ExecuteReader();
            if (!reader.Read()) throw Rule(404, "找不到管制圖資料。");
            owner = reader.GetString(0);
            if (reader.Read()) throw Rule(409, "FactorSerial 存在大小寫不同的重複列。");
        }
        var item = ReadQcItem(connection, transaction, owner, lockRow: true);
        var chart = ReadBasisForUpdate(connection, transaction, factorSerial);
        if (!string.Equals(chart.QcItemNo, owner, StringComparison.Ordinal))
            throw Rule(409, "管制圖的 QC Item 已變更，請重新載入；本次未保存。");
        return (item, chart);
    }

    private static void LockQcItemsForCopy(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string sourceNo, string targetNo)
    {
        EnsureTable(connection, transaction, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");
        // PostgreSQL chooses both casefold membership and lock ordering. Do
        // not sort source/target with a different .NET comparer/collation.
        using var command = new NpgsqlCommand("""
            SELECT q.qcitemno FROM "YMES".tblqcitemforvariables q
            WHERE EXISTS (SELECT 1 FROM unnest(@nos::text[]) AS requested(no)
                          WHERE lower(q.qcitemno)=lower(requested.no))
            ORDER BY lower(q.qcitemno),q.qcitemno FOR UPDATE OF q;
            """, connection, transaction);
        command.Parameters.Add("nos", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { sourceNo, targetNo };
        using var reader = command.ExecuteReader();
        while (reader.Read()) { }
    }

    private static void EnsureExpected(Qc301ChartBasisDto current, int? state, decimal? stamp)
    {
        if (current.IssueState != state || current.DataStamp != stamp) throw Conflict();
    }

    private static void ValidateVersion(int? state, decimal? stamp)
    {
        if (state is not null and not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 必須為 NULL、-1、0、1 或 2。");
        if (stamp is { } value && (value != decimal.Truncate(value) || value < -99999999999m || value > 99999999999m))
            throw Rule(400, "DataStamp 必須為 NULL 或 numeric(11,0) 範圍內的整數。");
    }

    private static decimal NextStamp(decimal? stamp)
    {
        if ((stamp ?? 0m) >= 99999999999m) throw Rule(409, "管制圖 DataStamp 已達資料表上限；資料未變更。");
        return (stamp ?? 0m) + 1m;
    }

    private static void ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc301ChartBasisDto current, int state, string actor)
    {
        var nextStamp = NextStamp(current.DataStamp);
        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tblspcqcchartbasis SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=@nextstamp
            WHERE factorserial=@serial AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
            """, connection, transaction);
        AddNumeric(command, "next", state); AddVarchar(command, "actor", actor.Trim(), 10);
        AddVarchar(command, "serial", current.FactorSerial, 50); AddNumeric(command, "state", current.IssueState);
        AddDecimal(command, "stamp", current.DataStamp);
        AddDecimal(command, "nextstamp", nextStamp);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
    }

    private static void InsertBasis(NpgsqlConnection connection, NpgsqlTransaction transaction, string serial, Qc301QcItemDto item,
        Qc301ChartWriteRequest request, Dictionary<string, string> factors, string actor)
    {
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblspcqcchartbasis
                (factorserial,qcitemno,creator,createdate,samplesize,issuestate,keynode,sponsor,charttitle,planno,chartunit,
                 memo,product,material,factorvalues,datastamp)
            VALUES(@serial,@qcitem,@actor,now(),@sample,0,@keynode,NULLIF(@sponsor,''),NULLIF(@title,''),
                   NULLIF(@plan,''),NULLIF(@unit,''),NULLIF(@memo,''),COALESCE(NULLIF(@product,''),'*'),
                   COALESCE(NULLIF(@material,''),'*'),@factors,0);
            """, connection, transaction);
        AddBasisParameters(command, serial, item, request, factors, actor);
        AddVarchar(command, "product", factors.GetValueOrDefault("PRODUCT", "*"), 61);
        AddVarchar(command, "material", factors.GetValueOrDefault("MATERIAL", "*"), 61);
        command.ExecuteNonQuery();
    }

    private static void AddBasisParameters(NpgsqlCommand command, string serial, Qc301QcItemDto item,
        Qc301ChartWriteRequest request, Dictionary<string, string> factors, string actor)
    {
        AddVarchar(command, "serial", serial, 50); AddVarchar(command, "qcitem", item.QcItemNo, 25);
        AddVarchar(command, "actor", actor.Trim(), 10); AddNumeric(command, "sample", request.SampleSize);
        AddVarchar(command, "keynode", ValidateKeyNode(request.KeyNode), 1); AddVarchar(command, "sponsor", ValidateCode(request.Sponsor, "Sponsor", 50, false), 50);
        AddVarchar(command, "title", ValidateCode(request.ChartTitle, "ChartTitle", 50, false), 50);
        AddVarchar(command, "plan", ValidateCode(request.PlanNo, "PlanNo", 25, false), 25);
        AddVarchar(command, "unit", ValidateCode(request.ChartUnit, "ChartUnit", 30, false), 30);
        AddVarchar(command, "memo", ValidateCode(request.Memo, "MEMO", 255, false), 255);
        command.Parameters.Add("factors", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(factors);
    }

    private static void ReplaceDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string serial,
        int? chartType, int specLineType, IReadOnlyList<Qc301ChartDetailWriteRequest>? requestDetails, string actor, bool writeHistory)
    {
        var detailRows = NormalizeDetails(chartType, specLineType, requestDetails);
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblspcqcchartdetail WHERE factorserial=@serial;", connection, transaction))
        {
            AddVarchar(delete, "serial", serial, 50); delete.ExecuteNonQuery();
        }
        foreach (var detail in detailRows)
        {
            using (var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblspcqcchartdetail(factorserial,spcid,us,cs,ls,ucl,cl,lcl,target,cpkgoal,yaxisfrom,yaxisto)
                VALUES(@serial,@spcid,NULLIF(@us,''),NULLIF(@cs,''),NULLIF(@ls,''),NULLIF(@ucl,''),NULLIF(@cl,''),
                       NULLIF(@lcl,''),NULLIF(@target,''),NULLIF(@cpk,''),NULLIF(@from,''),NULLIF(@to,''));
                """, connection, transaction))
            {
                AddDetailParameters(insert, serial, detail); insert.ExecuteNonQuery();
            }
            if (!writeHistory) continue;
            using var history = new NpgsqlCommand("""
                INSERT INTO "YMES".tblspcqcchartdetaillog
                    (factorserial,spcid,us,ls,ucl,lcl,cl,creator,starttime,target,cpkgoal,yaxisfrom,yaxisto,cs,memo)
                VALUES(@serial,@spcid,NULLIF(@us,''),NULLIF(@ls,''),NULLIF(@ucl,''),NULLIF(@lcl,''),NULLIF(@cl,''),
                       @actor,now(),NULLIF(@target,''),NULLIF(@cpk,''),NULLIF(@from,''),NULLIF(@to,''),NULLIF(@cs,''),NULLIF(@memo,''));
                """, connection, transaction);
            AddDetailParameters(history, serial, detail); AddVarchar(history, "actor", actor.Trim(), 10);
            AddVarchar(history, "memo", "QC301 新增/修改", 255); history.ExecuteNonQuery();
        }
    }

    private static List<Qc301ChartDetailWriteRequest> NormalizeDetails(int? chartType, int specLineType,
        IReadOnlyList<Qc301ChartDetailWriteRequest>? submitted)
    {
        var ids = SpcIds(chartType);
        var input = submitted ?? [];
        if (input.Count != ids.Length || input.Select(x => x.SpcId).Distinct().Count() != input.Count || input.Any(x => !ids.Contains(x.SpcId)))
            throw Rule(400, $"Detail SPCID 必須完全符合目前 ChartType：{string.Join(",", ids)}。");
        if (specLineType is < 0 or > 2) throw Rule(422, "SpecLineType 僅支援 0、1、2。");
        var result = new List<Qc301ChartDetailWriteRequest>();
        foreach (var source in input.OrderBy(x => x.SpcId))
        {
            var row = new Qc301ChartDetailWriteRequest
            {
                SpcId = source.SpcId,
                Us = ValidateNumeric(source.Us, "US", 15), Cs = ValidateNumeric(source.Cs, "CS", 15),
                Ls = ValidateNumeric(source.Ls, "LS", 15), Ucl = ValidateNumeric(source.Ucl, "UCL", 15),
                Cl = ValidateNumeric(source.Cl, "CL", 15), Lcl = ValidateNumeric(source.Lcl, "LCL", 15),
                Target = ValidateNumeric(source.Target, "Target", 10), CpkGoal = ValidateNumeric(source.CpkGoal, "CpkGoal", 10),
                YAxisFrom = ValidateNumeric(source.YAxisFrom, "YAxisFrom", 10), YAxisTo = ValidateNumeric(source.YAxisTo, "YAxisTo", 10)
            };
            if (row.YAxisFrom.Length > 0 && row.YAxisTo.Length > 0 &&
                decimal.Parse(row.YAxisFrom, CultureInfo.InvariantCulture) >= decimal.Parse(row.YAxisTo, CultureInfo.InvariantCulture))
                throw Rule(400, "YAxisFrom 必須小於 YAxisTo。");
            if (row.SpcId == 0 && chartType is >= 0)
            {
                if (specLineType == 0 && (row.Us.Length == 0 || row.Ls.Length == 0)) throw Rule(400, "雙邊規格須提供 Trend US 與 LS。");
                if (specLineType == 1 && row.Us.Length == 0) throw Rule(400, "上限規格須提供 Trend US。");
                if (specLineType == 2 && row.Ls.Length == 0) throw Rule(400, "下限規格須提供 Trend LS。");
            }
            result.Add(row);
        }
        return result;
    }

    private static void AddDetailParameters(NpgsqlCommand command, string serial, Qc301ChartDetailWriteRequest row)
    {
        AddVarchar(command, "serial", serial, 50); AddNumeric(command, "spcid", row.SpcId);
        AddVarchar(command, "us", row.Us, 15); AddVarchar(command, "cs", row.Cs, 15); AddVarchar(command, "ls", row.Ls, 15);
        AddVarchar(command, "ucl", row.Ucl, 15); AddVarchar(command, "cl", row.Cl, 15); AddVarchar(command, "lcl", row.Lcl, 15);
        AddVarchar(command, "target", row.Target, 10); AddVarchar(command, "cpk", row.CpkGoal, 10);
        AddVarchar(command, "from", row.YAxisFrom, 10); AddVarchar(command, "to", row.YAxisTo, 10);
    }

    private static void ValidateBasis(Qc301ChartWriteRequest request, Qc301QcItemDto item,
        IReadOnlyDictionary<string, string> factorValues, bool requireCompleteParameters,
        NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        if (request.SampleSize is <= 0 or > 99999) throw Rule(400, "SampleSize 必須是 1 至 99999 的整數。");
        if (item.ChartType == 2 && request.SampleSize != 1) throw Rule(400, "X-Rm 圖的 SampleSize 必須為 1。");
        _ = ValidateKeyNode(request.KeyNode);
        _ = ValidateCode(request.ChartTitle, "ChartTitle", 50, false);
        _ = ValidateCode(request.ChartUnit, "ChartUnit", 30, false);
        _ = ValidateCode(request.Sponsor, "Sponsor", 50, false);
        _ = ValidateCode(request.Memo, "MEMO", 255, false);
        var parameters = request.SamplingPlanParameters ?? [];
        if (parameters.Count > 100) throw Rule(400, "抽樣計畫參數最多 100 筆。");
        if (parameters.Any(x => string.IsNullOrWhiteSpace(x.ParameterNo) || x.ParameterNo.Trim().Length > 20 ||
                                (x.ParameterValue?.Length ?? 0) > 50 || (x.ParameterValue ?? "").Any(char.IsControl)))
            throw Rule(400, "抽樣計畫參數欄位格式不正確。");
        if (parameters.GroupBy(x => x.ParameterNo.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw Rule(400, "抽樣計畫 ParameterNo 不可重複。");
        if (requireCompleteParameters && item.ComponentCollect == 0 && !string.IsNullOrWhiteSpace(request.PlanNo) && parameters.Count == 0)
            throw Rule(400, "ComponentCollect=0 且指定 PlanNo 時，至少需要一筆抽樣計畫參數。");
        _ = factorValues;
        _ = connection;
        _ = transaction;
    }

    private static void ValidatePlan(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc301QcItemDto item,
        string? planNo, IReadOnlyList<Qc301SamplingPlanParameterDto>? parameters)
    {
        var plan = planNo?.Trim() ?? "";
        if (plan.Length == 0)
        {
            if (parameters is { Count: > 0 }) throw Rule(400, "未指定 PlanNo 時不可帶入抽樣計畫參數。");
            return;
        }
        var reference = QcSamplingPlanReference.ReadGeneral(connection, transaction, plan, item.ComponentCollect, 2, Rule);
        var expected = reference.Parameters.Select(value => value.ParameterNo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var supplied = (parameters ?? []).Select(x => x.ParameterNo.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (item.ComponentCollect == 0 && expected.Count == 0) throw Rule(422, $"抽樣計畫 {plan} 沒有已設定參數。");
        if (!supplied.SetEquals(expected)) throw Rule(422, $"抽樣計畫 {plan} 的 ParameterNo 必須與核准計畫參數清單一致。");
    }

    private static void ReplaceChartParameters(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string serial, IReadOnlyList<Qc301SamplingPlanParameterDto>? parameters)
    {
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblspcqcchartsamplanparameter WHERE factorserial=@serial;", connection, transaction))
        {
            AddVarchar(delete, "serial", serial, 50); delete.ExecuteNonQuery();
        }
        foreach (var item in parameters ?? [])
        {
            using var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblspcqcchartsamplanparameter(factorserial,parameterno,parametervalue)
                VALUES(@serial,@no,NULLIF(@value,''));
                """, connection, transaction);
            AddVarchar(insert, "serial", serial, 50); AddVarchar(insert, "no", item.ParameterNo.Trim(), 20);
            AddVarchar(insert, "value", item.ParameterValue, 50); insert.ExecuteNonQuery();
        }
    }

    private static void CopyDefaultRules(NpgsqlConnection connection, NpgsqlTransaction transaction, string serial, string qcItemNo)
    {
        CopyRulesWithParentLocks(connection, transaction, qcItemNo, serial, fromItemDefaults: true);
    }

    private static void CopyChildRows(NpgsqlConnection connection, NpgsqlTransaction transaction, string fromSerial, string toSerial, string actor)
    {
        CopyRulesWithParentLocks(connection, transaction, fromSerial, toSerial, fromItemDefaults: false);
        foreach (var (table, columns) in new[]
        {
            ("tblspcqcchartdetail", "spcid,us,ls,ucl,lcl,cl,target,cpkgoal,yaxisfrom,yaxisto,cs"),
            ("tblspcqcchartsamplanparameter", "parameterno,parametervalue")
        })
        {
            using var copy = new NpgsqlCommand($"INSERT INTO \"YMES\".{table}(factorserial,{columns}) SELECT @to,{columns} FROM \"YMES\".{table} WHERE factorserial=@from;", connection, transaction);
            AddVarchar(copy, "to", toSerial, 50); AddVarchar(copy, "from", fromSerial, 50); copy.ExecuteNonQuery();
        }
        using var details = new NpgsqlCommand("""
            INSERT INTO "YMES".tblspcqcchartdetaillog
                (factorserial,spcid,us,ls,ucl,lcl,cl,creator,starttime,target,cpkgoal,yaxisfrom,yaxisto,cs,memo)
            SELECT @to,spcid,us,ls,ucl,lcl,cl,@actor,now(),target,cpkgoal,yaxisfrom,yaxisto,cs,'QC301 Copy2'
            FROM "YMES".tblspcqcchartdetail WHERE factorserial=@from;
            """, connection, transaction);
        AddVarchar(details, "to", toSerial, 50); AddVarchar(details, "from", fromSerial, 50); AddVarchar(details, "actor", actor.Trim(), 10);
        details.ExecuteNonQuery();
    }

    private static void CopyRulesWithParentLocks(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string source, string targetSerial, bool fromItemDefaults)
    {
        // Materialize the source once: copying must not silently discard old
        // mappings via an approved-only join or pick up new, unlocked rows.
        var sourceQuery = fromItemDefaults
            ? "FROM \"YMES\".tblqcitemrule WHERE lower(qcitemno)=lower(@source)"
            : "FROM \"YMES\".tblspcqcchartrule WHERE factorserial=@source";
        var rows = new List<CopiedRule>();
        using (var read = new NpgsqlCommand($"SELECT spcid::int,ruleno,action::int,equipmentstate::int,erfstate::int,emailstate::int {sourceQuery} ORDER BY spcid,ruleno;", connection, transaction))
        {
            AddVarchar(read, "source", source, fromItemDefaults ? 25 : 50);
            using var reader = read.ExecuteReader();
            while (reader.Read()) rows.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetInt32(5)));
        }
        if (rows.Count == 0) return;
        EnsureTable(connection, transaction, "tblqcrulebasis", "TBLQCRULEBASIS 計量管制法則");
        var parents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var guard = new NpgsqlCommand("SELECT ruleno FROM \"YMES\".tblqcrulebasis WHERE lower(ruleno)=ANY(@names) ORDER BY ruleserial FOR SHARE;", connection, transaction))
        {
            guard.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = rows.Select(x => x.RuleNo.ToLowerInvariant()).Distinct().ToArray();
            using var reader = guard.ExecuteReader();
            while (reader.Read()) parents.Add(reader.GetString(0));
        }
        foreach (var row in rows)
        {
            if (!parents.Contains(row.RuleNo)) throw Rule(422, $"來源法則 {row.RuleNo} 的主檔不存在，複製交易未寫入。");
            using var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblspcqcchartrule(factorserial,spcid,ruleno,action,equipmentstate,erfstate,emailstate)
                VALUES(@serial,@spcid,@rule,@action,@equipment,@erf,@email);
                """, connection, transaction);
            AddVarchar(insert, "serial", targetSerial, 50); AddNumeric(insert, "spcid", row.SpcId);
            AddVarchar(insert, "rule", row.RuleNo, 30); AddNumeric(insert, "action", row.Action);
            AddNumeric(insert, "equipment", row.EquipmentState); AddNumeric(insert, "erf", row.ErfState); AddNumeric(insert, "email", row.EmailState);
            insert.ExecuteNonQuery();
        }
    }

    private sealed record CopiedRule(int SpcId, string RuleNo, int Action, int? EquipmentState, int? ErfState, int? EmailState);

    private static Dictionary<string, string> NormalizeFactorValues(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string qcItemNo, IReadOnlyDictionary<string, string>? submitted)
    {
        var factors = GetFactors(connection, transaction, qcItemNo);
        if (factors.Count == 0) throw Rule(422, "此 QC Item 沒有 FactorOnline=Y 的要因。");
        var keys = new HashSet<string>(factors.Select(x => x.FactorNo), StringComparer.OrdinalIgnoreCase);
        var submittedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in submitted?.Keys ?? [])
        {
            if (!submittedKeys.Add(key.Trim())) throw Rule(400, "要因值包含大小寫或前後空白重複的 FactorNo，不可任選其中一個值。");
            if (!keys.Contains(key.Trim())) throw Rule(400, $"FactorNo {key} 不屬於此 QC Item 核准的 FactorOnline。");
        }
        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var factor in factors)
        {
            var pair = (submitted ?? new Dictionary<string, string>()).FirstOrDefault(x => string.Equals(x.Key.Trim(), factor.FactorNo, StringComparison.OrdinalIgnoreCase));
            var value = pair.Key is null ? "" : pair.Value?.Trim() ?? "";
            if (value.Length == 0) throw Rule(400, $"線上要因 {factor.FactorNo} 必須填寫。");
            if (value.Length > 61 || value.Any(char.IsControl)) throw Rule(400, $"線上要因 {factor.FactorNo} 不可超過 61 個字元或含控制字元。");
            normalized.Add(factor.FactorNo, value);
        }
        return normalized;
    }

    private static List<Qc301FactorDto> GetFactors(NpgsqlConnection connection, NpgsqlTransaction? transaction, string qcItemNo)
    {
        EnsureTable(connection, transaction, "tblqcitemfactor", "TBLQCITEMFACTOR 要因設定");
        var hasParameterCatalog = TableExists(connection, transaction, "tblsysmesparameter");
        var sql = hasParameterCatalog
            ? "SELECT f.factorno,COALESCE(p.parametername,f.factorno),upper(COALESCE(f.factorspec,'N'))='Y',COALESCE(p.parametertype,0)::int FROM \"YMES\".tblqcitemfactor f LEFT JOIN \"YMES\".tblsysmesparameter p ON lower(p.parameterno)=lower(f.factorno) WHERE lower(f.qcitemno)=lower(@no) AND upper(COALESCE(f.factoronline,''))='Y' ORDER BY f.factorno;"
            : "SELECT factorno,factorno,upper(COALESCE(factorspec,'N'))='Y',0::int FROM \"YMES\".tblqcitemfactor WHERE lower(qcitemno)=lower(@no) AND upper(COALESCE(factoronline,''))='Y' ORDER BY factorno;";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        AddVarchar(command, "no", qcItemNo, 25);
        using var reader = command.ExecuteReader();
        var result = new List<Qc301FactorDto>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var no = reader.GetString(0);
            if (!names.Add(no)) throw Rule(409, "QC Item 的線上 FactorNo 存在大小寫歧義，請先整理來源因子。");
            result.Add(new(no, reader.GetString(1), reader.GetBoolean(2), reader.GetInt32(3)));
        }
        return result;
    }

    private static Qc301QcItemDto ReadQcItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string qcItemNo, bool lockRow)
    {
        EnsureTable(connection, transaction, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");
        using var command = new NpgsqlCommand("""
            SELECT qcitemno,qcitemname,charttype::int,COALESCE(speclinetype,0)::int,componentcollect::int,
                   COALESCE(unit,''),COALESCE(lotisvalid,0)::int,COALESCE(planno,''),COALESCE(samplesize,0)::int
            FROM "YMES".tblqcitemforvariables WHERE lower(qcitemno)=lower(@no)
            """ + " ORDER BY lower(qcitemno),qcitemno" + (lockRow ? " FOR UPDATE;" : ";"), connection, transaction);
        AddVarchar(command, "no", ValidateCode(qcItemNo, "QCItemNo", 25, true), 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到計量 QC Item {qcItemNo}。");
        var no = reader.GetString(0); var name = reader.GetString(1); int? chartType = reader.IsDBNull(2) ? null : reader.GetInt32(2);
        var spec = reader.GetInt32(3); int? collect = reader.IsDBNull(4) ? null : reader.GetInt32(4); var unit = reader.GetString(5);
        var lot = reader.GetInt32(6); var plan = reader.GetString(7); var sampleSize = reader.GetInt32(8);
        if (reader.Read()) throw Rule(409, "QC Item 存在大小寫不同的重複列，無法安全鎖定來源。");
        reader.Close();
        if (chartType is not null and not (-1 or 0 or 1 or 2)) throw Rule(422, $"QC Item {no} 的 ChartType 不支援 QC301。");
        if (spec is < 0 or > 2) throw Rule(422, $"QC Item {no} 的 SpecLineType 無效。");
        var factors = GetFactors(connection, transaction, no);
        return new(no, name, "Variable", chartType, spec, collect, unit, lot, plan, sampleSize, factors);
    }

    private static void EnsureNoDuplicate(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string qcItemNo, IReadOnlyDictionary<string, string> values, string? exceptSerial)
    {
        using var command = new NpgsqlCommand("""
            SELECT factorserial FROM "YMES".tblspcqcchartbasis
            WHERE lower(qcitemno)=lower(@no) AND factorvalues=@values AND (@except IS NULL OR factorserial<>@except)
            LIMIT 1 FOR UPDATE;
            """, connection, transaction);
        AddVarchar(command, "no", qcItemNo, 25); AddVarchar(command, "except", exceptSerial, 50);
        command.Parameters.Add("values", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(values);
        if (command.ExecuteScalar() is not null) throw Rule(409, "相同 QC Item 與要因組合已存在管制圖。");
    }

    private static int GetChartTypeBySerial(NpgsqlConnection connection, string serial)
    {
        using var command = new NpgsqlCommand("""
            SELECT COALESCE(q.charttype,-1)::int FROM "YMES".tblspcqcchartbasis b
            LEFT JOIN "YMES".tblqcitemforvariables q ON lower(q.qcitemno)=lower(b.qcitemno)
            WHERE lower(b.factorserial)=lower(@serial);
            """, connection);
        AddVarchar(command, "serial", ValidateCode(serial, "FactorSerial", 50, true), 50);
        var value = command.ExecuteScalar();
        return value is null ? throw Rule(404, "找不到管制圖資料。"): Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static int GetChartType(NpgsqlConnection connection, string qcItemNo) => GetChartType(connection, null, qcItemNo);
    private static int GetChartType(NpgsqlConnection connection, NpgsqlTransaction? transaction, string qcItemNo)
    {
        using var command = new NpgsqlCommand("SELECT COALESCE(charttype,-1)::int FROM \"YMES\".tblqcitemforvariables WHERE lower(qcitemno)=lower(@no);", connection, transaction);
        AddVarchar(command, "no", qcItemNo, 25);
        var value = command.ExecuteScalar();
        if (value is null) throw Rule(404, $"找不到計量 QC Item {qcItemNo}。");
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static void InsertDetailHistory(NpgsqlConnection connection, NpgsqlTransaction transaction, string serial, string actor)
    {
        EnsureTable(connection, transaction, "tblspcqcchartdetaillog", "QC Chart Detail Log 表 migration 0024");
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblspcqcchartdetaillog
                (factorserial,spcid,us,ls,ucl,lcl,cl,creator,starttime,target,cpkgoal,yaxisfrom,yaxisto,cs,memo)
            SELECT factorserial,spcid,us,ls,ucl,lcl,cl,@actor,now(),target,cpkgoal,yaxisfrom,yaxisto,cs,'QC301 Approve'
            FROM "YMES".tblspcqcchartdetail WHERE factorserial=@serial;
            """, connection, transaction);
        AddVarchar(command, "serial", serial, 50); AddVarchar(command, "actor", actor.Trim(), 10); command.ExecuteNonQuery();
    }

    private static bool MeasurementDataExists(NpgsqlConnection connection, string serial, NpgsqlTransaction? transaction = null)
    {
        if (!TableExists(connection, transaction, "tblspcvardetaillog")) return false;
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblspcvardetaillog WHERE factorserial=@serial LIMIT 1;", connection, transaction);
        AddVarchar(command, "serial", serial, 50);
        return command.ExecuteScalar() is not null;
    }

    private static bool WorkflowTablesExist(NpgsqlConnection connection, NpgsqlTransaction? transaction = null)
    {
        return new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }
            .Any(table => TableExists(connection, transaction, table));
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT to_regclass(format('%I.%I','YMES',@table)) IS NOT NULL;", connection, transaction);
        AddVarchar(command, "table", table, 63);
        return (bool)command.ExecuteScalar()!;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table, string label)
    {
        if (!TableExists(connection, transaction, table)) throw Rule(503, $"YMES 尚未建立 {label}；此操作未寫入資料。");
    }

    private static void EnsureChartTables(NpgsqlConnection connection, NpgsqlTransaction? transaction, bool includeSamplingParameters)
    {
        EnsureTable(connection, transaction, "tblspcqcchartbasis", "TBLSPCQCCHARTBASIS（migration 0024）");
        EnsureTable(connection, transaction, "tblspcqcchartdetail", "TBLSPCQCCHARTDETAIL（migration 0024）");
        EnsureTable(connection, transaction, "tblspcqcchartdetaillog", "TBLSPCQCCHARTDETAILLOG（migration 0024）");
        EnsureTable(connection, transaction, "tblspcqcchartrule", "TBLSPCQCCHARTRULE（migration 0024）");
        if (includeSamplingParameters) EnsureTable(connection, transaction, "tblspcqcchartsamplanparameter", "TBLSPCQCCHARTSAMPLANPARAMETER（migration 0025）");
    }

    private static int[] SpcIds(int? chartType) => chartType switch
    {
        0 => [0, 1, 2], 1 => [0, 1, 3], 2 => [0, 4], _ => [0]
    };

    private static string ChartName(int chartType, int spcId) => (chartType, spcId) switch
    {
        (0, 0) => "Trend Chart", (0, 1) => "Xbar Chart", (0, 2) => "R Chart",
        (1, 0) => "Trend Chart", (1, 1) => "Xbar Chart", (1, 3) => "S Chart",
        (2, 0) => "X Chart", (2, 4) => "Rm Chart", (_, 0) => "Trend Chart", _ => $"SPCID {spcId}"
    };

    private static string NewSerial() => $"FS{Guid.NewGuid():N}";
    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10) throw Rule(401, "登入帳號不可空白且不可超過 10 個字元。");
    }
    private static string ValidateCode(string? value, string name, int max, bool required)
    {
        var text = value?.Trim() ?? "";
        if ((required && text.Length == 0) || text.Length > max || text.Any(char.IsControl)) throw Rule(400, $"{name} {(required ? "為必填且" : "")}不可超過 {max} 個字元。");
        return text;
    }
    private static string ValidateKeyNode(string? value)
    {
        var text = value?.Trim().ToUpperInvariant() ?? "N";
        if (text.Length == 0) text = "N";
        if (text is not ("Y" or "N")) throw Rule(400, "KeyNode 僅允許 Y 或 N。");
        return text;
    }
    private static string ValidateNumeric(string? input, string field, int maxLength)
    {
        var value = input?.Trim() ?? "";
        if (value.Length > maxLength || (value.Length > 0 && !decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
            throw Rule(400, $"{field} 必須是 {maxLength} 字元內的數字。");
        return value;
    }
    private static Qc301RuleException Rule(int status, string message) => new(status, message);
    private static Qc301RuleException Conflict() => Rule(StatusCodes.Status409Conflict, "資料已變更，請重新載入後再操作。");
    private static void AddVarchar(NpgsqlCommand command, string name, string? value, int maxLength)
    {
        if (value is { Length: > 0 } && value.Length > maxLength) throw Rule(400, $"{name} 不可超過 {maxLength} 個字元。");
        command.Parameters.Add(name, NpgsqlDbType.Varchar, maxLength).Value = string.IsNullOrEmpty(value) ? DBNull.Value : value;
    }
    private static void AddNumeric(NpgsqlCommand command, string name, int? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value is null ? DBNull.Value : (decimal)value.Value;
    private static void AddDecimal(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static void TryRollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { } }

    private sealed class SpcRuleComparer : IEqualityComparer<(int SpcId, string Rule)>
    {
        public bool Equals((int SpcId, string Rule) x, (int SpcId, string Rule) y) => x.SpcId == y.SpcId && string.Equals(x.Rule, y.Rule, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((int SpcId, string Rule) obj) => HashCode.Combine(obj.SpcId, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Rule));
    }
}
