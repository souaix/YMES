using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed partial class Qc201Repository
{
    private sealed record Catalog(string ParameterNo, string Name, int ParameterType, int State, string? TableName, string? FieldName);

    public Qc201ChildSnapshot<Qc201ParameterRow> GetParameters(string no) => ReadChild(no,"parameters",ReadParameters);
    public Qc201ChildSnapshot<Qc201FactorRow> GetFactors(string no,string? factorOnline=null)
    {
        if (factorOnline is not (null or "Y" or "N")) throw Rule(400,"FactorOnline只允許Y或N。");
        var result=ReadChild(no,"factors",ReadFactors);
        return factorOnline is null?result:result with { Items=result.Items.Where(x=>x.FactorOnline==factorOnline).ToArray(),Writable=false,WriteUnavailableReason="這是篩選後清單；完整替換請重新載入未篩選的Factor快照。" };
    }
    public Qc201ChildSnapshot<Qc201ReasonMapDto> GetReasonMaps(string no) => ReadChild(no,"reasons",ReadReasonMaps);
    public Qc201ChildSnapshot<Qc201SamplePlanChangeRow> GetSamplePlanChanges(string no) => ReadChild(no,"samplePlanChanges",ReadPlanChanges);
    public Qc201ChildSnapshot<Qc201OperationRow> GetOperations(string no) => ReadChild(no,"operations",ReadOperations);

    private Qc201ChildSnapshot<T> ReadChild<T>(string no,string resource,Func<NpgsqlConnection,NpgsqlTransaction,string,IReadOnlyList<T>> read)
    {
        no=Key(no);
        using var connection=dataSource.OpenConnection();
        using var transaction=connection.BeginTransaction(IsolationLevel.ReadCommitted);
        QcItemIdentityGuard.Acquire(connection,transaction,[no]);
        QcItemIdentityGuard.EnsureAttributeOwner(connection,transaction,no);
        var parent=ReadItem(connection,transaction,no,false,true);
        EnsureChildOwners(connection,transaction,parent.QcItemNo);
        var result=Snapshot(resource,parent,read(connection,transaction,parent.QcItemNo));
        transaction.Commit();return result;
    }

    private static Qc201ChildSnapshot<T> Snapshot<T>(string resource,Qc201ItemDto parent,IReadOnlyList<T> rows)
    {
        var writable=resource=="operations"?parent.IssueState==2:parent.IssueState==0;
        return new(parent.QcItemNo,parent.IssueState,parent.DataStamp,Fingerprint(new { Resource=resource,Parent=parent,Rows=rows }),rows,writable,
            writable?null:resource=="operations"?"只有已核准(state2)項目可修改作業站對應。":"本批YMES保守規則：此明細只有未簽核(state0)可修改，NULL不視為0。");
    }

    private Qc201ChildSnapshot<TStored> WriteChild<TRequest,TStored>(string no,string resource,Qc201RowsRequest<TRequest> request,string actor,
        Func<NpgsqlConnection,NpgsqlTransaction,string,IReadOnlyList<TStored>> read,
        Action<NpgsqlConnection,NpgsqlTransaction,Qc201ItemDto,IReadOnlyList<TStored>,List<TRequest>> replace)
    {
        no=Key(no);actor=Actor(actor);var rows=Rows(request.Rows);RequireVersion(request.Version);
        using var connection=dataSource.OpenConnection();
        using var transaction=connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var parent=LockItem(connection,transaction,no);
        Expected(parent,request.IssueState,request.DataStamp);
        if (resource=="operations") { if (parent.IssueState!=2) throw Rule(409,"只有已核准項目可修改作業站對應。"); }
        else RequireDraft(parent);
        GuardIncrement(parent.DataStamp);
        var old=read(connection,transaction,parent.QcItemNo);
        if (!string.Equals(Snapshot(resource,parent,old).Version,request.Version,StringComparison.Ordinal)) throw Conflict();
        replace(connection,transaction,parent,old,rows);
        var updated=BumpParent(connection,transaction,parent,actor);
        var result=Snapshot(resource,updated,read(connection,transaction,updated.QcItemNo));
        transaction.Commit();Audit("Save"+resource,updated,actor);return result;
    }

    private static Qc201ItemDto BumpParent(NpgsqlConnection connection,NpgsqlTransaction transaction,Qc201ItemDto parent,string actor)
    {
        using var command=new NpgsqlCommand($"UPDATE \"YMES\".tblqcitemforattributes SET datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC' WHERE qcitemno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {ItemColumns};",connection,transaction);
        BindExpected(command,parent);Text(command,"actor",actor);return ReadReturned(command);
    }

    public Qc201ChildSnapshot<Qc201ParameterRow> SaveParameters(string no,Qc201RowsRequest<Qc201ParameterRow> request,string actor) =>
        WriteChild(no,"parameters",request,actor,ReadParameters,(c,t,parent,old,rows)=>
        {
            _=QcSamplingPlanReference.ReadGeneral(c,t,parent.PlanNo??"",parent.ComponentCollect,2,Rule);
            var normalized=rows.Select(x=>new Qc201ParameterRow { ParameterNo=Code(x.ParameterNo,"ParameterNo",20,true)!,ParameterValue=TextValue(x.ParameterValue,"ParameterValue",255,false) }).ToList();
            Unique(normalized.Select(x=>x.ParameterNo),"ParameterNo");
            ReplaceParameters(c,t,parent.QcItemNo,normalized);
        });

    public Qc201ChildSnapshot<Qc201FactorRow> SaveFactors(string no,Qc201RowsRequest<Qc201FactorRow> request,string actor) =>
        WriteChild(no,"factors",request,actor,ReadFactors,(c,t,parent,old,rows)=>
        {
            var normalized=rows.Select(NormalizeFactor).ToList();Unique(normalized.Select(x=>x.FactorNo),"FactorNo");
            var catalogs=LockCatalogs(c,t,normalized.Select(x=>x.FactorNo));
            foreach(var row in normalized)
            {
                var catalog=catalogs.Single(x=>Same(x.ParameterNo,row.FactorNo));row.FactorNo=catalog.ParameterNo;
                // Table/Field are stored as literal source metadata, never executed
                // as SQL. New overrides must agree with the trusted catalog; an
                // unchanged legacy pair can be retained without normalization.
                var previous=old.SingleOrDefault(x=>Same(x.FactorNo,row.FactorNo));
                if ((previous is null || previous.TableName!=row.TableName || previous.FieldName!=row.FieldName) &&
                    ((!string.IsNullOrEmpty(row.TableName)&&!Same(row.TableName,catalog.TableName)) || (!string.IsNullOrEmpty(row.FieldName)&&!Same(row.FieldName,catalog.FieldName))))
                    throw Rule(422,"新的TableName／FieldName必須來自真實MES參數目錄，不接受任意參照來源。");
            }
            foreach(var op in DatabaseOrder(c,t,normalized.Where(x=>!string.IsNullOrWhiteSpace(x.OpNo)).Select(x=>x.OpNo!)))
                _=ExistingOperation(c,t,op,false);
            ReplaceFactors(c,t,parent.QcItemNo,normalized);
        });

    public Qc201ChildSnapshot<Qc201ReasonMapDto> SaveReasons(string no,Qc201RowsRequest<Qc201ReasonRow> request,string actor) =>
        WriteChild(no,"reasons",request,actor,ReadReasonMaps,(c,t,parent,old,rows)=>
        {
            var nos=rows.Select(x=>Code(x.ReasonNo,"ReasonNo",20,true)!).ToList();Unique(nos,"ReasonNo");
            var canonical=LockReasons(c,t,nos);
            DeleteRows(c,t,"tblqcitemreasonmap",parent.QcItemNo);
            foreach(var reason in canonical)
            {
                using var command=new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemreasonmap(qcitemno,reasonno,creator,createdate,reviser,revisedate) VALUES(@no,@reason,@actor,now() AT TIME ZONE 'UTC',NULL,NULL);",c,t);
                Text(command,"no",parent.QcItemNo);Text(command,"reason",reason);Text(command,"actor",Actor(actor));command.ExecuteNonQuery();
            }
        });

    public Qc201ChildSnapshot<Qc201SamplePlanChangeRow> SaveSamplePlanChanges(string no,Qc201RowsRequest<Qc201SamplePlanChangeRow> request,string actor) =>
        WriteChild(no,"samplePlanChanges",request,actor,ReadPlanChanges,(c,t,parent,old,rows)=>
        {
            QcSamplingPlanReference.RequireGeneral(parent.ComponentCollect,Rule);
            var normalized=rows.Select(NormalizePlanChange).ToList();
            if (normalized.Select(x=>(x.OldSamplePlan.ToUpperInvariant(),x.AccLotQty,x.RejLotQty,x.LotRange)).Distinct().Count()!=normalized.Count)
                throw Rule(400,"抽樣計劃轉換複合鍵不可重複。");
            foreach(var plan in DatabaseOrder(c,t,normalized.SelectMany(x=>new[]{x.OldSamplePlan,x.NewSamplePlan}).Where(x=>!string.IsNullOrEmpty(x)).Select(x=>x!)))
                _=QcSamplingPlanReference.ReadGeneral(c,t,plan,parent.ComponentCollect,2,Rule);
            ReplacePlanChanges(c,t,parent.QcItemNo,normalized);
        });

    public Qc201ChildSnapshot<Qc201OperationRow> SaveOperations(string no,Qc201RowsRequest<Qc201OperationRow> request,string actor) =>
        WriteChild(no,"operations",request,actor,ReadOperations,(c,t,parent,old,rows)=>
        {
            if(old.Any(x=>x.QcItemType!=0)) throw Rule(409,"現有作業站對應含其他QCItemType，不能以計數項目覆寫。");
            var normalized=rows.Select(x=>new Qc201OperationRow { OpNo=Code(x.OpNo,"OPNo",20,true)!,Inline=x.Inline,ExecuteOrder=x.ExecuteOrder,QcItemType=x.QcItemType,InspTypeFai=x.InspTypeFai,InspTypeIpqc=x.InspTypeIpqc,InspTypePqc=x.InspTypePqc }).ToList();
            Unique(normalized.Select(x=>x.OpNo),"OPNo");
            foreach(var row in normalized)
            {
                if(row.QcItemType!=0 || row.Inline is not(0 or 1) || row.ExecuteOrder is <0 or >99) throw Rule(400,"QCItemType／Inline／ExecuteOrder無效。");
                if(row.Inline==1 && parent.LotIsValid!=1) throw Rule(422,"只有LotIsValid=1可以設定InLine作業站。");
                if(row.Inline==1 && row.InspTypeFai!=true && row.InspTypeIpqc!=true && row.InspTypePqc!=true) throw Rule(422,"InLine至少需要一種FAI／IPQC／PQC檢驗時機。");
            }
            var canonical=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(var key in DatabaseOrder(c,t,normalized.Select(x=>x.OpNo))) canonical[key]=ExistingOperation(c,t,key,true);
            DeleteRows(c,t,"tblopqcitem",parent.QcItemNo);
            foreach(var row in normalized)
            {
                var op=canonical.First(x=>Same(x.Key,row.OpNo)).Value;
                using var command=new NpgsqlCommand("INSERT INTO \"YMES\".tblopqcitem(opno,qcitemno,qcitemtype,inline,executeorder,insptype_fai,insptype_ipqc,insptype_pqc,reviser,revisedate) VALUES(@op,@no,0,@inline,@order,@fai,@ipqc,@pqc,@actor,now() AT TIME ZONE 'UTC');",c,t);
                Text(command,"op",op);Text(command,"no",parent.QcItemNo);Number(command,"inline",row.Inline);Number(command,"order",row.ExecuteOrder);
                Number(command,"fai",row.InspTypeFai is null?null:row.InspTypeFai.Value?1:0);Number(command,"ipqc",row.InspTypeIpqc is null?null:row.InspTypeIpqc.Value?1:0);Number(command,"pqc",row.InspTypePqc is null?null:row.InspTypePqc.Value?1:0);Text(command,"actor",Actor(actor));command.ExecuteNonQuery();
            }
        });

    private static IReadOnlyList<Qc201ParameterRow> ReadParameters(NpgsqlConnection c,NpgsqlTransaction t,string no)
    {
        EnsureTable(c,t,"tblqcitemparameter");using var command=new NpgsqlCommand("SELECT parameterno,parametervalue FROM \"YMES\".tblqcitemparameter WHERE qcitemno=@no ORDER BY parameterno;",c,t);Text(command,"no",no);
        using var reader=command.ExecuteReader();var rows=new List<Qc201ParameterRow>();while(reader.Read())rows.Add(new(){ParameterNo=reader.GetString(0),ParameterValue=String(reader,1)});AssertStoredUnique(rows.Select(x=>x.ParameterNo),"ParameterNo");return rows;
    }
    private static IReadOnlyList<Qc201FactorRow> ReadFactors(NpgsqlConnection c,NpgsqlTransaction t,string no)
    {
        EnsureTable(c,t,"tblqcitemfactor");var rows=new List<Qc201FactorRow>();
        using(var command=new NpgsqlCommand("SELECT factorno,factoronline,factorspec,opno,tablename,fieldname,factorshortrun FROM \"YMES\".tblqcitemfactor WHERE qcitemno=@no ORDER BY factorno;",c,t))
        {
            Text(command,"no",no);using var reader=command.ExecuteReader();while(reader.Read())rows.Add(new(){FactorNo=reader.GetString(0),FactorOnline=String(reader,1),FactorSpec=String(reader,2),OpNo=String(reader,3),TableName=String(reader,4),FieldName=String(reader,5),FactorShortRun=String(reader,6)});
        }
        AssertStoredUnique(rows.Select(x=>x.FactorNo),"FactorNo");
        var exists=TableExists(c,t,"tblsysmesparameter");
        foreach(var row in rows)
        {
            var metadata=exists?FindCatalog(c,t,row.FactorNo,false):null;
            row.FactorName=metadata?.Name;row.ParameterType=metadata?.ParameterType;row.MetadataAvailable=metadata?.State==2;
            row.MetadataUnavailableReason=row.MetadataAvailable?null:exists?"MES參數不存在或尚未核准。":"真實MES參數目錄尚未建立。";
        }
        return rows;
    }
    private static IReadOnlyList<Qc201ReasonMapDto> ReadReasonMaps(NpgsqlConnection c,NpgsqlTransaction t,string no)
    {
        EnsureTable(c,t,"tblqcitemreasonmap");EnsureTable(c,t,"tblqcreasonbasis");
        using var command=new NpgsqlCommand("""
            SELECT m.qcitemno,m.reasonno,r.reasonname,m.creator,m.createdate,m.reviser,m.revisedate,
                   count(r.reasonno) OVER(PARTITION BY m.qcitemno,m.reasonno)
            FROM "YMES".tblqcitemreasonmap m LEFT JOIN "YMES".tblqcreasonbasis r ON lower(btrim(r.reasonno))=lower(btrim(m.reasonno))
            WHERE m.qcitemno=@no ORDER BY m.reasonno;
            """,c,t);Text(command,"no",no);using var reader=command.ExecuteReader();var rows=new List<Qc201ReasonMapDto>();
        while(reader.Read()){if(reader.GetInt64(7)>1)throw Rule(409,"原因主檔存在大小寫／空白歧義。");rows.Add(new(reader.GetString(0),reader.GetString(1),String(reader,2),String(reader,3),Date(reader,4),String(reader,5),Date(reader,6)));}AssertStoredUnique(rows.Select(x=>x.ReasonNo),"ReasonNo");return rows;
    }
    private static IReadOnlyList<Qc201SamplePlanChangeRow> ReadPlanChanges(NpgsqlConnection c,NpgsqlTransaction t,string no)
    {
        EnsureTable(c,t,"tblqcsampleplanchange");using var command=new NpgsqlCommand("SELECT oldsampleplan,acclotqty::int,rejlotqty::int,newsampleplan,factorno,lotrange::int,aqlchkforsplanchg::int,qcitemtype::int FROM \"YMES\".tblqcsampleplanchange WHERE qcitemno=@no ORDER BY oldsampleplan,acclotqty,rejlotqty,lotrange;",c,t);Text(command,"no",no);using var reader=command.ExecuteReader();var rows=new List<Qc201SamplePlanChangeRow>();
        while(reader.Read())rows.Add(new(){OldSamplePlan=reader.GetString(0),AccLotQty=reader.GetInt32(1),RejLotQty=reader.GetInt32(2),NewSamplePlan=String(reader,3),FactorNo=reader.GetString(4),LotRange=reader.GetInt32(5),AqlChkForSPlanChg=Int(reader,6),QcItemType=Int(reader,7)});
        if(rows.Select(x=>(x.OldSamplePlan.Trim(' ').ToUpperInvariant(),x.AccLotQty,x.RejLotQty,x.LotRange)).Distinct().Count()!=rows.Count)throw Rule(409,"既有抽樣轉換複合鍵存在大小寫／空白歧義。");return rows;
    }
    private static IReadOnlyList<Qc201OperationRow> ReadOperations(NpgsqlConnection c,NpgsqlTransaction t,string no)
    {
        EnsureTable(c,t,"tblopqcitem");using var command=new NpgsqlCommand("""
            SELECT m.opno,o.opname,m.inline::int,m.executeorder::int,m.qcitemtype::int,m.insptype_fai::int,m.insptype_ipqc::int,m.insptype_pqc::int,m.reviser,m.revisedate,
                   count(o.opno) OVER(PARTITION BY m.opno,m.qcitemno)
            FROM "YMES".tblopqcitem m LEFT JOIN "YMES".tblopbasis o ON lower(btrim(o.opno))=lower(btrim(m.opno))
            WHERE m.qcitemno=@no ORDER BY m.executeorder,m.opno;
            """,c,t);Text(command,"no",no);using var reader=command.ExecuteReader();var rows=new List<Qc201OperationRow>();
        while(reader.Read()){if(reader.GetInt64(10)>1)throw Rule(409,"作業站主檔存在大小寫／空白歧義。");rows.Add(new(){OpNo=reader.GetString(0),OpName=String(reader,1),Inline=reader.GetInt32(2),ExecuteOrder=reader.GetInt32(3),QcItemType=reader.GetInt32(4),InspTypeFai=Int(reader,5) is {} fai?fai!=0:null,InspTypeIpqc=Int(reader,6) is {} ipqc?ipqc!=0:null,InspTypePqc=Int(reader,7) is {} pqc?pqc!=0:null,Reviser=String(reader,8),ReviseDate=Date(reader,9)});}AssertStoredUnique(rows.Select(x=>x.OpNo),"OPNo");return rows;
    }

    private static Catalog? FindCatalog(NpgsqlConnection c,NpgsqlTransaction t,string no,bool forShare)
    {
        EnsureTable(c,t,"tblsysmesparameter");using var command=new NpgsqlCommand("SELECT parameterno,parametername,parametertype::int,issuestate::int,tablename,fieldname FROM \"YMES\".tblsysmesparameter WHERE lower(btrim(parameterno))=lower(btrim(@no)) ORDER BY parameterno"+(forShare?" FOR SHARE;":";"),c,t);Text(command,"no",no);using var reader=command.ExecuteReader();
        if(!reader.Read())return null;var row=new Catalog(reader.GetString(0),reader.GetString(1),reader.GetInt32(2),reader.GetInt32(3),String(reader,4),String(reader,5));if(reader.Read())throw Rule(409,"MES ParameterNo存在大小寫／空白歧義。");return row;
    }
    private static Catalog ReadCatalog(NpgsqlConnection c,NpgsqlTransaction t,string no,bool forShare)
    {var row=FindCatalog(c,t,no,forShare);if(row is null||row.State!=2)throw Rule(422,"MES參數不存在或尚未核准。");return row;}
    private static IReadOnlyList<Catalog> LockCatalogs(NpgsqlConnection c,NpgsqlTransaction t,IEnumerable<string> nos)
    {var result=new List<Catalog>();foreach(var no in DatabaseOrder(c,t,nos))result.Add(ReadCatalog(c,t,no,true));return result;}
    private static IReadOnlyList<string> LockReasons(NpgsqlConnection c,NpgsqlTransaction t,IEnumerable<string> nos)
    {var result=new List<string>();foreach(var no in DatabaseOrder(c,t,nos))result.Add(ApprovedCode(c,t,"tblqcreasonbasis","reasonno",no));return result;}
    private static string ExistingOperation(NpgsqlConnection c,NpgsqlTransaction t,string no,bool requireApproved)
    {
        using var command=new NpgsqlCommand("SELECT opno,issuestate::int FROM \"YMES\".tblopbasis WHERE lower(btrim(opno))=lower(btrim(@no)) ORDER BY opno FOR SHARE;",c,t);Text(command,"no",no);using var reader=command.ExecuteReader();if(!reader.Read())throw Rule(422,"作業站不存在。");var canonical=reader.GetString(0);var state=Int(reader,1);if(reader.Read())throw Rule(409,"作業站存在大小寫／空白歧義。");if(requireApproved&&state!=2)throw Rule(422,"作業站尚未核准。");return canonical;
    }
    private static IReadOnlyList<string> DatabaseOrder(NpgsqlConnection c,NpgsqlTransaction t,IEnumerable<string> values)
    {
        using var command=new NpgsqlCommand("SELECT DISTINCT lower(btrim(value)) COLLATE \"C\" AS name FROM unnest(@names::text[]) AS requested(value) WHERE btrim(value)<>'' ORDER BY name;",c,t);
        command.Parameters.Add("names",NpgsqlDbType.Array|NpgsqlDbType.Text).Value=values.ToArray();using var reader=command.ExecuteReader();var rows=new List<string>();while(reader.Read())rows.Add(reader.GetString(0));return rows;
    }

    private static Qc201FactorRow NormalizeFactor(Qc201FactorRow row) => new(){FactorNo=Code(row.FactorNo,"FactorNo",30,true)!,FactorOnline=Flag(row.FactorOnline,"FactorOnline",false),FactorSpec=Flag(row.FactorSpec,"FactorSpec",false),OpNo=Code(row.OpNo,"OPNo",20,false),TableName=Code(row.TableName,"TableName",60,false),FieldName=Code(row.FieldName,"FieldName",30,false),FactorShortRun=Flag(row.FactorShortRun,"FactorShortRun",true)};
    private static string? Flag(string? value,string label,bool nullable)
    {if(value is null&&nullable)return null;if(value is not("Y" or "N"))throw Rule(400,label+"只允許Y／N"+(nullable?"或NULL。":"。"));return value;}
    private static Qc201SamplePlanChangeRow NormalizePlanChange(Qc201SamplePlanChangeRow row)
    {
        if(row.AccLotQty is < -999 or >999 || row.RejLotQty is < -999 or >999 || row.LotRange is < -999 or >999)throw Rule(400,"抽樣轉換數值超出numeric(3,0)。");
        if(row.AqlChkForSPlanChg is not(0 or 1) || row.QcItemType is not(null or 0))throw Rule(400,"AQL Check必須為0／1，QCItemType僅計數0或來源NULL。");
        return new(){OldSamplePlan=Code(row.OldSamplePlan,"OldSamplePlan",25,true)!,AccLotQty=row.AccLotQty,RejLotQty=row.RejLotQty,NewSamplePlan=Code(row.NewSamplePlan,"NewSamplePlan",25,false),FactorNo=Code(row.FactorNo,"FactorNo",100,true)!,LotRange=row.LotRange,AqlChkForSPlanChg=row.AqlChkForSPlanChg,QcItemType=row.QcItemType};
    }
    private static List<T> Rows<T>(List<T>? rows)
    {if(rows is null||rows.Count>1000||rows.Any(row=>row is null))throw Rule(400,"Rows必須明確提供，最多1000筆且不可含NULL列。");return rows;}
    private static void Unique(IEnumerable<string> values,string label)
    {if(values.GroupBy(x=>x.Trim(' '),StringComparer.OrdinalIgnoreCase).Any(x=>x.Count()>1))throw Rule(400,label+"不可重複。");}
    private static void AssertStoredUnique(IEnumerable<string> values,string label)
    {if(values.GroupBy(x=>x.Trim(' '),StringComparer.OrdinalIgnoreCase).Any(x=>x.Count()>1))throw Rule(409,"既有"+label+"存在大小寫／空白歧義，不能忽略或覆寫。");}
    private static void EnsureChildOwners(NpgsqlConnection c,NpgsqlTransaction t,string no)
    {
        foreach(var table in ChildTables)
        {
            EnsureTable(c,t,table);
            using var command=new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{table} WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemno<>@no LIMIT 1;",c,t);
            Text(command,"no",no);if(command.ExecuteScalar() is not null)throw Rule(409,"既有明細的QCItemNo含大小寫／空白別名，無法安全判定整組資料；本次未修改。");
        }
    }
    private static void DeleteRows(NpgsqlConnection c,NpgsqlTransaction t,string table,string no)
    {
        if(!ChildTables.Contains(table,StringComparer.Ordinal))throw new InvalidOperationException("Unknown QC201 child.");
        using var command=new NpgsqlCommand($"DELETE FROM \"YMES\".{table} WHERE qcitemno=@no;",c,t);Text(command,"no",no);command.ExecuteNonQuery();
    }
    private static void ReplaceParameters(NpgsqlConnection c,NpgsqlTransaction t,string no,IReadOnlyList<Qc201ParameterRow> rows)
    {
        DeleteRows(c,t,"tblqcitemparameter",no);foreach(var row in rows){using var command=new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemparameter(qcitemno,parameterno,parametervalue) VALUES(@no,@parameter,@value);",c,t);Text(command,"no",no);Text(command,"parameter",row.ParameterNo);Text(command,"value",row.ParameterValue);command.ExecuteNonQuery();}
    }
    private static void ReplaceFactors(NpgsqlConnection c,NpgsqlTransaction t,string no,IReadOnlyList<Qc201FactorRow> rows)
    {
        DeleteRows(c,t,"tblqcitemfactor",no);foreach(var row in rows){using var command=new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemfactor(qcitemno,factorno,factoronline,factorspec,opno,tablename,fieldname,factorshortrun) VALUES(@no,@factor,@online,@spec,@op,@table,@field,@short);",c,t);Text(command,"no",no);Text(command,"factor",row.FactorNo);Text(command,"online",row.FactorOnline);Text(command,"spec",row.FactorSpec);Text(command,"op",row.OpNo);Text(command,"table",row.TableName);Text(command,"field",row.FieldName);Text(command,"short",row.FactorShortRun);command.ExecuteNonQuery();}
    }
    private static void ReplacePlanChanges(NpgsqlConnection c,NpgsqlTransaction t,string no,IReadOnlyList<Qc201SamplePlanChangeRow> rows)
    {
        DeleteRows(c,t,"tblqcsampleplanchange",no);foreach(var row in rows){using var command=new NpgsqlCommand("INSERT INTO \"YMES\".tblqcsampleplanchange(qcitemno,oldsampleplan,acclotqty,rejlotqty,newsampleplan,factorno,lotrange,aqlchkforsplanchg,qcitemtype) VALUES(@no,@old,@acc,@rej,@new,@factor,@range,@aql,@type);",c,t);Text(command,"no",no);Text(command,"old",row.OldSamplePlan);Number(command,"acc",row.AccLotQty);Number(command,"rej",row.RejLotQty);Text(command,"new",row.NewSamplePlan);Text(command,"factor",row.FactorNo);Number(command,"range",row.LotRange);Number(command,"aql",row.AqlChkForSPlanChg);Number(command,"type",row.QcItemType);command.ExecuteNonQuery();}
    }

    private static Qc201CopyPreview BuildCopyPreview(NpgsqlConnection c,NpgsqlTransaction t,Qc201ItemDto source,Qc201CopyWriteRequest request,QcSamplingPlanReference.GeneralPlan plan)
    {
        var parameters=ReadParameters(c,t,source.QcItemNo);var factors=ReadFactors(c,t,source.QcItemNo);var reasons=ReadReasonMaps(c,t,source.QcItemNo);var changes=ReadPlanChanges(c,t,source.QcItemNo);
        var planParameters=plan.Parameters.Select(x=>new Qc201ParameterRow{ParameterNo=x.ParameterNo,ParameterValue=x.ParameterValue}).ToArray();
        var same=parameters.Count==planParameters.Length && parameters.All(x=>planParameters.Any(y=>Same(x.ParameterNo,y.ParameterNo)&&string.Equals(x.ParameterValue,y.ParameterValue,StringComparison.Ordinal)));
        var version=Fingerprint(new{Source=source,Parameters=parameters,Factors=factors,Reasons=reasons,Changes=changes,Plan=plan,request.CopyParameters,request.CopyFactors,request.CopyReasons,request.CopySamplePlanChanges});
        return new(source.QcItemNo,source.IssueState,source.DataStamp,version,!same,parameters,planParameters,request.CopyParameters?["item","plan"]:[]);
    }
    private static QcSamplingPlanReference.GeneralPlan LockCopyPlans(NpgsqlConnection c,NpgsqlTransaction t,Qc201ItemDto source,Qc201ItemWriteRequest value,Qc201CopyWriteRequest request)
    {
        QcSamplingPlanReference.RequireGeneral(value.ComponentCollect,Rule);
        var names = new List<string> { value.PlanNo! };
        if(request.CopySamplePlanChanges)
            names.AddRange(ReadPlanChanges(c,t,source.QcItemNo).SelectMany(x=>new[]{x.OldSamplePlan,x.NewSamplePlan}).Where(x=>!string.IsNullOrEmpty(x)).Select(x=>x!));
        QcSamplingPlanReference.GeneralPlan? target = null;
        foreach(var name in DatabaseOrder(c,t,names))
        {
            var plan = QcSamplingPlanReference.ReadGeneral(c,t,name,value.ComponentCollect,2,Rule);
            if(Same(plan.PlanNo,value.PlanNo)) target = plan;
        }
        return target ?? throw Rule(422,"找不到目標一般抽樣計劃。");
    }
    private static void CopyChildren(NpgsqlConnection c,NpgsqlTransaction t,Qc201ItemDto source,string target,Qc201CopyWriteRequest request,Qc201CopyPreview preview,string actor)
    {
        var factors=request.CopyFactors?ReadFactors(c,t,source.QcItemNo):[];
        var reasons=request.CopyReasons?ReadReasonMaps(c,t,source.QcItemNo):[];
        var changes=request.CopySamplePlanChanges?ReadPlanChanges(c,t,source.QcItemNo):[];
        // Parent already locked. Lock lookups in fixed Catalog→Reason→OP order;
        // plans were locked first by EnsureLookups/preview.
        if(request.CopyFactors)LockCatalogs(c,t,factors.Select(x=>x.FactorNo));
        var canonicalReasons=request.CopyReasons?LockReasons(c,t,reasons.Select(x=>x.ReasonNo)):[];
        if(request.CopyFactors)foreach(var op in DatabaseOrder(c,t,factors.Where(x=>!string.IsNullOrWhiteSpace(x.OpNo)).Select(x=>x.OpNo!)))_=ExistingOperation(c,t,op,false);
        if(request.CopyParameters)ReplaceParameters(c,t,target,request.ParameterSource=="plan"?preview.PlanParameters:preview.ItemParameters);
        else if(preview.PlanParameters.Count>0)throw Rule(503,"CopyParameters未勾選時的原生Plan初始化分支尚未交付，不能默認建立空參數設定。");
        if(request.CopyFactors)ReplaceFactors(c,t,target,factors);
        if(request.CopySamplePlanChanges) { foreach(var row in changes) _=NormalizePlanChange(row); ReplacePlanChanges(c,t,target,changes); }
        if(request.CopyReasons)foreach(var reason in canonicalReasons){using var command=new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemreasonmap(qcitemno,reasonno,creator,createdate,reviser,revisedate) VALUES(@no,@reason,@actor,now() AT TIME ZONE 'UTC',NULL,NULL);",c,t);Text(command,"no",target);Text(command,"reason",reason);Text(command,"actor",actor);command.ExecuteNonQuery();}
    }
}
