using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc303Repository(NpgsqlDataSource dataSource)
{
    private sealed record QcItem(string No, string Name, int ChartType, int SpecLineType,
        int ComponentCollect, string Unit, int LotIsValid);
    private sealed record Factor(string No, string Name, bool IsSpec, int ParameterType, string? ShowName);
    private sealed record Basis(string Serial, string QcItemNo, int IssueState, decimal DataStamp,
        string Description, string Creator, DateTime? CreateDate, string Reviser, DateTime? ReviseDate,
        Dictionary<string, string> FactorValues);
    private sealed record Detail(int SpcId, string Us, string Cs, string Ls, string Ucl, string Cl, string Lcl, string Memo);
    private sealed record NormalizedDetail(int SpcId, decimal? Us, decimal? Cs, decimal? Ls,
        string? Ucl, string? Cl, string? Lcl, string Memo);

    public IReadOnlyList<Qc303QcItemDto> QcItems()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");
        EnsureTable(connection, null, "tblqcitemfactor", "TBLQCITEMFACTOR 要因設定");
        var items = new List<QcItem>();
        using (var command = new NpgsqlCommand("""
            SELECT qcitemno,qcitemname,COALESCE(charttype,-1)::int,COALESCE(speclinetype,0)::int,
                   COALESCE(componentcollect,0)::int,COALESCE(unit,''),COALESCE(lotisvalid,0)::int
            FROM "YMES".tblqcitemforvariables ORDER BY qcitemno;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) items.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetString(5), reader.GetInt32(6)));
        }

        var result = new List<Qc303QcItemDto>(items.Count);
        foreach (var item in items)
        {
            var factors = GetFactors(connection, null, item.No);
            result.Add(new(item.No, item.Name, "計量", item.ChartType, item.SpecLineType,
                item.ComponentCollect, item.Unit, item.LotIsValid,
                factors.Select(f => new Qc303FactorDto(f.No, f.Name, f.IsSpec, f.ParameterType, f.ShowName)).ToArray()));
        }
        return result;
    }

    public IReadOnlyList<Qc303SpecBasisDto> List(string qcItemNo, int issueState)
    {
        ValidateState(issueState);
        var no = ValidateText(qcItemNo, "QCItemNo", 25, required: true);
        using var connection = dataSource.OpenConnection();
        EnsureSpecTables(connection, null);
        EnsureTable(connection, null, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");

        var result = new List<Qc303SpecBasisDto>();
        using var command = new NpgsqlCommand("""
            SELECT b.qcspecserialno,b.qcitemno,COALESCE(q.qcitemname,''),COALESCE(b.issuestate,0)::int,
                   COALESCE(b.datastamp,0),COALESCE(b.description,''),COALESCE(b.creator,''),b.createdate,
                   COALESCE(b.reviser,''),b.revisedate,b.factorvalues::text
            FROM "YMES".tblqcspecbasis b
            LEFT JOIN "YMES".tblqcitemforvariables q ON lower(q.qcitemno)=lower(b.qcitemno)
            WHERE lower(b.qcitemno)=lower(@item) AND b.issuestate=@state
            ORDER BY b.createdate DESC NULLS LAST,b.qcspecserialno;
            """, connection);
        AddVarchar(command, "item", no, 25); AddNumeric(command, "state", issueState);
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) result.Add(MapBasisList(reader));
        }
        return result;
    }

    public IReadOnlyList<Qc303SpecDetailDto> Details(string serial)
    {
        using var connection = dataSource.OpenConnection();
        EnsureSpecTables(connection, null);
        var basis = ReadBasis(connection, null, serial, forUpdate: false);
        var item = ReadQcItem(connection, null, basis.QcItemNo, lockRow: false);
        var stored = new Dictionary<int, Detail>();
        using (var command = new NpgsqlCommand("""
            SELECT spcid::int,COALESCE(us::text,''),COALESCE(cs::text,''),COALESCE(ls::text,''),
                   COALESCE(ucl,''),COALESCE(cl,''),COALESCE(lcl,''),COALESCE(memo,'')
            FROM "YMES".tblqcspecdetail WHERE qcspecserialno=@serial ORDER BY spcid;
            """, connection))
        {
            AddVarchar(command, "serial", basis.Serial, 35);
            using var reader = command.ExecuteReader();
            while (reader.Read()) stored[reader.GetInt32(0)] = new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7));
        }

        var rows = new List<Qc303SpecDetailDto>();
        foreach (var spcId in ExpectedSpcIds(item.ChartType))
        {
            var detail = stored.GetValueOrDefault(spcId) ?? new Detail(spcId, "", "", "", "", "", "", "");
            rows.Add(ToDetailDto(item.ChartType, detail));
        }
        rows.AddRange(stored.Values.Where(row => !ExpectedSpcIds(item.ChartType).Contains(row.SpcId))
            .OrderBy(row => row.SpcId).Select(row => ToDetailDto(item.ChartType, row)));
        return rows;
    }

    public IReadOnlyList<Qc303FactorOptionDto> FactorOptions(string qcItemNo, string factorNo)
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcitemfactor", "TBLQCITEMFACTOR 要因設定");
        var itemNo = ValidateText(qcItemNo, "QCItemNo", 25, required: true);
        var factorKey = ValidateText(factorNo, "FactorNo", 30, required: true);
        var factor = GetFactors(connection, null, itemNo)
            .SingleOrDefault(row => string.Equals(row.No, factorKey, StringComparison.OrdinalIgnoreCase) && row.IsSpec);
        if (factor is null) throw Rule(404, "找不到此 QC Item 的 FactorSpec=Y 要因。");
        if (factor.ParameterType == 2) return [];
        if (!TableExists(connection, null, "tblsysmesparameter"))
            throw Rule(503, "YMES 尚未建立 TBL SYSMESPARAMETER 參數目錄，無法提供 MES 因子正式選項。");

        // The source catalog stores lookup table/field identifiers as data. YMES
        // has no shared parameter-option service yet, so do not execute those
        // identifiers as SQL or invent option rows here.
        throw Rule(503, $"Factor {factor.No} 的 MES 參數選項尚未接入安全的 YMES 共用 lookup service。");
    }

    public Qc303SpecBasisDto Create(Qc303SpecWriteRequest request, string actor)
    {
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSpecTables(connection, transaction);
            var item = ReadQcItem(connection, transaction, request.QcItemNo, lockRow: true);
            var factors = NormalizeFactors(connection, transaction, item.No, request.FactorValues);
            var details = ValidateDetails(request.Details, item.ChartType, item.SpecLineType, request.Memo);
            EnsureNoDuplicate(connection, transaction, item.No, factors, exceptSerial: null);

            var serial = NewSerial();
            InsertBasis(connection, transaction, serial, item.No, request.Description, factors, actor);
            ReplaceDetails(connection, transaction, serial, details);
            transaction.Commit();
            return GetOne(serial);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw Rule(409, "相同 QC Item 與規格因子組合已存在。");
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc303SpecBasisDto Update(string serial, Qc303SpecWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var serialNo = ValidateText(serial, "QC Spec Serial No", 35, required: true);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSpecTables(connection, transaction);
            var current = ReadBasis(connection, transaction, serialNo, forUpdate: true);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有 UNFROZEN 規格可以修改。");
            if (!string.Equals(current.QcItemNo, request.QcItemNo?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw Rule(400, "QCItemNo 不可在修改時變更。");

            var item = ReadQcItem(connection, transaction, current.QcItemNo, lockRow: true);
            var factors = NormalizeFactors(connection, transaction, item.No, request.FactorValues);
            var details = ValidateDetails(request.Details, item.ChartType, item.SpecLineType, request.Memo);
            EnsureNoDuplicate(connection, transaction, item.No, factors, current.Serial);

            using (var update = new NpgsqlCommand("""
                UPDATE "YMES".tblqcspecbasis
                SET description=NULLIF(@description,''),factorvalues=@factors,reviser=@actor,
                    revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE qcspecserialno=@serial AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
                """, connection, transaction))
            {
                AddVarchar(update, "description", ValidateText(request.Description, "Description", 255, required: false), 255);
                AddJson(update, "factors", factors); AddVarchar(update, "actor", actor.Trim(), 10);
                AddVarchar(update, "serial", current.Serial, 35); AddNumeric(update, "stamp", request.DataStamp);
                if (update.ExecuteNonQuery() != 1) throw Conflict();
            }
            ReplaceDetails(connection, transaction, current.Serial, details);
            transaction.Commit();
            return GetOne(current.Serial);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw Rule(409, "相同 QC Item 與規格因子組合已存在。");
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc303StateResult Delete(string serial, Qc303SpecStateRequest request, string actor)
    {
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSpecTables(connection, transaction);
            var current = ReadBasis(connection, transaction, serial, forUpdate: true);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState == 1) throw Rule(409, "PENDING 規格不可刪除。");
            if (current.IssueState == 2)
            {
                using var retire = new NpgsqlCommand("""
                    UPDATE "YMES".tblqcspecbasis SET issuestate=-1,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                    WHERE qcspecserialno=@serial AND issuestate=2 AND COALESCE(datastamp,0)=@stamp;
                    """, connection, transaction);
                AddVarchar(retire, "actor", actor.Trim(), 10); AddVarchar(retire, "serial", current.Serial, 35);
                AddNumeric(retire, "stamp", request.DataStamp);
                if (retire.ExecuteNonQuery() != 1) throw Conflict();
                transaction.Commit();
                return new(current.Serial, -1, current.DataStamp + 1);
            }

            using (var detail = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcspecdetail WHERE qcspecserialno=@serial;", connection, transaction))
            {
                AddVarchar(detail, "serial", current.Serial, 35); detail.ExecuteNonQuery();
            }
            using (var basis = new NpgsqlCommand("""
                DELETE FROM "YMES".tblqcspecbasis
                WHERE qcspecserialno=@serial AND issuestate=@state AND COALESCE(datastamp,0)=@stamp;
                """, connection, transaction))
            {
                AddVarchar(basis, "serial", current.Serial, 35); AddNumeric(basis, "state", current.IssueState);
                AddNumeric(basis, "stamp", request.DataStamp);
                if (basis.ExecuteNonQuery() != 1) throw Conflict();
            }
            transaction.Commit();
            return new(current.Serial, current.IssueState, current.DataStamp);
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc303StateResult Approve(string serial, Qc303SpecStateRequest request, string actor)
    {
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSpecTables(connection, transaction);
            var current = ReadBasis(connection, transaction, serial, forUpdate: true);
            EnsureExpected(current, 0, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有 UNFROZEN 規格可以核准。");
            if (WorkflowTablesExist(connection, transaction))
                throw Rule(503, "YMES 已存在 Issue/Job 流程表，但 QC303 尚未接入共用簽核服務；資料未變更。");
            var nextStamp = ChangeState(connection, transaction, current, 2, actor);
            transaction.Commit();
            return new(current.Serial, 2, nextStamp);
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc303StateResult Disapprove(string serial, Qc303SpecStateRequest request, string actor)
    {
        ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSpecTables(connection, transaction);
            var current = ReadBasis(connection, transaction, serial, forUpdate: true);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState != 2) throw Rule(409, "只有 ACTIVE 規格可以取消核准。");
            if (WorkflowTablesExist(connection, transaction))
                throw Rule(503, "YMES 已存在 Issue/Job 流程表，但 QC303 尚未接入共用簽核服務；資料未變更。");
            var nextStamp = ChangeState(connection, transaction, current, 0, actor);
            transaction.Commit();
            return new(current.Serial, 0, nextStamp);
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc303CopyResult Copy(Qc303CopyRequest request, string actor)
    {
        ValidateActor(actor);
        if (request.CopyType is not (0 or 2 or 3)) throw Rule(400, "CopyType 僅允許 0、2 或 3。");
        var fromNo = ValidateText(request.FromQcItemNo, "FromQcItemNo", 25, required: true);
        var toNo = ValidateText(request.ToQcItemNo, "ToQcItemNo", 25, required: true);
        if (string.Equals(fromNo, toNo, StringComparison.OrdinalIgnoreCase)) throw Rule(400, "來源與目標 QC Item 不可相同。");

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSpecTables(connection, transaction);
            var sourceItem = ReadQcItem(connection, transaction, fromNo, lockRow: true);
            var targetItem = ReadQcItem(connection, transaction, toNo, lockRow: true);
            var sourceFactors = GetFactors(connection, transaction, sourceItem.No).Where(f => f.IsSpec).Select(f => f.No)
                .Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var targetFactors = GetFactors(connection, transaction, targetItem.No).Where(f => f.IsSpec).Select(f => f.No)
                .Order(StringComparer.OrdinalIgnoreCase).ToArray();
            if (!sourceFactors.SequenceEqual(targetFactors, StringComparer.OrdinalIgnoreCase))
                throw Rule(422, "來源與目標 QC Item 的 FactorSpec 名稱集合不一致，無法複製。");

            var stateFilter = request.CopyType switch { 0 => "issuestate=0", 2 => "issuestate=2", _ => "issuestate IN (0,2)" };
            var sourceRows = new List<Basis>();
            using (var list = new NpgsqlCommand($"""
                SELECT qcspecserialno,qcitemno,COALESCE(issuestate,0)::int,COALESCE(datastamp,0),COALESCE(description,''),
                       COALESCE(creator,''),createdate,COALESCE(reviser,''),revisedate,factorvalues::text
                FROM "YMES".tblqcspecbasis WHERE lower(qcitemno)=lower(@item) AND {stateFilter}
                ORDER BY qcspecserialno FOR UPDATE;
                """, connection, transaction))
            {
                AddVarchar(list, "item", sourceItem.No, 25);
                using var reader = list.ExecuteReader();
                while (reader.Read()) sourceRows.Add(MapBasisInternal(reader));
            }

            var copied = 0;
            foreach (var source in sourceRows)
            {
                var normalized = NormalizeFactors(connection, transaction, targetItem.No, source.FactorValues);
                if (HasDuplicate(connection, transaction, targetItem.No, normalized, null)) continue;
                var newSerial = NewSerial();
                InsertBasis(connection, transaction, newSerial, targetItem.No, source.Description, normalized, actor);
                CopyDetails(connection, transaction, source.Serial, newSerial);
                copied++;
            }
            transaction.Commit();
            return new(copied);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw Rule(409, "複製資料發生重複規格因子組合；交易已取消。");
        }
        catch { TryRollback(transaction); throw; }
    }

    private Qc303SpecBasisDto GetOne(string serial)
    {
        using var connection = dataSource.OpenConnection();
        var basis = ReadBasis(connection, null, serial, forUpdate: false);
        var item = ReadQcItem(connection, null, basis.QcItemNo, lockRow: false);
        return ToDto(basis, item.Name);
    }

    private static Qc303SpecBasisDto MapBasisList(NpgsqlDataReader reader)
    {
        var basis = new Basis(reader.GetString(0), reader.GetString(1), reader.GetInt32(3), reader.GetDecimal(4),
            reader.GetString(5), reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(10)) ?? new(StringComparer.Ordinal));
        return ToDto(basis, reader.GetString(2));
    }

    private static Basis MapBasisInternal(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetDecimal(3), reader.GetString(4),
        reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetDateTime(6), reader.GetString(7),
        reader.IsDBNull(8) ? null : reader.GetDateTime(8),
        JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(9)) ?? new(StringComparer.Ordinal));

    private static Qc303SpecBasisDto ToDto(Basis basis, string itemName) => new()
    {
        QcSpecSerialNo = basis.Serial,
        QcItemNo = basis.QcItemNo,
        QcItemName = itemName,
        IssueState = basis.IssueState,
        DataStamp = basis.DataStamp,
        Description = basis.Description,
        Creator = basis.Creator,
        CreateDate = basis.CreateDate,
        Reviser = basis.Reviser,
        ReviseDate = basis.ReviseDate,
        FactorValues = basis.FactorValues,
        FactorDisplayValues = new Dictionary<string, string>(),
        Details = []
    };

    private static Qc303SpecDetailDto ToDetailDto(int chartType, Detail row)
    {
        var name = (chartType, row.SpcId) switch
        {
            (-1, 0) => "Trend Chart",
            (0, 0) => "Trend Chart", (0, 1) => "Xbar Chart", (0, 2) => "R Chart",
            (1, 0) => "Trend Chart", (1, 1) => "Xbar Chart", (1, 3) => "S Chart",
            (2, 0) => "X Chart", (2, 4) => "Rm Chart",
            (_, 0) => "Trend Chart",
            _ => $"SPCID {row.SpcId}"
        };
        return new(row.SpcId, name, row.Us, row.Cs, row.Ls, row.Ucl, row.Cl, row.Lcl, row.Memo);
    }

    private static List<NormalizedDetail> ValidateDetails(IReadOnlyList<Qc303SpecDetailWriteRequest>? submitted,
        int chartType, int specLineType, string? requestMemo)
    {
        if (submitted is null) throw Rule(400, "請提供 QC Item Spec Detail。");
        var expected = ExpectedSpcIds(chartType);
        if (submitted.Select(x => x.SpcId).Distinct().Count() != submitted.Count ||
            !submitted.Select(x => x.SpcId).Order().SequenceEqual(expected.Order()))
            throw Rule(400, "Detail SPCID 必須與 QC Item ChartType 完全相符。");

        var rows = submitted.Select(row =>
        {
            var us = ParseDecimal(row.Us, "US"); var cs = ParseDecimal(row.Cs, "CS"); var ls = ParseDecimal(row.Ls, "LS");
            var ucl = NormalizeLimit(row.Ucl, "UCL"); var cl = NormalizeLimit(row.Cl, "CL"); var lcl = NormalizeLimit(row.Lcl, "LCL");
            var ordered = new[] { us, ParseLimitNumber(ucl, "UCL"), ParseLimitNumber(cl, "CL"), ParseLimitNumber(lcl, "LCL"), ls }
                .Where(value => value.HasValue).Select(value => value!.Value).ToArray();
            for (var index = 1; index < ordered.Length; index++)
                if (ordered[index - 1] < ordered[index]) throw Rule(400, "規格順序需符合 US ≥ UCL ≥ CL ≥ LCL ≥ LS。");
            return new NormalizedDetail(row.SpcId, us, cs, ls, ucl, cl, lcl,
                ValidateText(row.Memo?.Length > 0 ? row.Memo : requestMemo, "MEMO", 255, required: false));
        }).ToList();

        var first = rows[0];
        if (specLineType == 0 && (!first.Us.HasValue || !first.Ls.HasValue)) throw Rule(400, "雙邊規格必須填寫 US 與 LS。");
        if (specLineType == 1 && !first.Us.HasValue) throw Rule(400, "上限規格必須填寫 US。");
        if (specLineType == 2 && !first.Ls.HasValue) throw Rule(400, "下限規格必須填寫 LS。");
        return rows;
    }

    private static Qc303SpecDetailWriteRequest ToWriteRequest(Detail detail) => new()
    {
        SpcId = detail.SpcId, Us = detail.Us, Cs = detail.Cs, Ls = detail.Ls,
        Ucl = detail.Ucl, Cl = detail.Cl, Lcl = detail.Lcl, Memo = detail.Memo
    };

    private static decimal? ParseDecimal(string? input, string field)
    {
        var value = input?.Trim() ?? "";
        if (value.Length == 0) return null;
        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
            parsed < -99999999.9999m || parsed > 99999999.9999m || decimal.Round(parsed, 4) != parsed)
            throw Rule(400, $"{field} 必須是 -99999999.9999 到 99999999.9999 的 4 位小數數值。");
        return parsed;
    }

    private static string? NormalizeLimit(string? input, string field)
    {
        var value = input?.Trim() ?? "";
        if (value.Length == 0) return null;
        if (value.Length > 15 || value.Any(char.IsControl) ||
            !decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            throw Rule(400, $"{field} 如有填寫必須為 15 字元內的數值。");
        return value;
    }

    private static decimal? ParseLimitNumber(string? value, string field)
    {
        if (value is null) return null;
        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            throw Rule(400, $"{field} 必須是數值。");
        return parsed;
    }

    private static Dictionary<string, string> NormalizeFactors(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string qcItemNo, IReadOnlyDictionary<string, string>? submitted)
    {
        var factors = GetFactors(connection, transaction, qcItemNo).Where(row => row.IsSpec).ToList();
        var keys = new HashSet<string>(factors.Select(row => row.No), StringComparer.OrdinalIgnoreCase);
        var supplied = submitted ?? new Dictionary<string, string>();
        if (supplied.Keys.GroupBy(key => key.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw Rule(400, "factorValues 不可包含重複因子鍵。");
        foreach (var key in supplied.Keys)
            if (!keys.Contains(key.Trim())) throw Rule(400, $"FactorNo {key} 不屬於此 QC Item 的 FactorSpec=Y 因子。");

        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var factor in factors)
        {
            var pair = supplied.FirstOrDefault(row => string.Equals(row.Key.Trim(), factor.No, StringComparison.OrdinalIgnoreCase));
            var value = (pair.Key is null ? "" : pair.Value ?? "").Trim();
            if (value.Length > 61 || value.Any(char.IsControl)) throw Rule(400, $"因子 {factor.No} 不可超過 61 個字元或含控制字元。");
            normalized.Add(factor.No, value);
        }
        return normalized;
    }

    private static List<Factor> GetFactors(NpgsqlConnection connection, NpgsqlTransaction? transaction, string qcItemNo)
    {
        EnsureTable(connection, transaction, "tblqcitemfactor", "TBLQCITEMFACTOR 要因設定");
        var hasCatalog = TableExists(connection, transaction, "tblsysmesparameter");
        var sql = hasCatalog
            ? """
              SELECT f.factorno,COALESCE(p.parametername,f.factorno),upper(COALESCE(f.factorspec,'N'))='Y',
                     COALESCE(p.parametertype,0)::int,NULLIF(p.showname,'N/A')
              FROM "YMES".tblqcitemfactor f LEFT JOIN "YMES".tblsysmesparameter p ON lower(p.parameterno)=lower(f.factorno)
              WHERE lower(f.qcitemno)=lower(@no) AND upper(COALESCE(f.factoronline,''))='Y' ORDER BY f.factorno;
              """
            : "SELECT factorno,factorno,upper(COALESCE(factorspec,'N'))='Y',0::int,NULL::varchar FROM \"YMES\".tblqcitemfactor WHERE lower(qcitemno)=lower(@no) AND upper(COALESCE(factoronline,''))='Y' ORDER BY factorno;";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        AddVarchar(command, "no", qcItemNo, 25);
        using var reader = command.ExecuteReader();
        var result = new List<Factor>();
        while (reader.Read()) result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetBoolean(2), reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetString(4)));
        return result;
    }

    private static QcItem ReadQcItem(NpgsqlConnection connection, NpgsqlTransaction? transaction, string qcItemNo, bool lockRow)
    {
        EnsureTable(connection, transaction, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 計量品管項目");
        using var command = new NpgsqlCommand("""
            SELECT qcitemno,qcitemname,COALESCE(charttype,-1)::int,COALESCE(speclinetype,0)::int,
                   COALESCE(componentcollect,0)::int,COALESCE(unit,''),COALESCE(lotisvalid,0)::int
            FROM "YMES".tblqcitemforvariables WHERE lower(qcitemno)=lower(@no)
            """ + (lockRow ? " FOR UPDATE;" : ";"), connection, transaction);
        AddVarchar(command, "no", ValidateText(qcItemNo, "QCItemNo", 25, required: true), 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到計量 QC Item {qcItemNo}。");
        var result = new QcItem(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3),
            reader.GetInt32(4), reader.GetString(5), reader.GetInt32(6));
        reader.Close();
        if (result.ChartType is not (-1 or 0 or 1 or 2)) throw Rule(422, $"QC Item {result.No} 的 ChartType 不支援 QC303。");
        if (result.SpecLineType is < 0 or > 3) throw Rule(422, $"QC Item {result.No} 的 SpecLineType 無效。");
        return result;
    }

    private static Basis ReadBasis(NpgsqlConnection connection, NpgsqlTransaction? transaction, string serial, bool forUpdate)
    {
        var serialNo = ValidateText(serial, "QC Spec Serial No", 35, required: true);
        using var command = new NpgsqlCommand("""
            SELECT qcspecserialno,qcitemno,COALESCE(issuestate,0)::int,COALESCE(datastamp,0),COALESCE(description,''),
                   COALESCE(creator,''),createdate,COALESCE(reviser,''),revisedate,factorvalues::text
            FROM "YMES".tblqcspecbasis WHERE lower(qcspecserialno)=lower(@serial)
            """ + (forUpdate ? " FOR UPDATE;" : ";"), connection, transaction);
        AddVarchar(command, "serial", serialNo, 35);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到 QC Spec Serial No {serialNo}。");
        return MapBasisInternal(reader);
    }

    private static void InsertBasis(NpgsqlConnection connection, NpgsqlTransaction transaction, string serial,
        string qcItemNo, string? description, IReadOnlyDictionary<string, string> factors, string actor)
    {
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblqcspecbasis
                (qcitemno,qcspecserialno,issuestate,datastamp,description,creator,createdate,factorvalues)
            VALUES(@item,@serial,0,0,NULLIF(@description,''),@actor,now(),@factors);
            """, connection, transaction);
        AddVarchar(command, "item", qcItemNo, 25); AddVarchar(command, "serial", serial, 35);
        AddVarchar(command, "description", ValidateText(description, "Description", 255, required: false), 255);
        AddVarchar(command, "actor", actor.Trim(), 10); AddJson(command, "factors", factors); command.ExecuteNonQuery();
    }

    private static void ReplaceDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string serial,
        IReadOnlyList<NormalizedDetail> details)
    {
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcspecdetail WHERE qcspecserialno=@serial;", connection, transaction))
        {
            AddVarchar(delete, "serial", serial, 35); delete.ExecuteNonQuery();
        }
        foreach (var row in details)
        {
            using var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcspecdetail(spcid,us,ls,cs,qcspecserialno,ucl,cl,lcl,memo)
                VALUES(@spcid,@us,@ls,@cs,@serial,@ucl,@cl,@lcl,NULLIF(@memo,''));
                """, connection, transaction);
            AddNumeric(insert, "spcid", row.SpcId); AddNullableNumeric(insert, "us", row.Us);
            AddNullableNumeric(insert, "ls", row.Ls); AddNullableNumeric(insert, "cs", row.Cs);
            AddVarchar(insert, "serial", serial, 35); AddVarchar(insert, "ucl", row.Ucl, 15);
            AddVarchar(insert, "cl", row.Cl, 15); AddVarchar(insert, "lcl", row.Lcl, 15);
            AddVarchar(insert, "memo", row.Memo, 255); insert.ExecuteNonQuery();
        }
    }

    private static void CopyDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string fromSerial, string toSerial)
    {
        using var copy = new NpgsqlCommand("""
            INSERT INTO "YMES".tblqcspecdetail(spcid,us,ls,cs,qcspecserialno,ucl,cl,lcl,memo)
            SELECT spcid,us,ls,cs,@to,ucl,cl,lcl,memo
            FROM "YMES".tblqcspecdetail WHERE qcspecserialno=@from;
            """, connection, transaction);
        AddVarchar(copy, "to", toSerial, 35); AddVarchar(copy, "from", fromSerial, 35); copy.ExecuteNonQuery();
    }

    private static void EnsureNoDuplicate(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string qcItemNo, IReadOnlyDictionary<string, string> values, string? exceptSerial)
    {
        if (HasDuplicate(connection, transaction, qcItemNo, values, exceptSerial))
            throw Rule(409, "相同 QC Item 與規格因子組合已存在。");
    }

    private static bool HasDuplicate(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string qcItemNo, IReadOnlyDictionary<string, string> values, string? exceptSerial)
    {
        using var command = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tblqcspecbasis
            WHERE lower(qcitemno)=lower(@item) AND factorvalues=@factors AND (@except IS NULL OR qcspecserialno<>@except)
            LIMIT 1;
            """, connection, transaction);
        AddVarchar(command, "item", qcItemNo, 25); AddJson(command, "factors", values);
        AddVarchar(command, "except", exceptSerial, 35);
        return command.ExecuteScalar() is not null;
    }

    private static void EnsureExpected(Basis current, int issueState, decimal dataStamp)
    {
        ValidateState(issueState);
        if (current.IssueState != issueState || current.DataStamp != dataStamp) throw Conflict();
    }

    private static decimal ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Basis current, int nextState, string actor)
    {
        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tblqcspecbasis
            SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE qcspecserialno=@serial AND issuestate=@expected AND COALESCE(datastamp,0)=@stamp
            RETURNING datastamp;
            """, connection, transaction);
        AddNumeric(command, "next", nextState); AddVarchar(command, "actor", actor.Trim(), 10);
        AddVarchar(command, "serial", current.Serial, 35); AddNumeric(command, "expected", current.IssueState);
        AddNumeric(command, "stamp", current.DataStamp);
        return command.ExecuteScalar() is decimal stamp ? stamp : throw Conflict();
    }

    private static bool WorkflowTablesExist(NpgsqlConnection connection, NpgsqlTransaction transaction) =>
        new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }
            .Any(table => TableExists(connection, transaction, table));

    private static int[] ExpectedSpcIds(int chartType) => chartType switch
    {
        -1 => [0], 0 => [0, 1, 2], 1 => [0, 1, 3], 2 => [0, 4],
        _ => throw Rule(422, $"不支援的 ChartType {chartType}。")
    };

    private static string NewSerial() => "QCSN" + Guid.NewGuid().ToString("N").ToUpperInvariant()[..31];

    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10)
            throw Rule(401, "登入帳號不可空白且不可超過 10 個字元。");
    }

    private static void ValidateState(int issueState)
    {
        if (issueState is not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
    }

    private static string ValidateText(string? value, string field, int maxLength, bool required)
    {
        var text = value?.Trim() ?? "";
        if ((required && text.Length == 0) || text.Length > maxLength || text.Any(char.IsControl))
            throw Rule(400, $"{field} {(required ? "為必填且" : "")}不可超過 {maxLength} 個字元或含控制字元。");
        return text;
    }

    private static void EnsureSpecTables(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        EnsureTable(connection, transaction, "tblqcspecbasis", "TBLQCSPECBASIS（migration 0026）");
        EnsureTable(connection, transaction, "tblqcspecdetail", "TBLQCSPECDETAIL（migration 0026）");
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

    private static void AddVarchar(NpgsqlCommand command, string name, string? value, int length)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Varchar, length);
        parameter.Value = string.IsNullOrEmpty(value) ? DBNull.Value : value;
    }

    private static void AddNumeric(NpgsqlCommand command, string name, decimal value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static void AddNullableNumeric(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value is null ? DBNull.Value : value.Value;

    private static void AddJson(NpgsqlCommand command, string name, IReadOnlyDictionary<string, string> value) =>
        command.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(value);

    private static void TryRollback(NpgsqlTransaction transaction)
    {
        try { transaction.Rollback(); } catch { }
    }

    private static Qc303RuleException Conflict() => Rule(409, "資料已變更或狀態不符，請重新載入後再試。");
    private static Qc303RuleException Rule(int statusCode, string message) => new(statusCode, message);
}
