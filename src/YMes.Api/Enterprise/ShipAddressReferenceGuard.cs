using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

internal enum ShipAddressReference { Broker, Forwarder, Taker }

// Read compatibility only. Absence is discovered for every transaction; this
// guard never creates a provider, table, row, grant or long-lived absence cache.
internal static class ShipAddressReferenceGuard
{
    private const string RelationName = "tblentcustomershipaddr";
    private const string Table = "\"YMES\".tblentcustomershipaddr";

    internal static bool EnsureOptional(NpgsqlConnection connection, NpgsqlTransaction transaction,
        ShipAddressReference reference, Func<Exception> unavailable)
    {
        var relation = InspectRelation(connection, transaction, unavailable);
        EnsureTypes(connection, transaction, relation, unavailable);
        if (relation is null) return false;

        EnsureNativeShape(connection, transaction, relation.Value.Relation, unavailable);
        // Only the reviewed reference column needs SELECT. WHERE false reads no
        // business row and acquires ACCESS SHARE, retained by this transaction.
        using (var probe = new NpgsqlCommand($"SELECT {Column(reference)} FROM {Table} WHERE false;", connection, transaction))
            probe.ExecuteNonQuery();

        // DDL may have raced the initial catalog discovery before ACCESS SHARE.
        // Check the exact object again while the relation lock now prevents its
        // replacement or structural alteration through our transaction commit.
        var locked = InspectRelation(connection, transaction, unavailable);
        if (locked != relation) throw unavailable();
        EnsureTypes(connection, transaction, locked, unavailable);
        EnsureNativeShape(connection, transaction, relation.Value.Relation, unavailable);
        return true;
    }

