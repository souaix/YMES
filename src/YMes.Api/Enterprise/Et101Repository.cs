using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed partial class Et101Repository(NpgsqlDataSource dataSource)
{
    private const string Table = "\"YMES\".tblentcustomerbasis";
    private const string Columns = "customerno,customername,customersname,telno,faxno,www,taxcode,address,director,creator,createdate,description,issuestate::int,address2,country,status,currency,invtypeno,invaddr,invaddr2,invcustno,invremark,custtype,artype,freightterms,paymentterms,acctno,deliveryterm,forwarderassigned,brokerassigned,insurancecoverage,reviser,revisedate,customerename,erpno,datastamp,sap_fiskn,xx_name1,xx_name2,xx_rebarcode,xx_supplydata";
    private const string LogicalKey = "lower(btrim(customerno))=lower(btrim(@customer))";
    private const decimal MaxStamp = 99_999_999_999m;
    private sealed record MutableSpec(string Column, string Label, int Maximum, bool LockedOutsideDraft,
        bool Multiline, Func<Et101WriteRequest, string?> Input, Func<Et101CustomerDto, string?> Original);
    private static readonly MutableSpec[] Fields =
    [
        new("customername", "CustomerName", 100, true, false, x => x.CustomerName, x => x.CustomerName),
        new("customersname", "CustomerSName", 100, true, false, x => x.CustomerSName, x => x.CustomerSName),
        new("customerename", "CustomerEName", 50, false, false, x => x.CustomerEName, x => x.CustomerEName),
        new("telno", "TelNo", 25, true, false, x => x.TelNo, x => x.TelNo),
        new("faxno", "FaxNo", 25, true, false, x => x.FaxNo, x => x.FaxNo),
        new("www", "Www", 50, true, false, x => x.Www, x => x.Www),
        new("taxcode", "TaxCode", 25, true, false, x => x.TaxCode, x => x.TaxCode),
        new("address", "Address", 255, true, true, x => x.Address, x => x.Address),
        new("address2", "Address2", 255, true, true, x => x.Address2, x => x.Address2),
        new("director", "Director", 50, true, false, x => x.Director, x => x.Director),
        new("country", "Country", 50, true, false, x => x.Country, x => x.Country),
        new("description", "Description", 255, true, true, x => x.Description, x => x.Description),
        new("status", "Status", 5, false, false, x => x.Status, x => x.Status),
        new("currency", "Currency", 6, false, false, x => x.Currency, x => x.Currency),
        new("invtypeno", "InvTypeNo", 1, false, false, x => x.InvTypeNo, x => x.InvTypeNo),
        new("invaddr", "InvAddr", 255, false, true, x => x.InvAddr, x => x.InvAddr),
        new("invaddr2", "InvAddr2", 255, false, true, x => x.InvAddr2, x => x.InvAddr2),
        new("invcustno", "InvCustNo", 20, false, false, x => x.InvCustNo, x => x.InvCustNo),
        new("invremark", "InvRemark", 100, false, true, x => x.InvRemark, x => x.InvRemark),
        new("custtype", "CustType", 1, false, false, x => x.CustType, x => x.CustType),
        new("artype", "ArType", 1, false, false, x => x.ArType, x => x.ArType),
        new("freightterms", "FreightTerms", 20, false, false, x => x.FreightTerms, x => x.FreightTerms),
        new("paymentterms", "PaymentTerms", 20, false, false, x => x.PaymentTerms, x => x.PaymentTerms),
        new("acctno", "AcctNo", 50, false, false, x => x.AcctNo, x => x.AcctNo),
        new("deliveryterm", "DeliveryTerm", 50, false, false, x => x.DeliveryTerm, x => x.DeliveryTerm),
        new("forwarderassigned", "ForwarderAssigned", 50, false, false, x => x.ForwarderAssigned, x => x.ForwarderAssigned),
        new("brokerassigned", "BrokerAssigned", 50, false, false, x => x.BrokerAssigned, x => x.BrokerAssigned),
        new("insurancecoverage", "InsuranceCoverage", 50, false, false, x => x.InsuranceCoverage, x => x.InsuranceCoverage),
    ];

    public IReadOnlyList<Et101CustomerDto> List(string? keyword)
    {
        Search(keyword);
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureOwn(c, t);
        using var cmd = new NpgsqlCommand($"""
            WITH source AS (SELECT *,count(*) OVER(PARTITION BY lower(btrim(customerno))) AS aliases FROM {Table})
            SELECT {Columns},aliases FROM source
            WHERE @keyword IS NULL OR position(lower(@keyword) in lower(customerno))>0
            ORDER BY lower(btrim(customerno)) COLLATE "C",customerno COLLATE "C";
            """, c, t);
        Text(cmd, "keyword", keyword is "" ? null : keyword);
        var rows = new List<Et101CustomerDto>();
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                if (r.GetInt64(41) != 1) throw Alias();
                rows.Add(Map(r));
            }
        t.Commit(); return rows;
    }

    public Et101CustomerDto Get(string customerNo)
    {
        RawKey(customerNo);
        using var c = dataSource.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureOwn(c, t); var row = Read(c, t, customerNo) ?? throw Missing();
        t.Commit(); return row;
    }

    private static void AcquireCustomer(NpgsqlConnection c, NpgsqlTransaction t, string no)
    {
        using var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Customer:' || lower(btrim(@customer)),0));", c, t);
        Text(cmd, "customer", no); cmd.ExecuteNonQuery();
    }

    private static Et101CustomerDto LockCustomer(NpgsqlConnection c, NpgsqlTransaction t, string customerNo)
    {
        RawKey(customerNo); AcquireCustomer(c, t, customerNo); EnsureOwn(c, t);
        // Waiting here and reference reads are distinct READ COMMITTED commands.
        // OPB03 Create holds SHARE until its mapping commits; after this UPDATE
        // lock succeeds the subsequent reference statement sees that commit.
        return Read(c, t, customerNo, update: true) ?? throw Missing();
    }

    private static Et101CustomerDto? Read(NpgsqlConnection c, NpgsqlTransaction t, string no, bool update = false)
    {
        using var cmd = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE {LogicalKey} ORDER BY customerno COLLATE \"C\"" + (update ? " FOR UPDATE;" : ";"), c, t);
        Text(cmd, "customer", no); using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var row = Map(r); if (r.Read()) throw Alias(); return row;
    }

    private static void ValidateFields(Et101WriteRequest input, Et101CustomerDto? old = null)
    {
        foreach (var field in Fields)
        {
            var value = field.Input(input);
            var changed = old is null || !string.Equals(value, field.Original(old), StringComparison.Ordinal);
            if (old is not null && old.IssueState != 0 && field.LockedOutsideDraft && changed)
                throw Rule(409, $"非未簽核客戶不可修改 {field.Label}，資料未變更。");
            if (!changed) continue; // Preserve unchanged legacy raw text verbatim.
            if (InvalidText(value, field.Maximum, field.Multiline))
                throw Rule(400, $"{field.Label} 最多{field.Maximum}字元且不可含無效控制字元。");
            if (field.Column is "customername" or "customersname" && string.IsNullOrWhiteSpace(value))
                throw Rule(400, $"{field.Label} 必填，不可為NULL或空白。");
            var allowed = field.Column switch
            {
                "status" => new[] { "O", "C", "B", "P" },
                "invtypeno" or "custtype" or "artype" => new[] { "0", "1" },
                _ => null
            };
            if (allowed is not null && value is not (null or "") && !allowed.Contains(value, StringComparer.Ordinal))
                throw Rule(422, $"{field.Label} 選項不符原生字典；未變更的舊值可原樣保留。");
        }
    }

    private static void Tokens(Et101CustomerDto row, int? state, decimal? stamp)
    {
        if (state is < -9 or > 9) throw Rule(400, "IssueState 超出來源numeric(1,0)。");
        Stamp(stamp);
        if (row.IssueState != state || row.DataStamp != stamp) throw Conflict();
    }
    private static void Stamp(decimal? value)
    {
        if (value is { } stamp && (stamp != decimal.Truncate(stamp) || stamp < -MaxStamp || stamp > MaxStamp))
            throw Rule(400, "DataStamp 必須是NULL或來源numeric(11,0)範圍內整數。");
    }
    private static void Incrementable(decimal? value)
    { Stamp(value); if (value == MaxStamp) throw Rule(409, "DataStamp 已達來源上限，資料未變更。"); }
    // PostgreSQL UTF8 varchar(n) counts Unicode scalar values, not UTF16 units.
    // Reject ill-formed UTF16 before EnumerateRunes can replace lone surrogates.
    private static bool InvalidText(string? value, int maximum, bool multiline = false)
    {
        if (value is null) return false;
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); }
        catch (EncoderFallbackException) { return true; }
        return value.EnumerateRunes().Count() > maximum || value.Any(ch => char.IsControl(ch)
            && !(multiline && ch is '\r' or '\n' or '\t'));
    }
    private static void RawKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || InvalidText(value, 50))
            throw Rule(400, "CustomerNo 必填、最多50字元且不可含控制字元。");
    }
    private static void Search(string? value)
    {
        if (InvalidText(value, 50))
            throw Rule(400, "CustomerNo 查詢最多50字元且不可含控制字元。");
    }
    private static string Actor(string value, bool create)
    {
        if (string.IsNullOrWhiteSpace(value) || InvalidText(value, create ? 10 : 20))
            throw Rule(400, "登入使用者識別碼不符合來源稽核欄位長度。");
        return value;
    }

    private static void BindFields(NpgsqlCommand cmd, Et101WriteRequest input)
    { foreach (var field in Fields) Text(cmd, field.Column, field.Input(input)); }
    private static void BindExpected(NpgsqlCommand cmd, Et101CustomerDto old)
    { Text(cmd, "customer", old.CustomerNo); Number(cmd, "stamp", old.DataStamp); Number(cmd, "state", old.IssueState); }
    private static void Text(NpgsqlCommand cmd, string name, string? value) =>
        cmd.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand cmd, string name, decimal? value) =>
        cmd.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static string? Str(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static DateTime? Date(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc);
    private static Et101CustomerDto Map(NpgsqlDataReader r) => new(
        r.GetString(0),r.GetString(1),r.GetString(2),Str(r,3),Str(r,4),Str(r,5),Str(r,6),Str(r,7),Str(r,8),
        Str(r,9),Date(r,10),Str(r,11),r.IsDBNull(12)?null:r.GetInt32(12),Str(r,13),Str(r,14),Str(r,15),
        Str(r,16),Str(r,17),Str(r,18),Str(r,19),Str(r,20),Str(r,21),Str(r,22),Str(r,23),Str(r,24),
        Str(r,25),Str(r,26),Str(r,27),Str(r,28),Str(r,29),Str(r,30),Str(r,31),Date(r,32),Str(r,33),
        Str(r,34),r.IsDBNull(35)?null:r.GetDecimal(35),Str(r,36),Str(r,37),Str(r,38),Str(r,39),Str(r,40));
    private static Et101CustomerDto Returned(NpgsqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); if (!r.Read()) throw Conflict();
        var row = Map(r); if (r.Read()) throw Conflict(); return row;
    }
    private static Et101RuleException Rule(int status, string message) => new(status, message);
    private static Et101RuleException Conflict() => Rule(409, "客戶狀態或版本已變更，請重新查詢；本次未保存。");
    private static Et101RuleException Alias() => Rule(409, "客戶識別碼存在大小寫／空白歧義，原資料已保留。");
    private static Et101RuleException Missing() => Rule(404, "找不到指定客戶。");
    private static Et101RuleException Unavailable() => Rule(503, "必要客戶或引用結構／權限與已核對來源不符，未使用替代資料。");
}

