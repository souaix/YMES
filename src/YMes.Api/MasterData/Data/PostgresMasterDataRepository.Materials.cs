using System.Globalization;
using MES.Contracts;
using Npgsql;

namespace MES.Api.Data;

public sealed partial class PostgresMasterDataRepository
{
    private const string MaterialColumns = """
        materialno,
        materialtype,
        COALESCE(materialname, ''),
        COALESCE(materialspec, ''),
        COALESCE(description, ''),
        COALESCE(putinplace, 0)::numeric,
        COALESCE(countway, 0)::numeric,
        COALESCE(checklotno, 0)::numeric,
        COALESCE(unittype, ''),
        COALESCE(unitno, ''),
        COALESCE(safeqty, 0)::numeric,
        COALESCE(keymaterials, 0) = 1,
        COALESCE(openlimittimeflag, 0) = 1,
        openlimittime,
        COALESCE(resetopendate, 0) = 1,
        COALESCE(expiredateflag, 0) = 1,
        COALESCE(erpno, ''),
        issuestate::numeric,
        COALESCE(datastamp, 0)::numeric,
        COALESCE(createdate, now())
        """;

    public IReadOnlyList<MaterialDto> ListMaterials(string? keyword, int? issueState, string? materialType)
    {
        using var command = _dataSource.CreateCommand($"""
            SELECT {MaterialColumns}
            FROM "YMES".tblmtlmaterialbasis
            WHERE (@issueState IS NULL OR issuestate = @issueState)
              AND (@materialType IS NULL OR materialtype ILIKE @materialType)
              AND (@keyword IS NULL OR materialno ILIKE @keyword OR materialname ILIKE @keyword)
            ORDER BY materialno;
            """);
        AddNumeric(command, "issueState", issueState);
        AddVarchar(command, "materialType", string.IsNullOrWhiteSpace(materialType) ? null : materialType.Trim());
        AddVarchar(command, "keyword", string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%");
        using var reader = command.ExecuteReader();
        var rows = new List<MaterialDto>();
        while (reader.Read()) rows.Add(MapMaterial(reader));
        return rows;
    }

    public MaterialDto GetMaterial(string no)
    {
        using var command = _dataSource.CreateCommand($"""
            SELECT {MaterialColumns}
            FROM "YMES".tblmtlmaterialbasis
            WHERE lower(materialno) = lower(@no)
            LIMIT 1;
            """);
        AddVarchar(command, "no", no.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapMaterial(reader) : throw NotFound("物料", no);
    }

    public MaterialDto CreateMaterial(MaterialWriteRequest request, string actorUserNo)
    {
        ValidateMaterial(request);
        ValidateActor(actorUserNo);
        EnsureMaterialCodeAvailable(request.MaterialNo);
        using var command = _dataSource.CreateCommand("""
            INSERT INTO "YMES".tblmtlmaterialbasis
                (materialno, materialtype, issuestate, description, creator, createdate,
                 materialname, materialspec, putinplace, countway, checklotno, unittype,
                 safeqty, unitno, keymaterials, openlimittimeflag, openlimittime,
                 resetopendate, expiredateflag, erpno, datastamp)
            VALUES
                (@no, @type, 0, @description, @actor, now(), @name, @spec, @putInPlace,
                 @countWay, @checkLotNo, @unitType, @safeQty, @unitNo, @keyMaterials,
                 @openLimitFlag, @openLimitTime, @resetOpenDate, @expireDateFlag, @erpNo, 1);
            """);
        AddMaterialParameters(command, request);
        AddVarchar(command, "actor", actorUserNo.Trim());
        ExecuteInsert(command, $"物料編號 {NormalizeCode(request.MaterialNo)} 已存在。");
        return GetMaterial(request.MaterialNo);
    }

    public MaterialDto UpdateMaterial(string no, MaterialWriteRequest request, string actorUserNo)
    {
        ValidateMaterial(request);
        ValidateActor(actorUserNo);
        var current = GetMaterial(no);
        EnsureEditable(current.IssueState, current.DataStamp, request.DataStamp);
        if (!Same(current.MaterialNo, request.MaterialNo)) throw BadRequest("更新時不可變更 MaterialNo。");

        using var command = _dataSource.CreateCommand("""
            UPDATE "YMES".tblmtlmaterialbasis
            SET materialtype = @type,
                materialname = @name,
                materialspec = @spec,
                description = @description,
                putinplace = @putInPlace,
                countway = @countWay,
                checklotno = @checkLotNo,
                unittype = @unitType,
                unitno = @unitNo,
                safeqty = @safeQty,
                keymaterials = @keyMaterials,
                openlimittimeflag = @openLimitFlag,
                openlimittime = @openLimitTime,
                resetopendate = @resetOpenDate,
                expiredateflag = @expireDateFlag,
                erpno = @erpNo,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE lower(materialno) = lower(@no)
              AND COALESCE(datastamp, 0) = @dataStamp
              AND issuestate = 0;
            """);
        AddMaterialParameters(command, request);
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "no", current.MaterialNo);
        AddNumeric(command, "dataStamp", request.DataStamp);
        ExecuteOptimisticUpdate(command, "物料資料已變更，請重新載入後再儲存。");
        return GetMaterial(current.MaterialNo);
    }

    public MaterialDto ApproveMaterial(string no, int dataStamp, string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var command = _dataSource.CreateCommand("""
            UPDATE "YMES".tblmtlmaterialbasis
            SET issuestate = 2,
                datastamp = COALESCE(datastamp, 0) + 1,
                reviser = @actor,
                revisedate = now()
            WHERE lower(materialno) = lower(@no)
              AND COALESCE(datastamp, 0) = @dataStamp
              AND issuestate = 0;
            """);
        AddVarchar(command, "actor", actorUserNo.Trim());
        AddVarchar(command, "no", no.Trim());
        AddNumeric(command, "dataStamp", dataStamp);
        ExecuteOptimisticUpdate(command, "只有 UNFROZEN 且資料未變更的物料可以核准。");
        return GetMaterial(no);
    }

    public void DeleteMaterial(string no, int issueState, int dataStamp, string actorUserNo)
    {
        ValidateActor(actorUserNo);
        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = GetMaterial(connection, transaction, no, true);
        EnsureRemoval(current.IssueState, issueState, current.DataStamp, dataStamp);
        if (current.IssueState == 2)
        {
            using var retire = new NpgsqlCommand("""
                UPDATE "YMES".tblmtlmaterialbasis
                SET issuestate = -1, datastamp = COALESCE(datastamp, 0) + 1,
                    reviser = @actor, revisedate = now()
                WHERE lower(materialno) = lower(@no)
                  AND COALESCE(datastamp, 0) = @dataStamp AND issuestate = 2;
                """, connection, transaction);
            AddVarchar(retire, "actor", actorUserNo.Trim());
            AddVarchar(retire, "no", current.MaterialNo);
            AddNumeric(retire, "dataStamp", dataStamp);
            ExecuteOptimisticUpdate(retire, "物料狀態已變更，請重新載入後再操作。");
        }
        else
        {
            using (var dependent = new NpgsqlCommand("""
                SELECT EXISTS (
                    SELECT 1 FROM "YMES".ym_product_bom
                    WHERE component_type = 'MATERIAL' AND lower(component_no) = lower(@no))
                    OR EXISTS (
                    SELECT 1 FROM "YMES".tblmtlmaterialgroupdetail
                    WHERE lower(materialno) = lower(@no));
                """, connection, transaction))
            {
                AddVarchar(dependent, "no", current.MaterialNo);
                if ((bool)(dependent.ExecuteScalar() ?? false)) throw Conflict("物料仍被物料群組或產品 BOM 使用，不能物理刪除。");
            }
            using var delete = new NpgsqlCommand("""
                DELETE FROM "YMES".tblmtlmaterialbasis
                WHERE lower(materialno) = lower(@no)
                  AND COALESCE(datastamp, 0) = @dataStamp AND issuestate = @issueState;
                """, connection, transaction);
            AddVarchar(delete, "no", current.MaterialNo);
            AddNumeric(delete, "dataStamp", dataStamp);
            AddNumeric(delete, "issueState", issueState);
            ExecuteOptimisticUpdate(delete, "物料資料已變更，請重新載入後再操作。");
        }
        transaction.Commit();
    }

    private static MaterialDto GetMaterial(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"""
            SELECT {MaterialColumns}
            FROM "YMES".tblmtlmaterialbasis
            WHERE lower(materialno) = lower(@no)
            LIMIT 1{(forUpdate ? " FOR UPDATE" : "")};
            """, connection, transaction);
        AddVarchar(command, "no", no.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapMaterial(reader) : throw NotFound("物料", no);
    }

    private void EnsureMaterialCodeAvailable(string materialNo)
    {
        using var command = _dataSource.CreateCommand("""
            SELECT EXISTS (SELECT 1 FROM "YMES".tblopbasis WHERE lower(opno) = lower(@no))
                OR EXISTS (SELECT 1 FROM "YMES".tblprdproductbasis WHERE lower(productno) = lower(@no));
            """);
        AddVarchar(command, "no", materialNo.Trim());
        if ((bool)(command.ExecuteScalar() ?? false)) throw Conflict("物料編號不可與作業或產品編號重複。");
    }

    private static void AddMaterialParameters(NpgsqlCommand command, MaterialWriteRequest request)
    {
        AddVarchar(command, "no", RequiredCode(request.MaterialNo, "物料編號", 40));
        AddVarchar(command, "type", RequiredCode(request.MaterialType, "物料類別", 40));
        AddVarchar(command, "name", Required(request.MaterialName, "物料名稱", 200));
        AddVarchar(command, "spec", Optional(request.MaterialSpec, 200, "物料規格"));
        AddVarchar(command, "description", Optional(request.Description, 255, "說明"));
        AddNumeric(command, "putInPlace", ParsePutInPlace(request.PutInPlace));
        AddNumeric(command, "countWay", request.CountWay);
        AddNumeric(command, "checkLotNo", request.CheckLotNo);
        AddVarchar(command, "unitType", RequiredCode(request.UnitType, "單位類別", 30));
        AddVarchar(command, "unitNo", RequiredCode(request.UnitNo, "單位編號", 30));
        AddNumeric(command, "safeQty", request.SafeQty);
        AddNumeric(command, "keyMaterials", request.KeyMaterials ? 1 : 0);
        AddNumeric(command, "openLimitFlag", request.OpenLimitTimeFlag ? 1 : 0);
        AddNumeric(command, "openLimitTime", request.OpenLimitTime);
        AddNumeric(command, "resetOpenDate", request.ResetOpenDate ? 1 : 0);
        AddNumeric(command, "expireDateFlag", request.ExpireDateFlag ? 1 : 0);
        AddVarchar(command, "erpNo", Optional(request.ErpNo, 50, "ERP 編號"));
    }

    private static void ValidateMaterial(MaterialWriteRequest request)
    {
        _ = RequiredCode(request.MaterialNo, "物料編號", 40);
        _ = RequiredCode(request.MaterialType, "物料類別", 40);
        _ = Required(request.MaterialName, "物料名稱", 200);
        _ = Optional(request.MaterialSpec, 200, "物料規格");
        _ = Optional(request.Description, 255, "說明");
        _ = ParsePutInPlace(request.PutInPlace);
        _ = RequiredCode(request.UnitType, "單位類別", 30);
        _ = RequiredCode(request.UnitNo, "單位編號", 30);
        _ = Optional(request.ErpNo, 50, "ERP 編號");
        if (request.CountWay is < 0 or > 9 || request.CheckLotNo is < 0 or > 1)
            throw BadRequest("計量方法必須介於 0 到 9，批號檢查只能為 0 或 1。");
        if (request.SafeQty is < 0 or > 99999999.99999999m || decimal.Round(request.SafeQty, 8) != request.SafeQty)
            throw BadRequest("安全存量須介於 0 到 99,999,999.99999999，最多八位小數。");
        if (request.OpenLimitTime is < 0 or > 9999.99m || (request.OpenLimitTime.HasValue && decimal.Round(request.OpenLimitTime.Value, 2) != request.OpenLimitTime.Value))
            throw BadRequest("開封時限須介於 0 到 9,999.99，最多兩位小數。");
    }

    private static int ParsePutInPlace(string? value)
    {
        var text = (value ?? "").Trim();
        if (text.Equals("OP", StringComparison.OrdinalIgnoreCase)) return 1;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var code) && code is >= 0 and <= 9) return code;
        throw BadRequest("投入點需為 OP 或原始資料表支援的 0 到 9 數值代碼。");
    }

    private static string PutInPlaceLabel(object value)
    {
        var code = ToInt32(value);
        return code == 1 ? "OP" : code.ToString(CultureInfo.InvariantCulture);
    }

    private static MaterialDto MapMaterial(NpgsqlDataReader reader) => new()
    {
        MaterialNo = reader.GetString(0),
        MaterialType = reader.GetString(1),
        MaterialName = reader.GetString(2),
        MaterialSpec = reader.GetString(3),
        Description = reader.GetString(4),
        PutInPlace = PutInPlaceLabel(reader.GetValue(5)),
        CountWay = ToInt32(reader.GetValue(6)),
        CheckLotNo = ToInt32(reader.GetValue(7)),
        UnitType = reader.GetString(8),
        UnitNo = reader.GetString(9),
        SafeQty = reader.GetDecimal(10),
        KeyMaterials = reader.GetBoolean(11),
        OpenLimitTimeFlag = reader.GetBoolean(12),
        OpenLimitTime = reader.IsDBNull(13) ? null : reader.GetDecimal(13),
        ResetOpenDate = reader.GetBoolean(14),
        ExpireDateFlag = reader.GetBoolean(15),
        ErpNo = reader.GetString(16),
        IssueState = ToInt32(reader.GetValue(17)),
        DataStamp = ToInt32(reader.GetValue(18)),
        CreatedAt = reader.GetDateTime(19),
    };
}
