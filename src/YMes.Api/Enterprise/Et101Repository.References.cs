using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed partial class Et101Repository
{
    private sealed record AssignedParent(string Table, string Key, string Namespace);
    private static readonly AssignedParent Broker = new("tblentbrokerbasis", "brokerno", "Broker");
    private static readonly AssignedParent Forwarder = new("tblentforwarderbasis", "forwarderno", "Forwarder");
    private static readonly (string Table, string Column, string Type, bool NotNull)[] ReferenceColumns =
    [
        ("tblentcustomerbasis", "customerno", "character varying(50)", true),
        ("tblentcustomercontactor", "customerno", "character varying(50)", true),
        ("tblentcustomershipaddr", "customerno", "character varying(50)", true),
        ("tblentcustomershipdeclaration", "customerno", "character varying(50)", true),
        ("tbl_bopcustopmapbasis", "customerno", "character varying(50)", true),
        ("tbl_bopcustopmapdetail", "customerno", "character varying(50)", true),
        ("tblprdproductbasis", "customerno", "character varying(50)", false),
        ("tblusruserbasis", "customerno", "character varying(20)", false)
    ];

    public IReadOnlyList<Et101BrokerOption> Brokers()
    {
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureAssignedParent(c, t, Broker);
        using var cmd = new NpgsqlCommand("""
            SELECT brokerno,contactorname,issuestate::int,count(*) OVER(PARTITION BY lower(btrim(brokerno)))
            FROM "YMES".tblentbrokerbasis ORDER BY lower(btrim(brokerno)) COLLATE "C",brokerno COLLATE "C";
            """, c, t);
        var rows = new List<Et101BrokerOption>();
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                if (r.GetInt64(3) != 1) throw Rule(409, "報關行有大小寫／空白歧義，不能任選來源。");
                rows.Add(new(r.GetString(0), Str(r, 1), r.IsDBNull(2) ? null : r.GetInt32(2)));
            }
        t.Commit(); return rows;
    }

    public IReadOnlyList<Et101ForwarderOption> Forwarders()
    {
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureAssignedParent(c, t, Forwarder);
        using var cmd = new NpgsqlCommand("""
            SELECT forwarderno,contactorname,brokerno,issuestate::int,
              count(*) OVER(PARTITION BY lower(btrim(forwarderno)))
            FROM "YMES".tblentforwarderbasis ORDER BY lower(btrim(forwarderno)) COLLATE "C",forwarderno COLLATE "C";
            """, c, t);
        var rows = new List<Et101ForwarderOption>();
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                if (r.GetInt64(4) != 1) throw Rule(409, "貨運行有大小寫／空白歧義，不能任選來源。");
                rows.Add(new(r.GetString(0), Str(r, 1), r.GetString(2), r.IsDBNull(3) ? null : r.GetInt32(3)));
            }
        t.Commit(); return rows;
    }

    private static void CheckAssignedParents(NpgsqlConnection c, NpgsqlTransaction t,
        Et101WriteRequest input, Et101CustomerDto? old)
    {
        // Entire old/new union per rank; database normalization and C ordering.
        AcquireAssigned(c, t, Broker, [old?.BrokerAssigned, input.BrokerAssigned]);
        CheckAssigned(c, t, Broker, input.BrokerAssigned, old?.BrokerAssigned, old is not null);
        AcquireAssigned(c, t, Forwarder, [old?.ForwarderAssigned, input.ForwarderAssigned]);
        CheckAssigned(c, t, Forwarder, input.ForwarderAssigned, old?.ForwarderAssigned, old is not null);
    }

    private static void AcquireAssigned(NpgsqlConnection c, NpgsqlTransaction t, AssignedParent spec, string?[] inputs)
    {
        using var sort = new NpgsqlCommand("SELECT DISTINCT lower(btrim(v)) COLLATE \"C\" AS key FROM unnest(@keys) v WHERE v IS NOT NULL ORDER BY key;", c, t);
        sort.Parameters.Add("keys", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = inputs;
        var keys = new List<string>(); using (var r = sort.ExecuteReader()) while (r.Read()) keys.Add(r.GetString(0));
        foreach (var key in keys)
        {
            using var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@prefix || @key,0));", c, t);
            Text(cmd, "prefix", "YMES:ENT:" + spec.Namespace + ":"); Text(cmd, "key", key); cmd.ExecuteNonQuery();
        }
    }

    private static void CheckAssigned(NpgsqlConnection c, NpgsqlTransaction t, AssignedParent spec,
        string? requested, string? previous, bool existing)
    {
        if (existing && string.Equals(requested, previous, StringComparison.Ordinal)) return;
        if (requested is null || requested.Trim(' ').Length == 0) return;
        EnsureAssignedParent(c, t, spec);
        using var cmd = new NpgsqlCommand($"SELECT {spec.Key} FROM \"YMES\".{spec.Table} WHERE lower(btrim({spec.Key}))=lower(btrim(@key)) ORDER BY {spec.Key} COLLATE \"C\" FOR SHARE;", c, t);
        Text(cmd, "key", requested); using var r = cmd.ExecuteReader();
        if (!r.Read()) throw Rule(422, "新增或變更的指定報關行／貨運行必須真實存在，資料未變更。");
        var raw = r.GetString(0);
        if (r.Read()) throw Rule(409, "指定報關行／貨運行存在大小寫／空白歧義。");
        if (!string.Equals(raw, requested, StringComparison.Ordinal))
            throw Rule(422, "指定報關行／貨運行必須使用真實原始識別碼，請由查找重新選擇。");
        // Source Customer text fields do not prove a state2 requirement.
        // All actual parent states, including NULL, qualify by existence only.
    }

    private static bool AnyOwnedReference(NpgsqlConnection c, NpgsqlTransaction t, string no, bool swrPresent)
    {
        // Whitelisted relation names only; the metadata registry and zero-row
        // probes have completed before this first business reference query.
        foreach (var spec in ReferenceColumns.Where(x => x.Table != "tblentcustomerbasis"))
        {
            using var cmd = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM \"YMES\".{spec.Table} WHERE lower(btrim(customerno))=lower(btrim(@customer)));", c, t);
            Text(cmd, "customer", no); if (cmd.ExecuteScalar() is true) return true;
        }
        return SwrReferenceGuard.HasReferences(c, t, SwrReference.Customer, no, swrPresent);
    }

    private static bool HasNonCascadingReferences(NpgsqlConnection c, NpgsqlTransaction t, string no, bool swrPresent)
    {
        // Each statement runs after the own parent UPDATE lock has waited.
        // MVCC reads, not reverse FOR UPDATE/SHARE on OP/Product/User rows.
        using var cmd = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM "YMES".tblentcustomershipdeclaration WHERE lower(btrim(customerno))=lower(btrim(@customer)))
              OR EXISTS(SELECT 1 FROM "YMES".tbl_bopcustopmapbasis WHERE lower(btrim(customerno))=lower(btrim(@customer)))
              OR EXISTS(SELECT 1 FROM "YMES".tbl_bopcustopmapdetail WHERE lower(btrim(customerno))=lower(btrim(@customer)))
              OR EXISTS(SELECT 1 FROM "YMES".tblprdproductbasis WHERE lower(btrim(customerno))=lower(btrim(@customer)))
              OR EXISTS(SELECT 1 FROM "YMES".tblusruserbasis WHERE lower(btrim(customerno))=lower(btrim(@customer)));
            """, c, t);
        Text(cmd, "customer", no);
        return cmd.ExecuteScalar() is true
            || SwrReferenceGuard.HasReferences(c, t, SwrReference.Customer, no, swrPresent);
    }

    private static void AssertNativeChildOwners(NpgsqlConnection c, NpgsqlTransaction t, string raw)
    {
        using var cmd = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM "YMES".tblentcustomershipaddr
                WHERE lower(btrim(customerno))=lower(btrim(@customer)) AND customerno<>@customer)
              OR EXISTS(SELECT 1 FROM "YMES".tblentcustomercontactor
                WHERE lower(btrim(customerno))=lower(btrim(@customer)) AND customerno<>@customer);
            """, c, t);
        Text(cmd, "customer", raw);
        if (cmd.ExecuteScalar() is true) throw Rule(409, "原生子資料含不同大小寫／空白owner，未擴大刪除範圍。");
    }
}
