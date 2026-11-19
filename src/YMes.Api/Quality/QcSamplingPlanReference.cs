using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

// A namespace is selected by the trusted QC Item, never by a fabricated plan
// column or by searching the other namespace for the same PlanNo.
internal static class QcSamplingPlanReference
{
    internal sealed record Parameter(string ParameterNo, string? ParameterValue);
    internal sealed record GeneralPlan(string PlanNo, int PlanType, int IssueState, IReadOnlyList<Parameter> Parameters);

    internal static int Namespace(int? componentCollect, Func<int, string, Exception> error, int invalidStatus = 422)
    {
        if (componentCollect is not (0 or 1))
            throw error(invalidStatus, "QC Item 的 ComponentCollect 遺失或無效，無法判定抽樣計劃來源；不可將 NULL 或未知值當成一般計劃。");
        return componentCollect.Value;
    }

    internal static void RequireGeneral(int? componentCollect, Func<int, string, Exception> error, int invalidStatus = 422)
    {
        if (Namespace(componentCollect, error, invalidStatus) != 0)
            throw error(503, "ComponentCollect 非 0 使用 Wafer 抽樣計劃；Wafer provider 尚未完成，不能改讀一般抽樣計劃的同名 PlanNo。");
    }

    // Caller owns a transaction (and, for chart mutations, the QC Item/chart
    // mutex). QC103 modifies state/parameters only after FOR UPDATE on this
    // same parent, so approved status and parameter rows stay aligned to commit.
    internal static GeneralPlan ReadGeneral(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string planNo, int? componentCollect, int? expectedState, Func<int, string, Exception> error,
        int stateMismatchStatus = 422, int missingStatus = 422)
    {
        RequireGeneral(componentCollect, error);
        var no = planNo.Trim();
        if (no.Length is < 1 or > 25 || no.Any(char.IsControl)) throw error(400, "PlanNo 必填、最多 25 字元，且不可含控制字元。");
        EnsureTable(connection, transaction, "tblqcsamplingplan", error);
        EnsureTable(connection, transaction, "tblqcsamplingparameter", error);
        string canonical;
        int type, state;
        using (var command = new NpgsqlCommand("SELECT planno,plantype::int,issuestate::int FROM \"YMES\".tblqcsamplingplan WHERE lower(planno)=lower(@no) ORDER BY planno FOR SHARE;", connection, transaction))
        {
            command.Parameters.Add("no", NpgsqlDbType.Varchar, 25).Value = no;
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw error(missingStatus, $"找不到一般抽樣計劃 {no}。");
            canonical = reader.GetString(0);
            if (reader.IsDBNull(1) || reader.IsDBNull(2)) throw error(422, "一般抽樣計劃缺少 PlanType 或 IssueState。");
            type = reader.GetInt32(1); state = reader.GetInt32(2);
            if (reader.Read()) throw error(409, "一般 PlanNo 存在僅大小寫不同的重複主檔，請先整理。");
        }
        if (expectedState.HasValue && state != expectedState.Value)
            throw error(stateMismatchStatus, $"一般抽樣計劃 {canonical} 狀態不符，請重新載入已核准計劃。");
        var parameters = new List<Parameter>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = new NpgsqlCommand("SELECT parameterno,parametervalue FROM \"YMES\".tblqcsamplingparameter WHERE lower(planno)=lower(@no) ORDER BY parameterno FOR SHARE;", connection, transaction))
        {
            command.Parameters.Add("no", NpgsqlDbType.Varchar, 25).Value = canonical;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var key = reader.GetString(0);
                if (!keys.Add(key)) throw error(409, "一般抽樣計劃的 ParameterNo 存在大小寫重複，無法安全套用。");
                parameters.Add(new(key, reader.IsDBNull(1) ? null : reader.GetString(1)));
            }
        }
        return new(canonical, type, state, parameters);
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction transaction, string table,
        Func<int, string, Exception> error)
    {
        // table is an internal allowlisted constant; parameters remain values.
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p'));", connection, transaction);
        command.Parameters.Add("table", NpgsqlDbType.Varchar).Value = table;
        if (command.ExecuteScalar() is not true) throw error(503, $"YMES 尚未建立 {table}，一般抽樣計劃相依資料未完成。");
    }
}