    internal static bool HasReferences(NpgsqlConnection connection, NpgsqlTransaction transaction,
        ShipAddressReference reference, string no, bool present)
    {
        // Callers pass only their current transaction's successful guard result.
        // Never parse/query the optional relation in the absent branch.
        if (!present) return false;
        using var command = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Table} WHERE lower(btrim({Column(reference)}))=lower(btrim(@no)));", connection, transaction);
        command.Parameters.Add("no", NpgsqlDbType.Varchar).Value = no;
        return command.ExecuteScalar() is true;
    }

    private static string Column(ShipAddressReference reference) => reference switch
    {
        ShipAddressReference.Broker => "broker",
        ShipAddressReference.Forwarder => "forwarderno",
        ShipAddressReference.Taker => "takerno",
        _ => throw new ArgumentOutOfRangeException(nameof(reference))
    };

    private static (long Relation, long RowType)? InspectRelation(NpgsqlConnection connection,
        NpgsqlTransaction transaction, Func<Exception> unavailable)
    {
        // Include every relation kind in name discovery, so a same-fold index,
        // view, shadow schema/case or other occupied name cannot mean absent.
        using var command = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,c.relrowsecurity,c.relforcerowsecurity,
                   c.oid::bigint,c.reltype::bigint,c.relispartition,
                   EXISTS(SELECT 1 FROM pg_catalog.pg_inherits i WHERE i.inhrelid=c.oid OR i.inhparent=c.oid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)=@name
            ORDER BY n.nspname COLLATE "C",c.relname COLLATE "C";
            """, connection, transaction);
        command.Parameters.Add("name", NpgsqlDbType.Text).Value = RelationName;
        using var reader = command.ExecuteReader();
        (long Relation, long RowType)? result = null;
        while (reader.Read())
        {
            if (result is not null || reader.GetString(0)!="YMES" || reader.GetString(1)!=RelationName
                || reader.GetString(2)!="r" || reader.GetBoolean(3) || reader.GetBoolean(4)
                || reader.GetBoolean(7) || reader.GetBoolean(8)) throw unavailable();
            result = (reader.GetInt64(5), reader.GetInt64(6));
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
        command.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { RelationName, "_" + RelationName };
        using var reader = command.ExecuteReader();
        long? rowArray = null; long? arrayOid = null;
        while (reader.Read())
        {
            if (relation is null || reader.GetString(0)!="YMES") throw unavailable();
            if (reader.GetString(1)==RelationName && rowArray is null)
            {
                if (reader.GetString(2)!="c" || reader.GetInt64(3)!=relation.Value.Relation
                    || reader.GetInt64(4)!=0 || reader.GetInt64(6)!=relation.Value.RowType) throw unavailable();
                rowArray = reader.GetInt64(5);
            }
            else if (reader.GetString(1)=="_"+RelationName && arrayOid is null)
            {
                if (reader.GetString(2)!="b" || reader.GetInt64(3)!=0
                    || reader.GetInt64(4)!=relation.Value.RowType) throw unavailable();
                arrayOid = reader.GetInt64(6);
            }
            else throw unavailable();
        }
        if (relation is not null && (rowArray is null || arrayOid is null || rowArray!=arrayOid)) throw unavailable();
    }

    private static void EnsureNativeShape(NpgsqlConnection connection, NpgsqlTransaction transaction,
        long relation, Func<Exception> unavailable)
    {
        var expected = new Dictionary<string,(int Ordinal,string Type,bool NotNull)>(StringComparer.Ordinal)
        {
            ["shipaddr"]=(1,"character varying(255)",true),["customerno"]=(2,"character varying(50)",true),
            ["telno"]=(3,"character varying(25)",false),["contactorname"]=(4,"character varying(25)",false),
            ["shipcustomername"]=(5,"character varying(30)",false),["creator"]=(6,"character varying(10)",false),
            ["createdate"]=(7,"timestamp without time zone",false),["description"]=(8,"character varying(255)",false),
            ["flow"]=(9,"character varying(10)",true),["declarationflag"]=(10,"character varying(1)",true),
            ["overseaflag"]=(11,"character varying(1)",false),["faxno"]=(12,"character varying(25)",false),
            ["email"]=(13,"character varying(255)",false),["shipaddr2"]=(14,"character varying(500)",false),
            ["country"]=(15,"character varying(50)",false),["companyname"]=(16,"character varying(150)",false),
            ["takerno"]=(17,"character varying(50)",false),["forwarderno"]=(18,"character varying(50)",false),
            ["broker"]=(19,"character varying(50)",false),["datastamp"]=(20,"numeric(11,0)",false),
            ["reviser"]=(21,"character varying(10)",false),["revisedate"]=(22,"timestamp without time zone",false)
        };
        using (var command = new NpgsqlCommand("""
            SELECT a.attname,a.attnum::int,format_type(a.atttypid,a.atttypmod),a.attnotnull,
                   a.attidentity::text,a.attgenerated::text,cn.nspname,co.collname,co.collisdeterministic
            FROM pg_catalog.pg_attribute a
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE a.attrelid=CAST(@relation AS oid)
              AND a.attnum>0 AND NOT a.attisdropped ORDER BY a.attnum;
            """, connection, transaction))
        {
            command.Parameters.Add("relation", NpgsqlDbType.Bigint).Value = relation;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                // Match existing ENT read guards: retain unrelated extensions.
                // Global reference scanners still reject new semantic mappings.
                if (!expected.TryGetValue(reader.GetString(0),out var shape)) continue;
                if (shape.Ordinal!=reader.GetInt32(1) || shape.Type!=reader.GetString(2)
                    || shape.NotNull!=reader.GetBoolean(3) || reader.GetString(4)!="" || reader.GetString(5)!="")
                    throw unavailable();
                // Actual parent keys use deterministic pg_catalog.default. A
                // differently collated reference could lower/compare differently
                // and hide a real reference. Other fields/extensions stay free.
                if (reader.GetString(0) is "broker" or "forwarderno" or "takerno")
                {
                    if (reader.IsDBNull(6) || reader.IsDBNull(7) || reader.IsDBNull(8)
                        || reader.GetString(6)!="pg_catalog" || reader.GetString(7)!="default"
                        || !reader.GetBoolean(8)) throw unavailable();
                }
                expected.Remove(reader.GetString(0));
            }
            if (expected.Count!=0) throw unavailable();
        }
        // The native PK identifies an address, not a declaration. Preserve its
        // ordered two-column shape without depending on a generated index name.
        using var key = new NpgsqlCommand("""
            SELECT p.convalidated,p.condeferrable,p.condeferred,
                   ARRAY(SELECT a.attname::text FROM unnest(p.conkey) WITH ORDINALITY k(attnum,ord)
                         JOIN pg_catalog.pg_attribute a ON a.attrelid=p.conrelid AND a.attnum=k.attnum ORDER BY k.ord),
                   i.indisprimary,i.indisunique,i.indisvalid,i.indisready
            FROM pg_catalog.pg_constraint p JOIN pg_catalog.pg_index i ON i.indexrelid=p.conindid
            WHERE p.conrelid=CAST(@relation AS oid) AND p.contype='p';
            """, connection, transaction);
        key.Parameters.Add("relation", NpgsqlDbType.Bigint).Value = relation;
        using var pk = key.ExecuteReader();
        if (!pk.Read() || !pk.GetBoolean(0) || pk.GetBoolean(1) || pk.GetBoolean(2)
            || !pk.GetFieldValue<string[]>(3).SequenceEqual(new[] { "customerno", "shipaddr" },StringComparer.Ordinal)
            || !pk.GetBoolean(4) || !pk.GetBoolean(5) || !pk.GetBoolean(6) || !pk.GetBoolean(7)) throw unavailable();
        if (pk.Read()) throw unavailable();
        // No default/audit/extension write is performed. Defaults and unrelated
        // extra constraints are schema-release checks, not needed to read refs.
    }
}
