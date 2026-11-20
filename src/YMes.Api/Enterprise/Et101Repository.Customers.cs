using System.Data;
using Npgsql;

namespace YMes.Api.Enterprise;

public sealed partial class Et101Repository
{
    public Et101CustomerDto Create(Et101WriteRequest input, string actor)
    {
        RawKey(input.CustomerNo); ValidateFields(input); actor = Actor(actor, create: true);
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        string no;
        using (var canonical = new NpgsqlCommand("SELECT upper(btrim(@customer));", c, t))
        { Text(canonical, "customer", input.CustomerNo); no = (string)canonical.ExecuteScalar()!; }
        RawKey(no); AcquireCustomer(c, t, no); EnsureOwn(c, t);
        if (Read(c, t, no, update: true) is not null) throw Rule(409, "客戶已存在，不可接管既有資料。");
        var swrPresent = EnsureReferences(c, t);
        if (AnyOwnedReference(c, t, no, swrPresent))
            throw Rule(409, "此客戶編號已有孤立子資料；新增不會默認接管，請先釐清原始歸屬。");
        CheckAssignedParents(c, t, input, null);
        var names = string.Join(',', Fields.Select(x => x.Column));
        var parameters = string.Join(',', Fields.Select(x => "@" + x.Column));
        using var cmd = new NpgsqlCommand($"""
            INSERT INTO {Table} (customerno,{names},creator,createdate,issuestate,datastamp,reviser,revisedate,
                erpno,sap_fiskn,xx_name1,xx_name2,xx_rebarcode,xx_supplydata)
            VALUES (@customer,{parameters},@actor,now() AT TIME ZONE 'UTC',0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL)
            RETURNING {Columns};
            """, c, t);
        Text(cmd, "customer", no); Text(cmd, "actor", actor); BindFields(cmd, input);
        var saved = Returned(cmd); t.Commit(); return saved;
    }

    public Et101CustomerDto Update(string customerNo, Et101UpdateRequest input, string actor)
    {
        RawKey(customerNo); RawKey(input.CustomerNo); Stamp(input.DataStamp); actor = Actor(actor, create: false);
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var old = LockCustomer(c, t, customerNo);
        Tokens(old, input.IssueState, input.DataStamp);
        if (!string.Equals(old.CustomerNo, input.CustomerNo, StringComparison.Ordinal))
            throw Rule(400, "修改時 CustomerNo 必須保留原始識別碼，不可轉大寫、去空白或改號。");
        ValidateFields(input, old); Incrementable(old.DataStamp);
        CheckAssignedParents(c, t, input, old);
        var assignments = string.Join(',', Fields.Select(x => x.Column + "=@" + x.Column));
        using var cmd = new NpgsqlCommand($"""
            UPDATE {Table} SET {assignments},reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',
                datastamp=COALESCE(datastamp,0)+1
            WHERE customerno=@customer AND issuestate IS NOT DISTINCT FROM @state
              AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
            """, c, t);
        BindExpected(cmd, old); BindFields(cmd, input); Text(cmd, "actor", actor);
        var saved = Returned(cmd); t.Commit(); return saved;
    }

    public Et101CustomerDto Approve(string customerNo, Et101ExpectedRequest input, string actor)
    {
        RawKey(customerNo); Stamp(input.DataStamp); actor = Actor(actor, create: false);
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var old = LockCustomer(c, t, customerNo); Tokens(old, input.IssueState, input.DataStamp);
        if (old.IssueState != 0) throw Rule(409, "只有未簽核客戶可提出核准。");
        Incrementable(old.DataStamp);
        // Root clarification: source-supported direct approval is available
        // only after a fresh all-schema catalogue proves no workflow candidate.
        // Existing or unknown workflow sources are never a fake direct success.
        if (WorkflowMayExist(c, t))
            throw Rule(503, "偵測到簽核來源，但CUSTOMER BASIS簽核服務尚未接上，本次資料未變更。");
        using var cmd = new NpgsqlCommand($"""
            UPDATE {Table} SET issuestate=2,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',
              datastamp=COALESCE(datastamp,0)+1
            WHERE customerno=@customer AND issuestate IS NOT DISTINCT FROM @state
              AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
            """, c, t);
        BindExpected(cmd, old); Text(cmd, "actor", actor); var saved = Returned(cmd);
        t.Commit(); return saved;
    }

    public Et101DeleteResult Delete(string customerNo, Et101ExpectedRequest input, string actor)
    {
        RawKey(customerNo); Stamp(input.DataStamp); actor = Actor(actor, create: false);
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var old = LockCustomer(c, t, customerNo); Tokens(old, input.IssueState, input.DataStamp);
        if (old.IssueState == 1) throw Rule(409, "簽核中客戶不可刪除。");
        if (old.IssueState == 2)
        {
            // Referenced customers may retire; children/references remain.
            Incrementable(old.DataStamp);
            using var cmd = new NpgsqlCommand($"""
                UPDATE {Table} SET issuestate=-1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',
                  datastamp=COALESCE(datastamp,0)+1
                WHERE customerno=@customer AND issuestate IS NOT DISTINCT FROM @state
                  AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
                """, c, t);
            BindExpected(cmd, old); Text(cmd, "actor", actor); var saved = Returned(cmd);
            t.Commit(); return new("retired", old.CustomerNo, 0, 0, saved);
        }
        if (old.IssueState is not (0 or -1))
            throw Rule(422, "原始NULL或未知客戶狀態無已證刪除流程，資料已保留。");

        // Own parent FOR UPDATE has already waited. Every probe/count below
        // uses a fresh READ COMMITTED statement; no reverse child-parent locks.
        var swrPresent = EnsureReferences(c, t);
        if (HasNonCascadingReferences(c, t, old.CustomerNo, swrPresent))
            throw Rule(409, "客戶仍被報關設定、站點對應、產品、使用者或特殊生產需求引用；所有資料已保留。");
        AssertNativeChildOwners(c, t, old.CustomerNo);
        int addresses, contacts;
        using (var cmd = new NpgsqlCommand("DELETE FROM \"YMES\".tblentcustomershipaddr WHERE customerno=@customer;", c, t))
        { Text(cmd, "customer", old.CustomerNo); addresses = cmd.ExecuteNonQuery(); }
        using (var cmd = new NpgsqlCommand("DELETE FROM \"YMES\".tblentcustomercontactor WHERE customerno=@customer;", c, t))
        { Text(cmd, "customer", old.CustomerNo); contacts = cmd.ExecuteNonQuery(); }
        using (var cmd = new NpgsqlCommand($"""
            DELETE FROM {Table} WHERE customerno=@customer
              AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
            """, c, t))
        { BindExpected(cmd, old); if (cmd.ExecuteNonQuery() != 1) throw Conflict(); }
        t.Commit(); return new("deleted", old.CustomerNo, addresses, contacts, null);
    }
    private static bool WorkflowMayExist(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname NOT IN ('pg_catalog','information_schema')
                AND c.relkind IN ('r','p','v','m','f')
                AND (lower(c.relname)=ANY(@known) OR lower(c.relname) LIKE 'tblisu%'
                  OR lower(c.relname) LIKE '%issue%job%' OR lower(c.relname) LIKE '%job%issue%'));
            """, connection, transaction);
        command.Parameters.Add("known", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text).Value = new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" };
        return command.ExecuteScalar() is true;
    }

}
