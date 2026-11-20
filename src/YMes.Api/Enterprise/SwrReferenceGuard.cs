using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

internal enum SwrReference { Customer, Operation }

// Read compatibility only. Never allocates serials, writes SWR, changes schema,
// grants privileges or caches a missing table beyond this transaction.
internal static class SwrReferenceGuard
{
    private const string Name = "tblwipswrbasis";
    private const string Table = "\"YMES\".tblwipswrbasis";

    internal static bool EnsureOptional(NpgsqlConnection connection, NpgsqlTransaction transaction,
        SwrReference reference, Func<Exception> unavailable)
    {
        var relation = InspectRelation(connection, transaction, unavailable);
        EnsureTypes(connection, transaction, relation, unavailable);
        if (relation is null) return false;

        EnsureNativeShape(connection, transaction, relation.Value.Relation, unavailable);
        // A zero-row probe requires only the actual reference-column SELECT,
        // reads no business row, and retains ACCESS SHARE through commit.
        using (var probe = new NpgsqlCommand($"SELECT {Column(reference)} FROM {Table} WHERE false;", connection, transaction))
            probe.ExecuteNonQuery();

        // The object could have changed before our relation lock was obtained.
        // Recheck identity/types/shape while replacement and structural ALTER
        // now conflict with the held lock. This is not a global DDL freeze.
        var locked = InspectRelation(connection, transaction, unavailable);
        if (locked != relation) throw unavailable();
        EnsureTypes(connection, transaction, locked, unavailable);
        EnsureNativeShape(connection, transaction, relation.Value.Relation, unavailable);
        return true;
    }

    internal static bool HasReferences(NpgsqlConnection connection, NpgsqlTransaction transaction,
        SwrReference reference, string no, bool present)
    {
        // The caller holds its real parent UPDATE lock. This must be a fresh
        // READ COMMITTED statement after any parent-lock wait; no reverse lock
        // on a SWR row/namespace is acquired by the parent deletion path.
        if (!present) return false;
        using var command = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Table} WHERE lower(btrim({Column(reference)}))=lower(btrim(@no)));", connection, transaction);
        command.Parameters.Add("no", NpgsqlDbType.Varchar).Value = no;
        return command.ExecuteScalar() is true;
    }

    private static string Column(SwrReference reference) => reference switch
    {
        SwrReference.Customer => "customerno",
        SwrReference.Operation => "opno",
        _ => throw new ArgumentOutOfRangeException(nameof(reference))
    };

    private static (long Relation, long RowType)? InspectRelation(NpgsqlConnection connection,
        NpgsqlTransaction transaction, Func<Exception> unavailable)
    {
        // Discover all relation kinds, including an index that occupies the
        // name; a shadow schema/case or wrong kind must not mean absent.
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
            ("swrno", "character varying(20)", true),
            ("swrtype", "numeric(1,0)", true),
            ("status", "numeric(2,0)", true),
            ("customerno", "character varying(50)", true),
            ("opno", "character varying(20)", true),
            ("deviceno", "character varying(50)", false),
            ("starttime", "timestamp without time zone", false),
            ("endtime", "timestamp without time zone", false),
            ("customerlotno", "character varying(50)", false),
            ("mono", "character varying(50)", false),
            ("attachfile", "bytea", false),
            ("swrdesc", "character varying(400)", true),
            ("creator", "character varying(50)", false),
            ("createdate", "timestamp without time zone", false),
            ("attachfilepath", "character varying(200)", false)
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
                if (field.Name is "swrno" or "customerno" or "opno")
                    if (reader.IsDBNull(6) || reader.IsDBNull(7) || reader.IsDBNull(8)
                        || reader.GetString(6) != "pg_catalog" || reader.GetString(7) != "default"
                        || !reader.GetBoolean(8)) throw unavailable();
            }
            if (ordinal != expected.Length) throw unavailable();
        }

        using var key = new NpgsqlCommand("""
            SELECT p.convalidated,p.condeferrable,p.condeferred,
              ARRAY(SELECT a.attname::text FROM unnest(p.conkey) WITH ORDINALITY k(attnum,ord)
                    JOIN pg_catalog.pg_attribute a ON a.attrelid=p.conrelid AND a.attnum=k.attnum ORDER BY k.ord),
              i.indisprimary,i.indisunique,i.indisvalid,i.indisready,i.indislive,
              i.indnkeyatts::int,i.indnatts::int,pg_get_expr(i.indpred,i.indrelid),i.indexprs IS NULL,
              am.amname,(SELECT count(*) FROM pg_catalog.pg_index ix WHERE ix.indrelid=p.conrelid)
            FROM pg_catalog.pg_constraint p JOIN pg_catalog.pg_index i ON i.indexrelid=p.conindid
            JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid JOIN pg_catalog.pg_am am ON am.oid=ic.relam
            WHERE p.conrelid=CAST(@relation AS oid) AND p.contype='p';
            """, connection, transaction);
        key.Parameters.Add("relation", NpgsqlDbType.Bigint).Value = relation;
        using var pk = key.ExecuteReader();
        if (!pk.Read() || !pk.GetBoolean(0) || pk.GetBoolean(1) || pk.GetBoolean(2)
            || !pk.GetFieldValue<string[]>(3).SequenceEqual(new[] { "swrno" }, StringComparer.Ordinal)
            || !pk.GetBoolean(4) || !pk.GetBoolean(5) || !pk.GetBoolean(6) || !pk.GetBoolean(7) || !pk.GetBoolean(8)
            || pk.GetInt32(9) != 1 || pk.GetInt32(10) != 1 || !pk.IsDBNull(11) || !pk.GetBoolean(12)
            || pk.GetString(13) != "btree" || pk.GetInt64(14) != 1 || pk.Read()) throw unavailable();
        // Source defaults are absent, but this read-only compatibility path
        // does not rely on INSERT/default behavior. The schema release owns
        // its exact default/ACL allowlist; no write is authorized here.
    }
}
