using Npgsql;
using NpgsqlTypes;
using YMes.Api.Enterprise;

namespace YMes.Api.Wip;

// Candidate only. Metadata/zero-row SELECT/locks/statement preparation; no
// business read, DDL, grant, fixture or execution of the prepared EventLog INSERT.
internal static class Wp109WriteSchema
{
    private sealed record Column(string Name, string Type, bool NotNull, bool ZeroDefault = false);
    private readonly record struct Relation(long Id, long RowType);
    private static Exception Unavailable() => new Wp109WriteRuleException(503, "必要來源結構或權限尚未就緒。");
    private static string Table(string name) => "\"YMES\"." + name;
    private const string EventInsertSql = """
        INSERT INTO "YMES".tblsyseventlog
            (userid,recorddate,servicename,description,keyfield,keyvalue,computername)
        VALUES (@actor,@at,'wsWIP',@description,'SWRNo',@no,NULL);
        """;

    internal static void EnsureGroup(NpgsqlConnection c, NpgsqlTransaction t, bool write) => Ready(() =>
    {
        if (write)
        {
            // Reject a wrong-kind view before LOCK can recursively lock its
            // outside base relations. These read guards retain ACCESS SHARE;
            // recheck below after stronger locks close trigger/FK DDL races.
            if (!SwrReferenceGuard.EnsureOptional(c, t, SwrReference.Customer, Unavailable)) throw Unavailable();
            Wp109DetailReadGuard.Ensure(c, t, Unavailable);
            Lock(c, t, "tblwipswrbasis", "ROW EXCLUSIVE");
            Lock(c, t, "tblwipswrdetail", "SHARE ROW EXCLUSIVE");
        }
        if (!SwrReferenceGuard.EnsureOptional(c, t, SwrReference.Customer, Unavailable)) throw Unavailable();
        Wp109DetailReadGuard.Ensure(c, t, Unavailable);
        EnsureSwrRegistry(c, t);
        if (write)
        {
            // Read guards already verify physical columns/PK/types/no FK or
            // triggers. INSERT additionally relies on the native no-default,
            // no-extra-constraint shape rather than a read-only compatibility.
            NoWriteExtensions(c, t, "tblwipswrbasis");
            NoWriteExtensions(c, t, "tblwipswrdetail");
            Privileges(c, t, "tblwipswrbasis", "SELECT", "INSERT", "UPDATE", "DELETE");
            Privileges(c, t, "tblwipswrdetail", "SELECT", "DELETE");
        }
    });

    internal static void EnsureSerial(NpgsqlConnection c, NpgsqlTransaction t, bool write) => Ready(() =>
    {
        var columns = new[]
        {
            new Column("functionname", "character varying(20)", true),
            new Column("serialno", "numeric(9,0)", true)
        };
        var key = new[] { "functionname" };
        EnsureNative(c, t, "tblsysserialno", columns, key, "functionname,serialno");
        if (write)
        {
            Lock(c, t, "tblsysserialno", "ROW EXCLUSIVE");
            EnsureNative(c, t, "tblsysserialno", columns, key, "functionname,serialno");
        }
        Privileges(c, t, "tblsysserialno", write ? new[] { "SELECT", "INSERT", "UPDATE" } : new[] { "SELECT" });
    });

    internal static void EnsureParameter(NpgsqlConnection c, NpgsqlTransaction t) => Ready(() =>
        EnsureNative(c, t, "tblsysparameter", new[]
        {
            new Column("parametertype", "character varying(50)", true),
            new Column("parameterno", "character varying(50)", true),
            new Column("parametervalue", "character varying(255)", true),
            new Column("parametervisable", "numeric(1,0)", true, true),
            new Column("parametercategory", "character varying(50)", false),
            new Column("categoryorder", "numeric(3,0)", false),
            new Column("parameterorder", "numeric(3,0)", false, true),
            new Column("parameterdatatype", "numeric(1,0)", false),
            new Column("parameterinputtype", "numeric(1,0)", false),
            new Column("encryptionflag", "numeric(1,0)", false, true),
            new Column("validlist", "character varying(2000)", false),
            new Column("activemodule", "character varying(50)", false),
            new Column("ic_ingot", "numeric(1,0)", false, true),
            new Column("epi_wafer", "numeric(1,0)", false, true),
            new Column("semi_wafer", "numeric(1,0)", false, true),
            new Column("ic_cp_pkg", "numeric(1,0)", false, true),
            new Column("ic_ft_pkg", "numeric(1,0)", false, true),
            new Column("discrete_pkg", "numeric(1,0)", false, true),
            new Column("led_pkg", "numeric(1,0)", false, true),
            new Column("ic_smt", "numeric(1,0)", false, true),
            new Column("creator", "character varying(10)", false),
            new Column("createdate", "timestamp without time zone", false),
            new Column("reviser", "character varying(10)", false),
            new Column("revisedate", "timestamp without time zone", false),
            new Column("datastamp", "numeric(11,0)", false, true)
        }, new[] { "parameterno" }, "parameterno,parametervalue"));

