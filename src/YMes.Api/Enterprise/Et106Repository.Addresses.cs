using System.Data;
using Npgsql;

namespace YMes.Api.Enterprise;

public sealed partial class Et106Repository
{
    public IReadOnlyList<Et106AddressDto> ListAddresses(string customerNo)
    {
        Raw(customerNo, 50, "CustomerNo");
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, false); AddressSchema(c, t);
        using var cmd = new NpgsqlCommand($"SELECT {AddressColumns} FROM {Addresses} WHERE customerno=@customer ORDER BY shipaddr COLLATE \"C\";", c, t);
        Text(cmd, "customer", owner); var rows = new List<Et106AddressDto>();
        using (var r = cmd.ExecuteReader()) while (r.Read()) rows.Add(MapAddress(r));
        t.Commit(); return rows;
    }

    public Et106AddressDto GetAddress(string customerNo, string shipAddr)
    {
        Raw(customerNo, 50, "CustomerNo"); Raw(shipAddr, 255, "ShipAddr");
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, false); AddressSchema(c, t);
        var row = ReadAddress(c, t, owner, shipAddr) ?? throw Rule(404, "指定出貨地址不存在。");
        t.Commit(); return row;
    }

    public Et106AddressDto CreateAddress(string customerNo, Et106AddressWriteRequest request, string actor)
    {
        ValidateAddress(request, true); actor = Actor(actor);
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, true); AddressSchema(c, t);
        var refs = References(c, t, request, null);
        AcquireOwn(c, t, "ShipAddress", owner, [request.ShipAddr!]);
        if (ReadAddress(c, t, owner, request.ShipAddr!) is not null) throw Rule(409, "此客戶已有完全相同的地址鍵，不能接管或覆寫。");
        using var cmd = new NpgsqlCommand($"""
            INSERT INTO {Addresses} ({AddressColumns})
            VALUES(@address,@customer,@tel,@contactor,@shipname,@actor,now() AT TIME ZONE 'UTC',@description,
              @flow,@declaration,@oversea,@fax,@email,@address2,@country,@company,@taker,@forwarder,@broker,0,NULL,NULL)
            RETURNING {AddressColumns};
            """, c, t);
        BindAddress(cmd, owner, request, refs, actor);
        var saved = ReturnedAddress(cmd); t.Commit(); return saved;
    }

    public Et106AddressDto UpdateAddress(string customerNo, string oldShipAddr, Et106AddressUpdateRequest request, string actor)
    {
        Raw(customerNo, 50, "CustomerNo"); Raw(oldShipAddr, 255, "ShipAddr");
        ValidateAddress(request, false); Stamp(request.DataStamp); actor = Actor(actor);
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, false); AddressSchema(c, t);
        var preview = ReadAddress(c, t, owner, oldShipAddr) ?? throw Rule(404, "原始出貨地址不存在。");
        if (!string.Equals(preview.ShipAddr, request.ShipAddr, StringComparison.Ordinal)) Required(request.ShipAddr, 255, "ShipAddr");
        // Customer namespace serializes cooperating address/declaration writers.
        // Read before child lock to acquire all old/new reference ranks first.
        var refs = References(c, t, request, preview);
        AcquireOwn(c, t, "ShipAddress", owner, [oldShipAddr], [request.ShipAddr!]);
        var current = ReadAddress(c, t, owner, oldShipAddr, true) ?? throw Rule(409, "出貨地址已變更，請重新查詢。");
        if (current != preview) throw Rule(409, "鎖定前來源資料已變更，請重新查詢。");
        Expected(current.DataStamp, request.DataStamp); Incrementable(current.DataStamp);
        ImmutableAddress(current, request.Flow, request.ShipCustomerName, request.DeclarationFlag);
        if (!string.Equals(oldShipAddr, request.ShipAddr, StringComparison.Ordinal)
            && ReadAddress(c, t, owner, request.ShipAddr!) is not null) throw Rule(409, "目的地址鍵已存在，原資料未變更。");
        using var cmd = new NpgsqlCommand($"""
            UPDATE {Addresses} SET shipaddr=@address,telno=@tel,contactorname=@contactor,description=@description,
              overseaflag=@oversea,faxno=@fax,email=@email,shipaddr2=@address2,country=@country,companyname=@company,
              takerno=@taker,forwarderno=@forwarder,broker=@broker,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',
              datastamp=CASE WHEN datastamp IS NULL THEN 1 ELSE datastamp+1 END
            WHERE customerno=@customer AND shipaddr=@oldaddress AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {AddressColumns};
            """, c, t);
        BindAddress(cmd, owner, request, refs, actor); Text(cmd, "oldaddress", current.ShipAddr); Number(cmd, "stamp", request.DataStamp);
        var saved = ReturnedAddress(cmd); t.Commit(); return saved;
    }

    public void DeleteAddress(string customerNo, string shipAddr, Et106AddressExpectedRequest request, string actor)
    {
        Raw(customerNo, 50, "CustomerNo"); Raw(shipAddr, 255, "ShipAddr"); Stamp(request.DataStamp); _ = Actor(actor);
        Field(request.ExpectedFlow, 10, "ExpectedFlow"); Field(request.ExpectedShipCustomerName, 30, "ExpectedShipCustomerName");
        Field(request.ExpectedDeclarationFlag, 1, "ExpectedDeclarationFlag");
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        var owner = Owner(c, t, customerNo, false); AddressSchema(c, t);
        var preview = ReadAddress(c, t, owner, shipAddr) ?? throw Rule(404, "指定出貨地址不存在。");
        AcquireLogical(c, t, BrokerSpec, [preview.Broker]);
        AcquireLogical(c, t, ForwarderSpec, [preview.ForwarderNo]);
        AcquireLogical(c, t, TakerSpec, [preview.TakerNo]);
        AcquireOwn(c, t, "ShipAddress", owner, [shipAddr]);
        var current = ReadAddress(c, t, owner, shipAddr, true) ?? throw Rule(409, "出貨地址已變更，請重新查詢。");
        if (current != preview) throw Rule(409, "鎖定前來源資料已變更，請重新查詢。");
        Expected(current.DataStamp, request.DataStamp);
        ImmutableAddress(current, request.ExpectedFlow, request.ExpectedShipCustomerName, request.ExpectedDeclarationFlag);
        using var cmd = new NpgsqlCommand($"DELETE FROM {Addresses} WHERE {AddressKeySql} AND datastamp IS NOT DISTINCT FROM @stamp;", c, t);
        Text(cmd, "customer", owner); Text(cmd, "address", shipAddr); Number(cmd, "stamp", request.DataStamp);
        if (cmd.ExecuteNonQuery() != 1) throw Rule(409, "刪除版本已變更，原資料未變更。");
        // Source Declaration is independent; no inferred cascade or ShipTo rewrite.
        t.Commit();
    }

    private static Et106AddressDto? ReadAddress(NpgsqlConnection c, NpgsqlTransaction t,
        string owner, string address, bool update = false, bool share = false)
    {
        using var cmd = new NpgsqlCommand($"SELECT {AddressColumns} FROM {Addresses} WHERE {AddressKeySql}" +
            (update ? " FOR UPDATE;" : share ? " FOR SHARE;" : ";"), c, t);
        Text(cmd, "customer", owner); Text(cmd, "address", address); using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var row = MapAddress(r); if (r.Read()) throw Rule(409, "來源地址主鍵不唯一，原列已保留。"); return row;
    }
    private static Et106AddressDto MapAddress(NpgsqlDataReader r) => new(r.GetString(0), r.GetString(1),
        Str(r, 2), Str(r, 3), Str(r, 4), Str(r, 5), Date(r, 6), Str(r, 7), r.GetString(8), r.GetString(9),
        Str(r, 10), Str(r, 11), Str(r, 12), Str(r, 13), Str(r, 14), Str(r, 15), Str(r, 16), Str(r, 17), Str(r, 18),
        Num(r, 19), Str(r, 20), Date(r, 21));
    private static Et106AddressDto ReturnedAddress(NpgsqlCommand cmd)
    {
        using var r = cmd.ExecuteReader();
        if (!r.Read()) throw Rule(409, "資料版本已變更，未取得保存結果。");
        var row = MapAddress(r); if (r.Read()) throw Rule(409, "來源地址主鍵不唯一，交易已取消。"); return row;
    }
    private static void ImmutableAddress(Et106AddressDto row, string? flow, string? name, string? declaration)
    {
        if (!string.Equals(row.Flow, flow, StringComparison.Ordinal) || !string.Equals(row.ShipCustomerName, name, StringComparison.Ordinal)
            || !string.Equals(row.DeclarationFlag, declaration, StringComparison.Ordinal))
            throw Rule(422, "Flow、ShipCustomerName與DeclarationFlag不可修改，請保留原始值。");
    }
    private static void ValidateAddress(Et106AddressWriteRequest r, bool create)
    {
        Raw(r.ShipAddr, 255, "ShipAddr");
        if (create) { Required(r.ShipAddr, 255, "ShipAddr"); Required(r.ShipCustomerName, 30, "ShipCustomerName"); Required(r.Flow, 10, "Flow"); Required(r.DeclarationFlag, 1, "DeclarationFlag"); }
        Field(r.TelNo, 25, "TelNo"); Field(r.ContactorName, 25, "ContactorName"); Field(r.ShipCustomerName, 30, "ShipCustomerName");
        Field(r.Description, 255, "Description"); Field(r.Flow, 10, "Flow"); Field(r.DeclarationFlag, 1, "DeclarationFlag");
        Field(r.OverseaFlag, 1, "OverseaFlag"); Field(r.FaxNo, 25, "FaxNo"); Field(r.Email, 255, "Email");
        Field(r.ShipAddr2, 500, "ShipAddr2"); Field(r.Country, 50, "Country"); Field(r.CompanyName, 150, "CompanyName");
        Field(r.TakerNo, 50, "TakerNo"); Field(r.ForwarderNo, 50, "ForwarderNo"); Field(r.Broker, 50, "Broker");
    }
    private static void BindAddress(NpgsqlCommand cmd, string owner, Et106AddressWriteRequest r,
        (string? Broker, string? Forwarder, string? Taker) refs, string actor)
    {
        Text(cmd, "customer", owner); Text(cmd, "address", r.ShipAddr); Text(cmd, "tel", r.TelNo); Text(cmd, "contactor", r.ContactorName);
        Text(cmd, "shipname", r.ShipCustomerName); Text(cmd, "actor", actor); Text(cmd, "description", r.Description); Text(cmd, "flow", r.Flow);
        Text(cmd, "declaration", r.DeclarationFlag); Text(cmd, "oversea", r.OverseaFlag); Text(cmd, "fax", r.FaxNo); Text(cmd, "email", r.Email);
        Text(cmd, "address2", r.ShipAddr2); Text(cmd, "country", r.Country); Text(cmd, "company", r.CompanyName);
        Text(cmd, "taker", refs.Taker); Text(cmd, "forwarder", refs.Forwarder); Text(cmd, "broker", refs.Broker);
    }
}
