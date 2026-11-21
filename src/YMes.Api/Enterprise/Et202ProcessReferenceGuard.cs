using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

// PREPARED ONLY. Optional read compatibility for the native ET202 table.
// Does not create objects, grant privileges, write rows or cache absence.
internal static class Et202ProcessReferenceGuard
{
    private const string Name = "tblentositem";
    private const string Table = "\"YMES\".tblentositem";

    internal static void LockProcessNamespace(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string processNo, Func<int, string, Exception>? ruleFailure = null)
    {
        // Preserve the existing PRS mutex key exactly (.Trim + database lower),
        // while validating PostgreSQL character counts rather than UTF-16
        // code units and returning this module's actual rule exception.
        Exception Failure(int status, string message) => ruleFailure?.Invoke(status, message) ?? Rule(status, message);
        var value = (processNo ?? "").Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); }
        catch (EncoderFallbackException) { throw Failure(400, "ProcessNo 含不完整 Unicode 字元。"); }
        var length = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.IsControl(rune)) throw Failure(400, "ProcessNo 不可含控制字元。");
            length++;
        }
        if (length is < 1 or > 30) throw Failure(400, "ProcessNo 必填且最多 30 字元。");
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:PRS:IDENTIFIER:' || lower(@no),0));", connection, transaction);
        command.Parameters.Add("no", NpgsqlDbType.Varchar).Value = value;
        command.ExecuteNonQuery();
    }

    // Call before the ET202 own namespace/row lock. Lock every matching
    // version in stable database order: ET202 stores ProcessNo, not a version.
    internal static string LockQualifyingProcess(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string processNo)
    {
        LockProcessNamespace(connection, transaction, processNo);
        using var command = new NpgsqlCommand("""
            SELECT processno,processversion,issuestate::numeric,processclass::numeric
            FROM "YMES".tblprsprocessbasis
            WHERE lower(btrim(processno))=lower(btrim(@no))
            ORDER BY processno COLLATE "C",processversion COLLATE "C"
            FOR SHARE;
            """, connection, transaction);
        command.Parameters.Add("no", NpgsqlDbType.Varchar).Value = processNo;
        using var reader = command.ExecuteReader();
        string? canonical = null;
        var qualified = false;
        while (reader.Read())
        {
            var raw = reader.GetString(0);
            if (canonical is not null && !StringComparer.Ordinal.Equals(canonical, raw))
                throw Rule(409, "ProcessNo 存在大小寫或空白不同的多個識別，請先整理後重新選擇。");
            canonical = raw;
            // No CurVersion restriction: the source allows any approved
            // ProcessClass=3 version of this single ProcessNo identity.
            qualified |= !reader.IsDBNull(2) && !reader.IsDBNull(3)
                && reader.GetDecimal(2) == 2m && reader.GetDecimal(3) == 3m;
        }
        if (canonical is null || !qualified)
            throw Rule(422, "外包項目必須選擇至少有一個已核准（狀態 2）、ProcessClass=3 版本的流程。");
        return canonical;
    }

    internal static bool EnsureOptional(NpgsqlConnection connection,
        NpgsqlTransaction transaction, Func<Exception> unavailable)
    {
        var relation = InspectRelation(connection, transaction, unavailable);
        EnsureTypes(connection, transaction, relation, unavailable);
        if (relation is null) return false;

        EnsureNativeShape(connection, transaction, relation.Value.Relation, unavailable);
        // Only processno SELECT is required by the parent reference guard.
        // No app table-wide grant or write privilege is assumed here.
        using (var probe = new NpgsqlCommand($"SELECT processno FROM {Table} WHERE false;", connection, transaction))
            probe.ExecuteNonQuery();

        var locked = InspectRelation(connection, transaction, unavailable);
        if (locked != relation) throw unavailable();
        EnsureTypes(connection, transaction, locked, unavailable);
        EnsureNativeShape(connection, transaction, relation.Value.Relation, unavailable);
        return true;
    }

    internal static bool HasReferences(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string processNo, bool present)
    {
        // Parent deletion already holds the real Process row FOR UPDATE.
        // A new READ COMMITTED statement after that lock wait observes any
        // cooperating ET202 writer that committed before the parent lock.
        if (!present) return false;
        using var command = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Table} WHERE lower(btrim(processno))=lower(btrim(@no)));", connection, transaction);
        command.Parameters.Add("no", NpgsqlDbType.Varchar).Value = processNo;
        return command.ExecuteScalar() is true;
    }

    private static (long Relation, long RowType)? InspectRelation(NpgsqlConnection connection,
        NpgsqlTransaction transaction, Func<Exception> unavailable)
    {
        using var command = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,c.relpersistence::text,
              pg_get_userbyid(c.relowner),c.relrowsecurity,c.relforcerowsecurity,
              c.oid::bigint,c.reltype::bigint,c.relispartition,c.relhasrules,
              EXISTS(SELECT 1 FROM pg_catalog.pg_inherits i WHERE i.inhrelid=c.oid OR i.inhparent=c.oid),
              EXISTS(SELECT 1 FROM pg_catalog.pg_trigger g WHERE g.tgrelid=c.oid),
              EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k WHERE k.contype='f'
                AND (k.conrelid=c.oid OR k.confrelid=c.oid))
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)=@name
            ORDER BY n.nspname COLLATE "C",c.relname COLLATE "C";
            """, connection, transaction);
        command.Parameters.Add("name", NpgsqlDbType.Text).Value = Name;
        using var reader = command.ExecuteReader();
        (long Relation, long RowType)? result = null;
        while (reader.Read())
        {
            if (result is not null || reader.GetString(0) != "YMES" || reader.GetString(1) != Name
                || reader.GetString(2) != "r" || reader.GetString(3) != "p" || reader.GetString(4) != "postgres"
                || reader.GetBoolean(5) || reader.GetBoolean(6) || reader.GetBoolean(9)
                || reader.GetBoolean(10) || reader.GetBoolean(11) || reader.GetBoolean(12) || reader.GetBoolean(13))
                throw unavailable();
            result = (reader.GetInt64(7), reader.GetInt64(8));
        }
        return result;
    }

    private static void EnsureTypes(NpgsqlConnection connection, NpgsqlTransaction transaction,
        (long Relation, long RowType)? relation, Func<Exception> unavailable)
    {
        using var command = new NpgsqlCommand("""
            SELECT n.nspname,t.typname,t.typtype::text,t.typrelid::bigint,t.typelem::bigint,
              t.typarray::bigint,t.oid::bigint
            FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(t.typname)=ANY(@names)
            ORDER BY n.nspname COLLATE "C",t.typname COLLATE "C";
            """, connection, transaction);
        command.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { Name, "_" + Name };
        using var reader = command.ExecuteReader();
        long? rowArray = null; long? arrayOid = null;
        while (reader.Read())
        {
            if (relation is null || reader.GetString(0) != "YMES") throw unavailable();
            if (reader.GetString(1) == Name && rowArray is null)
            {
                if (reader.GetString(2) != "c" || reader.GetInt64(3) != relation.Value.Relation
                    || reader.GetInt64(4) != 0 || reader.GetInt64(6) != relation.Value.RowType) throw unavailable();
                rowArray = reader.GetInt64(5);
            }
            else if (reader.GetString(1) == "_" + Name && arrayOid is null)
            {
                if (reader.GetString(2) != "b" || reader.GetInt64(3) != 0
                    || reader.GetInt64(4) != relation.Value.RowType) throw unavailable();
                arrayOid = reader.GetInt64(6);
            }
            else throw unavailable();
        }
        if (relation is not null && (rowArray is null || arrayOid is null || rowArray != arrayOid)) throw unavailable();
    }

    private static void EnsureNativeShape(NpgsqlConnection connection, NpgsqlTransaction transaction,
        long relation, Func<Exception> unavailable)
    {
        var expected = new (string Name, string Type, bool NotNull)[]
        {
            ("ositemno", "character varying(2)", true),
            ("ositemname", "character varying(50)", true),
            ("plandays", "numeric(3,0)", true),
            ("processno", "character varying(30)", true),
            ("returntype", "numeric(1,0)", true),
            ("description", "character varying(255)", false),
            ("creator", "character varying(10)", false),
            ("createdate", "timestamp without time zone", false),
            ("datastamp", "numeric(11,0)", false),
            ("reviser", "character varying(10)", false),
            ("revisedate", "timestamp without time zone", false)
        };
        using (var command = new NpgsqlCommand("""
            SELECT a.attname,a.attnum::int,format_type(a.atttypid,a.atttypmod),a.attnotnull,
              a.attidentity::text,a.attgenerated::text,cn.nspname,co.collname,co.collisdeterministic,a.attisdropped
            FROM pg_catalog.pg_attribute a
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE a.attrelid=CAST(@relation AS oid) AND a.attnum>0
            ORDER BY a.attnum;
            """, connection, transaction))
        {
            command.Parameters.Add("relation", NpgsqlDbType.Bigint).Value = relation;
            using var reader = command.ExecuteReader();
            var ordinal = 0;
            while (reader.Read())
            {
                if (reader.GetBoolean(9) || ordinal >= expected.Length) throw unavailable();
                var field = expected[ordinal++];
                if (reader.GetString(0) != field.Name || reader.GetInt32(1) != ordinal
                    || reader.GetString(2) != field.Type || reader.GetBoolean(3) != field.NotNull
                    || reader.GetString(4) != "" || reader.GetString(5) != "") throw unavailable();
                if (field.Type.StartsWith("character varying(", StringComparison.Ordinal))
                    if (reader.IsDBNull(6) || reader.IsDBNull(7) || reader.IsDBNull(8)
                        || reader.GetString(6) != "pg_catalog" || reader.GetString(7) != "default"
                        || !reader.GetBoolean(8)) throw unavailable();
            }
            if (ordinal != expected.Length) throw unavailable();
        }

        using var key = new NpgsqlCommand("""
            SELECT p.conname,p.convalidated,p.condeferrable,p.condeferred,
              ARRAY(SELECT a.attname::text FROM unnest(p.conkey) WITH ORDINALITY k(attnum,ord)
                    JOIN pg_catalog.pg_attribute a ON a.attrelid=p.conrelid AND a.attnum=k.attnum ORDER BY k.ord),
              i.indisprimary,i.indisunique,i.indisvalid,i.indisready,i.indislive,
              i.indnkeyatts::int,i.indnatts::int,pg_get_expr(i.indpred,i.indrelid),i.indexprs IS NULL,
              am.amname,ic.relname,ns.nspname,
              (SELECT count(*) FROM pg_catalog.pg_index ix WHERE ix.indrelid=p.conrelid),
              (SELECT count(*) FROM pg_catalog.pg_constraint k WHERE k.conrelid=p.conrelid)
            FROM pg_catalog.pg_constraint p JOIN pg_catalog.pg_index i ON i.indexrelid=p.conindid
            JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid JOIN pg_catalog.pg_namespace ns ON ns.oid=ic.relnamespace
            JOIN pg_catalog.pg_am am ON am.oid=ic.relam
            WHERE p.conrelid=CAST(@relation AS oid) AND p.contype='p';
            """, connection, transaction);
        key.Parameters.Add("relation", NpgsqlDbType.Bigint).Value = relation;
        using var pk = key.ExecuteReader();
        if (!pk.Read() || pk.GetString(0) != "pk_tblentositem" || !pk.GetBoolean(1) || pk.GetBoolean(2) || pk.GetBoolean(3)
            || !pk.GetFieldValue<string[]>(4).SequenceEqual(new[] { "ositemno" }, StringComparer.Ordinal)
            || !pk.GetBoolean(5) || !pk.GetBoolean(6) || !pk.GetBoolean(7) || !pk.GetBoolean(8) || !pk.GetBoolean(9)
            || pk.GetInt32(10) != 1 || pk.GetInt32(11) != 1 || !pk.IsDBNull(12) || !pk.GetBoolean(13)
            || pk.GetString(14) != "btree" || pk.GetString(15) != "pk_tblentositem" || pk.GetString(16) != "YMES"
            || pk.GetInt64(17) != 1 || pk.GetInt64(18) != 1 || pk.Read()) throw unavailable();
        // The optional parent read does not depend on defaults or table-wide
        // ACLs. Exact DEFAULT 0 and own CRUD are separate schema/writer gates.
    }

    private static Et202RuleException Rule(int status, string message) => new(status, message);
}
