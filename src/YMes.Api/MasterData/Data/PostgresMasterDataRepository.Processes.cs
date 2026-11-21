using MES.Contracts;
using Npgsql;

namespace MES.Api.Data;

public sealed partial class PostgresMasterDataRepository
{
    private const string ProcessColumns = @"
        processno, processversion, processtype, psno,
        COALESCE(processclass, 0)::numeric, COALESCE(description, ''),
        COALESCE(tuningprocess, 0) = 1, COALESCE(partialtype, 0)::numeric,
        issuestate::numeric, curversion = 1, COALESCE(datastamp, 0)::numeric,
        COALESCE(createdate, now())
        ";

    public IReadOnlyList<ProcessDto> ListProcesses(string? keyword, int? issueState)
    {
        using var command = _dataSource.CreateCommand($@"
            SELECT {ProcessColumns} FROM ""YMES"".tblprsprocessbasis
            WHERE (@state IS NULL OR issuestate = @state)
              AND (@keyword IS NULL OR processno ILIKE @keyword OR description ILIKE @keyword)
            ORDER BY processno, processversion;
            ");
        AddNumeric(command, "state", issueState);
        AddVarchar(command, "keyword", string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%");
        using var reader = command.ExecuteReader();
        var rows = new List<ProcessDto>();
        while (reader.Read()) rows.Add(MapProcess(reader));
        return rows;
    }

    public ProcessDto GetProcess(string no, string version)
    {
        using var command = _dataSource.CreateCommand($"SELECT {ProcessColumns} FROM \"YMES\".tblprsprocessbasis WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version);");
        AddVarchar(command, "no", no.Trim()); AddVarchar(command, "version", version.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapProcess(reader) : throw NotFound("流程", $"{no}/{version}");
    }

    public ProcessDto CreateProcess(ProcessWriteRequest request, string actorUserNo)
    {
        ValidateProcess(request); ValidateActor(actorUserNo);
        var no = RequiredCode(request.ProcessNo, "流程編號", 30);
        var version = RequiredCode(request.ProcessVersion, "流程版本", 3);
        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        string processType;
        try
        {
            YMes.Api.Processes.PrsIdentifierGuard.EnsureProcessIdentifierAvailable(connection, transaction, no);
            processType = YMes.Api.Processes.PrsIdentifierGuard.LockApprovedType(connection, transaction, request.ProcessType);
        }
        catch (NpgsqlException)
        {
            throw new MasterDataRuleException(503, "無法核對流程識別碼與已核准流程類別；請確認流程／類別資料表與權限已部署。");
        }
        using (var command = new NpgsqlCommand(@"
            INSERT INTO ""YMES"".tblprsprocessbasis
              (processno,processclass,psno,processtype,issuestate,creator,createdate,description,
               tuningprocess,processversion,curversion,partialtype,datastamp)
            VALUES (@no,@class,@segment,@type,0,@actor,now(),@description,@tuning,@version,0,@partial,1);
            ", connection, transaction))
        {
            AddVarchar(command,"no",no); AddNumeric(command,"class",request.ProcessClass);
            AddVarchar(command,"segment",RequiredCode(request.ProductionSegmentNo,"製程段",50));
            AddVarchar(command,"type",processType);
            AddVarchar(command,"actor",actorUserNo.Trim()); AddVarchar(command,"description",Optional(request.Description,255,"說明"));
            AddNumeric(command,"tuning",request.TuningProcess ? 1 : 0); AddVarchar(command,"version",version); AddNumeric(command,"partial",request.PartialType);
            ExecuteInsert(command,$"流程 {no}/{version} 已存在。");
        }
        InsertDefaultGraph(connection, transaction, no, version);
        transaction.Commit();
        return GetProcess(no, version);
    }

    public ProcessDto UpdateProcess(string no, string version, ProcessWriteRequest request, string actorUserNo)
    {
        ValidateProcess(request); ValidateActor(actorUserNo);
        var processNo = RequiredCode(no, "流程編號", 30);
        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        // Cooperating PRS writers always acquire the identifier mutex before
        // parent row locks. Edit keeps the source ProcessType relationship.
        YMes.Api.Processes.PrsIdentifierGuard.LockIdentifier(connection, transaction, processNo);
        var current = GetProcessForUpdate(connection, transaction, processNo, version);
        EnsureEditable(current.IssueState,current.DataStamp,request.DataStamp);
        if (!Same(current.ProcessNo,request.ProcessNo) || !Same(current.ProcessVersion,request.ProcessVersion)) throw BadRequest("更新時不可變更 ProcessNo/ProcessVersion。");
        if (!Same(current.ProcessType,request.ProcessType)) throw BadRequest("編輯流程時不可變更流程類別（ProcessType）。");
        using var command = new NpgsqlCommand(@"
            UPDATE ""YMES"".tblprsprocessbasis SET processclass=@class, psno=@segment,
              description=@description, tuningprocess=@tuning, partialtype=@partial,
              datastamp=COALESCE(datastamp,0)+1, reviser=@actor, revisedate=now()
            WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version)
              AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
            ", connection, transaction);
        AddProcessEditParameters(command,request); AddVarchar(command,"actor",actorUserNo.Trim()); AddVarchar(command,"no",current.ProcessNo);
        AddVarchar(command,"version",current.ProcessVersion); AddNumeric(command,"stamp",request.DataStamp);
        ExecuteOptimisticUpdate(command,"流程資料已變更，請重新載入後再儲存。");
        var result = GetProcessForUpdate(connection, transaction, current.ProcessNo, current.ProcessVersion);
        transaction.Commit();
        return result;
    }

    public ProcessDto ApproveProcess(string no, string version, int dataStamp, string actorUserNo)
    {
        ValidateActor(actorUserNo);
        var graph = GetGraph(no,version); ValidateGraph(graph);
        using var command = _dataSource.CreateCommand(@"
            UPDATE ""YMES"".tblprsprocessbasis SET issuestate=2, datastamp=COALESCE(datastamp,0)+1,
              reviser=@actor,revisedate=now()
            WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version)
              AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
            ");
        AddVarchar(command,"actor",actorUserNo.Trim()); AddVarchar(command,"no",no.Trim()); AddVarchar(command,"version",version.Trim()); AddNumeric(command,"stamp",dataStamp);
        ExecuteOptimisticUpdate(command,"只有 UNFROZEN 且資料未變更的流程可以核准。");
        return GetProcess(no,version);
    }

    public ProcessGraphDto GetGraph(string no, string version)
    {
        var process = GetProcess(no,version);
        using var connection = _dataSource.OpenConnection();
        var nodes = new List<ProcessNodeDto>();
        using (var command = new NpgsqlCommand(@"
            SELECT nodeid,nodeno,nodetype,sequence,COALESCE(operationno,''),description
            FROM ""YMES"".ym_prsprocess_node WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version) ORDER BY sequence,nodeid;
            ", connection))
        {
            AddVarchar(command,"no",process.ProcessNo); AddVarchar(command,"version",process.ProcessVersion);
            using var reader = command.ExecuteReader(); while(reader.Read()) nodes.Add(new(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetInt32(3),reader.GetString(4),reader.GetString(5)));
        }
        var edges = new List<ProcessEdgeDto>();
        using (var command = new NpgsqlCommand(@"
            SELECT from_node_id,to_node_id,action FROM ""YMES"".ym_prsprocess_edge
            WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version) ORDER BY from_node_id,to_node_id,action;
            ", connection))
        {
            AddVarchar(command,"no",process.ProcessNo); AddVarchar(command,"version",process.ProcessVersion);
            using var reader=command.ExecuteReader(); while(reader.Read()) edges.Add(new(reader.GetString(0),reader.GetString(1),reader.GetString(2)));
        }
        if (nodes.Count == 0) return DefaultGraph(process.DataStamp);
        return new(nodes,edges,process.DataStamp);
    }

    public ProcessGraphDto UpdateGraph(string no, string version, ProcessGraphDto request, string actorUserNo)
    {
        ValidateActor(actorUserNo); var current=GetProcess(no,version); EnsureEditable(current.IssueState,current.DataStamp,request.DataStamp); ValidateGraph(request);
        using var connection=_dataSource.OpenConnection(); using var transaction=connection.BeginTransaction();
        using (var command=new NpgsqlCommand(@"
            DELETE FROM ""YMES"".ym_prsprocess_node WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version);
            ",connection,transaction)) { AddVarchar(command,"no",current.ProcessNo); AddVarchar(command,"version",current.ProcessVersion); command.ExecuteNonQuery(); }
        foreach(var node in request.Nodes)
        {
            using var command=new NpgsqlCommand(@"
                INSERT INTO ""YMES"".ym_prsprocess_node(processno,processversion,nodeid,nodeno,nodetype,sequence,operationno,description)
                VALUES(@no,@version,@id,@number,@type,@sequence,@operation,@description);
                ",connection,transaction);
            AddVarchar(command,"no",current.ProcessNo); AddVarchar(command,"version",current.ProcessVersion); AddVarchar(command,"id",node.NodeId.Trim());
            AddVarchar(command,"number",node.NodeNo.Trim()); AddVarchar(command,"type",node.NodeType.Trim().ToUpperInvariant()); AddNumeric(command,"sequence",node.Sequence);
            AddVarchar(command,"operation",Same(node.NodeType,"OP")?RequiredCode(node.OperationNo,"作業編號",20):null); AddVarchar(command,"description",node.Description.Trim()); command.ExecuteNonQuery();
        }
        foreach(var edge in request.Edges)
        {
            using var command=new NpgsqlCommand(@"
                INSERT INTO ""YMES"".ym_prsprocess_edge(processno,processversion,from_node_id,to_node_id,action)
                VALUES(@no,@version,@from,@to,@action);
                ",connection,transaction);
            AddVarchar(command,"no",current.ProcessNo); AddVarchar(command,"version",current.ProcessVersion); AddVarchar(command,"from",edge.FromNodeId.Trim()); AddVarchar(command,"to",edge.ToNodeId.Trim()); AddVarchar(command,"action",edge.Action.Trim()); command.ExecuteNonQuery();
        }
        using(var command=new NpgsqlCommand(@"
            UPDATE ""YMES"".tblprsprocessbasis SET datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now()
            WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version) AND issuestate=0 AND COALESCE(datastamp,0)=@stamp;
            ",connection,transaction))
        {
            AddVarchar(command,"actor",actorUserNo.Trim()); AddVarchar(command,"no",current.ProcessNo); AddVarchar(command,"version",current.ProcessVersion); AddNumeric(command,"stamp",request.DataStamp);
            ExecuteOptimisticUpdate(command,"流程資料已變更，請重新載入後再儲存。");
        }
        transaction.Commit(); return GetGraph(no,version);
    }

    public ProcessDto CreateProcessVersion(string no,string version,string actorUserNo)
    {
        ValidateActor(actorUserNo); var old=GetProcess(no,version);
        if(!int.TryParse(old.ProcessVersion,out var number)) throw BadRequest("流程版本格式不是數字，無法自動產生下一版。");
        var next=(number+1).ToString(new string('0',Math.Max(2,old.ProcessVersion.Length)));
        if(next.Length>3) throw BadRequest("流程版本已達資料表可用長度，無法建立下一版。");
        using var connection=_dataSource.OpenConnection(); using var transaction=connection.BeginTransaction();
        YMes.Api.Enterprise.Et202ProcessReferenceGuard.LockProcessNamespace(connection, transaction, old.ProcessNo, (status, message) => new MasterDataRuleException(status, message));
        using(var command=new NpgsqlCommand(@"
            INSERT INTO ""YMES"".tblprsprocessbasis(processno,processclass,psno,processtype,issuestate,creator,createdate,description,tuningprocess,processversion,curversion,partialtype,datastamp)
            SELECT processno,processclass,psno,processtype,0,@actor,now(),description,tuningprocess,@next,0,partialtype,1
            FROM ""YMES"".tblprsprocessbasis WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version);
            ",connection,transaction))
        {AddVarchar(command,"actor",actorUserNo.Trim());AddVarchar(command,"next",next);AddVarchar(command,"no",old.ProcessNo);AddVarchar(command,"version",old.ProcessVersion);ExecuteInsert(command,$"流程 {no}/{next} 已存在。");}
        using(var command=new NpgsqlCommand(@"
            INSERT INTO ""YMES"".ym_prsprocess_node(processno,processversion,nodeid,nodeno,nodetype,sequence,operationno,description)
            SELECT processno,@next,nodeid,nodeno,nodetype,sequence,operationno,description FROM ""YMES"".ym_prsprocess_node WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version);
            ",connection,transaction)){AddVarchar(command,"next",next);AddVarchar(command,"no",old.ProcessNo);AddVarchar(command,"version",old.ProcessVersion);command.ExecuteNonQuery();}
        using(var command=new NpgsqlCommand(@"
            INSERT INTO ""YMES"".ym_prsprocess_edge(processno,processversion,from_node_id,to_node_id,action)
            SELECT processno,@next,from_node_id,to_node_id,action FROM ""YMES"".ym_prsprocess_edge WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version);
            ",connection,transaction)){AddVarchar(command,"next",next);AddVarchar(command,"no",old.ProcessNo);AddVarchar(command,"version",old.ProcessVersion);command.ExecuteNonQuery();}
        transaction.Commit(); return GetProcess(no,next);
    }

    public ProcessDto SetCurrentProcess(string no,string version,int dataStamp,string actorUserNo)
    {
        ValidateActor(actorUserNo); var current=GetProcess(no,version); if(current.IssueState!=2) throw Conflict("只有 ACTIVE 流程版本可以設為目前版本。"); EnsureStamp(current.DataStamp,dataStamp);
        using var connection=_dataSource.OpenConnection(); using var transaction=connection.BeginTransaction();
        YMes.Api.Enterprise.Et202ProcessReferenceGuard.LockProcessNamespace(connection, transaction, current.ProcessNo, (status, message) => new MasterDataRuleException(status, message));
        using(var command=new NpgsqlCommand(@"UPDATE ""YMES"".tblprsprocessbasis SET curversion=0,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() WHERE lower(processno)=lower(@no) AND curversion=1 AND lower(processversion)<>lower(@version);",connection,transaction))
        {AddVarchar(command,"actor",actorUserNo.Trim());AddVarchar(command,"no",current.ProcessNo);AddVarchar(command,"version",current.ProcessVersion);command.ExecuteNonQuery();}
        using(var command=new NpgsqlCommand(@"UPDATE ""YMES"".tblprsprocessbasis SET curversion=1,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version) AND issuestate=2 AND COALESCE(datastamp,0)=@stamp;",connection,transaction))
        {AddVarchar(command,"actor",actorUserNo.Trim());AddVarchar(command,"no",current.ProcessNo);AddVarchar(command,"version",current.ProcessVersion);AddNumeric(command,"stamp",dataStamp);ExecuteOptimisticUpdate(command,"流程版本已變更，請重新載入後再操作。");}
        transaction.Commit();return GetProcess(no,version);
    }

    public void DeleteProcess(string no,string version,int issueState,int dataStamp,string actorUserNo)
    {
        ValidateActor(actorUserNo); using var connection=_dataSource.OpenConnection(); using var transaction=connection.BeginTransaction();
        // ET202 writers use the existing PRS identifier mutex before Process
        // SHARE locks; deletion takes the same namespace before parent UPDATE.
        YMes.Api.Enterprise.Et202ProcessReferenceGuard.LockProcessNamespace(connection, transaction, no, (status, message) => new MasterDataRuleException(status, message));
        var current=GetProcessForUpdate(connection,transaction,no,version); EnsureRemoval(current.IssueState,issueState,current.DataStamp,dataStamp);
        if(current.IssueState==2)
        {
            using var command=new NpgsqlCommand(@"UPDATE ""YMES"".tblprsprocessbasis SET issuestate=-1,curversion=0,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() WHERE processno=@no AND processversion=@version AND COALESCE(datastamp,0)=@stamp;",connection,transaction);
            AddVarchar(command,"actor",actorUserNo.Trim());AddVarchar(command,"no",current.ProcessNo);AddVarchar(command,"version",current.ProcessVersion);AddNumeric(command,"stamp",dataStamp);ExecuteOptimisticUpdate(command,"流程資料已變更，請重新載入後再操作。");
        }
        else
        {
            if (current.IssueState is not (0 or -1)) throw Conflict("只有 UNFROZEN0 或 UNUSED-1 流程可實體刪除；未知狀態資料已保留。");
            try
            {
                var osItemsPresent = YMes.Api.Enterprise.Et202ProcessReferenceGuard.EnsureOptional(
                    connection, transaction, () => new MasterDataRuleException(503, "外包項目原生結構尚未確認，暫停流程實體刪除。"));
                if (YMes.Api.Enterprise.Et202ProcessReferenceGuard.HasReferences(connection, transaction, current.ProcessNo, osItemsPresent))
                    throw Conflict("流程仍被外包項目引用，不能物理刪除；已核准流程仍可依既有規則退用。");
            }
            catch (NpgsqlException)
            {
                throw new MasterDataRuleException(503, "無法核對外包項目的流程引用，請確認資料表與 processno 讀取權限。");
            }
            using(var dependent=new NpgsqlCommand(@"SELECT EXISTS(SELECT 1 FROM ""YMES"".ym_product_process WHERE lower(processno)=lower(@no) AND (process_version_selector='*' OR lower(process_version_selector)=lower(@version)));",connection,transaction))
            {AddVarchar(dependent,"no",current.ProcessNo);AddVarchar(dependent,"version",current.ProcessVersion);if((bool)(dependent.ExecuteScalar()??false))throw Conflict("流程仍被產品流程使用，不能物理刪除。");}
            using var command=new NpgsqlCommand(@"DELETE FROM ""YMES"".tblprsprocessbasis WHERE processno=@no AND processversion=@version AND issuestate=@state AND issuestate IN (0,-1) AND COALESCE(datastamp,0)=@stamp;",connection,transaction);
            AddVarchar(command,"no",current.ProcessNo);AddVarchar(command,"version",current.ProcessVersion);AddNumeric(command,"state",current.IssueState);AddNumeric(command,"stamp",dataStamp);ExecuteOptimisticUpdate(command,"流程資料已變更，請重新載入後再操作。");
        }
        transaction.Commit();
    }

    private void ValidateGraph(ProcessGraphDto graph)
    {
        if(graph.Nodes.Count<2) throw BadRequest("流程圖至少需要 START 與 END 節點。");
        if(graph.Nodes.Select(x=>x.NodeId).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=graph.Nodes.Count) throw BadRequest("NodeId 不可重複。");
        if(graph.Nodes.Select(x=>x.NodeNo).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=graph.Nodes.Count) throw BadRequest("節點編號不可重複。");
        if(graph.Nodes.Select(x=>x.Sequence).Distinct().Count()!=graph.Nodes.Count) throw BadRequest("節點順序不可重複。");
        if(graph.Nodes.Count(x=>Same(x.NodeType,"START"))!=1 || graph.Nodes.Count(x=>Same(x.NodeType,"END"))!=1) throw BadRequest("流程圖必須且只能有一個 START 和一個 END。");
        var ids=graph.Nodes.Select(x=>x.NodeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var n in graph.Nodes){_ = Required(n.NodeId,"NodeId",50);_ = Required(n.NodeNo,"節點編號",50);_ = Required(n.NodeType,"節點類型",10);_ = Optional(n.Description,255,"節點說明"); if(n.Sequence<0||n.Sequence>9999) throw BadRequest("節點順序須介於 0 到 9999。"); if(!Same(n.NodeType,"START")&&!Same(n.NodeType,"END")&&!Same(n.NodeType,"OP")) throw BadRequest("節點類型只支援 START、OP、END。");}
        if(graph.Edges.Select(x=>$"{x.FromNodeId}\u001f{x.ToNodeId}\u001f{x.Action}").Distinct(StringComparer.OrdinalIgnoreCase).Count()!=graph.Edges.Count)throw BadRequest("流程圖連線不可重複。");
        foreach(var edge in graph.Edges){if(!ids.Contains(edge.FromNodeId)||!ids.Contains(edge.ToNodeId))throw BadRequest("連線的起點與終點都必須存在於流程圖。");if(Same(edge.FromNodeId,edge.ToNodeId))throw BadRequest("流程圖不可建立自我連線。");_ = Optional(edge.Action,255,"連線條件");}
        foreach(var node in graph.Nodes.Where(x=>Same(x.NodeType,"OP")))
        {
            if(string.IsNullOrWhiteSpace(node.OperationNo)) throw BadRequest($"節點 {node.NodeNo} 必須指定作業。");
            using var command=_dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblopbasis WHERE lower(opno)=lower(@no));"); AddVarchar(command,"no",node.OperationNo.Trim()); if(!(bool)(command.ExecuteScalar()??false))throw BadRequest($"節點 {node.NodeNo} 引用不存在的作業 {node.OperationNo}。");
        }
    }

    private static void ValidateProcess(ProcessWriteRequest request)
    {
        _=RequiredCode(request.ProcessNo,"流程編號",30);_=RequiredCode(request.ProcessVersion,"流程版本",3);_=RequiredCode(request.ProcessType,"流程類別",20);_=RequiredCode(request.ProductionSegmentNo,"製程段",50);_=Optional(request.Description,255,"說明");
        if(request.ProcessClass is <0 or >99 || request.PartialType is <0 or >9)throw BadRequest("流程類別或部分流程代碼超出資料表範圍。");
    }

    private static void AddProcessEditParameters(NpgsqlCommand command,ProcessWriteRequest request)
    {AddNumeric(command,"class",request.ProcessClass);AddVarchar(command,"segment",RequiredCode(request.ProductionSegmentNo,"製程段",50));AddVarchar(command,"description",Optional(request.Description,255,"說明"));AddNumeric(command,"tuning",request.TuningProcess?1:0);AddNumeric(command,"partial",request.PartialType);}

    private static ProcessDto MapProcess(NpgsqlDataReader r)=>new(){ProcessNo=r.GetString(0),ProcessVersion=r.GetString(1),ProcessType=r.GetString(2),ProductionSegmentNo=r.GetString(3),ProcessClass=ToInt32(r.GetValue(4)),Description=r.GetString(5),TuningProcess=r.GetBoolean(6),PartialType=ToInt32(r.GetValue(7)),IssueState=ToInt32(r.GetValue(8)),CurrentVersion=r.GetBoolean(9),DataStamp=ToInt32(r.GetValue(10)),CreatedAt=r.GetDateTime(11)};

    private static ProcessGraphDto DefaultGraph(int stamp)=>new([new("start","START","START",0,"","開始"),new("end","END","END",10,"","結束")],[new("start","end","pass")],stamp);
    private static void InsertDefaultGraph(NpgsqlConnection c,NpgsqlTransaction t,string no,string version)
    {foreach(var n in DefaultGraph(1).Nodes){using var cmd=new NpgsqlCommand("INSERT INTO \"YMES\".ym_prsprocess_node(processno,processversion,nodeid,nodeno,nodetype,sequence,operationno,description) VALUES(@no,@v,@id,@number,@type,@sequence,NULL,@description);",c,t);AddVarchar(cmd,"no",no);AddVarchar(cmd,"v",version);AddVarchar(cmd,"id",n.NodeId);AddVarchar(cmd,"number",n.NodeNo);AddVarchar(cmd,"type",n.NodeType);AddNumeric(cmd,"sequence",n.Sequence);AddVarchar(cmd,"description",n.Description);cmd.ExecuteNonQuery();}using var edge=new NpgsqlCommand("INSERT INTO \"YMES\".ym_prsprocess_edge(processno,processversion,from_node_id,to_node_id,action) VALUES(@no,@v,'start','end','pass');",c,t);AddVarchar(edge,"no",no);AddVarchar(edge,"v",version);edge.ExecuteNonQuery();}

    private static ProcessDto GetProcessForUpdate(NpgsqlConnection c,NpgsqlTransaction t,string no,string version)
    {using var cmd=new NpgsqlCommand($"SELECT {ProcessColumns} FROM \"YMES\".tblprsprocessbasis WHERE lower(processno)=lower(@no) AND lower(processversion)=lower(@version) FOR UPDATE;",c,t);AddVarchar(cmd,"no",no.Trim());AddVarchar(cmd,"version",version.Trim());using var r=cmd.ExecuteReader();return r.Read()?MapProcess(r):throw NotFound("流程",$"{no}/{version}");}
}
