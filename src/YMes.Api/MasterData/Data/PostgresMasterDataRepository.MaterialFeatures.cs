using MES.Contracts;
using Npgsql;

namespace MES.Api.Data;

public sealed partial class PostgresMasterDataRepository
{
    public IReadOnlyList<MaterialPropertyDto> GetMaterialProperties(string no)
    {
        EnsureMaterialExists(no);
        using var command = _dataSource.CreateCommand("""
            SELECT p.propertyno, COALESCE(p.defaultvalue,''), COALESCE(p.propertysequence,0)::int,
                   COALESCE(p.issuestate,0)::int, COALESCE(e.controlled,0)::int,
                   COALESCE(e.elementname,''), COALESCE(p.datastamp,0)::int
            FROM "YMES".tblmtlmaterialproperty p
            LEFT JOIN "YMES".tblsyselementbasis e ON lower(e.elementno)=lower(p.propertyno)
            WHERE lower(p.material)=lower(@no)
            ORDER BY p.propertysequence NULLS LAST, p.propertyno;
            """);
        AddVarchar(command, "no", no.Trim());
        using var reader = command.ExecuteReader();
        var rows = new List<MaterialPropertyDto>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetString(5), reader.GetInt32(6)));
        return rows;
    }

    public MaterialPropertyDto SaveMaterialProperty(string no, MaterialPropertyWriteRequest request, string actor)
    {
        ValidateActor(actor); EnsureMaterialOpen(no);
        var propertyNo = RequiredCode(request.PropertyNo, "屬性編號", 20);
        var value = Optional(request.Value, 255, "屬性值");
        if (request.Sequence is < 0 or > 99) throw BadRequest("屬性順序須介於 0 到 99。");
        using var command = _dataSource.CreateCommand("""
            INSERT INTO "YMES".tblmtlmaterialproperty
                (material,propertyno,defaultvalue,propertysequence,issuestate,datastamp,creator,createdate)
            VALUES(@material,@property,@value,@sequence,0,1,@actor,now())
            ON CONFLICT (material,propertyno) DO UPDATE SET
                defaultvalue=EXCLUDED.defaultvalue, propertysequence=EXCLUDED.propertysequence,
                datastamp=COALESCE("YMES".tblmtlmaterialproperty.datastamp,0)+1,
                reviser=@actor,revisedate=now()
            WHERE "YMES".tblmtlmaterialproperty.issuestate=0
              AND COALESCE("YMES".tblmtlmaterialproperty.datastamp,0)=@stamp;
            """);
        AddVarchar(command, "material", no.Trim()); AddVarchar(command, "property", propertyNo); AddVarchar(command, "value", value);
        AddNumeric(command, "sequence", request.Sequence); AddVarchar(command, "actor", actor.Trim()); AddNumeric(command, "stamp", request.DataStamp);
        try { if (command.ExecuteNonQuery() == 0) throw Conflict("屬性已核准或資料版本已變更。"); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { throw Conflict($"屬性 {propertyNo} 已存在。"); }
        return GetMaterialProperties(no).Single(x => Same(x.PropertyNo, propertyNo));
    }

    public void DeleteMaterialProperty(string no, string propertyNo, int dataStamp, string actor)
    {
        ValidateActor(actor); EnsureMaterialOpen(no);
        using var command = _dataSource.CreateCommand("""
            DELETE FROM "YMES".tblmtlmaterialproperty
            WHERE lower(material)=lower(@no) AND lower(propertyno)=lower(@property)
              AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
            """);
        AddVarchar(command, "no", no.Trim()); AddVarchar(command, "property", propertyNo.Trim()); AddNumeric(command, "stamp", dataStamp);
        ExecuteOptimisticUpdate(command, "屬性已核准或資料版本已變更。");
    }

    public MaterialPropertyDto ApproveMaterialProperty(string no, string propertyNo, int dataStamp, string actor)
    {
        ValidateActor(actor); EnsureMaterialOpen(no);
        using var command = _dataSource.CreateCommand("""
            UPDATE "YMES".tblmtlmaterialproperty
            SET issuestate=2,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now()
            WHERE lower(material)=lower(@no) AND lower(propertyno)=lower(@property)
              AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
            """);
        AddVarchar(command,"actor",actor.Trim()); AddVarchar(command,"no",no.Trim()); AddVarchar(command,"property",propertyNo.Trim()); AddNumeric(command,"stamp",dataStamp);
        ExecuteOptimisticUpdate(command,"只有未簽核且資料版本未變更的屬性可以核准。");
        return GetMaterialProperties(no).Single(x=>Same(x.PropertyNo,propertyNo));
    }

    public IReadOnlyList<MaterialQCItemDto> GetMaterialQCAssigned(string no)
    {
        EnsureMaterialExists(no);
        using var command = _dataSource.CreateCommand("""
            SELECT qcitemno, qcitemtype::int
            FROM "YMES".tblmtlqcitem WHERE lower(mtlno)=lower(@no) ORDER BY qcitemtype,qcitemno;
            """);
        AddVarchar(command, "no", no.Trim()); using var reader = command.ExecuteReader();
        var rows = new List<MaterialQCItemDto>(); while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetInt32(1))); return rows;
    }

    public MaterialQCItemsDto GetMaterialQCItems(string no)
    {
        var material = GetMaterial(no);
        var assigned = GetMaterialQCAssigned(no);
        using var command = _dataSource.CreateCommand("""
            SELECT qcitemno, qcitemtype::int FROM "YMES".tblmtltypeqcitem
            WHERE lower(materialtype)=lower(@type) ORDER BY qcitemtype,qcitemno;
            """);
        AddVarchar(command, "type", material.MaterialType); using var reader = command.ExecuteReader();
        var available = new List<MaterialQCItemDto>(); while (reader.Read()) available.Add(new(reader.GetString(0), reader.GetInt32(1)));
        foreach (var row in assigned) if (!available.Any(x => Same(x.QCItemNo, row.QCItemNo))) available.Add(row);
        return new(available, assigned);
    }

    public void SaveMaterialQCItems(string no, MaterialQCItemsWriteRequest request, string actor)
    {
        ValidateActor(actor); EnsureMaterialExists(no);
        if (request.Items.Count > 500) throw BadRequest("單一物料最多設定 500 個 QC Item。");
        var items = request.Items.Select(x => new MaterialQCItemDto(RequiredCode(x.QCItemNo, "QC Item 編號", 25), x.QCItemType)).ToList();
        if (items.Any(x => x.QCItemType is < 0 or > 2)) throw BadRequest("QC Item 類型須為 0、1 或 2。");
        if (items.Select(x => x.QCItemNo).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Count) throw BadRequest("QC Item 編號不可重複。");
        using var connection = _dataSource.OpenConnection(); using var transaction = connection.BeginTransaction();
        var material = GetMaterial(connection, transaction, no, true);
        if (material.IssueState != 0) throw Conflict("只有未簽核物料可以修改 QC Item。");
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblmtlqcitem WHERE lower(mtlno)=lower(@no);", connection, transaction))
        { AddVarchar(delete, "no", no.Trim()); delete.ExecuteNonQuery(); }
        foreach (var item in items)
        {
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblmtlqcitem(mtlno,qcitemno,qcitemtype,reviser,revisedate) VALUES(@no,@item,@type,@actor,now());", connection, transaction);
            AddVarchar(insert, "no", no.Trim()); AddVarchar(insert, "item", item.QCItemNo); AddNumeric(insert, "type", item.QCItemType); AddVarchar(insert, "actor", actor.Trim()); insert.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public IReadOnlyList<MaterialVendorDto> GetMaterialVendors(string no)
    {
        EnsureMaterialExists(no);
        using var directoryCommand = _dataSource.CreateCommand("""SELECT to_regclass('"YMES".tbleqpvendor') IS NOT NULL;""");
        var hasVendorDirectory = directoryCommand.ExecuteScalar() is true;
        var sql = hasVendorDirectory
            ? "SELECT v.materialno,v.vendorno,COALESCE(d.vendorname,''),COALESCE(v.description,''),COALESCE(v.datastamp,0)::int FROM \"YMES\".tblmtlmaterialvendormap v LEFT JOIN \"YMES\".tbleqpvendor d ON lower(d.vendorno)=lower(v.vendorno) WHERE lower(v.materialno)=lower(@no) ORDER BY v.vendorno;"
            : "SELECT materialno,vendorno,''::varchar,COALESCE(description,''),COALESCE(datastamp,0)::int FROM \"YMES\".tblmtlmaterialvendormap WHERE lower(materialno)=lower(@no) ORDER BY vendorno;";
        using var command = _dataSource.CreateCommand(sql); AddVarchar(command, "no", no.Trim()); using var reader = command.ExecuteReader();
        var rows = new List<MaterialVendorDto>(); while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4))); return rows;
    }

    public MaterialVendorDto SaveMaterialVendor(string no, MaterialVendorWriteRequest request, string actor, bool update)
    {
        ValidateActor(actor); EnsureMaterialOpen(no);
        var vendor = RequiredCode(request.VendorNo, "供應商編號", 20); var description = Optional(request.Description, 255, "說明");
        var sql = update
            ? "UPDATE \"YMES\".tblmtlmaterialvendormap SET description=@description,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() WHERE lower(materialno)=lower(@no) AND lower(vendorno)=lower(@vendor) AND COALESCE(datastamp,0)=@stamp;"
            : "INSERT INTO \"YMES\".tblmtlmaterialvendormap(materialno,vendorno,description,datastamp,creator,createdate) VALUES(@no,@vendor,@description,1,@actor,now());";
        using var command = _dataSource.CreateCommand(sql); AddVarchar(command,"no",no.Trim()); AddVarchar(command,"vendor",vendor); AddVarchar(command,"description",description); AddVarchar(command,"actor",actor.Trim()); AddNumeric(command,"stamp",request.DataStamp);
        try { if (command.ExecuteNonQuery()==0 && update) throw Conflict("供應商對應已變更，請重新載入。"); }
        catch (PostgresException ex) when (ex.SqlState==PostgresErrorCodes.UniqueViolation) { throw Conflict($"供應商 {vendor} 已對應此物料。"); }
        return GetMaterialVendors(no).Single(x=>Same(x.VendorNo,vendor));
    }

    public void DeleteMaterialVendor(string no, string vendorNo, int dataStamp, string actor)
    {
        ValidateActor(actor); EnsureMaterialOpen(no);
        using var command=_dataSource.CreateCommand("DELETE FROM \"YMES\".tblmtlmaterialvendormap WHERE lower(materialno)=lower(@no) AND lower(vendorno)=lower(@vendor) AND COALESCE(datastamp,0)=@stamp;");
        AddVarchar(command,"no",no.Trim()); AddVarchar(command,"vendor",vendorNo.Trim()); AddNumeric(command,"stamp",dataStamp); ExecuteOptimisticUpdate(command,"供應商對應已變更，請重新載入。");
    }

    public MaterialGumHourDto GetMaterialGumHour(string no)
    {
        var material=GetMaterial(no);
        using var command=_dataSource.CreateCommand("""
            SELECT materialtype,materialno,accmin,accmax,refrozenflag,frozenmin,reforzentimes,reacclimateflag,reaccmin,reaccmax,COALESCE(datastamp,0)::int
            FROM "YMES".tblmtltypeacclimation
            WHERE lower(materialtype)=lower(@type) AND (lower(materialno)=lower(@no) OR materialno='*')
            ORDER BY CASE WHEN lower(materialno)=lower(@no) THEN 0 ELSE 1 END LIMIT 1;
            """);
        AddVarchar(command,"type",material.MaterialType); AddVarchar(command,"no",material.MaterialNo); using var r=command.ExecuteReader();
        return r.Read()?new(r.GetString(0),r.GetString(1),r.GetDecimal(2),r.GetDecimal(3),Convert.ToInt32(r.GetValue(4))==1,r.IsDBNull(5)?null:r.GetDecimal(5),Convert.ToInt32(r.GetValue(6)),Convert.ToInt32(r.GetValue(7))==1,r.IsDBNull(8)?null:r.GetDecimal(8),r.IsDBNull(9)?null:r.GetDecimal(9),r.GetInt32(10)):
            new(material.MaterialType,material.MaterialNo,0,0,false,null,0,false,null,null,0);
    }

    public MaterialGumHourDto SaveMaterialGumHour(string no, MaterialGumHourWriteRequest request, string actor)
    {
        ValidateActor(actor); var material=GetMaterial(no); EnsureMaterialOpen(no); ValidateGumHour(request);
        using var command=_dataSource.CreateCommand("""
            INSERT INTO "YMES".tblmtltypeacclimation(materialtype,materialno,accmin,accmax,refrozenflag,frozenmin,reforzentimes,reacclimateflag,reaccmin,reaccmax,datastamp,creator,createdate)
            VALUES(@type,@no,@amin,@amax,@refrozen,@frozen,@times,@reacclimate,@rmin,@rmax,1,@actor,now())
            ON CONFLICT(materialtype,materialno) DO UPDATE SET accmin=EXCLUDED.accmin,accmax=EXCLUDED.accmax,refrozenflag=EXCLUDED.refrozenflag,frozenmin=EXCLUDED.frozenmin,reforzentimes=EXCLUDED.reforzentimes,reacclimateflag=EXCLUDED.reacclimateflag,reaccmin=EXCLUDED.reaccmin,reaccmax=EXCLUDED.reaccmax,datastamp=COALESCE("YMES".tblmtltypeacclimation.datastamp,0)+1,reviser=@actor,revisedate=now()
            WHERE COALESCE("YMES".tblmtltypeacclimation.datastamp,0)=@stamp;
            """);
        AddVarchar(command,"type",material.MaterialType); AddVarchar(command,"no",material.MaterialNo); AddNumeric(command,"amin",request.AccMin); AddNumeric(command,"amax",request.AccMax); AddNumeric(command,"refrozen",request.RefrozenFlag?1:0); AddNumeric(command,"frozen",request.RefrozenFlag?request.FrozenMin:null); AddNumeric(command,"times",request.RefrozenFlag?request.ReForzenTimes:0); AddNumeric(command,"reacclimate",request.ReacclimateFlag?1:0); AddNumeric(command,"rmin",request.ReacclimateFlag?request.ReAccMin:null); AddNumeric(command,"rmax",request.ReacclimateFlag?request.ReAccMax:null); AddVarchar(command,"actor",actor.Trim()); AddNumeric(command,"stamp",request.DataStamp);
        ExecuteOptimisticUpdate(command,"GumHour 設定已變更，請重新載入。"); return GetMaterialGumHour(no);
    }

    public IReadOnlyList<MaterialOptionDto> GetMaterialTypes()
    {
        using var command=_dataSource.CreateCommand("SELECT materialtype,COALESCE(materialtypename,materialtype) FROM \"YMES\".tblmtlmaterialtype WHERE issuestate=2 ORDER BY materialtype;");
        using var r=command.ExecuteReader(); var rows=new List<MaterialOptionDto>(); while(r.Read()) rows.Add(new(r.GetString(0),r.GetString(1))); return rows;
    }

    public IReadOnlyList<MaterialOptionDto> GetMaterialUnits()
    {
        using var command=_dataSource.CreateCommand("SELECT unitno,unitno,unittype FROM \"YMES\".tblsysunitbasis WHERE issuestate<>-1 ORDER BY unitno;");
        using var r=command.ExecuteReader(); var rows=new List<MaterialOptionDto>(); while(r.Read()) rows.Add(new(r.GetString(0),r.GetString(1),r.GetString(2))); return rows;
    }

    public IReadOnlyList<MaterialOptionDto> GetMaterialUnitTypes()
    {
        using var command=_dataSource.CreateCommand("SELECT DISTINCT unittype,unittype FROM \"YMES\".tblsysunitbasis WHERE issuestate<>-1 ORDER BY unittype;");
        using var r=command.ExecuteReader(); var rows=new List<MaterialOptionDto>(); while(r.Read()) rows.Add(new(r.GetString(0),r.GetString(1))); return rows;
    }

    public string ResolveMaterialUnitType(string unitNo)
    {
        using var command=_dataSource.CreateCommand("SELECT unittype FROM \"YMES\".tblsysunitbasis WHERE lower(unitno)=lower(@unit) AND issuestate<>-1 LIMIT 1;");
        AddVarchar(command,"unit",unitNo.Trim()); return command.ExecuteScalar() as string ?? "";
    }

    private void EnsureMaterialExists(string no) { _ = GetMaterial(no); }
    private void EnsureMaterialOpen(string no) { var x=GetMaterial(no); if(x.IssueState!=0) throw Conflict("只有未簽核物料可以修改子資料。"); }
    private static void ValidateGumHour(MaterialGumHourWriteRequest x)
    {
        static bool Fits(decimal? v)=>!v.HasValue||(v.Value>=0&&v.Value<=999.99m&&decimal.Round(v.Value,2)==v.Value);
        if(!Fits(x.AccMin)||!Fits(x.AccMax)||!Fits(x.FrozenMin)||!Fits(x.ReAccMin)||!Fits(x.ReAccMax)) throw BadRequest("GumHour 時間欄位須介於 0 到 999.99，最多兩位小數。");
        if((x.AccMin!=0||x.AccMax!=0)&&x.AccMax<=x.AccMin) throw BadRequest("ACCMax 必須大於 ACCMin。");
        if(x.RefrozenFlag&&(x.FrozenMin is null||x.ReForzenTimes is < 1 or > 999)) throw BadRequest("啟用再冷凍時，FROZENMIN 與再冷凍次數必填。");
        if(x.ReacclimateFlag&&(x.ReAccMin is null||x.ReAccMax is null||((x.ReAccMin!=0||x.ReAccMax!=0)&&x.ReAccMax<=x.ReAccMin))) throw BadRequest("啟用再回溫時，REACCMIN/REACCMAX 必填且上限大於下限。");
    }
}