    internal static void EnsureParentKeys(NpgsqlConnection c, NpgsqlTransaction t) => Ready(() =>
    {
        // Parent tables are read only here. Their other columns, defaults,
        // indexes and legitimate RI/audit triggers are outside this projection.
        EnsureParent(c, t, "tblentcustomerbasis", "customerno", "character varying(50)");
        EnsureParent(c, t, "tblopbasis", "opno", "character varying(20)");
    });

    internal static NpgsqlCommand PrepareEventInsert(NpgsqlConnection c, NpgsqlTransaction t)
    {
        NpgsqlCommand? insert = null;
        try
        {
            var before = EventShape(c, t);
            Privileges(c, t, "tblsyseventlog", "INSERT");
            // PostgreSQL reuses a named Npgsql prepared statement on the same
            // physical connection; preparing that cached statement again does
            // not reacquire its relation lock for the new transaction. Explain
            // the exact INSERT on each transaction to obtain RowExclusiveLock
            // during planning, without executing DML or firing triggers.
            using (var plan = new NpgsqlCommand("EXPLAIN (COSTS OFF)\n" + EventInsertSql, c, t))
            {
                EventParameters(plan);
                plan.ExecuteNonQuery();
            }
            insert = new NpgsqlCommand(EventInsertSql, c, t);
            EventParameters(insert);
            // Keep the actual parameterized INSERT prepared for the later write.
            insert.Prepare();
            using (var held = new NpgsqlCommand("""
                SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_locks l
                    WHERE l.locktype='relation' AND l.pid=pg_backend_pid()
                    AND l.database=(SELECT d.oid FROM pg_catalog.pg_database d WHERE d.datname=current_database())
                    AND l.relation=CAST(@id AS oid) AND l.mode='RowExclusiveLock' AND l.granted);
                """, c, t))
            {
                Number(held, "id", before.Id);
                if (held.ExecuteScalar() is not true) throw Unavailable();
            }
            if (EventShape(c, t) != before) throw Unavailable();
            return insert;
        }
        catch (NpgsqlException)
        {
            insert?.Dispose();
            throw Unavailable();
        }
        catch
        {
            insert?.Dispose();
            throw;
        }
    }

