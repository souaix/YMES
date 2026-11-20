using System.Data;
using System.Text.RegularExpressions;
using Npgsql;

namespace YMes.Api.Enterprise;

public sealed partial class Et106Repository
{
    private sealed record LockedDeclaration(Et106DeclarationDto Row, string Tid);
    public Et106DeclarationList ListDeclarations(string customerNo)
    {
        Raw(customerNo, 50, "CustomerNo");
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, false); DeclarationSchema(c, t);
        using var cmd = new NpgsqlCommand($"""
            SELECT {DeclarationColumns} FROM {Declarations} WHERE customerno=@customer
            ORDER BY shipto COLLATE "C",flow COLLATE "C",overseaflag COLLATE "C",declarationflag COLLATE "C",
              bondflag COLLATE "C",handcarryflag COLLATE "C",declarationway COLLATE "C",ctid;
            """, c, t);
        Text(cmd, "customer", owner); var items = new List<Et106DeclarationDto>();
        using (var r = cmd.ExecuteReader()) while (r.Read()) items.Add(MapDeclaration(r));
        var groups = items.GroupBy(KeyOf).Select(g => Metadata(owner, g.Key, g.ToList())).ToList();
        t.Commit(); return new(items, groups);
    }

    public Et106DeclarationCreated CreateDeclaration(string customerNo, Et106DeclarationWriteRequest request, string actor)
    {
        var key = NewDeclarationKey(request); Required(request.DeclarationWay, 50, "DeclarationWay");
        Raw(request.SourceShipAddr, 255, "SourceShipAddr"); actor = Actor(actor);
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, true); DeclarationSchema(c, t); AddressSchema(c, t);
        // Selected Address is existence-only New context, not a new stored FK or
        // an inference from ShipTo (source widths255 vs10 do not match).
        AcquireOwn(c, t, "ShipAddress", owner, [request.SourceShipAddr!]);
        if (ReadAddress(c, t, owner, request.SourceShipAddr!, share: true) is null)
            throw Rule(404, "新增報關使用的出貨地址已不存在，請重新選取。");
        AcquireOwn(c, t, "ShipDeclaration", owner, KeyParts(key));
        using var cmd = new NpgsqlCommand($"""
            INSERT INTO {Declarations} ({DeclarationColumns})
            VALUES(@customer,@shipto,@flow,@oversea,@declaration,@bond,@hand,@way,@actor,
              now() AT TIME ZONE 'UTC',0,NULL,NULL)
            RETURNING {DeclarationColumns};
            """, c, t);
        BindKey(cmd, owner, key); Text(cmd, "way", request.DeclarationWay); Text(cmd, "actor", actor);
        Et106DeclarationDto inserted;
        using (var r = cmd.ExecuteReader())
        {
            if (!r.Read()) throw Rule(409, "未取得新增報關結果，交易已取消。");
            inserted = MapDeclaration(r); if (r.Read()) throw Rule(409, "新增報關筆數異常，交易已取消。");
        }
        // Native has no unique constraint: append legal duplicates, keep all.
        var saved = ReadGroup(c, t, owner, key, false).Select(x => x.Row).ToList();
        var metadata = Metadata(owner, key, saved); t.Commit(); return new(inserted, metadata, saved, 1);
    }

    public Et106DeclarationSaved UpdateDeclarationGroup(string customerNo, Et106DeclarationUpdateRequest request, string actor)
    {
        Raw(customerNo, 50, "CustomerNo"); var key = ExistingDeclarationKey(request.Key);
        var version = Version(request.ExpectedGroupVersion); Required(request.DeclarationWay, 50, "DeclarationWay"); actor = Actor(actor);
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, false); DeclarationSchema(c, t);
        AcquireOwn(c, t, "ShipDeclaration", owner, KeyParts(key));
        var locked = ReadGroup(c, t, owner, key, true); VerifyGroup(owner, key, locked, version);
        foreach (var row in locked) Incrementable(row.Row.DataStamp);
        var saved = new List<Et106DeclarationDto>();
        foreach (var row in locked)
        {
            // ctid is only an internal locator from this very locked transaction;
            // no persistence or client identity, including identical duplicates.
            using var cmd = new NpgsqlCommand($"""
                UPDATE {Declarations} SET declarationway=@way,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',
                  datastamp=CASE WHEN datastamp IS NULL THEN 1 ELSE datastamp+1 END
                WHERE ctid=CAST(@tid AS tid) AND {GroupKeySql} AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {DeclarationColumns};
                """, c, t);
            BindKey(cmd, owner, key); Text(cmd, "tid", row.Tid); Text(cmd, "way", request.DeclarationWay);
            Text(cmd, "actor", actor); Number(cmd, "stamp", row.Row.DataStamp);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) throw Rule(409, "整組報關版本已變更，全部修改已取消。");
            saved.Add(MapDeclaration(r)); if (r.Read()) throw Rule(409, "報關實際影響筆數異常，全部修改已取消。");
        }
        if (saved.Count != locked.Count) throw Rule(409, "報關實際影響筆數異常，全部修改已取消。");
        var result = new Et106DeclarationSaved(Metadata(owner, key, saved), saved, saved.Count);
        t.Commit(); return result;
    }

    public Et106DeclarationDeleted DeleteDeclarationGroup(string customerNo, Et106DeclarationExpectedRequest request, string actor)
    {
        Raw(customerNo, 50, "CustomerNo"); var key = ExistingDeclarationKey(request.Key);
        var version = Version(request.ExpectedGroupVersion); _ = Actor(actor);
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, false); DeclarationSchema(c, t);
        AcquireOwn(c, t, "ShipDeclaration", owner, KeyParts(key));
        var locked = ReadGroup(c, t, owner, key, true); VerifyGroup(owner, key, locked, version);
        var affected = 0;
        foreach (var row in locked)
        {
            using var cmd = new NpgsqlCommand($"DELETE FROM {Declarations} WHERE ctid=CAST(@tid AS tid) AND {GroupKeySql} AND datastamp IS NOT DISTINCT FROM @stamp;", c, t);
            BindKey(cmd, owner, key); Text(cmd, "tid", row.Tid); Number(cmd, "stamp", row.Row.DataStamp);
            var count = cmd.ExecuteNonQuery(); if (count != 1) throw Rule(409, "整組報關版本已變更，全部刪除已取消。");
            affected += count;
        }
        if (affected != locked.Count) throw Rule(409, "報關影響筆數異常，全部刪除已取消。");
        t.Commit(); return new(key, 0, [], null, affected);
    }

    private static List<LockedDeclaration> ReadGroup(NpgsqlConnection c, NpgsqlTransaction t,
        string owner, Et106DeclarationKey key, bool update)
    {
        using var cmd = new NpgsqlCommand($"SELECT {DeclarationColumns},ctid::text FROM {Declarations} WHERE {GroupKeySql} ORDER BY ctid" + (update ? " FOR UPDATE;" : ";"), c, t);
        BindKey(cmd, owner, key); using var r = cmd.ExecuteReader(); var rows = new List<LockedDeclaration>();
        while (r.Read()) rows.Add(new(MapDeclaration(r), r.GetString(13))); return rows;
    }
    private static void VerifyGroup(string owner, Et106DeclarationKey key, List<LockedDeclaration> rows, string expected)
    {
        if (rows.Count == 0) throw Rule(404, "指定的原始報關群組已不存在。");
        if (!string.Equals(Et106DeclarationVersion.Compute(owner, key, rows.Select(x => x.Row)), expected, StringComparison.Ordinal))
            throw Rule(409, "報關群組內容或筆數已變更，請重新查詢。");
    }
    private static Et106DeclarationGroup Metadata(string owner, Et106DeclarationKey key, IReadOnlyList<Et106DeclarationDto> rows) =>
        new(key, Et106DeclarationVersion.Compute(owner, key, rows), rows.Count);
    private static Et106DeclarationDto MapDeclaration(NpgsqlDataReader r) => new(r.GetString(0), r.GetString(1), r.GetString(2),
        r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), Str(r, 8), Date(r, 9), Num(r, 10), Str(r, 11), Date(r, 12));
    private static Et106DeclarationKey KeyOf(Et106DeclarationDto row) =>
        new(row.ShipTo, row.Flow, row.OverseaFlag, row.DeclarationFlag, row.BondFlag, row.HandCarryFlag);
    private static string[] KeyParts(Et106DeclarationKey key) =>
        [key.ShipTo, key.Flow, key.OverseaFlag, key.DeclarationFlag, key.BondFlag, key.HandCarryFlag];
    private static Et106DeclarationKey NewDeclarationKey(Et106DeclarationWriteRequest r)
    {
        Required(r.ShipTo, 10, "ShipTo"); Required(r.Flow, 10, "Flow"); Required(r.OverseaFlag, 1, "OverseaFlag");
        Required(r.DeclarationFlag, 1, "DeclarationFlag"); Required(r.BondFlag, 1, "BondFlag"); Required(r.HandCarryFlag, 1, "HandCarryFlag");
        return new(r.ShipTo!, r.Flow!, r.OverseaFlag!, r.DeclarationFlag!, r.BondFlag!, r.HandCarryFlag!);
    }
    private static Et106DeclarationKey ExistingDeclarationKey(Et106DeclarationKey? key)
    {
        if (key is null) throw Rule(400, "必須明確提供原始報關定位群組。");
        Raw(key.ShipTo, 10, "ShipTo"); Raw(key.Flow, 10, "Flow"); Raw(key.OverseaFlag, 1, "OverseaFlag");
        Raw(key.DeclarationFlag, 1, "DeclarationFlag"); Raw(key.BondFlag, 1, "BondFlag"); Raw(key.HandCarryFlag, 1, "HandCarryFlag");
        return key; // Preserve legitimate source empty/padded/unknown flag codes.
    }
    private static string Version(string? value)
    {
        if (value is null || !Regex.IsMatch(value, "\\Aet106-decl-v1:[0-9a-f]{64}\\z", RegexOptions.CultureInvariant))
            throw Rule(400, "必須提供此群組的完整有效版本。");
        return value;
    }
    private static void BindKey(NpgsqlCommand cmd, string owner, Et106DeclarationKey key)
    {
        Text(cmd, "customer", owner); Text(cmd, "shipto", key.ShipTo); Text(cmd, "flow", key.Flow);
        Text(cmd, "oversea", key.OverseaFlag); Text(cmd, "declaration", key.DeclarationFlag);
        Text(cmd, "bond", key.BondFlag); Text(cmd, "hand", key.HandCarryFlag);
    }
}
