using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed record Wp109DeviceRow(string ProductNo, string ProductVersion,
    string ProductName, string? DeviceNo);
public sealed record Wp109DeviceResult(IReadOnlyList<Wp109DeviceRow> Items, int Limit, bool HasMore);

// New read projection, not an extension of ProductDto or a Product writer.
public sealed class Wp109DeviceLookup(NpgsqlDataSource source)
{
    public Wp109DeviceResult List(string? keyword, int limit)
    {
        if (limit is < 1 or > 500) throw new Wp109WriteRuleException(400, "查找上限須為1–500。");
        if (keyword is not null)
        {
            try { _ = new UTF8Encoding(false, true).GetByteCount(keyword); }
            catch (EncoderFallbackException) { throw new Wp109WriteRuleException(400, "查詢含無效Unicode。"); }
            if (keyword.Contains('\0') || keyword.EnumerateRunes().Count() > 300)
                throw new Wp109WriteRuleException(400, "查詢文字不符合來源。");
        }
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        using (var ro = new NpgsqlCommand("SET TRANSACTION READ ONLY;", c, t)) ro.ExecuteNonQuery();
        var oid = Inspect(c, t);
        using (var probe = new NpgsqlCommand("SELECT productno,productversion,productname,deviceno,issuestate,curversion FROM \"YMES\".tblprdproductbasis WHERE false;", c, t)) probe.ExecuteNonQuery();
        if (Inspect(c, t) != oid) throw Unavailable();
        using var cmd = new NpgsqlCommand("""
            SELECT productno,productversion,productname,deviceno
            FROM "YMES".tblprdproductbasis
            WHERE issuestate=2 AND curversion=1
             AND (@keyword IS NULL OR position(lower(@keyword) in lower(productno))>0
              OR position(lower(@keyword) in lower(productname))>0
              OR position(lower(@keyword) in lower(deviceno))>0)
            ORDER BY productno COLLATE "C",productversion COLLATE "C" LIMIT @take;
            """, c, t);
        cmd.Parameters.Add("keyword", NpgsqlDbType.Varchar).Value = (object?)(keyword is "" ? null : keyword) ?? DBNull.Value;
        cmd.Parameters.Add("take", NpgsqlDbType.Integer).Value = limit + 1;
        var rows = new List<Wp109DeviceRow>();
        using (var r = cmd.ExecuteReader())
            while (r.Read()) rows.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3)));
        var more = rows.Count > limit; if (more) rows.RemoveAt(rows.Count - 1);
        t.Commit(); return new(rows, limit, more);
    }

    private static long Inspect(NpgsqlConnection c, NpgsqlTransaction t)
    {
        using var cmd = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,c.relpersistence::text,
             pg_get_userbyid(c.relowner),c.relrowsecurity,c.relforcerowsecurity,c.relispartition,c.relhasrules,c.oid::bigint,
             EXISTS(SELECT 1 FROM pg_inherits i WHERE i.inhrelid=c.oid OR i.inhparent=c.oid)
            FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)='tblprdproductbasis';
            """, c, t);
        long oid;
        using (var r = cmd.ExecuteReader())
        {
            if (!r.Read() || r.GetString(0) != "YMES" || r.GetString(1) != "tblprdproductbasis"
                || r.GetString(2) != "r" || r.GetString(3) != "p" || r.GetString(4) != "postgres"
                || r.GetBoolean(5) || r.GetBoolean(6) || r.GetBoolean(7) || r.GetBoolean(8) || r.GetBoolean(10)) throw Unavailable();
            oid = r.GetInt64(9); if (r.Read()) throw Unavailable();
        }
        using var attributes = new NpgsqlCommand("""
            SELECT a.attname,format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attisdropped,
             a.attidentity::text,a.attgenerated::text,n.nspname,l.collname,l.collisdeterministic
            FROM pg_attribute a LEFT JOIN pg_collation l ON l.oid=a.attcollation
             LEFT JOIN pg_namespace n ON n.oid=l.collnamespace
            WHERE a.attrelid=@oid::oid AND a.attnum>0
             AND a.attname=ANY(ARRAY['productno','productversion','productname','deviceno','issuestate','curversion']);
            """, c, t);
        attributes.Parameters.Add("oid", NpgsqlDbType.Bigint).Value = oid;
        var expected = new Dictionary<string, (string Type, bool NotNull)>(StringComparer.Ordinal)
        {
            ["productno"] = ("character varying(50)", true), ["productversion"] = ("character varying(5)", true),
            ["productname"] = ("character varying(300)", true), ["deviceno"] = ("character varying(50)", false),
            ["issuestate"] = ("numeric(1,0)", true), ["curversion"] = ("numeric(1,0)", true)
        };
        using (var r = attributes.ExecuteReader())
            while (r.Read())
            {
                if (!expected.Remove(r.GetString(0), out var shape) || r.GetString(1) != shape.Type || r.GetBoolean(2) != shape.NotNull
                    || r.GetBoolean(3) || r.GetString(4) != "" || r.GetString(5) != "") throw Unavailable();
                if (shape.Type.StartsWith("character varying", StringComparison.Ordinal)
                    && (r.IsDBNull(6) || r.GetString(6) != "pg_catalog" || r.GetString(7) != "default" || !r.GetBoolean(8))) throw Unavailable();
            }
        if (expected.Count != 0) throw Unavailable();
        using var primary = new NpgsqlCommand("""
            SELECT array_agg(a.attname::text ORDER BY k.ordinality)
            FROM pg_constraint p CROSS JOIN LATERAL unnest(p.conkey) WITH ORDINALITY k(attnum,ordinality)
            JOIN pg_attribute a ON a.attrelid=p.conrelid AND a.attnum=k.attnum
            WHERE p.conrelid=@oid::oid AND p.contype='p';
            """, c, t);
        primary.Parameters.Add("oid", NpgsqlDbType.Bigint).Value = oid;
        if (primary.ExecuteScalar() is not string[] keys || !keys.SequenceEqual(new[] { "productno", "productversion" })) throw Unavailable();
        // Product has existing internal RI triggers. This projection writes
        // nothing and does not reject or alter those constraints/triggers.
        return oid;
    }

    private static Wp109WriteRuleException Unavailable() => new(503, "Device原生查找來源尚未就緒。");
}
