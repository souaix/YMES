using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed partial class Et106Repository(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource source = dataSource;
    private const decimal MaxStamp = 99_999_999_999m;
    private const string Addresses = "\"YMES\".tblentcustomershipaddr";
    private const string Declarations = "\"YMES\".tblentcustomershipdeclaration";
    private const string AddressColumns = "shipaddr,customerno,telno,contactorname,shipcustomername,creator,createdate,description,flow,declarationflag,overseaflag,faxno,email,shipaddr2,country,companyname,takerno,forwarderno,broker,datastamp,reviser,revisedate";
    private const string DeclarationColumns = "customerno,shipto,flow,overseaflag,declarationflag,bondflag,handcarryflag,declarationway,creator,createdate,datastamp,reviser,revisedate";
    private const string AddressKeySql = "customerno=@customer AND shipaddr=@address";
    private const string GroupKeySql = "customerno=@customer AND shipto=@shipto AND flow=@flow AND overseaflag=@oversea AND declarationflag=@declaration AND bondflag=@bond AND handcarryflag=@hand";
    private sealed record Parent(string No, int? State, string? Name, string? ShortName);
    private sealed record ParentSpec(string Table, string Key, int Length, string Namespace, bool Customer = false);
    private static readonly ParentSpec CustomerSpec = new("tblentcustomerbasis", "customerno", 50, "Customer", true);
    private static readonly ParentSpec BrokerSpec = new("tblentbrokerbasis", "brokerno", 50, "Broker");
    private static readonly ParentSpec ForwarderSpec = new("tblentforwarderbasis", "forwarderno", 50, "Forwarder");
    private static readonly ParentSpec TakerSpec = new("tblenttakerbasis", "takerno", 50, "Taker");

    public IReadOnlyList<Et106CustomerOption> Customers()
    {
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureParent(c, t, CustomerSpec);
        using var cmd = new NpgsqlCommand("""
            SELECT customerno,customername,customersname,issuestate::int,
             count(*) OVER(PARTITION BY lower(btrim(customerno)))
            FROM "YMES".tblentcustomerbasis
            ORDER BY lower(btrim(customerno)) COLLATE "C",customerno COLLATE "C";
            """, c, t);
        var rows = new List<Et106CustomerOption>();
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                if (r.GetInt64(4) > 1) throw Rule(409, "客戶編號有大小寫／空白歧義，不能任選來源。");
                if (!r.IsDBNull(3) && r.GetInt32(3) == 2)
                    rows.Add(new(r.GetString(0), Str(r, 1), Str(r, 2), 2));
            }
        t.Commit(); return rows;
    }

    private static string Owner(NpgsqlConnection c, NpgsqlTransaction t, string raw, bool create)
    {
        Raw(raw, 50, "CustomerNo");
        if (create) Required(raw, 50, "CustomerNo");
        AcquireLogical(c, t, CustomerSpec, [raw]);
        EnsureParent(c, t, CustomerSpec);
        var parent = ReadParent(c, t, CustomerSpec, raw);
        if (create)
        {
            if (parent is null || parent.State != 2) throw Rule(422, "新增需要唯一且已簽核的真實客戶。");
            return parent.No;
        }
        // An inactive/missing parent row does not rewrite or disable existing
        // source owner identities. The real parent provider must still exist.
        return raw;
    }

    private static Parent? ReadParent(NpgsqlConnection c, NpgsqlTransaction t, ParentSpec spec, string key)
    {
        var labels = spec.Customer ? ",customername,customersname" : ",contactorname,NULL::varchar";
        using var cmd = new NpgsqlCommand($"SELECT {spec.Key},issuestate::int{labels} FROM \"YMES\".{spec.Table} WHERE lower(btrim({spec.Key}))=lower(btrim(@key)) ORDER BY {spec.Key} COLLATE \"C\" FOR SHARE;", c, t);
        Text(cmd, "key", key); using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var row = new Parent(r.GetString(0), r.IsDBNull(1) ? null : r.GetInt32(1), Str(r, 2), Str(r, 3));
        if (r.Read()) throw Rule(409, "參照編號存在大小寫／空白歧義，資料未變更。");
        return row;
    }

    private static void AcquireLogical(NpgsqlConnection c, NpgsqlTransaction t, ParentSpec spec, IEnumerable<string?> inputs)
    {
        var values = inputs.Where(x => x is not null).Cast<string>().ToArray();
        if (values.Length == 0) return;
        // Database normalization and C order, never .NET culture/case folding.
        using var sort = new NpgsqlCommand("SELECT DISTINCT lower(btrim(v)) COLLATE \"C\" AS normalized FROM unnest(@keys) v ORDER BY normalized;", c, t);
        sort.Parameters.Add("keys", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = values;
        var keys = new List<string>(); using (var r = sort.ExecuteReader()) while (r.Read()) keys.Add(r.GetString(0));
        foreach (var key in keys)
        {
            using var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@prefix || @key,0));", c, t);
            Text(cmd, "prefix", "YMES:ENT:" + spec.Namespace + ":"); Text(cmd, "key", key); cmd.ExecuteNonQuery();
        }
    }

    private static void AcquireOwn(NpgsqlConnection c, NpgsqlTransaction t, string name, string customer, params string[][] identities)
    {
        // jsonb_build_array is the canonical own-namespace representation; no
        // delimiter ambiguity, including slash/pipe/percent/empty/raw spaces.
        var tokens = new List<string>();
        foreach (var identity in identities)
        {
            using var cmd = new NpgsqlCommand("SELECT jsonb_build_array(VARIADIC @values)::text;", c, t);
            cmd.Parameters.Add("values", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { customer }.Concat(identity).ToArray();
            tokens.Add((string)cmd.ExecuteScalar()!);
        }
        using var sort = new NpgsqlCommand("SELECT DISTINCT v COLLATE \"C\" AS token FROM unnest(@tokens) v ORDER BY token;", c, t);
        sort.Parameters.Add("tokens", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = tokens.ToArray();
        var ordered = new List<string>(); using (var r = sort.ExecuteReader()) while (r.Read()) ordered.Add(r.GetString(0));
        foreach (var token in ordered)
        {
            using var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@prefix || @token,0));", c, t);
            Text(cmd, "prefix", "YMES:ENT:" + name + ":"); Text(cmd, "token", token); cmd.ExecuteNonQuery();
        }
    }

    private static (string? Broker, string? Forwarder, string? Taker) References(NpgsqlConnection c,
        NpgsqlTransaction t, Et106AddressWriteRequest request, Et106AddressDto? old)
    {
        // Take the union of old/new keys in every rank before taking a later
        // rank. ET104 New itself orders Broker -> Forwarder.
        string? Resolve(ParentSpec spec, string? value, string? previous)
        {
            if (old is not null && string.Equals(value, previous, StringComparison.Ordinal)) return previous;
            if (value is null || value.Trim(' ').Length == 0) return value;
            EnsureParent(c, t, spec);
            var row = ReadParent(c, t, spec, value);
            if (row is null || row.State != 2) throw Rule(422, "新增或變更的參照必須是唯一已簽核的真實主檔。");
            return row.No;
        }
        AcquireLogical(c, t, BrokerSpec, [old?.Broker, request.Broker]);
        var broker = Resolve(BrokerSpec, request.Broker, old?.Broker);
        AcquireLogical(c, t, ForwarderSpec, [old?.ForwarderNo, request.ForwarderNo]);
        var forwarder = Resolve(ForwarderSpec, request.ForwarderNo, old?.ForwarderNo);
        AcquireLogical(c, t, TakerSpec, [old?.TakerNo, request.TakerNo]);
        var taker = Resolve(TakerSpec, request.TakerNo, old?.TakerNo);
        return (broker, forwarder, taker);
    }

    private static void EnsureParent(NpgsqlConnection c, NpgsqlTransaction t, ParentSpec spec)
    {
        var fields = new Dictionary<string, (string Type, bool Null)> { [spec.Key] = ($"character varying({spec.Length})", false), ["issuestate"] = ("numeric(1,0)", true) };
        if (spec.Customer) { fields["customername"] = ("character varying(100)", false); fields["customersname"] = ("character varying(100)", false); }
        else fields["contactorname"] = ("character varying(50)", true);
        // Parent source versions have nullable label differences; only the key
        // and type matter for reference qualification. Do not invent a parent.
        var oid = Relation(c, t, spec.Table);
        using (var probe = new NpgsqlCommand($"SELECT {string.Join(',', fields.Keys)} FROM \"YMES\".{spec.Table} WHERE false;", c, t)) probe.ExecuteNonQuery();
        CheckColumns(c, t, oid, fields.ToDictionary(x => x.Key, x => x.Value.Type), false);
        using var pk = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_attribute a ON a.attrelid=k.conrelid AND a.attname=@key WHERE k.conrelid=CAST(@oid AS oid) AND k.contype='p' AND k.conkey=ARRAY[a.attnum]::smallint[]);", c, t);
        Text(pk, "key", spec.Key); Big(pk, "oid", oid);
        if (pk.ExecuteScalar() is not true) throw Unavailable();
        if (Relation(c, t, spec.Table) != oid) throw Unavailable();
    }

    private static long Relation(NpgsqlConnection c, NpgsqlTransaction t, string table)
    {
        using var cmd = new NpgsqlCommand("""
            SELECT c.oid::bigint,n.nspname,c.relname,c.relkind::text,c.relrowsecurity,c.relforcerowsecurity,
             c.relispartition,pg_get_userbyid(c.relowner),EXISTS(SELECT 1 FROM pg_catalog.pg_inherits i WHERE i.inhrelid=c.oid OR i.inhparent=c.oid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname NOT IN('pg_catalog','information_schema') AND lower(c.relname)=@name;
            """, c, t);
        Text(cmd, "name", table); using var r = cmd.ExecuteReader();
        if (!r.Read()) throw Unavailable();
        var oid = r.GetInt64(0);
        if (r.GetString(1) != "YMES" || r.GetString(2) != table || r.GetString(3) != "r" || r.GetBoolean(4)
            || r.GetBoolean(5) || r.GetBoolean(6) || r.GetString(7) != "postgres" || r.GetBoolean(8) || r.Read()) throw Unavailable();
        return oid;
    }

    private static void CheckColumns(NpgsqlConnection c, NpgsqlTransaction t, long oid,
        IReadOnlyDictionary<string,string> expected, bool strictOrdinals)
    {
        using var cmd = new NpgsqlCommand("""
            SELECT a.attname,format_type(a.atttypid,a.atttypmod),a.attnum::int,a.attisdropped,a.attidentity::text,
             a.attgenerated::text,cn.nspname,co.collname,co.collisdeterministic
            FROM pg_catalog.pg_attribute a
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE a.attrelid=CAST(@oid AS oid) AND a.attnum>0 ORDER BY a.attnum;
            """, c, t);
        Big(cmd, "oid", oid); var seen = new HashSet<string>(StringComparer.Ordinal); using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (!expected.TryGetValue(r.GetString(0), out var type)) continue;
            if (r.GetString(1) != type || r.GetBoolean(3) || r.GetString(4) != "" || r.GetString(5) != "") throw Unavailable();
            if (strictOrdinals && r.GetInt32(2) != expected.Keys.ToList().IndexOf(r.GetString(0)) + 1) throw Unavailable();
            if (type.StartsWith("character varying", StringComparison.Ordinal) && (r.IsDBNull(6) || r.GetString(6) != "pg_catalog"
                || r.IsDBNull(7) || r.GetString(7) != "default" || r.IsDBNull(8) || !r.GetBoolean(8))) throw Unavailable();
            seen.Add(r.GetString(0));
        }
        if (seen.Count != expected.Count) throw Unavailable();
    }

    private static void AddressSchema(NpgsqlConnection c, NpgsqlTransaction t)
    {
        if (!ShipAddressReferenceGuard.EnsureOptional(c, t, ShipAddressReference.Broker, Unavailable)) throw Unavailable();
        var oid = Relation(c, t, "tblentcustomershipaddr");
        using var probe = new NpgsqlCommand($"SELECT {AddressColumns} FROM {Addresses} WHERE false;", c, t); probe.ExecuteNonQuery();
        // The shared native22 guard owns types/order/NN/PK. Check all text
        // comparison columns against the reviewed deterministic collation too.
        using var coll = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation JOIN pg_catalog.pg_namespace n ON n.oid=co.collnamespace WHERE a.attrelid=CAST(@oid AS oid) AND a.attname=ANY(@names) AND (n.nspname<>'pg_catalog' OR co.collname<>'default' OR NOT co.collisdeterministic));", c, t);
        Big(coll, "oid", oid); coll.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { "customerno", "shipaddr", "flow", "declarationflag", "shipcustomername" };
        if (coll.ExecuteScalar() is true || Relation(c, t, "tblentcustomershipaddr") != oid) throw Unavailable();
    }

    private static readonly Dictionary<string,string> DeclarationShape = new(StringComparer.Ordinal)
    {
        ["customerno"]="character varying(50)",["shipto"]="character varying(10)",["flow"]="character varying(10)",
        ["overseaflag"]="character varying(1)",["declarationflag"]="character varying(1)",["bondflag"]="character varying(1)",
        ["handcarryflag"]="character varying(1)",["declarationway"]="character varying(50)",["creator"]="character varying(10)",
        ["createdate"]="timestamp without time zone",["datastamp"]="numeric(11,0)",["reviser"]="character varying(10)",["revisedate"]="timestamp without time zone"
    };
    private static void DeclarationSchema(NpgsqlConnection c, NpgsqlTransaction t)
    {
        var oid = Relation(c, t, "tblentcustomershipdeclaration");
        using (var probe = new NpgsqlCommand($"SELECT {DeclarationColumns} FROM {Declarations} WHERE false;", c, t)) probe.ExecuteNonQuery();
        CheckColumns(c, t, oid, DeclarationShape, true);
        using (var identity = new NpgsqlCommand("""
            SELECT (SELECT count(*) FROM pg_catalog.pg_attribute WHERE attrelid=c.oid AND attnum>0)=13
             AND ty.typtype='c' AND ty.typrelid=c.oid AND tn.nspname='YMES'
             AND ty.typname='tblentcustomershipdeclaration' AND arr.typelem=ty.oid
             AND arr.typtype='b' AND an.nspname='YMES' AND arr.typname='_tblentcustomershipdeclaration'
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_type ty ON ty.oid=c.reltype
            JOIN pg_catalog.pg_namespace tn ON tn.oid=ty.typnamespace
            JOIN pg_catalog.pg_type arr ON arr.oid=ty.typarray
            JOIN pg_catalog.pg_namespace an ON an.oid=arr.typnamespace
            WHERE c.oid=CAST(@oid AS oid);
            """, c, t))
        {
            Big(identity, "oid", oid); if (identity.ExecuteScalar() is not true) throw Unavailable();
        }
        using (var nullable = new NpgsqlCommand("SELECT count(*) FROM pg_catalog.pg_attribute WHERE attrelid=CAST(@oid AS oid) AND attnum BETWEEN 1 AND 13 AND attnotnull IS DISTINCT FROM (attnum<=8);", c, t))
        {
            Big(nullable, "oid", oid); if ((long)nullable.ExecuteScalar()! != 0) throw Unavailable();
        }
        using var constraints = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_constraint WHERE conrelid=CAST(@oid AS oid)) OR EXISTS(SELECT 1 FROM pg_catalog.pg_index WHERE indrelid=CAST(@oid AS oid)) OR EXISTS(SELECT 1 FROM pg_catalog.pg_trigger WHERE tgrelid=CAST(@oid AS oid));", c, t);
        Big(constraints, "oid", oid);
        if (constraints.ExecuteScalar() is true || Relation(c, t, "tblentcustomershipdeclaration") != oid) throw Unavailable();
    }

    private static void Raw(string? value, int max, string name)
    {
        if (value is null) throw Rule(400, name + " 必須明確提供原始字串。");
        Field(value, max, name);
    }
    private static void Required(string? value, int max, string name)
    {
        Field(value, max, name);
        if (value is null || value.Trim(' ').Length == 0) throw Rule(422, name + " 不可空白。");
    }
    private static void Field(string? value, int max, string name)
    {
        if (value is null) return;
        if (value.Contains('\0') || value.EnumerateRunes().Count() > max) throw Rule(422, name + " 超過來源長度或包含無法儲存的字元。");
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); }
        catch (EncoderFallbackException) { throw Rule(422, name + " 包含無效文字。"); }
    }
    private static string Actor(string value) { Required(value, 10, "登入使用者"); return value; }
    private static void Stamp(decimal? value)
    { if (value is { } v && (decimal.Truncate(v) != v || v < -MaxStamp || v > MaxStamp)) throw Rule(400, "DataStamp 必須是明確NULL或來源numeric(11,0)範圍內整數。"); }
    private static void Expected(decimal? actual, decimal? expected)
    { Stamp(expected); if (actual != expected) throw Rule(409, "資料已變更，請重新查詢。"); }
    private static void Incrementable(decimal? stamp)
    { Stamp(stamp); if (stamp == MaxStamp) throw Rule(409, "DataStamp 已達來源上限，整筆資料未變更。"); }
    private static string? Str(NpgsqlDataReader r, int n) => r.IsDBNull(n) ? null : r.GetString(n);
    private static DateTime? Date(NpgsqlDataReader r, int n) => r.IsDBNull(n) ? null : DateTime.SpecifyKind(r.GetDateTime(n), DateTimeKind.Utc);
    private static decimal? Num(NpgsqlDataReader r, int n) => r.IsDBNull(n) ? null : r.GetDecimal(n);
    private static void Text(NpgsqlCommand cmd, string key, string? value) => cmd.Parameters.Add(key, NpgsqlDbType.Text).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand cmd, string key, decimal? value) => cmd.Parameters.Add(key, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static void Big(NpgsqlCommand cmd, string key, long value) => cmd.Parameters.Add(key, NpgsqlDbType.Bigint).Value = value;
    private static Et106RuleException Rule(int status, string message) => new(status, message);
    private static Et106RuleException Unavailable() => Rule(503, "必要的出貨地址、報關或參照來源結構／權限尚未就緒。");
}
