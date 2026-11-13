using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresEquipmentVendorRepository(NpgsqlDataSource dataSource)
{
    private const string VendorColumns = "vendorno,vendorname,description,issuestate::int,erpno,COALESCE(datastamp,0)::bigint,creator,createdate,reviser,revisedate";
    private const string ContactorColumns = "vendorno,contactorname,telno,faxno,title,address,email,description,COALESCE(datastamp,0)::bigint,creator,createdate,reviser,revisedate";

    public IReadOnlyList<EquipmentVendorDto> List(string? keyword, int? issueState)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {VendorColumns}
            FROM "YMES".tbleqpvendor
            WHERE (@issueState IS NULL OR issuestate=@issueState)
              AND (@keyword IS NULL OR vendorno ILIKE @keyword)
            ORDER BY vendorno;
            """);
        AddNumeric(command, "issueState", issueState);
        AddVarchar(command, "keyword", string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%");
        using var reader = command.ExecuteReader();
        var rows = new List<EquipmentVendorDto>();
        while (reader.Read()) rows.Add(MapVendor(reader));
        return rows;
    }

    public EquipmentVendorDto Get(string vendorNo)
    {
        using var command = dataSource.CreateCommand($"SELECT {VendorColumns} FROM \"YMES\".tbleqpvendor WHERE lower(vendorno)=lower(@no) LIMIT 1;");
        AddVarchar(command, "no", vendorNo.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapVendor(reader) : throw NotFound(vendorNo);
    }

    public IReadOnlyList<EquipmentVendorContactorDto> Contactors(string vendorNo)
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {ContactorColumns}
            FROM "YMES".tbleqpvendorcontactor
            WHERE lower(vendorno)=lower(@no)
            ORDER BY contactorname;
            """);
        AddVarchar(command, "no", vendorNo.Trim());
        using var reader = command.ExecuteReader();
        var rows = new List<EquipmentVendorContactorDto>();
        while (reader.Read()) rows.Add(MapContactor(reader));
        return rows;
    }

    public EquipmentVendorDto Create(EquipmentVendorWriteRequest request, string actor)
    {
        var vendorNo = Required(request.VendorNo, "供應商編號", 20);
        var vendorName = Required(request.VendorName, "供應商名稱", 220);
        var description = Optional(request.Description, 255, "說明");
        var erpNo = Optional(request.ErpNo, 50, "ERP 編號");
        ValidateActor(actor);
        using var command = dataSource.CreateCommand("""
            INSERT INTO "YMES".tbleqpvendor(vendorno,vendorname,description,creator,createdate,issuestate,erpno,datastamp)
            VALUES(@no,@name,@description,@actor,now(),0,@erp,0);
            """);
        AddVarchar(command,"no",vendorNo); AddVarchar(command,"name",vendorName); AddVarchar(command,"description",description);
        AddVarchar(command,"actor",actor.Trim()); AddVarchar(command,"erp",erpNo);
        try { command.ExecuteNonQuery(); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { throw new EquipmentRuleException(409, $"供應商編號 {vendorNo} 已存在。"); }
        return Get(vendorNo);
    }

    public EquipmentVendorDto Update(string vendorNo, EquipmentVendorWriteRequest request, string actor)
    {
        var routeNo = Required(vendorNo, "供應商編號", 20);
        if (!Same(routeNo, request.VendorNo)) throw new EquipmentRuleException(400, "網址與資料內容的 VendorNo 必須相同。");
        var vendorName = Required(request.VendorName, "供應商名稱", 220);
        var description = Optional(request.Description, 255, "說明");
        var erpNo = Optional(request.ErpNo, 50, "ERP 編號");
        ValidateActor(actor);
        var current = Get(routeNo);
        EnsureEditable(current, request.DataStamp);
        using var command = dataSource.CreateCommand("""
            UPDATE "YMES".tbleqpvendor
            SET vendorname=@name,description=@description,erpno=@erp,datastamp=COALESCE(datastamp,0)+1,
                reviser=@actor,revisedate=now()
            WHERE lower(vendorno)=lower(@no) AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
            """);
        AddVarchar(command,"name",vendorName); AddVarchar(command,"description",description); AddVarchar(command,"erp",erpNo);
        AddVarchar(command,"actor",actor.Trim()); AddVarchar(command,"no",routeNo); AddNumeric(command,"stamp",request.DataStamp);
        ExecuteOptimistic(command,"供應商資料已變更，請重新載入後再儲存。");
        return Get(routeNo);
    }

    public void Approve(string vendorNo, long dataStamp, string actor)
    {
        ValidateActor(actor);
        using var command = dataSource.CreateCommand("""
            UPDATE "YMES".tbleqpvendor
            SET issuestate=2,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now()
            WHERE lower(vendorno)=lower(@no) AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
            """);
        AddVarchar(command,"actor",actor.Trim()); AddVarchar(command,"no",vendorNo.Trim()); AddNumeric(command,"stamp",dataStamp);
        ExecuteOptimistic(command,"只有未簽核且資料版本未變更的供應商可以核准。");
    }

    public void Delete(string vendorNo, int issueState, long dataStamp)
    {
        var no = Required(vendorNo, "供應商編號", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = Get(connection, transaction, no, true);
        if (current.IssueState != issueState) throw new EquipmentRuleException(409, "供應商狀態已變更，請重新載入後再操作。");
        if (current.IssueState is not (0 or 2 or -1)) throw new EquipmentRuleException(409, "只有未簽核、已簽核或不使用的供應商可以刪除。");
        EnsureStamp(current.DataStamp, dataStamp);
        using (var references = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM "YMES".tbleqpequipmentbasis WHERE lower(vendorno)=lower(@no))
                OR EXISTS(SELECT 1 FROM "YMES".tblmtlmaterialvendormap WHERE lower(vendorno)=lower(@no));
            """, connection, transaction))
        {
            AddVarchar(references,"no",no);
            if ((bool)(references.ExecuteScalar() ?? false))
                throw new EquipmentRuleException(409,"供應商仍被設備或物料使用，不能刪除。");
        }
        using (var children = new NpgsqlCommand("DELETE FROM \"YMES\".tbleqpvendorcontactor WHERE lower(vendorno)=lower(@no);", connection, transaction))
        { AddVarchar(children,"no",no); children.ExecuteNonQuery(); }
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tbleqpvendor WHERE lower(vendorno)=lower(@no) AND issuestate=@state AND COALESCE(datastamp,0)=@stamp;", connection, transaction))
        { AddVarchar(delete,"no",no); AddNumeric(delete,"state",(int?)issueState); AddNumeric(delete,"stamp",dataStamp); ExecuteOptimistic(delete,"供應商資料已變更，請重新載入後再操作。"); }
        transaction.Commit();
    }

    public EquipmentVendorContactorDto CreateContactor(string vendorNo, EquipmentVendorContactorWriteRequest request, string actor)
    {
        var no = Required(vendorNo,"供應商編號",20);
        if (!Same(no, request.VendorNo)) throw new EquipmentRuleException(400,"網址與資料內容的 VendorNo 必須相同。");
        var contactor = Required(request.ContactorName,"聯絡人",50);
        var fields = ValidateContactor(request);
        ValidateActor(actor);
        using var connection=dataSource.OpenConnection(); using var transaction=connection.BeginTransaction();
        _ = Get(connection,transaction,no,true);
        using var command=new NpgsqlCommand("""
            INSERT INTO "YMES".tbleqpvendorcontactor(vendorno,contactorname,telno,faxno,title,address,email,description,datastamp,creator,createdate)
            VALUES(@vendor,@name,@tel,@fax,@title,@address,@email,@description,0,@actor,now());
            """,connection,transaction);
        AddVarchar(command,"vendor",no); AddVarchar(command,"name",contactor); AddContactorFields(command,fields);
        AddVarchar(command,"actor",actor.Trim());
        try { command.ExecuteNonQuery(); }
        catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.UniqueViolation) { throw new EquipmentRuleException(409,$"供應商 {no} 已有相同聯絡人 {contactor}。"); }
        transaction.Commit();
        return Contactors(no).Single(x=>Same(x.ContactorName,contactor));
    }

    public EquipmentVendorContactorDto UpdateContactor(string vendorNo, string contactorName, EquipmentVendorContactorWriteRequest request, string actor)
    {
        var no=Required(vendorNo,"供應商編號",20); var name=Required(contactorName,"聯絡人",50);
        if(!Same(no,request.VendorNo)||!Same(name,request.ContactorName)) throw new EquipmentRuleException(400,"更新聯絡人時不可變更 VendorNo 或 ContactorName。");
        var fields=ValidateContactor(request); ValidateActor(actor);
        using var connection=dataSource.OpenConnection(); using var transaction=connection.BeginTransaction();
        _ = Get(connection,transaction,no,true);
        using var command=new NpgsqlCommand("""
            UPDATE "YMES".tbleqpvendorcontactor
            SET telno=@tel,faxno=@fax,title=@title,address=@address,email=@email,description=@description,
                datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now()
            WHERE lower(vendorno)=lower(@vendor) AND lower(contactorname)=lower(@name) AND COALESCE(datastamp,0)=@stamp;
            """,connection,transaction);
        AddVarchar(command,"vendor",no); AddVarchar(command,"name",name); AddContactorFields(command,fields); AddVarchar(command,"actor",actor.Trim()); AddNumeric(command,"stamp",request.DataStamp);
        ExecuteOptimistic(command,"聯絡人資料已變更，請重新載入後再儲存。");
        transaction.Commit();
        return Contactors(no).Single(x=>Same(x.ContactorName,name));
    }

    public void DeleteContactor(string vendorNo, string contactorName, long dataStamp)
    {
        var no=Required(vendorNo,"供應商編號",20); var name=Required(contactorName,"聯絡人",50);
        using var connection=dataSource.OpenConnection(); using var transaction=connection.BeginTransaction();
        _ = Get(connection,transaction,no,true);
        using var command=new NpgsqlCommand("DELETE FROM \"YMES\".tbleqpvendorcontactor WHERE lower(vendorno)=lower(@vendor) AND lower(contactorname)=lower(@name) AND COALESCE(datastamp,0)=@stamp;",connection,transaction);
        AddVarchar(command,"vendor",no); AddVarchar(command,"name",name); AddNumeric(command,"stamp",dataStamp);
        ExecuteOptimistic(command,"聯絡人資料已變更，請重新載入後再操作。");
        transaction.Commit();
    }

    private static EquipmentVendorDto Get(NpgsqlConnection connection,NpgsqlTransaction transaction,string vendorNo,bool forUpdate)
    {
        using var command=new NpgsqlCommand($"SELECT {VendorColumns} FROM \"YMES\".tbleqpvendor WHERE lower(vendorno)=lower(@no) LIMIT 1{(forUpdate?" FOR UPDATE":"")};",connection,transaction);
        AddVarchar(command,"no",vendorNo); using var reader=command.ExecuteReader(); return reader.Read()?MapVendor(reader):throw NotFound(vendorNo);
    }

    private static EquipmentVendorDto MapVendor(NpgsqlDataReader r)=>new()
    {
        VendorNo=r.GetString(0),VendorName=r.GetString(1),Description=r.IsDBNull(2)?null:r.GetString(2),IssueState=r.GetInt32(3),
        ErpNo=r.IsDBNull(4)?null:r.GetString(4),DataStamp=r.GetInt64(5),Creator=r.IsDBNull(6)?null:r.GetString(6),
        CreatedAt=r.IsDBNull(7)?null:r.GetDateTime(7),Reviser=r.IsDBNull(8)?null:r.GetString(8),RevisedAt=r.IsDBNull(9)?null:r.GetDateTime(9),
    };

    private static EquipmentVendorContactorDto MapContactor(NpgsqlDataReader r)=>new()
    {
        VendorNo=r.GetString(0),ContactorName=r.GetString(1),TelNo=r.IsDBNull(2)?null:r.GetString(2),FaxNo=r.IsDBNull(3)?null:r.GetString(3),
        Title=r.IsDBNull(4)?null:r.GetString(4),Address=r.IsDBNull(5)?null:r.GetString(5),Email=r.IsDBNull(6)?null:r.GetString(6),
        Description=r.IsDBNull(7)?null:r.GetString(7),DataStamp=r.GetInt64(8),Creator=r.IsDBNull(9)?null:r.GetString(9),
        CreatedAt=r.IsDBNull(10)?null:r.GetDateTime(10),Reviser=r.IsDBNull(11)?null:r.GetString(11),RevisedAt=r.IsDBNull(12)?null:r.GetDateTime(12),
    };

    private static (string? Tel,string? Fax,string? Title,string? Address,string? Email,string? Description) ValidateContactor(EquipmentVendorContactorWriteRequest x)=>(
        Optional(x.TelNo,20,"電話"),Optional(x.FaxNo,20,"傳真"),Optional(x.Title,20,"職稱"),Optional(x.Address,50,"地址"),Optional(x.Email,50,"Email"),Optional(x.Description,255,"說明"));

    private static void AddContactorFields(NpgsqlCommand c,(string? Tel,string? Fax,string? Title,string? Address,string? Email,string? Description) x)
    { AddVarchar(c,"tel",x.Tel);AddVarchar(c,"fax",x.Fax);AddVarchar(c,"title",x.Title);AddVarchar(c,"address",x.Address);AddVarchar(c,"email",x.Email);AddVarchar(c,"description",x.Description); }

    private static string Required(string? value,string label,int max)
    { var text=value?.Trim()??"";if(text.Length==0)throw new EquipmentRuleException(400,$"{label}不可空白。");if(text.Length>max)throw new EquipmentRuleException(400,$"{label}不可超過 {max} 個字元。");return text; }
    private static string? Optional(string? value,int max,string label)
    { var text=value?.Trim();if(string.IsNullOrEmpty(text))return null;if(text.Length>max)throw new EquipmentRuleException(400,$"{label}不可超過 {max} 個字元。");return text; }
    private static void ValidateActor(string actor){if(string.IsNullOrWhiteSpace(actor)||actor.Trim().Length>10)throw new EquipmentRuleException(400,"登入使用者編號長度不符合設備供應商資料欄位。");}
    private static void EnsureEditable(EquipmentVendorDto x,long stamp){EnsureStamp(x.DataStamp,stamp);if(x.IssueState!=0)throw new EquipmentRuleException(409,"只有未簽核供應商可以修改。");}
    private static void EnsureStamp(long actual,long requested){if(actual!=requested)throw new EquipmentRuleException(409,"資料版本已變更，請重新載入後再操作。");}
    private static void ExecuteOptimistic(NpgsqlCommand c,string message){if(c.ExecuteNonQuery()==0)throw new EquipmentRuleException(409,message);}
    private static void AddVarchar(NpgsqlCommand c,string name,string? value)=>c.Parameters.Add(name,NpgsqlDbType.Varchar).Value=(object?)value??DBNull.Value;
    private static void AddNumeric(NpgsqlCommand c,string name,int? value)=>c.Parameters.Add(name,NpgsqlDbType.Numeric).Value=(object?)value??DBNull.Value;
    private static void AddNumeric(NpgsqlCommand c,string name,long value)=>c.Parameters.Add(name,NpgsqlDbType.Numeric).Value=value;
    private static bool Same(string? x,string? y)=>string.Equals(x?.Trim(),y?.Trim(),StringComparison.OrdinalIgnoreCase);
    private static EquipmentRuleException NotFound(string no)=>new(404,$"找不到設備供應商 {no}。");
}
