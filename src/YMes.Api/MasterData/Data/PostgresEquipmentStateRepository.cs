using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed class PostgresEquipmentStateRepository(NpgsqlDataSource dataSource)
{
    private const string Columns = "equipmentstate::int,statetype::int,statename,statecolor::int,description,engineergroupno,utilizationtype::int,statecolor_hex";
    private static readonly IReadOnlyList<ValueOptionDto> StateTypes =
    [
        new(0, "System"),
        new(1, "User Define"),
    ];
    private static readonly IReadOnlyList<ValueOptionDto> UtilizationTypes =
    [
        new(0, "Off"),
        new(1, "On"),
    ];

    public IReadOnlyList<EquipmentStateDto> List()
    {
        using var command = dataSource.CreateCommand($"""
            SELECT {Columns}
            FROM "YMES".tbleqpstatebasis
            ORDER BY equipmentstate;
            """);
        using var reader = command.ExecuteReader();
        var rows = new List<EquipmentStateDto>();
        while (reader.Read()) rows.Add(Map(reader));
        return rows;
    }

    public EquipmentStateOptionsDto Options() => new(StateTypes, UtilizationTypes);

    public EquipmentStateDto Create(EquipmentStateWriteRequest request)
    {
        var values = Validate(request);
        using var command = dataSource.CreateCommand("""
            INSERT INTO "YMES".tbleqpstatebasis
                (equipmentstate,statetype,statename,statecolor,description,utilizationtype,statecolor_hex)
            VALUES (@equipmentState,@stateType,@stateName,@stateColor,@description,@utilizationType,@stateColorHex);
            """);
        AddNumeric(command, "equipmentState", values.EquipmentState);
        AddNumeric(command, "stateType", values.StateType);
        AddVarchar(command, "stateName", values.StateName);
        AddNumeric(command, "stateColor", values.StateColor);
        AddVarchar(command, "description", values.Description);
        AddNumeric(command, "utilizationType", values.UtilizationType);
        AddVarchar(command, "stateColorHex", values.StateColorHex);
        try { command.ExecuteNonQuery(); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new EquipmentRuleException(409, $"設備狀態編號 {values.EquipmentState} 已存在。");
        }
        return Get(values.EquipmentState);
    }

    public EquipmentStateDto Update(int equipmentState, EquipmentStateWriteRequest request)
    {
        if (equipmentState != request.EquipmentState)
            throw new EquipmentRuleException(400, "網址與資料內容的 EquipmentState 必須相同。");

        var values = Validate(request);
        using var command = dataSource.CreateCommand("""
            UPDATE "YMES".tbleqpstatebasis
            SET statetype=@stateType,
                statename=@stateName,
                statecolor=@stateColor,
                description=@description,
                utilizationtype=@utilizationType,
                statecolor_hex=COALESCE(@stateColorHex,statecolor_hex)
            WHERE equipmentstate=@equipmentState;
            """);
        AddNumeric(command, "equipmentState", values.EquipmentState);
        AddNumeric(command, "stateType", values.StateType);
        AddVarchar(command, "stateName", values.StateName);
        AddNumeric(command, "stateColor", values.StateColor);
        AddVarchar(command, "description", values.Description);
        AddNumeric(command, "utilizationType", values.UtilizationType);
        AddVarchar(command, "stateColorHex", values.StateColorHex);
        if (command.ExecuteNonQuery() == 0) throw NotFound(equipmentState);
        return Get(equipmentState);
    }

    public void Delete(int equipmentState)
    {
        ValidateEquipmentState(equipmentState);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);

        try
        {
        using (var available = new NpgsqlCommand("""
            SELECT to_regclass('"YMES".tblemsequipmentstate') IS NOT NULL,
                   to_regclass('"YMES".tbleqpstatecontrol') IS NOT NULL,
                   to_regclass('"YMES".tbleqpqclistbasis') IS NOT NULL;
            """, connection, transaction))
        using (var reader = available.ExecuteReader())
        {
            _ = reader.Read();
            if (!reader.GetBoolean(0) || !reader.GetBoolean(1) || !reader.GetBoolean(2))
                throw new EquipmentRuleException(503, "設備狀態引用檢查所需的 MES 來源表尚未完整建立；目前已停用刪除，避免遺漏引用資料。");
        }

        using (var references = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM "YMES".tblemsequipmentstate WHERE equipmentstate=@equipmentState)
                OR EXISTS(SELECT 1 FROM "YMES".tbleqpstatecontrol WHERE equipmentstate=@equipmentState)
                OR EXISTS(SELECT 1 FROM "YMES".tbleqpqclistbasis WHERE unvalidaction=@equipmentState);
            """, connection, transaction))
        {
            AddNumeric(references, "equipmentState", equipmentState);
            if ((bool)(references.ExecuteScalar() ?? false))
                throw new EquipmentRuleException(409, "設備狀態已被設備現況、設備狀態控制或設備點檢表引用，不能刪除。");
        }

        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tbleqpstatebasis WHERE equipmentstate=@equipmentState;", connection, transaction))
        {
            AddNumeric(delete, "equipmentState", equipmentState);
            if (delete.ExecuteNonQuery() == 0) throw NotFound(equipmentState);
        }

        transaction.Commit();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new EquipmentRuleException(409, "設備狀態或引用資料已同時變更，請重新載入後再操作。");
        }
    }

    private EquipmentStateDto Get(int equipmentState)
    {
        using var command = dataSource.CreateCommand($"SELECT {Columns} FROM \"YMES\".tbleqpstatebasis WHERE equipmentstate=@equipmentState;");
        AddNumeric(command, "equipmentState", equipmentState);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : throw NotFound(equipmentState);
    }

    private static EquipmentStateDto Map(NpgsqlDataReader reader)
    {
        var stateType = reader.GetInt32(1);
        var utilizationType = reader.GetInt32(6);
        return new EquipmentStateDto
        {
            EquipmentState = reader.GetInt32(0),
            StateType = stateType,
            StateName = reader.GetString(2),
            StateColor = reader.GetInt32(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
            EngineerGroupNo = reader.IsDBNull(5) ? null : reader.GetString(5),
            UtilizationType = utilizationType,
            StateColorHex = reader.IsDBNull(7) ? null : reader.GetString(7),
            StateTypeText = StateTypes.FirstOrDefault(option => option.Value == stateType)?.Label ?? stateType.ToString(),
            UtilizationTypeText = UtilizationTypes.FirstOrDefault(option => option.Value == utilizationType)?.Label ?? utilizationType.ToString(),
        };
    }

    private static (int EquipmentState, int StateType, string StateName, int StateColor, string? Description, int UtilizationType, string? StateColorHex) Validate(EquipmentStateWriteRequest request)
    {
        var equipmentState = request.EquipmentState ?? throw new EquipmentRuleException(400, "設備狀態編號為必填。");
        var stateType = request.StateType ?? throw new EquipmentRuleException(400, "狀態類型為必填。");
        var stateColor = request.StateColor ?? throw new EquipmentRuleException(400, "狀態顏色為必填。");
        var utilizationType = request.UtilizationType ?? throw new EquipmentRuleException(400, "稼動類型為必填。");
        ValidateEquipmentState(equipmentState);
        if (!StateTypes.Any(option => option.Value == stateType))
            throw new EquipmentRuleException(400, "狀態類型需使用目前已核實的 System (0) 或 User Define (1)。");
        if (stateType == 1 && equipmentState < 11)
            throw new EquipmentRuleException(400, "User Define 類型不可使用 0 到 10。");
        if (utilizationType is not (0 or 1))
            throw new EquipmentRuleException(400, "稼動類型需為 Off (0) 或 On (1)。");

        var stateName = request.StateName?.Trim() ?? "";
        if (stateName.Length == 0) throw new EquipmentRuleException(400, "狀態名稱不可空白。");
        if (stateName.Length > 50) throw new EquipmentRuleException(400, "狀態名稱不可超過 50 個字元。");
        var description = Optional(request.Description, 255, "說明");
        var colorHex = Optional(request.StateColorHex, 15, "狀態顏色色碼");
        if (colorHex is not null && (colorHex.Length != 6 || !colorHex.All(Uri.IsHexDigit)))
            throw new EquipmentRuleException(400, "狀態顏色色碼需為不含 # 的六位 RGB 十六進位字串。");

        return (equipmentState, stateType, stateName, stateColor, description, utilizationType, colorHex?.ToUpperInvariant());
    }

    private static void ValidateEquipmentState(int equipmentState)
    {
        if (equipmentState is < 0 or > 99)
            throw new EquipmentRuleException(400, "設備狀態編號需介於 0 到 99。");
    }

    private static string? Optional(string? value, int max, string label)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > max) throw new EquipmentRuleException(400, $"{label}不可超過 {max} 個字元。");
        return text;
    }

    private static void AddVarchar(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, int value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static EquipmentRuleException NotFound(int equipmentState) =>
        new(404, $"找不到設備狀態 {equipmentState}。");
}