    private static Relation EventShape(NpgsqlConnection c, NpgsqlTransaction t)
    {
        var relation = Inspect(c, t, "tblsyseventlog", forbidEffects: true);
        Types(c, t, "tblsyseventlog", relation);
        Columns(c, t, relation, new[]
        {
            new Column("userid", "character varying(10)", true),
            new Column("recorddate", "timestamp(0) without time zone", true),
            new Column("servicename", "character varying(30)", true),
            new Column("description", "text", false),
            new Column("keyfield", "character varying(1000)", false),
            new Column("keyvalue", "character varying(1000)", false),
            new Column("computername", "character varying(50)", false)
        });
        using var command = new NpgsqlCommand("""
            SELECT (SELECT count(*) FROM pg_catalog.pg_constraint k WHERE k.conrelid=CAST(@id AS oid)),
                ARRAY(SELECT a.attname::text FROM unnest(i.indkey) WITH ORDINALITY x(attnum,ord)
                    JOIN pg_catalog.pg_attribute a ON a.attrelid=i.indrelid AND a.attnum=x.attnum ORDER BY x.ord),
                i.indisprimary,i.indisunique,i.indisvalid,i.indisready,i.indislive,
                i.indnkeyatts::int,i.indnatts::int,i.indpred IS NULL,i.indexprs IS NULL,
                am.amname,i.indoption::text
            FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid
            JOIN pg_catalog.pg_am am ON am.oid=ic.relam WHERE i.indrelid=CAST(@id AS oid);
            """, c, t);
        Number(command, "id", relation.Id);
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetInt64(0) != 0
            || !reader.GetFieldValue<string[]>(1).SequenceEqual(new[] { "recorddate" }, StringComparer.Ordinal)
            || reader.GetBoolean(2) || reader.GetBoolean(3) || !reader.GetBoolean(4) || !reader.GetBoolean(5)
            || !reader.GetBoolean(6) || reader.GetInt32(7) != 1 || reader.GetInt32(8) != 1
            || !reader.GetBoolean(9) || !reader.GetBoolean(10) || reader.GetString(11) != "btree"
            || reader.GetString(12) != "0" || reader.Read()) throw Unavailable();
        return relation;
    }

    private static void EnsureNative(NpgsqlConnection c, NpgsqlTransaction t, string name,
        Column[] columns, string[] primaryKey, string projection)
    {
        var before = Inspect(c, t, name, forbidEffects: true);
        Types(c, t, name, before); Columns(c, t, before, columns); PrimaryKey(c, t, before, primaryKey, strict: true);
        Probe(c, t, name, projection);
        var locked = Inspect(c, t, name, forbidEffects: true);
        if (locked != before) throw Unavailable();
        Types(c, t, name, locked); Columns(c, t, locked, columns); PrimaryKey(c, t, locked, primaryKey, strict: true);
    }

    private static void EnsureParent(NpgsqlConnection c, NpgsqlTransaction t, string name, string key, string type)
    {
        var before = Inspect(c, t, name, forbidEffects: false);
        Types(c, t, name, before); ParentColumn(c, t, before, key, type); PrimaryKey(c, t, before, new[] { key }, strict: false);
        Probe(c, t, name, key);
        var locked = Inspect(c, t, name, forbidEffects: false);
        if (locked != before) throw Unavailable();
        Types(c, t, name, locked); ParentColumn(c, t, locked, key, type); PrimaryKey(c, t, locked, new[] { key }, strict: false);
    }

    private static Relation Inspect(NpgsqlConnection c, NpgsqlTransaction t, string name, bool forbidEffects)
    {
        using var command = new NpgsqlCommand("""
            SELECT n.nspname,r.relname,r.relkind::text,r.relpersistence::text,pg_get_userbyid(r.relowner),
                r.relrowsecurity,r.relforcerowsecurity,r.relispartition,r.relhasrules,r.oid::bigint,r.reltype::bigint,
                EXISTS(SELECT 1 FROM pg_catalog.pg_inherits x WHERE x.inhrelid=r.oid OR x.inhparent=r.oid),
                EXISTS(SELECT 1 FROM pg_catalog.pg_trigger g WHERE g.tgrelid=r.oid),
                EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k WHERE k.contype='f' AND (k.conrelid=r.oid OR k.confrelid=r.oid))
            FROM pg_catalog.pg_class r JOIN pg_catalog.pg_namespace n ON n.oid=r.relnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(r.relname)=@name
            ORDER BY n.nspname COLLATE "C",r.relname COLLATE "C";
            """, c, t);
        Text(command, "name", name);
        using var reader = command.ExecuteReader();
        Relation? result = null;
        while (reader.Read())
        {
            if (result is not null || reader.GetString(0) != "YMES" || reader.GetString(1) != name
                || reader.GetString(2) != "r" || reader.GetString(3) != "p" || reader.GetString(4) != "postgres"
                || reader.GetBoolean(5) || reader.GetBoolean(6) || reader.GetBoolean(7) || reader.GetBoolean(8)
                || reader.GetBoolean(11) || (forbidEffects && (reader.GetBoolean(12) || reader.GetBoolean(13)))) throw Unavailable();
            result = new Relation(reader.GetInt64(9), reader.GetInt64(10));
        }
        return result ?? throw Unavailable();
    }

    private static void Types(NpgsqlConnection c, NpgsqlTransaction t, string name, Relation relation)
    {
        using var command = new NpgsqlCommand("""
            SELECT n.nspname,y.typname,y.typtype::text,y.typrelid::bigint,y.typelem::bigint,
                y.typarray::bigint,y.oid::bigint,pg_get_userbyid(y.typowner),y.typisdefined
            FROM pg_catalog.pg_type y JOIN pg_catalog.pg_namespace n ON n.oid=y.typnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(y.typname)=ANY(@names)
            ORDER BY n.nspname COLLATE "C",y.typname COLLATE "C";
            """, c, t);
        command.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { name, "_" + name };
        using var reader = command.ExecuteReader();
        long? rowArray = null; long? array = null;
        while (reader.Read())
        {
            if (reader.GetString(0) != "YMES" || reader.GetString(7) != "postgres" || !reader.GetBoolean(8)) throw Unavailable();
            if (reader.GetString(1) == name && rowArray is null)
            {
                if (reader.GetString(2) != "c" || reader.GetInt64(3) != relation.Id || reader.GetInt64(4) != 0
                    || reader.GetInt64(6) != relation.RowType) throw Unavailable();
                rowArray = reader.GetInt64(5);
            }
            else if (reader.GetString(1) == "_" + name && array is null)
            {
                if (reader.GetString(2) != "b" || reader.GetInt64(3) != 0 || reader.GetInt64(4) != relation.RowType) throw Unavailable();
                array = reader.GetInt64(6);
            }
            else throw Unavailable();
        }
        if (rowArray is null || array is null || rowArray != array) throw Unavailable();
    }

    private static void Columns(NpgsqlConnection c, NpgsqlTransaction t, Relation relation, Column[] columns)
    {
        using var command = ColumnCommand(c, t, relation, null);
        using var reader = command.ExecuteReader();
        var ordinal = 0;
        while (reader.Read())
        {
            if (ordinal >= columns.Length || reader.GetInt32(1) != ordinal + 1) throw Unavailable();
            CheckColumn(reader, columns[ordinal++]);
        }
        if (ordinal != columns.Length) throw Unavailable();
    }

    private static void ParentColumn(NpgsqlConnection c, NpgsqlTransaction t, Relation relation, string key, string type)
    {
        using var command = ColumnCommand(c, t, relation, key);
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetBoolean(6) || reader.GetString(0) != key || reader.GetString(2) != type
            || !reader.GetBoolean(3) || reader.GetString(4) != "" || reader.GetString(5) != "") throw Unavailable();
        Collation(reader);
        if (reader.Read()) throw Unavailable();
    }

    private static NpgsqlCommand ColumnCommand(NpgsqlConnection c, NpgsqlTransaction t, Relation relation, string? key)
    {
        var command = new NpgsqlCommand("""
            SELECT a.attname,a.attnum::int,format_type(a.atttypid,a.atttypmod),a.attnotnull,
                a.attidentity::text,a.attgenerated::text,a.attisdropped,a.atthasdef,pg_get_expr(d.adbin,d.adrelid),
                cn.nspname,co.collname,co.collisdeterministic
            FROM pg_catalog.pg_attribute a LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE a.attrelid=CAST(@id AS oid) AND a.attnum>0 AND (@key IS NULL OR a.attname=@key) ORDER BY a.attnum;
            """, c, t);
        Number(command, "id", relation.Id);
        command.Parameters.Add("key", NpgsqlDbType.Text).Value = (object?)key ?? DBNull.Value;
        return command;
    }

    private static void CheckColumn(NpgsqlDataReader reader, Column expected)
    {
        if (reader.GetBoolean(6) || reader.GetString(0) != expected.Name || reader.GetString(2) != expected.Type
            || reader.GetBoolean(3) != expected.NotNull || reader.GetString(4) != "" || reader.GetString(5) != ""
            || reader.GetBoolean(7) != expected.ZeroDefault) throw Unavailable();
        if (expected.ZeroDefault && (reader.IsDBNull(8) || reader.GetString(8) is not ("0" or "0::numeric" or "'0'::numeric" or "(0)::numeric"))) throw Unavailable();
        if (expected.Type.StartsWith("character varying", StringComparison.Ordinal) || expected.Type == "text") Collation(reader);
    }

    private static void Collation(NpgsqlDataReader reader)
    {
        if (reader.IsDBNull(9) || reader.IsDBNull(10) || reader.IsDBNull(11) || reader.GetString(9) != "pg_catalog"
            || reader.GetString(10) != "default" || !reader.GetBoolean(11)) throw Unavailable();
    }

    private static void PrimaryKey(NpgsqlConnection c, NpgsqlTransaction t, Relation relation, string[] expected, bool strict)
    {
        using var command = new NpgsqlCommand("""
            SELECT k.convalidated,k.condeferrable,k.condeferred,
                ARRAY(SELECT a.attname::text FROM unnest(k.conkey) WITH ORDINALITY x(attnum,ord)
                    JOIN pg_catalog.pg_attribute a ON a.attrelid=k.conrelid AND a.attnum=x.attnum ORDER BY x.ord),
                i.indisprimary,i.indisunique,i.indisvalid,i.indisready,i.indislive,i.indnkeyatts::int,i.indnatts::int,
                i.indpred IS NULL,i.indexprs IS NULL,am.amname,
                (SELECT count(*) FROM pg_catalog.pg_index ix WHERE ix.indrelid=k.conrelid),
                (SELECT count(*) FROM pg_catalog.pg_constraint cx WHERE cx.conrelid=k.conrelid)
            FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_index i ON i.indexrelid=k.conindid
            JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid JOIN pg_catalog.pg_am am ON am.oid=ic.relam
            WHERE k.conrelid=CAST(@id AS oid) AND k.contype='p';
            """, c, t);
        Number(command, "id", relation.Id);
        using var reader = command.ExecuteReader();
        if (!reader.Read() || !reader.GetBoolean(0) || reader.GetBoolean(1) || reader.GetBoolean(2)
            || !reader.GetFieldValue<string[]>(3).SequenceEqual(expected, StringComparer.Ordinal)
            || !reader.GetBoolean(4) || !reader.GetBoolean(5) || !reader.GetBoolean(6) || !reader.GetBoolean(7) || !reader.GetBoolean(8)
            || reader.GetInt32(9) != expected.Length || reader.GetInt32(10) != expected.Length
            || !reader.GetBoolean(11) || !reader.GetBoolean(12) || reader.GetString(13) != "btree"
            || (strict && (reader.GetInt64(14) != 1 || reader.GetInt64(15) != 1)) || reader.Read()) throw Unavailable();
    }

    private static void EnsureSwrRegistry(NpgsqlConnection c, NpgsqlTransaction t)
    {
        using var command = new NpgsqlCommand("""
            SELECT n.nspname,r.relname,a.attname FROM pg_catalog.pg_attribute a
            JOIN pg_catalog.pg_class r ON r.oid=a.attrelid JOIN pg_catalog.pg_namespace n ON n.oid=r.relnamespace
            WHERE a.attnum>0 AND NOT a.attisdropped AND lower(a.attname)='swrno'
                AND r.relkind IN ('r','p','v','m','f') AND n.nspname NOT IN ('pg_catalog','information_schema','pg_toast')
                AND n.nspname !~ '^pg_(toast|temp)_';
            """, c, t);
        using var reader = command.ExecuteReader();
        var expected = new HashSet<(string, string, string)>
        { ("YMES", "tblwipswrbasis", "swrno"), ("YMES", "tblwipswrdetail", "swrno") };
        while (reader.Read())
            if (!expected.Remove((reader.GetString(0), reader.GetString(1), reader.GetString(2)))) throw Unavailable();
        if (expected.Count != 0) throw Unavailable();
    }

    private static void NoWriteExtensions(NpgsqlConnection c, NpgsqlTransaction t, string name)
    {
        using var command = new NpgsqlCommand("""
            SELECT NOT EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a WHERE a.attrelid=CAST(@table AS regclass)
                AND a.attnum>0 AND a.atthasdef)
                AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k WHERE k.conrelid=CAST(@table AS regclass) AND k.contype<>'p');
            """, c, t);
        Text(command, "table", Table(name));
        if (command.ExecuteScalar() is not true) throw Unavailable();
    }

    private static void Privileges(NpgsqlConnection c, NpgsqlTransaction t, string name, params string[] privileges)
    {
        using var command = new NpgsqlCommand("""
            SELECT NOT EXISTS(SELECT 1 FROM unnest(@privileges) AS wanted(privilege)
                WHERE NOT has_table_privilege('ymes_app',@table,wanted.privilege));
            """, c, t);
        Text(command, "table", Table(name));
        command.Parameters.Add("privileges", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = privileges;
        if (command.ExecuteScalar() is not true) throw Unavailable();
    }

    private static void Probe(NpgsqlConnection c, NpgsqlTransaction t, string name, string projection)
    {
        using var probe = new NpgsqlCommand($"SELECT {projection} FROM {Table(name)} WHERE false;", c, t);
        probe.ExecuteNonQuery();
    }

    private static void Lock(NpgsqlConnection c, NpgsqlTransaction t, string name, string mode)
    {
        using var command = new NpgsqlCommand($"LOCK TABLE {Table(name)} IN {mode} MODE;", c, t);
        command.ExecuteNonQuery();
    }

    private static void Ready(Action action)
    {
        try { action(); }
        catch (NpgsqlException) { throw Unavailable(); }
    }

    private static void Text(NpgsqlCommand command, string name, string value) => command.Parameters.Add(name, NpgsqlDbType.Text).Value = value;
    private static void Number(NpgsqlCommand command, string name, long value) => command.Parameters.Add(name, NpgsqlDbType.Bigint).Value = value;

    private static void EventParameters(NpgsqlCommand command)
    {
        command.Parameters.Add("actor", NpgsqlDbType.Varchar).Value = DBNull.Value;
        command.Parameters.Add("at", NpgsqlDbType.Timestamp).Value = DBNull.Value;
        command.Parameters.Add("description", NpgsqlDbType.Text).Value = DBNull.Value;
        command.Parameters.Add("no", NpgsqlDbType.Varchar).Value = DBNull.Value;
    }
}
