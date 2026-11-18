using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc501Qc502Repository(NpgsqlDataSource dataSource)
{
    // Source IssueState NULL is shown as the UI's default state 0. DataStamp
    // retains NULL so optimistic concurrency distinguishes it from numeric 0.
    private const string TypeColumns = "inspmachinetype,inspmachinetypename,insptype,baudrate,paritybit,databit,stopbit,endcode,format1,format2,chno,inspdriverno,description,creator,createdate,COALESCE(issuestate,0)::int,datastamp,reviser,revisedate";
    private const string MachineColumns = "inspmachineno,inspmachinename,inspmachinetype,insptype,baudrate,paritybit,databit,stopbit,endcode,format1,format2,chno,inspdriverno,description,creator,createdate,COALESCE(issuestate,0)::int,datastamp,reviser,revisedate";

    public IReadOnlyList<Qc501MachineTypeDto> ListTypes(int issueState, string? search)
    {
        ValidateState(issueState);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcinspmachineparametertype");
        var result = new List<Qc501MachineTypeDto>();
        using var command = new NpgsqlCommand($"SELECT {TypeColumns} FROM \"YMES\".tblqcinspmachineparametertype WHERE COALESCE(issuestate,0)=@state AND (@search='' OR position(lower(@search) in lower(inspmachinetype))>0) ORDER BY inspmachinetype;", connection);
        AddNumeric(command, "state", issueState); AddVarchar(command, "search", (search ?? "").Trim(), 50);
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(ReadType(reader));
        return result;
    }

    public Qc501MachineTypeDto GetType(string inspMachineType)
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcinspmachineparametertype");
        return ReadType(connection, null, NormalizeKey(inspMachineType, "InspMachineType", 50), false);
    }

    public IReadOnlyList<Qc501MachineTypeDto> TypeLookup()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcinspmachineparametertype");
        var result = new List<Qc501MachineTypeDto>();
        using var command = new NpgsqlCommand($"SELECT {TypeColumns} FROM \"YMES\".tblqcinspmachineparametertype ORDER BY inspmachinetype;", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(ReadType(reader));
        return result;
    }

    public IReadOnlyList<QcInspectionDriverDto> DriverLookup()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcinspdriver");
        var result = new List<QcInspectionDriverDto>();
        using var command = new NpgsqlCommand("SELECT inspdriverno,functionname,description FROM \"YMES\".tblqcinspdriver ORDER BY inspdriverno;", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(new(reader.GetString(0), NullableText(reader, 1), NullableText(reader, 2)));
        return result;
    }

    public IReadOnlyList<Qc502InspectionMachineDto> ListMachines(int issueState, string? search)
    {
        ValidateState(issueState);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcinspmachineparameter");
        var result = new List<Qc502InspectionMachineDto>();
        using var command = new NpgsqlCommand($"SELECT {MachineColumns} FROM \"YMES\".tblqcinspmachineparameter WHERE COALESCE(issuestate,0)=@state AND (@search='' OR position(lower(@search) in lower(inspmachineno))>0) ORDER BY inspmachineno;", connection);
        AddNumeric(command, "state", issueState); AddVarchar(command, "search", (search ?? "").Trim(), 50);
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(ReadMachine(reader));
        return result;
    }

    public Qc502InspectionMachineDto GetMachine(string inspMachineNo)
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcinspmachineparameter");
        return ReadMachine(connection, null, NormalizeKey(inspMachineNo, "InspMachineNo", 50), false);
    }

    public Qc501MachineTypeDto CreateType(Qc501MachineTypeWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var value = NormalizeType(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparametertype");
            EnsureUniqueType(connection, transaction, value.InspMachineType, null);
            EnsureDriver(connection, transaction, value.InspType, value.DriverNo);
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcinspmachineparametertype
                    (inspmachinetype,inspmachinetypename,insptype,baudrate,paritybit,databit,stopbit,endcode,format1,format2,chno,inspdriverno,description,creator,createdate,issuestate,datastamp)
                VALUES (@no,@name,@type,@baud,@parity,@data,@stop,@end,@format1,@format2,@channel,@driver,@description,@actor,now(),0,0);
                """, connection, transaction);
            BindType(command, value); AddVarchar(command, "actor", actor.Trim(), 10); command.ExecuteNonQuery();
            transaction.Commit();
            return GetType(value.InspMachineType);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(StatusCodes.Status409Conflict, "InspMachineType 已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc501MachineTypeDto UpdateType(string key, Qc501MachineTypeWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var no = NormalizeKey(key, "InspMachineType", 50);
        var value = NormalizeType(request);
        if (!string.Equals(no, value.InspMachineType, StringComparison.OrdinalIgnoreCase)) throw Rule(400, "InspMachineType 不可修改。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparametertype");
            var current = ReadType(connection, transaction, no, true);
            EnsureExpected(current.IssueState, current.DataStamp, request.IssueState, request.DataStamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的資料不可修改。");
            if (current.InspType != value.InspType) throw Rule(400, "InspType 不可在修改時變更。");
            EnsureDriver(connection, transaction, value.InspType, value.DriverNo);
            using var command = new NpgsqlCommand("""
                UPDATE "YMES".tblqcinspmachineparametertype
                SET inspmachinetypename=@name,baudrate=@baud,paritybit=@parity,databit=@data,stopbit=@stop,
                    endcode=@end,format1=@format1,format2=@format2,chno=@channel,inspdriverno=@driver,
                    description=@description,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE lower(inspmachinetype)=lower(@no) AND COALESCE(issuestate,0)=@state AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction);
            BindType(command, value); AddVarchar(command, "actor", actor.Trim(), 10);
            AddNumeric(command, "state", current.IssueState); AddNumeric(command, "stamp", current.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return GetType(no);
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc502InspectionMachineDto CreateMachine(Qc502InspectionMachineWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var value = NormalizeMachine(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparameter");
            EnsureUniqueMachine(connection, transaction, value.InspMachineNo, null);
            if (value.TypeKey is not null) value = ApplyType(connection, transaction, value);
            EnsureDriver(connection, transaction, value.InspType, value.DriverNo);
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcinspmachineparameter
                    (inspmachineno,inspmachinename,inspmachinetype,insptype,baudrate,paritybit,databit,stopbit,endcode,format1,format2,chno,inspdriverno,description,creator,createdate,issuestate,datastamp)
                VALUES (@no,@name,@typekey,@type,@baud,@parity,@data,@stop,@end,@format1,@format2,@channel,@driver,@description,@actor,now(),0,0);
                """, connection, transaction);
            BindMachine(command, value); AddVarchar(command, "actor", actor.Trim(), 10); command.ExecuteNonQuery();
            transaction.Commit();
            return GetMachine(value.InspMachineNo);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(StatusCodes.Status409Conflict, "InspMachineNo 已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc502InspectionMachineDto UpdateMachine(string key, Qc502InspectionMachineWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var no = NormalizeKey(key, "InspMachineNo", 50);
        var value = NormalizeMachine(request);
        if (!string.Equals(no, value.InspMachineNo, StringComparison.OrdinalIgnoreCase)) throw Rule(400, "InspMachineNo 不可修改。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparameter");
            var current = ReadMachine(connection, transaction, no, true);
            EnsureExpected(current.IssueState, current.DataStamp, request.IssueState, request.DataStamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的資料不可修改。");
            if (value.TypeKey is null && current.InspType != value.InspType)
                throw Rule(400, "未套用類別時，InspType 不可在修改時變更。");
            if (current.InspMachineType is not null && value.TypeKey is null)
                throw Rule(400, "舊服務不支援清除既有 InspMachineType 關聯；請保留或改選其他類別。");
            if (value.TypeKey is not null) value = ApplyType(connection, transaction, value);
            EnsureDriver(connection, transaction, value.InspType, value.DriverNo);
            using var command = new NpgsqlCommand("""
                UPDATE "YMES".tblqcinspmachineparameter
                SET inspmachinename=@name,inspmachinetype=@typekey,insptype=@type,baudrate=@baud,paritybit=@parity,databit=@data,stopbit=@stop,
                    endcode=@end,format1=@format1,format2=@format2,chno=@channel,inspdriverno=@driver,
                    description=@description,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE lower(inspmachineno)=lower(@no) AND COALESCE(issuestate,0)=@state AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction);
            BindMachine(command, value); AddVarchar(command, "actor", actor.Trim(), 10);
            AddNumeric(command, "state", current.IssueState); AddNumeric(command, "stamp", current.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return GetMachine(no);
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc502InspectionMachineDto DeleteMachine(string key, int issueState, decimal? dataStamp)
    {
        var no = NormalizeKey(key, "InspMachineNo", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparameter");
            var current = ReadMachine(connection, transaction, no, true);
            EnsureExpected(current.IssueState, current.DataStamp, issueState, dataStamp);
            EnsureDeletableState(current.IssueState);
            EnsureNoReferences(connection, transaction, "tblqcinspmachinebasis", "inspmachineno", no, "此儀器已被儀器基礎資料引用，不能刪除。");
            EnsureNoReferences(connection, transaction, "tblqcinspmachineproperty", "inspmachine", no, "此儀器已有屬性資料，不能刪除。");
            using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcinspmachineparameter WHERE lower(inspmachineno)=lower(@no) AND COALESCE(issuestate,0)=@state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
            AddVarchar(command, "no", no, 50); AddNumeric(command, "state", current.IssueState); AddNumeric(command, "stamp", current.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return current;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc501MachineTypeDto DeleteType(string key, int issueState, decimal? dataStamp)
    {
        var no = NormalizeKey(key, "InspMachineType", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparametertype");
            var current = ReadType(connection, transaction, no, true);
            EnsureExpected(current.IssueState, current.DataStamp, issueState, dataStamp);
            EnsureDeletableState(current.IssueState);
            EnsureNoReferences(connection, transaction, "tblqcinspmachineparameter", "inspmachinetype", no, "此儀器類別已被檢測儀器引用，不能刪除。");
            EnsureNoReferences(connection, transaction, "tblqcinspmachinetype", "inspmachinetype", no, "此儀器類別已被儀器類型資料引用，不能刪除。");
            using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcinspmachineparametertype WHERE lower(inspmachinetype)=lower(@no) AND COALESCE(issuestate,0)=@state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
            AddVarchar(command, "no", no, 50); AddNumeric(command, "state", current.IssueState); AddNumeric(command, "stamp", current.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return current;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc501MachineTypeDto ApproveType(string key, QcInspectionStateRequest request, string actor)
    {
        ValidateActor(actor);
        if (request.IssueState != 0) throw Rule(400, "核准只接受未簽核狀態。");
        var no = NormalizeKey(key, "InspMachineType", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparametertype");
            var current = ReadType(connection, transaction, no, true);
            EnsureExpected(current.IssueState, current.DataStamp, 0, request.DataStamp);
            EnsureApprovalCanComplete(connection, transaction);
            ChangeTypeState(connection, transaction, no, current, actor, 2);
            transaction.Commit();
            return GetType(no);
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc502InspectionMachineDto ApproveMachine(string key, QcInspectionStateRequest request, string actor)
    {
        ValidateActor(actor);
        if (request.IssueState != 0) throw Rule(400, "核准只接受未簽核狀態。");
        var no = NormalizeKey(key, "InspMachineNo", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcinspmachineparameter");
            var current = ReadMachine(connection, transaction, no, true);
            EnsureExpected(current.IssueState, current.DataStamp, 0, request.DataStamp);
            EnsureApprovalCanComplete(connection, transaction);
            ChangeMachineState(connection, transaction, no, current, actor, 2);
            transaction.Commit();
            return GetMachine(no);
        }
        catch { Rollback(transaction); throw; }
    }

    private static Qc501MachineTypeDto ReadType(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate, bool forShare = false)
    {
        using var command = new NpgsqlCommand($"SELECT {TypeColumns} FROM \"YMES\".tblqcinspmachineparametertype WHERE lower(inspmachinetype)=lower(@no){(forUpdate ? " FOR UPDATE" : forShare ? " FOR SHARE" : "")};", connection, transaction);
        AddVarchar(command, "no", no, 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(StatusCodes.Status404NotFound, $"找不到檢測儀器類別 {no}。");
        return ReadType(reader);
    }

    private static Qc502InspectionMachineDto ReadMachine(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {MachineColumns} FROM \"YMES\".tblqcinspmachineparameter WHERE lower(inspmachineno)=lower(@no){(forUpdate ? " FOR UPDATE" : "")};", connection, transaction);
        AddVarchar(command, "no", no, 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(StatusCodes.Status404NotFound, $"找不到檢測儀器 {no}。");
        return ReadMachine(reader);
    }

    private static Qc501MachineTypeDto ReadType(NpgsqlDataReader r) => new(
        r.GetString(0), NullableText(r, 1), Convert.ToInt32(r.GetValue(2)), NullableDecimal(r, 3), NullableText(r, 4), NullableInt(r, 5), NullableInt(r, 6),
        NullableText(r, 7), NullableText(r, 8), NullableText(r, 9), NullableText(r, 10), NullableText(r, 11), NullableText(r, 12),
        NullableText(r, 13), NullableDate(r, 14), r.GetInt32(15), NullableDecimal(r, 16), NullableText(r, 17), NullableDate(r, 18));

    private static Qc502InspectionMachineDto ReadMachine(NpgsqlDataReader r) => new(
        r.GetString(0), NullableText(r, 1), NullableText(r, 2), Convert.ToInt32(r.GetValue(3)), NullableDecimal(r, 4), NullableText(r, 5), NullableInt(r, 6), NullableInt(r, 7),
        NullableText(r, 8), NullableText(r, 9), NullableText(r, 10), NullableText(r, 11), NullableText(r, 12), NullableText(r, 13),
        NullableText(r, 14), NullableDate(r, 15), r.GetInt32(16), NullableDecimal(r, 17), NullableText(r, 18), NullableDate(r, 19));

    private sealed record TypeValue(string InspMachineType, string? Name, int InspType, decimal? BaudRate, string? ParityBit, int? DataBit,
        int? StopBit, string? EndCode, string? Format1, string? Format2, string? ChNo, string? DriverNo, string? Description);
    private sealed record MachineValue(string InspMachineNo, string? Name, string? TypeKey, int InspType, decimal? BaudRate, string? ParityBit,
        int? DataBit, int? StopBit, string? EndCode, string? Format1, string? Format2, string? ChNo, string? DriverNo, string? Description);

    private static TypeValue NormalizeType(Qc501MachineTypeWriteRequest r)
    {
        var no = NormalizeKey(r.InspMachineType, "InspMachineType", 50);
        var name = OptionalText(r.InspMachineTypeName, "InspMachineTypeName", 50);
        var description = OptionalText(r.Description, "Description", 255);
        if (r.InspType is not (0 or 1)) throw Rule(400, "InspType 僅允許 0 (RS232) 或 1 (UserDefine)。");
        var fields = NormalizeParameters(r.InspType, r.BaudRate, r.ParityBit, r.DataBit, r.StopBit, r.EndCode, r.Format1, r.Format2, r.ChNo, r.InspDriverNo);
        return new(no, name, r.InspType, fields.Baud, fields.Parity, fields.Data, fields.Stop, fields.End, fields.Format1, fields.Format2, fields.ChNo, fields.Driver, description);
    }

    private static MachineValue NormalizeMachine(Qc502InspectionMachineWriteRequest r)
    {
        var no = NormalizeKey(r.InspMachineNo, "InspMachineNo", 50);
        var name = OptionalText(r.InspMachineName, "InspMachineName", 50);
        var typeKey = string.IsNullOrWhiteSpace(r.InspMachineType) ? null : NormalizeKey(r.InspMachineType, "InspMachineType", 50);
        var description = OptionalText(r.Description, "Description", 255);
        if (r.InspType is not (0 or 1)) throw Rule(400, "InspType 僅允許 0 (RS232) 或 1 (UserDefine)。");
        // A selected QC501 class is authoritative. ApplyType reloads and validates
        // its communication parameters inside the write transaction.
        if (typeKey is not null)
            return new(no, name, typeKey, r.InspType, null, null, null, null, null, null, null, null, null, description);
        var fields = NormalizeParameters(r.InspType, r.BaudRate, r.ParityBit, r.DataBit, r.StopBit, r.EndCode, r.Format1, r.Format2, r.ChNo, r.InspDriverNo);
        return new(no, name, typeKey, r.InspType, fields.Baud, fields.Parity, fields.Data, fields.Stop, fields.End, fields.Format1, fields.Format2, fields.ChNo, fields.Driver, description);
    }

    private sealed record ParameterValue(decimal? Baud, string? Parity, int? Data, int? Stop, string? End, string? Format1, string? Format2, string? ChNo, string? Driver);
    private static ParameterValue NormalizeParameters(int inspType, decimal? baud, string? parity, int? data, int? stop,
        string? end, string? format1, string? format2, string? chNo, string? driver)
    {
        if (inspType == 1)
        {
            var driverNo = NormalizeKey(driver, "InspDriverNo", 25);
            return new(null, null, null, null, null, null, null, null, driverNo);
        }
        if (baud is not (600 or 1200 or 2400 or 4800 or 9600 or 19200)) throw Rule(400, "BaudRate 必須為 600、1200、2400、4800、9600 或 19200。");
        if (parity is not ("None" or "Odd" or "Even" or "Mark")) throw Rule(400, "ParityBit 必須為 None、Odd、Even 或 Mark。");
        if (data is not (7 or 8)) throw Rule(400, "DataBit 必須為 7 或 8。");
        if (stop is not (1 or 2)) throw Rule(400, "StopBit 必須為 1 或 2。");
        var endCode = (end ?? "").Trim();
        if (endCode is not ("0" or "3" or "10" or "13")) throw Rule(400, "EndCode 必須為 0、3、10 或 13。");
        var format = NormalizeKey(format1, "Format1", 50);
        var formatTwo = (format2 ?? "").Trim().ToUpperInvariant();
        var channel = (chNo ?? "").Trim().ToUpperInvariant();
        if (formatTwo.Length > 50) throw Rule(400, "Format2 不可超過 50 個字元。");
        if (channel.Length > 3) throw Rule(400, "ChNo 不可超過資料表欄位長度 3 個字元。");
        return new(baud, parity, data, stop, endCode, format, formatTwo.Length == 0 ? "NA" : formatTwo, channel.Length == 0 ? "NA" : channel, null);
    }

    private static void BindType(NpgsqlCommand command, TypeValue v)
    {
        AddVarchar(command, "no", v.InspMachineType, 50); AddVarchar(command, "name", v.Name, 50);
        AddNumeric(command, "type", v.InspType); AddNumeric(command, "baud", v.BaudRate); AddVarchar(command, "parity", v.ParityBit, 10);
        AddNumeric(command, "data", v.DataBit); AddNumeric(command, "stop", v.StopBit); AddVarchar(command, "end", v.EndCode, 6);
        AddVarchar(command, "format1", v.Format1, 50); AddVarchar(command, "format2", v.Format2, 50); AddVarchar(command, "channel", v.ChNo, 3);
        AddVarchar(command, "driver", v.DriverNo, 25); AddVarchar(command, "description", v.Description, 255);
    }

    private static void BindMachine(NpgsqlCommand command, MachineValue v)
    {
        AddVarchar(command, "no", v.InspMachineNo, 50); AddVarchar(command, "name", v.Name, 50); AddVarchar(command, "typekey", v.TypeKey, 50);
        AddNumeric(command, "type", v.InspType); AddNumeric(command, "baud", v.BaudRate); AddVarchar(command, "parity", v.ParityBit, 10);
        AddNumeric(command, "data", v.DataBit); AddNumeric(command, "stop", v.StopBit); AddVarchar(command, "end", v.EndCode, 6);
        AddVarchar(command, "format1", v.Format1, 50); AddVarchar(command, "format2", v.Format2, 50); AddVarchar(command, "channel", v.ChNo, 3);
        AddVarchar(command, "driver", v.DriverNo, 25); AddVarchar(command, "description", v.Description, 255);
    }

    private static MachineValue ApplyType(NpgsqlConnection connection, NpgsqlTransaction transaction, MachineValue machine)
    {
        // Keep the referenced class stable until the child write commits so a
        // concurrent class update/delete cannot leave copied settings or links stale.
        var type = ReadType(connection, transaction, machine.TypeKey!, false, forShare: true);
        var applied = machine with
        {
            InspType = type.InspType, BaudRate = type.BaudRate, ParityBit = type.ParityBit, DataBit = type.DataBit,
            StopBit = type.StopBit, EndCode = type.EndCode, Format1 = type.Format1, Format2 = type.Format2,
            ChNo = type.ChNo, DriverNo = type.InspDriverNo
        };
        var fields = NormalizeParameters(applied.InspType, applied.BaudRate, applied.ParityBit, applied.DataBit,
            applied.StopBit, applied.EndCode, applied.Format1, applied.Format2, applied.ChNo, applied.DriverNo);
        return applied with { BaudRate = fields.Baud, ParityBit = fields.Parity, DataBit = fields.Data, StopBit = fields.Stop,
            EndCode = fields.End, Format1 = fields.Format1, Format2 = fields.Format2, ChNo = fields.ChNo, DriverNo = fields.Driver };
    }

    private static void EnsureDriver(NpgsqlConnection connection, NpgsqlTransaction transaction, int inspType, string? driverNo)
    {
        if (inspType == 0) return;
        EnsureTable(connection, transaction, "tblqcinspdriver");
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcinspdriver WHERE lower(inspdriverno)=lower(@no) FOR KEY SHARE;", connection, transaction);
        AddVarchar(command, "no", driverNo ?? "", 25);
        if (command.ExecuteScalar() is null) throw Rule(422, "找不到所選的 InspDriverNo。");
    }

    private static void EnsureUniqueType(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, string? except)
    {
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcinspmachineparametertype WHERE lower(inspmachinetype)=lower(@no) AND (@except IS NULL OR lower(inspmachinetype)<>lower(@except));", connection, transaction);
        AddVarchar(command, "no", no, 50); AddVarchar(command, "except", except, 50);
        if (command.ExecuteScalar() is not null) throw Rule(StatusCodes.Status409Conflict, "InspMachineType 已存在。");
    }

    private static void EnsureUniqueMachine(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, string? except)
    {
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcinspmachineparameter WHERE lower(inspmachineno)=lower(@no) AND (@except IS NULL OR lower(inspmachineno)<>lower(@except));", connection, transaction);
        AddVarchar(command, "no", no, 50); AddVarchar(command, "except", except, 50);
        if (command.ExecuteScalar() is not null) throw Rule(StatusCodes.Status409Conflict, "InspMachineNo 已存在。");
    }

    private static void ChangeTypeState(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, Qc501MachineTypeDto current, string actor, int next)
    {
        using var command = new NpgsqlCommand("UPDATE \"YMES\".tblqcinspmachineparametertype SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1 WHERE lower(inspmachinetype)=lower(@no) AND COALESCE(issuestate,0)=0 AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
        AddNumeric(command, "next", next); AddVarchar(command, "actor", actor.Trim(), 10); AddVarchar(command, "no", no, 50); AddNumeric(command, "stamp", current.DataStamp);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
    }

    private static void ChangeMachineState(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, Qc502InspectionMachineDto current, string actor, int next)
    {
        using var command = new NpgsqlCommand("UPDATE \"YMES\".tblqcinspmachineparameter SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1 WHERE lower(inspmachineno)=lower(@no) AND COALESCE(issuestate,0)=0 AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
        AddNumeric(command, "next", next); AddVarchar(command, "actor", actor.Trim(), 10); AddVarchar(command, "no", no, 50); AddNumeric(command, "stamp", current.DataStamp);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
    }

    private static void EnsureApprovalCanComplete(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }
            .Any(table => TableExists(connection, transaction, table)))
            throw Rule(StatusCodes.Status503ServiceUnavailable, "YMES 已存在 Issue/Job 流程表，但 QC501/QC502 尚未接入共用簽核服務；資料未變更。");
    }

    private static void EnsureNoReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string column, string value, string message)
    {
        if (!TableExists(connection, transaction, table)) return;
        using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{table} WHERE lower({column})=lower(@value) LIMIT 1;", connection, transaction);
        AddVarchar(command, "value", value, 50);
        if (command.ExecuteScalar() is not null) throw Rule(StatusCodes.Status409Conflict, message);
    }

    private static void EnsureExpected(int currentState, decimal? currentStamp, int expectedState, decimal? expectedStamp)
    {
        ValidateState(expectedState);
        if (currentState != expectedState || currentStamp != expectedStamp) throw Conflict();
    }

    private static void EnsureDeletableState(int state)
    {
        if (state == 1) throw Rule(StatusCodes.Status409Conflict, "簽核中的資料不可刪除。");
        if (state is not (0 or 2 or -1)) throw Rule(400, "此狀態不允許刪除。");
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='YMES' AND table_name=@name);", connection, transaction);
        AddVarchar(command, "name", table, 63);
        return command.ExecuteScalar() is true;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        if (!TableExists(connection, transaction, table))
            throw Rule(StatusCodes.Status503ServiceUnavailable, $"YMES 尚未建立 {table.ToUpperInvariant()}（QC501-QC503 migration）。");
    }

    private static string NormalizeKey(string? value, string field, int max)
    {
        var text = (value ?? "").Trim().ToUpperInvariant();
        if (text.Length == 0 || text.Length > max || text.Any(char.IsControl)) throw Rule(400, $"{field} 為必填，且不可超過 {max} 個字元或含控制字元。");
        return text;
    }

    private static string? OptionalText(string? value, string field, int max)
    {
        var text = (value ?? "").Trim();
        if (text.Length > max || text.Any(char.IsControl)) throw Rule(400, $"{field} 不可超過 {max} 個字元或含控制字元。");
        return text.Length == 0 ? null : text;
    }

    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10) throw Rule(StatusCodes.Status401Unauthorized, "登入帳號不可空白且不可超過 10 個字元。");
    }

    private static void ValidateState(int state)
    {
        if (state is not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
    }

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static int? NullableInt(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);

    private static void AddVarchar(NpgsqlCommand command, string name, string? value, int size)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Varchar, size);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static void AddNumeric(NpgsqlCommand command, string name, decimal? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Numeric);
        parameter.Value = (object?)value ?? DBNull.Value;
    }
    private static void AddNumeric(NpgsqlCommand command, string name, int value) => AddNumeric(command, name, (decimal)value);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* preserve the original exception */ } }
    private static QcInspectionRuleException Rule(int status, string message) => new(status, message);
    private static QcInspectionRuleException Conflict() => Rule(StatusCodes.Status409Conflict, "資料已被其他作業修改，請重新查詢後再試。");
}
