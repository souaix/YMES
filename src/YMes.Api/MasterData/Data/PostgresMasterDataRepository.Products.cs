using MES.Contracts;
using Npgsql;

namespace MES.Api.Data;

public sealed partial class PostgresMasterDataRepository
{
    private const string ProductColumns = @"
        productno,productversion,productname,producttype,COALESCE(unittype,''),COALESCE(unitno,''),
        COALESCE(description,''),COALESCE(specno,''),COALESCE(productcode,''),COALESCE(serialtypeno_lot,''),
        COALESCE(serialtypeno_comp,''),COALESCE(lotstdqty,0)::numeric,COALESCE(erpno,''),
        issuestate::int,curversion=1,datastamp,createdate,sap_plnal,creator,reviser,revisedate
        ";

    public IReadOnlyList<ProductDto> ListProducts(string? keyword,int? issueState)
    {
        using var command=_dataSource.CreateCommand($@"
            SELECT {ProductColumns} FROM ""YMES"".tblprdproductbasis
            WHERE (@state IS NULL OR issuestate=@state) AND (@keyword IS NULL OR productno ILIKE @keyword OR productname ILIKE @keyword)
            ORDER BY productno,productversion;
            ");
        AddNumeric(command,"state",issueState);AddVarchar(command,"keyword",string.IsNullOrWhiteSpace(keyword)?null:$"%{keyword.Trim()}%");
        using var reader=command.ExecuteReader();var rows=new List<ProductDto>();while(reader.Read())rows.Add(MapProduct(reader));return rows;
    }

    public ProductDto GetProduct(string no,string version)
    {
        using var connection=_dataSource.OpenConnection();using var transaction=connection.BeginTransaction();
        var context=ReadProductVersionContext(connection,transaction,no,version,forUpdate:false);
        var result=ReadProductDto(connection,transaction,context);
        transaction.Commit();return result;
    }

    public ProductDto CreateProduct(ProductWriteRequest request,string actorUserNo)
    {
        ValidateProduct(request);ValidateActor(actorUserNo);
        ValidateProductSapPlnal(request.SapPlnal);
        var no=RequiredCode(request.ProductNo,"產品編號",50);
        var version=RequiredCode(request.ProductVersion,"產品版本",5);
        using var connection=_dataSource.OpenConnection();using var transaction=connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        YMes.Api.Products.PrdProductTypeReference.LockCreationNamespace(connection,transaction,no);
        YMes.Api.Products.PrdProductTypeReference.EnsureNewProductNamespace(connection,transaction,no);
        LockProductCreationIdentity(connection,transaction,no,version);
        EnsureNewProductIdentity(connection,transaction,no,version);
        var productType=YMes.Api.Products.PrdProductTypeReference.LockApproved(connection,transaction,request.ProductType);
        using(var conflict=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblmtlmaterialbasis WHERE lower(materialno)=lower(@no));",connection,transaction))
        {AddVarchar(conflict,"no",no);if((bool)(conflict.ExecuteScalar()??false))throw Conflict("產品編號不可與物料編號重複。");}
        using var command=new NpgsqlCommand(@"
            INSERT INTO ""YMES"".tblprdproductbasis
              (productno,productversion,productname,producttype,issuestate,creator,createdate,description,curversion,
               unitno,unittype,specno,cartonqty,palletqty,productcode,serialtypeno_lot,serialtypeno_comp,lotstdqty,erpno,datastamp,sap_plnal)
            VALUES(@no,@version,@name,@type,0,@actor,(now() AT TIME ZONE 'UTC'),@description,0,@unitNo,@unitType,@spec,0,0,@code,@lotSerial,@componentSerial,@lotQty,@erp,1,@sapPlnal);
            ",connection,transaction);
        AddProductParameters(command,request,productType);AddVarchar(command,"actor",actorUserNo.Trim());ExecuteInsert(command,$"產品 {no}/{version} 已存在。");
        var context=ReadProductVersionContext(connection,transaction,no,version,forUpdate:true);
        var result=ReadProductDto(connection,transaction,context);
        transaction.Commit();return result;
    }

    public ProductDto UpdateProduct(string no,string version,ProductWriteRequest request,string actorUserNo)
    {
        ValidateProduct(request,validateType:false);ValidateActor(actorUserNo);
        using var connection=_dataSource.OpenConnection();using var transaction=connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        var current=GetProductForUpdate(connection,transaction,no,version);
        ValidateProductSapPlnal(request.SapPlnal,current.SapPlnal);
        EnsureProductExpected(current,request.IssueState,request.DataStamp);
        if(current.IssueState!=0)throw Conflict("只有 UNFROZEN 產品可以編輯。");
        EnsureProductStampCanAdvance(current.DataStamp);
        if(!Same(current.ProductNo,request.ProductNo)||!Same(current.ProductVersion,request.ProductVersion))throw BadRequest("更新時不可變更 ProductNo/ProductVersion。");
        var productType=YMes.Api.Products.PrdProductTypeReference.ResolveForUpdate(connection,transaction,request.ProductType,current.ProductType);
        using var command=new NpgsqlCommand(@"
            UPDATE ""YMES"".tblprdproductbasis SET productname=@name,producttype=@type,unittype=@unitType,unitno=@unitNo,
              description=@description,specno=@spec,productcode=@code,serialtypeno_lot=@lotSerial,serialtypeno_comp=@componentSerial,
              lotstdqty=@lotQty,erpno=@erp,sap_plnal=COALESCE(@sapPlnal,sap_plnal),datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC')
            WHERE productno=@no AND productversion=@version AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
            ",connection,transaction);
        AddProductParameters(command,request,productType);AddVarchar(command,"actor",actorUserNo.Trim());AddVarchar(command,"no",current.ProductNo);AddVarchar(command,"version",current.ProductVersion);AddNumeric(command,"state",request.IssueState);AddNumeric(command,"stamp",request.DataStamp);
        ExecuteOptimisticUpdate(command,"產品資料已變更，請重新載入後再儲存。");
        var result=ReadProductDto(connection,transaction,new(current.ProductNo,current.ProductVersion,current.IssueState,current.DataStamp));
        transaction.Commit();return result;
    }

    public ProductDto ApproveProduct(string no,string version,int? issueState,decimal? dataStamp,string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var connection=_dataSource.OpenConnection();using var transaction=connection.BeginTransaction();
        var product=GetProductForUpdate(connection,transaction,no,version);
        EnsureProductExpected(product,issueState,dataStamp);
        if(product.IssueState!=0)throw Conflict("只有 UNFROZEN 產品可以核准。");
        EnsureProductStampCanAdvance(product.DataStamp);
        using(var link=new NpgsqlCommand(@"
            SELECT EXISTS(SELECT 1 FROM ""YMES"".ym_product_process WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version) AND default_process);
            ",connection,transaction))
        {AddVarchar(link,"no",product.ProductNo);AddVarchar(link,"version",product.ProductVersion);if(!(bool)(link.ExecuteScalar()??false))throw Conflict("產品至少需要一條預設流程才能核准。");}
        using var command=new NpgsqlCommand(@"
            UPDATE ""YMES"".tblprdproductbasis SET issuestate=2,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC')
            WHERE productno=@no AND productversion=@version AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
            ",connection,transaction);
        AddVarchar(command,"actor",actorUserNo.Trim());AddVarchar(command,"no",product.ProductNo);AddVarchar(command,"version",product.ProductVersion);AddNumeric(command,"state",issueState);AddNumeric(command,"stamp",dataStamp);ExecuteOptimisticUpdate(command,"只有 UNFROZEN 且資料未變更的產品可以核准。");
        var result=ReadProductDto(connection,transaction,new(product.ProductNo,product.ProductVersion,product.IssueState,product.DataStamp));
        transaction.Commit();return result;
    }

    public ProductDto SetCurrentProduct(string no,string version,int? issueState,decimal? dataStamp,string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var connection=_dataSource.OpenConnection();using var transaction=connection.BeginTransaction();
        // Switching current versions can touch several parents. Take the same
        // deterministic product/version lock order before selecting the target.
        using(var guard=new NpgsqlCommand("SELECT productno FROM \"YMES\".tblprdproductbasis WHERE lower(productno)=lower(@no) ORDER BY productno,productversion FOR UPDATE;",connection,transaction))
        {AddVarchar(guard,"no",RequiredCode(no,"產品編號",50));using var reader=guard.ExecuteReader();while(reader.Read()){} }
        var current=GetProductForUpdate(connection,transaction,no,version);
        EnsureProductExpected(current,issueState,dataStamp);
        if(current.IssueState!=2)throw Conflict("只有 ACTIVE 產品版本可以設為目前版本。");
        EnsureProductStampCanAdvance(current.DataStamp);
        using(var stamps=new NpgsqlCommand("SELECT datastamp FROM \"YMES\".tblprdproductbasis WHERE lower(productno)=lower(@no) AND curversion=1 AND lower(productversion)<>lower(@version);",connection,transaction))
        {AddVarchar(stamps,"no",current.ProductNo);AddVarchar(stamps,"version",current.ProductVersion);using var reader=stamps.ExecuteReader();while(reader.Read())EnsureProductStampCanAdvance(reader.IsDBNull(0)?null:reader.GetDecimal(0));}
        using(var clear=new NpgsqlCommand(@"
            UPDATE ""YMES"".tblprdproductbasis SET curversion=0,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC') WHERE lower(productno)=lower(@no) AND curversion=1 AND lower(productversion)<>lower(@version);
            ",connection,transaction))
        {AddVarchar(clear,"actor",actorUserNo.Trim());AddVarchar(clear,"no",current.ProductNo);AddVarchar(clear,"version",current.ProductVersion);clear.ExecuteNonQuery();}
        using(var set=new NpgsqlCommand(@"
            UPDATE ""YMES"".tblprdproductbasis SET curversion=1,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC') WHERE productno=@no AND productversion=@version AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;
            ",connection,transaction))
        {AddVarchar(set,"actor",actorUserNo.Trim());AddVarchar(set,"no",current.ProductNo);AddVarchar(set,"version",current.ProductVersion);AddNumeric(set,"state",issueState);AddNumeric(set,"stamp",dataStamp);ExecuteOptimisticUpdate(set,"產品版本已變更，請重新載入後再操作。");}
        var result=ReadProductDto(connection,transaction,new(current.ProductNo,current.ProductVersion,current.IssueState,current.DataStamp));
        transaction.Commit();return result;
    }

    public ProductDto CreateProductVersion(string no,string version,string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var connection=_dataSource.OpenConnection();using var transaction=connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        var old=ReadProductVersionContext(connection,transaction,no,version,forUpdate:true);
        if(!int.TryParse(old.ProductVersion,out var number))throw BadRequest("產品版本格式不是數字。");
        var next=(number+1).ToString(new string('0',Math.Max(2,old.ProductVersion.Length)));if(next.Length>5)throw BadRequest("產品版本已達資料表可用長度，無法建立下一版。");
        LockProductCreationIdentity(connection,transaction,old.ProductNo,next);
        EnsureNewProductIdentity(connection,transaction,old.ProductNo,next);
        var productType=YMes.Api.Products.PrdProductTypeReference.LockApproved(connection,transaction,ReadProductDto(connection,transaction,old).ProductType);
        using(var command=new NpgsqlCommand(@"
            INSERT INTO ""YMES"".tblprdproductbasis(productno,productversion,productname,producttype,issuestate,creator,createdate,description,curversion,unitno,unittype,specno,cartonqty,palletqty,productcode,serialtypeno_lot,serialtypeno_comp,lotstdqty,erpno,datastamp,sap_plnal)
            SELECT productno,@next,productname,@type,0,@actor,(now() AT TIME ZONE 'UTC'),description,0,unitno,unittype,specno,cartonqty,palletqty,productcode,serialtypeno_lot,serialtypeno_comp,lotstdqty,erpno,1,sap_plnal
            FROM ""YMES"".tblprdproductbasis WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version);
            ",connection,transaction))
        {AddVarchar(command,"next",next);AddVarchar(command,"type",productType);AddVarchar(command,"actor",actorUserNo.Trim());AddVarchar(command,"no",old.ProductNo);AddVarchar(command,"version",old.ProductVersion);ExecuteInsert(command,$"產品 {no}/{next} 已存在。");}
        using(var command=new NpgsqlCommand(@"
            INSERT INTO ""YMES"".ym_product_process(productno,productversion,motypeno,production_segment_no,segment_order,processno,process_version_selector,default_process,have_component,have_level)
            SELECT productno,@next,motypeno,production_segment_no,segment_order,processno,process_version_selector,default_process,have_component,have_level FROM ""YMES"".ym_product_process WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version);
            ",connection,transaction)){AddVarchar(command,"next",next);AddVarchar(command,"no",old.ProductNo);AddVarchar(command,"version",old.ProductVersion);command.ExecuteNonQuery();}
        using(var command=new NpgsqlCommand(@"
            INSERT INTO ""YMES"".ym_product_bom(id,productno,productversion,component_type,component_no,input_operation_no,standard_qty,put_in_place_type,specified,decrease_rate,description)
            SELECT gen_random_uuid(),productno,@next,component_type,component_no,input_operation_no,standard_qty,put_in_place_type,specified,decrease_rate,description FROM ""YMES"".ym_product_bom WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version);
            ",connection,transaction)){AddVarchar(command,"next",next);AddVarchar(command,"no",old.ProductNo);AddVarchar(command,"version",old.ProductVersion);command.ExecuteNonQuery();}
        var target=ReadProductVersionContext(connection,transaction,old.ProductNo,next,forUpdate:true);
        YMes.Api.Products.PrdRecipeGroupRepository.CopyAssignmentsForVersion(connection,transaction,old.ProductNo,old.ProductVersion,next);
        YMes.Api.Products.PrdProgramRepository.CopyAssignmentsForVersion(connection,transaction,old.ProductNo,old.ProductVersion,next);
        var result=ReadProductDto(connection,transaction,target);
        transaction.Commit();return result;
    }

    public void DeleteProduct(string no,string version,int? issueState,decimal? dataStamp,string actorUserNo)
    {
        ValidateActor(actorUserNo);using var connection=_dataSource.OpenConnection();using var transaction=connection.BeginTransaction();var current=GetProductForUpdate(connection,transaction,no,version);
        EnsureProductExpected(current,issueState,dataStamp);
        if(current.IssueState==2)
        {EnsureProductStampCanAdvance(current.DataStamp);using var retire=new NpgsqlCommand(@"UPDATE ""YMES"".tblprdproductbasis SET issuestate=-1,curversion=0,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC') WHERE productno=@no AND productversion=@version AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;",connection,transaction);AddVarchar(retire,"actor",actorUserNo.Trim());AddVarchar(retire,"no",current.ProductNo);AddVarchar(retire,"version",current.ProductVersion);AddNumeric(retire,"state",issueState);AddNumeric(retire,"stamp",dataStamp);ExecuteOptimisticUpdate(retire,"產品資料已變更，請重新載入後再操作。");}
        else
        {
            if(current.IssueState is not (0 or -1))throw Conflict("只有未簽核或不使用的產品版本可以實體刪除。");
            YMes.Api.Products.PrdRecipeGroupRepository.EnsureNoAssignmentsForProductDelete(connection,transaction,current.ProductNo,current.ProductVersion);
            YMes.Api.Products.PrdProgramRepository.EnsureNoAssignmentsForProductDelete(connection,transaction,current.ProductNo,current.ProductVersion);
            using(var used=new NpgsqlCommand(@"SELECT EXISTS(SELECT 1 FROM ""YMES"".ym_product_bom WHERE component_type='PRODUCT' AND lower(component_no)=lower(@no));",connection,transaction)){AddVarchar(used,"no",current.ProductNo);if((bool)(used.ExecuteScalar()??false))throw Conflict("產品仍被其他產品 BOM 使用，不能物理刪除。");}
            using var delete=new NpgsqlCommand(@"DELETE FROM ""YMES"".tblprdproductbasis WHERE productno=@no AND productversion=@version AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;",connection,transaction);AddVarchar(delete,"no",current.ProductNo);AddVarchar(delete,"version",current.ProductVersion);AddNumeric(delete,"state",issueState);AddNumeric(delete,"stamp",dataStamp);ExecuteOptimisticUpdate(delete,"產品資料已變更，請重新載入後再操作。");
        }
        transaction.Commit();
    }

    public IReadOnlyList<ProductProcessDto> GetProductProcesses(string no,string version)
    {
        _=GetProduct(no,version);using var command=_dataSource.CreateCommand(@"
            SELECT p.productno,p.productversion,p.motypeno::numeric,p.production_segment_no,p.segment_order,p.processno,p.process_version_selector,
              COALESCE(resolved.processversion,''),p.default_process,p.have_component,p.have_level
            FROM ""YMES"".ym_product_process p
            LEFT JOIN LATERAL (SELECT b.processversion FROM ""YMES"".tblprsprocessbasis b WHERE lower(b.processno)=lower(p.processno) AND b.issuestate=2 AND ((p.process_version_selector='*' AND b.curversion=1) OR lower(b.processversion)=lower(p.process_version_selector)) ORDER BY b.curversion DESC LIMIT 1) resolved ON true
            WHERE lower(p.productno)=lower(@no) AND lower(p.productversion)=lower(@version) ORDER BY p.segment_order,p.default_process DESC,p.processno;
            ");AddVarchar(command,"no",no.Trim());AddVarchar(command,"version",version.Trim());using var reader=command.ExecuteReader();var rows=new List<ProductProcessDto>();while(reader.Read())rows.Add(new(reader.GetString(0),reader.GetString(1),ToInt32(reader.GetValue(2)),reader.GetString(3),reader.GetInt32(4),reader.GetString(5),reader.GetString(6),string.IsNullOrEmpty(reader.GetString(7))?null:reader.GetString(7),reader.GetBoolean(8),reader.GetBoolean(9),reader.GetBoolean(10)));return rows;
    }

    public ProductProcessDto AddProductProcess(string no,string version,ProductProcessWriteRequest request,string actorUserNo)
    {
        ValidateActor(actorUserNo);var product=GetProduct(no,version);if(product.IssueState!=0)throw Conflict("只有 UNFROZEN 產品可以新增流程指派。");var process=ResolveActiveProcess(request.ProcessNo,request.ProcessVersionSelector);
        if(!Same(process.ProductionSegmentNo,request.ProductionSegmentNo))throw BadRequest("產品流程的製程段必須與流程主檔一致。");ValidateMoType(request.MoTypeNo);
        using var command=_dataSource.CreateCommand(@"
            INSERT INTO ""YMES"".ym_product_process(productno,productversion,motypeno,production_segment_no,segment_order,processno,process_version_selector,default_process,have_component,have_level)
            VALUES(@product,@productVersion,@mo,@segment,@order,@process,@selector,@default,@component,@level);
            ");AddVarchar(command,"product",product.ProductNo);AddVarchar(command,"productVersion",product.ProductVersion);AddNumeric(command,"mo",request.MoTypeNo);AddVarchar(command,"segment",RequiredCode(request.ProductionSegmentNo,"製程段",40));AddNumeric(command,"order",SegmentOrder(request.ProductionSegmentNo));AddVarchar(command,"process",process.ProcessNo);AddVarchar(command,"selector",NormalizeSelector(request.ProcessVersionSelector));AddBoolean(command,"default",request.DefaultProcess);AddBoolean(command,"component",SegmentHasComponent(request.ProductionSegmentNo));AddBoolean(command,"level",SegmentHasLevel(request.ProductionSegmentNo));
        try{command.ExecuteNonQuery();}catch(PostgresException e) when(e.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation){throw Conflict(e.SqlState==PostgresErrorCodes.UniqueViolation?"這條產品流程已存在，或同一組合已設定預設流程。":"產品流程參照的版本不存在。");}
        return GetProductProcesses(no,version).Single(x=>Same(x.ProcessNo,process.ProcessNo)&&x.MoTypeNo==request.MoTypeNo&&Same(x.ProductionSegmentNo,request.ProductionSegmentNo)&&Same(x.ProcessVersionSelector,NormalizeSelector(request.ProcessVersionSelector)));
    }

    public ProductProcessDto UpdateProductProcess(string no,string version,string processNo,string selector,ProductProcessWriteRequest request,string actorUserNo)
    {
        ValidateActor(actorUserNo);var product=GetProduct(no,version);if(product.IssueState!=0)throw Conflict("只有 UNFROZEN 產品可以修改流程指派。");var resolved=ResolveActiveProcess(request.ProcessNo,request.ProcessVersionSelector);var oldSelector=NormalizeSelector(selector);var newSelector=NormalizeSelector(request.ProcessVersionSelector);
        if(!Same(resolved.ProductionSegmentNo,request.ProductionSegmentNo))throw BadRequest("產品流程的製程段必須與流程主檔一致。");
        using var command=_dataSource.CreateCommand(@"
            UPDATE ""YMES"".ym_product_process SET production_segment_no=@segment,segment_order=@order,processno=@newProcess,process_version_selector=@newSelector,default_process=@default,have_component=@component,have_level=@level
            WHERE lower(productno)=lower(@product) AND lower(productversion)=lower(@productVersion) AND motypeno=@mo AND lower(production_segment_no)=lower(@oldSegment) AND lower(processno)=lower(@oldProcess) AND lower(process_version_selector)=lower(@oldSelector);
            ");AddVarchar(command,"segment",RequiredCode(request.ProductionSegmentNo,"製程段",40));AddNumeric(command,"order",SegmentOrder(request.ProductionSegmentNo));AddVarchar(command,"newProcess",resolved.ProcessNo);AddVarchar(command,"newSelector",newSelector);AddBoolean(command,"default",request.DefaultProcess);AddBoolean(command,"component",SegmentHasComponent(request.ProductionSegmentNo));AddBoolean(command,"level",SegmentHasLevel(request.ProductionSegmentNo));AddVarchar(command,"product",product.ProductNo);AddVarchar(command,"productVersion",product.ProductVersion);AddNumeric(command,"mo",request.MoTypeNo);AddVarchar(command,"oldSegment",request.ProductionSegmentNo);AddVarchar(command,"oldProcess",processNo);AddVarchar(command,"oldSelector",oldSelector);
        try{if(command.ExecuteNonQuery()==0)throw NotFound("產品流程",$"{no}/{version}/{processNo}");}catch(PostgresException e) when(e.SqlState==PostgresErrorCodes.UniqueViolation){throw Conflict("同一組合已存在或已設定另一條預設流程。");}
        return GetProductProcesses(no,version).Single(x=>Same(x.ProcessNo,resolved.ProcessNo)&&x.MoTypeNo==request.MoTypeNo&&Same(x.ProductionSegmentNo,request.ProductionSegmentNo)&&Same(x.ProcessVersionSelector,newSelector));
    }

    public void DeleteProductProcess(string no,string version,int moTypeNo,string segmentNo,string processNo,string selector,string actorUserNo)
    {ValidateActor(actorUserNo);var product=GetProduct(no,version);if(product.IssueState!=0)throw Conflict("只有 UNFROZEN 產品可以刪除流程指派。");using var command=_dataSource.CreateCommand(@"DELETE FROM ""YMES"".ym_product_process WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version) AND motypeno=@mo AND lower(production_segment_no)=lower(@segment) AND lower(processno)=lower(@process) AND lower(process_version_selector)=lower(@selector);");AddVarchar(command,"no",no.Trim());AddVarchar(command,"version",version.Trim());AddNumeric(command,"mo",moTypeNo);AddVarchar(command,"segment",segmentNo.Trim());AddVarchar(command,"process",processNo.Trim());AddVarchar(command,"selector",NormalizeSelector(selector));if(command.ExecuteNonQuery()==0)throw NotFound("產品流程",$"{no}/{version}/{processNo}");}

    public IReadOnlyList<ProductBomDto> GetBom(string no,string version)
    {_=GetProduct(no,version);using var command=_dataSource.CreateCommand(@"SELECT id::text,productno,productversion,component_type,component_no,input_operation_no,standard_qty,put_in_place_type,specified,decrease_rate,description FROM ""YMES"".ym_product_bom WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version) ORDER BY component_type,component_no,input_operation_no;");AddVarchar(command,"no",no.Trim());AddVarchar(command,"version",version.Trim());using var reader=command.ExecuteReader();var rows=new List<ProductBomDto>();while(reader.Read())rows.Add(MapBom(reader));return rows;}

    public ProductBomDto AddBom(string no,string version,ProductBomWriteRequest request,string actorUserNo)
    {
        ValidateActor(actorUserNo);var product=GetProduct(no,version);if(product.IssueState!=0)throw Conflict("只有 UNFROZEN 產品可以修改 BOM。");ValidateBom(request,product.ProductNo);
        using var command=_dataSource.CreateCommand(@"
            INSERT INTO ""YMES"".ym_product_bom(id,productno,productversion,component_type,component_no,input_operation_no,standard_qty,put_in_place_type,specified,decrease_rate,description)
            VALUES(@id,@product,@version,@type,@component,@operation,@qty,@place,@specified,@rate,@description);
            ");var id=Guid.NewGuid();AddUuid(command,"id",id);AddVarchar(command,"product",product.ProductNo);AddVarchar(command,"version",product.ProductVersion);AddVarchar(command,"type",request.ComponentType.Trim().ToUpperInvariant());AddVarchar(command,"component",RequiredCode(request.ComponentNo,"元件編號",50));AddVarchar(command,"operation",RequiredCode(request.InputOperationNo,"投入作業",20));AddNumeric(command,"qty",request.StandardQty);AddVarchar(command,"place",Optional(request.PutInPlaceType,40,"投入點類型"));AddBoolean(command,"specified",request.Specified);AddNumeric(command,"rate",request.DecreaseRate);AddVarchar(command,"description",Optional(request.Description,1000,"說明"));
        try{command.ExecuteNonQuery();}catch(PostgresException e) when(e.SqlState==PostgresErrorCodes.UniqueViolation){throw Conflict("同一產品、作業與 component 的 BOM 已存在。");}
        return GetBom(no,version).Single(x=>x.Id.Equals(id.ToString(),StringComparison.OrdinalIgnoreCase));
    }

    public void DeleteBom(string no,string version,string id,string actorUserNo)
    {ValidateActor(actorUserNo);var product=GetProduct(no,version);if(product.IssueState!=0)throw Conflict("只有 UNFROZEN 產品可以刪除 BOM。");using var command=_dataSource.CreateCommand(@"DELETE FROM ""YMES"".ym_product_bom WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version) AND id::text=@id;");AddVarchar(command,"no",no.Trim());AddVarchar(command,"version",version.Trim());AddVarchar(command,"id",id.Trim());if(command.ExecuteNonQuery()==0)throw NotFound("BOM",id);}

    private ProcessDto ResolveActiveProcess(string processNo,string selector)
    {
        var normalized=NormalizeSelector(selector);
        using var command=_dataSource.CreateCommand($"SELECT {ProcessColumns} FROM \"YMES\".tblprsprocessbasis WHERE lower(processno)=lower(@no) AND issuestate=2 AND ((@selector='*' AND curversion=1) OR lower(processversion)=lower(@selector)) ORDER BY curversion DESC LIMIT 1;");AddVarchar(command,"no",RequiredCode(processNo,"流程編號",30));AddVarchar(command,"selector",normalized);using var reader=command.ExecuteReader();return reader.Read()?MapProcess(reader):throw BadRequest(normalized=="*"?$"流程 {processNo} 沒有 ACTIVE 的目前版本。":$"找不到 ACTIVE 流程 {processNo}/{normalized}。");
    }

    private void ValidateBom(ProductBomWriteRequest request,string productNo)
    {
        var type=request.ComponentType.Trim().ToUpperInvariant();if(type is not ("MATERIAL" or "PRODUCT"))throw BadRequest("ComponentType 僅能為 MATERIAL 或 PRODUCT。");var component=RequiredCode(request.ComponentNo,"元件編號",50);
        if(type=="PRODUCT"&&Same(component,productNo))throw BadRequest("產品 BOM 不可引用自己。");
        var table=type=="MATERIAL"?"tblmtlmaterialbasis":"tblprdproductbasis";var column=type=="MATERIAL"?"materialno":"productno";
        using(var check=_dataSource.CreateCommand($"SELECT EXISTS(SELECT 1 FROM \"YMES\".{table} WHERE lower({column})=lower(@no));")){AddVarchar(check,"no",component);if(!(bool)(check.ExecuteScalar()??false))throw BadRequest($"找不到{(type=="MATERIAL"?"物料":"產品")} {component}。");}
        using(var check=_dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblopbasis WHERE lower(opno)=lower(@no));")){AddVarchar(check,"no",RequiredCode(request.InputOperationNo,"投入作業",20));if(!(bool)(check.ExecuteScalar()??false))throw BadRequest($"找不到投入作業 {request.InputOperationNo}。");}
        if(request.StandardQty<=0||request.StandardQty>99999999.9999m||decimal.Round(request.StandardQty,4)!=request.StandardQty)throw BadRequest("標準用量須大於 0、不可超過 99,999,999.9999，最多四位小數。");if(request.DecreaseRate is <0 or >100||decimal.Round(request.DecreaseRate,2)!=request.DecreaseRate)throw BadRequest("損耗率須介於 0 到 100，最多兩位小數。");_ = Optional(request.Description,1000,"說明");_ = Optional(request.PutInPlaceType,40,"投入點類型");
    }

    private static void ValidateProduct(ProductWriteRequest x,bool validateType=true)
    {
        _=RequiredCode(x.ProductNo,"產品編號",50);_=RequiredCode(x.ProductVersion,"產品版本",5);_=Required(x.ProductName,"產品名稱",200);if(validateType)_=RequiredCode(x.ProductType,"產品類別",50);_=RequiredCode(x.UnitNo,"單位編號",30);_=Optional(x.UnitType,30,"單位類別");_=Optional(x.Description,1000,"說明");_=Optional(x.SpecNo,120,"規格編號");_=Optional(x.ProductCode,10,"產品代碼");_=Optional(x.SerialTypeNoLot,50,"批號序號類別");_=Optional(x.SerialTypeNoComp,50,"元件序號類別");_=Optional(x.ErpNo,50,"ERP 編號");if(x.LotStdQty<0||x.LotStdQty>99999999.9999m||decimal.Round(x.LotStdQty,4)!=x.LotStdQty)throw BadRequest("批量標準數量超出資料表範圍。");
    }
    private static string? ProductSapPlnal(string? value) => string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static void ValidateProductSapPlnal(string? value,string? existing=null)
    {
        var code=ProductSapPlnal(value);
        // Legacy unchanged values remain readable/editable without inventing a
        // replacement. New choices are the five verified source selections.
        if(code is not null && code!=existing && code is not ("11" or "21" or "51" or "31" or "61"))
            throw BadRequest("SAP_PLNAL 僅允許 11、21、51、31、61；未提供時保留原值。");
    }
    private static void AddProductParameters(NpgsqlCommand c,ProductWriteRequest x,string? validatedProductType=null)
    {AddVarchar(c,"no",RequiredCode(x.ProductNo,"產品編號",50));AddVarchar(c,"version",RequiredCode(x.ProductVersion,"產品版本",5));AddVarchar(c,"name",Required(x.ProductName,"產品名稱",200));AddVarchar(c,"type",validatedProductType??RequiredCode(x.ProductType,"產品類別",50));AddVarchar(c,"unitType",Optional(x.UnitType,30,"單位類別"));AddVarchar(c,"unitNo",RequiredCode(x.UnitNo,"單位編號",30));AddVarchar(c,"description",Optional(x.Description,1000,"說明"));AddVarchar(c,"spec",Optional(x.SpecNo,120,"規格編號"));AddVarchar(c,"code",Optional(x.ProductCode,10,"產品代碼"));AddVarchar(c,"lotSerial",Optional(x.SerialTypeNoLot,50,"批號序號類別"));AddVarchar(c,"componentSerial",Optional(x.SerialTypeNoComp,50,"元件序號類別"));AddNumeric(c,"lotQty",x.LotStdQty);AddVarchar(c,"erp",Optional(x.ErpNo,50,"ERP 編號"));AddVarchar(c,"sapPlnal",ProductSapPlnal(x.SapPlnal));}
    private static ProductDto MapProduct(NpgsqlDataReader r)=>new(){ProductNo=r.GetString(0),ProductVersion=r.GetString(1),ProductName=r.GetString(2),ProductType=r.GetString(3),UnitType=r.GetString(4),UnitNo=r.GetString(5),Description=r.GetString(6),SpecNo=r.GetString(7),ProductCode=r.GetString(8),SerialTypeNoLot=r.GetString(9),SerialTypeNoComp=r.GetString(10),LotStdQty=r.GetDecimal(11),ErpNo=r.GetString(12),IssueState=r.IsDBNull(13)?null:r.GetInt32(13),CurrentVersion=r.GetBoolean(14),DataStamp=r.IsDBNull(15)?null:r.GetDecimal(15),CreatedAt=ProductAuditDate(r,16),SapPlnal=r.IsDBNull(17)?null:r.GetString(17),Creator=r.IsDBNull(18)?null:r.GetString(18),Reviser=r.IsDBNull(19)?null:r.GetString(19),RevisedAt=ProductAuditDate(r,20)};
    private static DateTime? ProductAuditDate(NpgsqlDataReader reader,int index) => reader.IsDBNull(index)?null:DateTime.SpecifyKind(reader.GetDateTime(index),DateTimeKind.Utc);
    private static ProductDto GetProductForUpdate(NpgsqlConnection c,NpgsqlTransaction t,string no,string version)
    {
        var context=ReadProductVersionContext(c,t,no,version,forUpdate:true);
        return ReadProductDto(c,t,context);
    }
    private static ProductDto ReadProductDto(NpgsqlConnection c,NpgsqlTransaction t,ProductVersionContext context)
    {
        using var cmd=new NpgsqlCommand($"SELECT {ProductColumns} FROM \"YMES\".tblprdproductbasis WHERE productno=@no AND productversion=@version;",c,t);
        AddVarchar(cmd,"no",context.ProductNo);AddVarchar(cmd,"version",context.ProductVersion);
        using var r=cmd.ExecuteReader();return r.Read()?MapProduct(r):throw NotFound("產品",$"{context.ProductNo}/{context.ProductVersion}");
    }
    private static void EnsureProductExpected(ProductDto current,int? state,decimal? stamp)
    {
        if(state.HasValue && state.Value is not (-1 or 0 or 1 or 2))throw BadRequest("產品 IssueState 只允許 -1、0、1、2 或明確 NULL。");
        if(stamp.HasValue && (decimal.Truncate(stamp.Value)!=stamp.Value || stamp.Value is < -99999999999m or > 99999999999m))throw BadRequest("產品 DataStamp 必須是 11 位數範圍內的整數或明確 NULL。");
        if(current.IssueState!=state || current.DataStamp!=stamp)throw Conflict("產品狀態或版本已變更，請重新載入後再操作。");
    }
    private static void EnsureProductStampCanAdvance(decimal? stamp)
    {
        if((stamp??0)>=99999999999m)throw Conflict("產品 DataStamp 已達來源欄位上限，無法再遞增版本。");
    }
    private static void LockProductCreationIdentity(NpgsqlConnection connection,NpgsqlTransaction transaction,string no,string version)
    {
        // Same complete-key mutex for ordinary creation and Version targets.
        // Use PostgreSQL's own case folding, matching the actual lookup
        // predicate, and an unambiguous JSON pair instead of a code delimiter.
        using var command=new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ProductVersion:' || jsonb_build_array(lower(@no),lower(@version))::text,0));",connection,transaction);
        AddVarchar(command,"no",no);AddVarchar(command,"version",version);command.ExecuteNonQuery();
    }
    private static void EnsureNewProductIdentity(NpgsqlConnection connection,NpgsqlTransaction transaction,string no,string version)
    {
        using var command=new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblprdproductbasis WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version) LIMIT 1;",connection,transaction);
        AddVarchar(command,"no",no);AddVarchar(command,"version",version);
        if(command.ExecuteScalar() is not null)throw Conflict($"產品 {no}/{version} 已存在（不區分大小寫）。");
    }
    private static ProductBomDto MapBom(NpgsqlDataReader r)=>new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetDecimal(6),r.GetString(7),r.GetBoolean(8),r.GetDecimal(9),r.GetString(10));
    private static string NormalizeSelector(string value){var normalized=RequiredCode(value,"流程版本選擇",3);return normalized=="*"?"*":normalized;}
    private static void ValidateMoType(int value){if(value is <0 or >99)throw BadRequest("工單類型代碼須介於 0 到 99。");}
    private static int SegmentOrder(string value)=>NormalizeCode(value) switch{"FRONT"=>10,"BACK"=>20,"ASSY"=>30,_=>99};
    private static bool SegmentHasComponent(string value)=>NormalizeCode(value) is "FRONT" or "ASSY";
    private static bool SegmentHasLevel(string value)=>NormalizeCode(value)=="ASSY";
}
