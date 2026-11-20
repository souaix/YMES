using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed partial class Et101Repository
{
    private sealed record Shape(string Name, int Ordinal, string Type, bool NotNull);
    private static readonly Shape[] CustomerShape =
    [
        new("customerno", 1, "character varying(50)", true),
        new("customername", 2, "character varying(100)", true),
        new("customersname", 3, "character varying(100)", true),
        new("telno", 4, "character varying(25)", false),
        new("faxno", 5, "character varying(25)", false),
        new("www", 6, "character varying(50)", false),
        new("taxcode", 7, "character varying(25)", false),
        new("address", 8, "character varying(255)", false),
        new("director", 9, "character varying(50)", false),
        new("creator", 10, "character varying(10)", false),
        new("createdate", 11, "timestamp without time zone", false),
        new("description", 12, "character varying(255)", false),
        new("issuestate", 13, "numeric(1,0)", false),
        new("address2", 14, "character varying(255)", false),
        new("country", 15, "character varying(50)", false),
        new("status", 16, "character varying(5)", false),
        new("currency", 17, "character varying(6)", false),
        new("invtypeno", 18, "character varying(1)", false),
        new("invaddr", 19, "character varying(255)", false),
        new("invaddr2", 20, "character varying(255)", false),
        new("invcustno", 21, "character varying(20)", false),
        new("invremark", 22, "character varying(100)", false),
        new("custtype", 23, "character varying(1)", false),
        new("artype", 24, "character varying(1)", false),
        new("freightterms", 25, "character varying(20)", false),
        new("paymentterms", 26, "character varying(20)", false),
        new("acctno", 27, "character varying(50)", false),
        new("deliveryterm", 28, "character varying(50)", false),
        new("forwarderassigned", 29, "character varying(50)", false),
        new("brokerassigned", 30, "character varying(50)", false),
        new("insurancecoverage", 31, "character varying(50)", false),
        new("reviser", 32, "character varying(20)", false),
        new("revisedate", 33, "timestamp without time zone", false),
        new("customerename", 34, "character varying(50)", false),
        new("erpno", 35, "character varying(50)", false),
        new("datastamp", 36, "numeric(11,0)", false),
        new("sap_fiskn", 37, "character varying(50)", false),
        new("xx_name1", 38, "character varying(35)", false),
        new("xx_name2", 39, "character varying(35)", false),
        new("xx_rebarcode", 40, "character varying(1)", false),
        new("xx_supplydata", 41, "character varying(1)", false),
    ];

    private static readonly Shape[] ContactorShape =
    [
        new("contactorname", 1, "character varying(50)", true),
        new("customerno", 2, "character varying(50)", true),
        new("telno", 3, "character varying(25)", false),
        new("faxno", 4, "character varying(25)", false),
        new("email", 5, "character varying(50)", false),
        new("title", 6, "character varying(25)", false),
        new("deptname", 7, "character varying(25)", false),
        new("creator", 8, "character varying(10)", false),
        new("createdate", 9, "timestamp without time zone", false),
        new("description", 10, "character varying(255)", false),
        new("datastamp", 11, "numeric(11,0)", false),
        new("reviser", 12, "character varying(10)", false),
        new("revisedate", 13, "timestamp without time zone", false),
    ];

    private static readonly Shape[] AddressShape =
    [
        new("shipaddr", 1, "character varying(255)", true),
        new("customerno", 2, "character varying(50)", true),
        new("telno", 3, "character varying(25)", false),
        new("contactorname", 4, "character varying(25)", false),
        new("shipcustomername", 5, "character varying(30)", false),
        new("creator", 6, "character varying(10)", false),
        new("createdate", 7, "timestamp without time zone", false),
        new("description", 8, "character varying(255)", false),
        new("flow", 9, "character varying(10)", true),
        new("declarationflag", 10, "character varying(1)", true),
        new("overseaflag", 11, "character varying(1)", false),
        new("faxno", 12, "character varying(25)", false),
        new("email", 13, "character varying(255)", false),
        new("shipaddr2", 14, "character varying(500)", false),
        new("country", 15, "character varying(50)", false),
        new("companyname", 16, "character varying(150)", false),
        new("takerno", 17, "character varying(50)", false),
        new("forwarderno", 18, "character varying(50)", false),
        new("broker", 19, "character varying(50)", false),
        new("datastamp", 20, "numeric(11,0)", false),
        new("reviser", 21, "character varying(10)", false),
        new("revisedate", 22, "timestamp without time zone", false),
    ];

    private static void EnsureOwn(NpgsqlConnection c, NpgsqlTransaction t)
    {
        var oid = Relation(c, t, "tblentcustomerbasis");
        using (var probe = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE false;", c, t))
            probe.ExecuteNonQuery();
        if (Relation(c, t, "tblentcustomerbasis") != oid) throw Unavailable();
        CheckShape(c, t, oid, CustomerShape);
        CheckPrimaryKey(c, t, oid, ["customerno"]);
        // Runtime does not rely on defaults: every one of the41 INSERT fields
        // is explicit. Default reconciliation is a separate Root migration.
    }

    private static void EnsureReferences(NpgsqlConnection c, NpgsqlTransaction t)
    {
        var ids = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var field in ReferenceColumns)
        {
            var oid = Relation(c, t, field.Table);
            using (var probe = new NpgsqlCommand($"SELECT {field.Column} FROM \"YMES\".{field.Table} WHERE false;", c, t))
                probe.ExecuteNonQuery();
            if (Relation(c, t, field.Table) != oid) throw Unavailable();
            ids.Add(field.Table, oid);
        }
        Registry(c, t);
        CheckShape(c, t, ids["tblentcustomercontactor"], ContactorShape);
        CheckPrimaryKey(c, t, ids["tblentcustomercontactor"], ["customerno", "contactorname"]);
        CheckShape(c, t, ids["tblentcustomershipaddr"], AddressShape);
        CheckPrimaryKey(c, t, ids["tblentcustomershipaddr"], ["customerno", "shipaddr"]);
        // Validate after ACCESS SHARE probes; no unreviewed relation is read.
        foreach (var field in ReferenceColumns)
            CheckNeeded(c, t, ids[field.Table], [(field.Column, field.Type, field.NotNull)]);
        Registry(c, t);
    }

    private static void EnsureAssignedParent(NpgsqlConnection c, NpgsqlTransaction t, AssignedParent parent)
    {
        var oid = Relation(c, t, parent.Table);
        var fields = new List<(string Name, string Type, bool NotNull)>
        {
            (parent.Key, "character varying(50)", true),
            ("contactorname", "character varying(50)", false),
            ("issuestate", "numeric(1,0)", false)
        };
        if (parent == Forwarder) fields.Add(("brokerno", "character varying(50)", true));
        using (var probe = new NpgsqlCommand($"SELECT {string.Join(',', fields.Select(x => x.Name))} FROM \"YMES\".{parent.Table} WHERE false;", c, t))
            probe.ExecuteNonQuery();
        if (Relation(c, t, parent.Table) != oid) throw Unavailable();
        CheckNeeded(c, t, oid, fields);
        CheckPrimaryKey(c, t, oid, [parent.Key]);
    }

    private static long Relation(NpgsqlConnection c, NpgsqlTransaction t, string name)
    {
        using var cmd = new NpgsqlCommand("""
            SELECT c.oid::bigint,n.nspname,c.relname,c.relkind::text,c.relpersistence::text,
              pg_get_userbyid(c.relowner),c.relrowsecurity,c.relforcerowsecurity,c.relispartition,
              EXISTS(SELECT 1 FROM pg_catalog.pg_inherits i WHERE i.inhrelid=c.oid OR i.inhparent=c.oid),
              EXISTS(SELECT 1 FROM pg_catalog.pg_trigger g WHERE g.tgrelid=c.oid),
              EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k WHERE k.contype='f'
                AND (k.conrelid=c.oid OR k.confrelid=c.oid))
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND n.nspname !~ '^pg_(toast|temp)_'
              AND lower(c.relname)=@name ORDER BY n.nspname COLLATE "C",c.relname COLLATE "C";
            """, c, t);
        Text(cmd, "name", name); using var r = cmd.ExecuteReader();
        var writes = name is "tblentcustomerbasis" or "tblentcustomershipaddr" or "tblentcustomercontactor";
        if (!r.Read() || r.GetString(1) != "YMES" || r.GetString(2) != name || r.GetString(3) != "r"
            || r.GetString(4) != "p" || r.GetString(5) != "postgres" || r.GetBoolean(6) || r.GetBoolean(7)
            || r.GetBoolean(8) || r.GetBoolean(9) || (writes && (r.GetBoolean(10) || r.GetBoolean(11)))) throw Unavailable();
        var oid = r.GetInt64(0); if (r.Read()) throw Unavailable(); return oid;
    }

    private static void Registry(NpgsqlConnection c, NpgsqlTransaction t)
    {
        var remaining = ReferenceColumns.ToDictionary(x => x.Table, StringComparer.Ordinal);
        using var cmd = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,c.relrowsecurity,c.relforcerowsecurity,
              a.attname,format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attidentity::text,a.attgenerated::text,
              cn.nspname,co.collname,co.collisdeterministic
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND n.nspname !~ '^pg_(toast|temp)_'
              AND c.relkind IN ('r','p','v','m','f') AND lower(a.attname)='customerno'
            ORDER BY n.nspname COLLATE "C",c.relname COLLATE "C",a.attnum;
            """, c, t);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (!remaining.Remove(r.GetString(1), out var field) || r.GetString(0) != "YMES" || r.GetString(2) != "r"
                || r.GetBoolean(3) || r.GetBoolean(4) || r.GetString(5) != field.Column || r.GetString(6) != field.Type
                || r.GetBoolean(7) != field.NotNull || r.GetString(8) != "" || r.GetString(9) != ""
                || r.IsDBNull(10) || r.GetString(10) != "pg_catalog" || r.IsDBNull(11) || r.GetString(11) != "default"
                || r.IsDBNull(12) || !r.GetBoolean(12)) throw Unavailable();
        }
        if (remaining.Count != 0) throw Unavailable();
    }

    private static void CheckShape(NpgsqlConnection c, NpgsqlTransaction t, long oid, Shape[] expected)
    {
        using var cmd = new NpgsqlCommand("""
            SELECT a.attname,a.attnum::int,format_type(a.atttypid,a.atttypmod),a.attnotnull,
              a.attisdropped,a.attidentity::text,a.attgenerated::text,cn.nspname,co.collname,co.collisdeterministic
            FROM pg_catalog.pg_attribute a
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE a.attrelid=CAST(@oid AS oid) AND a.attnum>0 ORDER BY a.attnum;
            """, c, t);
        Oid(cmd, oid); var i = 0; using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (i >= expected.Length) throw Unavailable();
            var field = expected[i++];
            if (r.GetString(0) != field.Name || r.GetInt32(1) != field.Ordinal || r.GetString(2) != field.Type
                || r.GetBoolean(3) != field.NotNull || r.GetBoolean(4) || r.GetString(5) != "" || r.GetString(6) != "")
                throw Unavailable();
            if (field.Type.StartsWith("character varying", StringComparison.Ordinal)
                && (r.IsDBNull(7) || r.GetString(7) != "pg_catalog" || r.IsDBNull(8) || r.GetString(8) != "default"
                    || r.IsDBNull(9) || !r.GetBoolean(9))) throw Unavailable();
        }
        if (i != expected.Length) throw Unavailable();
    }

    private static void CheckNeeded(NpgsqlConnection c, NpgsqlTransaction t, long oid,
        IEnumerable<(string Name, string Type, bool NotNull)> fields)
    {
        var remaining = fields.ToDictionary(x => x.Name, StringComparer.Ordinal);
        using var cmd = new NpgsqlCommand("""
            SELECT a.attname,format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attidentity::text,a.attgenerated::text,
              cn.nspname,co.collname,co.collisdeterministic
            FROM pg_catalog.pg_attribute a
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE a.attrelid=CAST(@oid AS oid) AND a.attnum>0 AND NOT a.attisdropped AND a.attname=ANY(@names);
            """, c, t);
        Oid(cmd, oid); cmd.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = remaining.Keys.ToArray();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (!remaining.Remove(r.GetString(0), out var field) || r.GetString(1) != field.Type
                || r.GetBoolean(2) != field.NotNull || r.GetString(3) != "" || r.GetString(4) != "") throw Unavailable();
            if (field.Type.StartsWith("character varying", StringComparison.Ordinal)
                && (r.IsDBNull(5) || r.GetString(5) != "pg_catalog" || r.IsDBNull(6) || r.GetString(6) != "default"
                    || r.IsDBNull(7) || !r.GetBoolean(7))) throw Unavailable();
        }
        if (remaining.Count != 0) throw Unavailable();
    }

    private static void CheckPrimaryKey(NpgsqlConnection c, NpgsqlTransaction t, long oid, string[] keys)
    {
        using var cmd = new NpgsqlCommand("""
            SELECT k.convalidated,k.condeferrable,k.condeferred,
              ARRAY(SELECT a.attname::text FROM unnest(k.conkey) WITH ORDINALITY u(attnum,ord)
                JOIN pg_catalog.pg_attribute a ON a.attrelid=k.conrelid AND a.attnum=u.attnum ORDER BY u.ord),
              i.indisvalid,i.indisready,i.indisunique,i.indisprimary
            FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_index i ON i.indexrelid=k.conindid
            WHERE k.conrelid=CAST(@oid AS oid) AND k.contype='p';
            """, c, t);
        Oid(cmd, oid); using var r = cmd.ExecuteReader();
        if (!r.Read() || !r.GetBoolean(0) || r.GetBoolean(1) || r.GetBoolean(2)
            || !r.GetFieldValue<string[]>(3).SequenceEqual(keys, StringComparer.Ordinal)
            || !r.GetBoolean(4) || !r.GetBoolean(5) || !r.GetBoolean(6) || !r.GetBoolean(7) || r.Read())
            throw Unavailable();
    }
    private static void Oid(NpgsqlCommand cmd, long oid) =>
        cmd.Parameters.Add("oid", NpgsqlDbType.Bigint).Value = oid;
}

