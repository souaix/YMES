using System.Data;
using Npgsql;
using Microsoft.AspNetCore.Http;

namespace YMes.Api.Quality;

public sealed class Qc501SpcInspMachineSetupRepository(NpgsqlDataSource dataSource)
{
    private const int MaxLookupRows = 5000;

    public Qc501SpcSetupLookupsDto Lookups()
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using (var setReadOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            setReadOnly.ExecuteNonQuery();

        EnsureTable(connection, transaction, "tblqcitemforvariables");
        EnsureTable(connection, transaction, "tblqcitemgroupbasis");
        EnsureTable(connection, transaction, "tblqcitemgroupdetail");
        EnsureTable(connection, transaction, "tblqcinspmachineparameter");

        var variableItems = ReadVariableItems(connection, transaction);
        var groupItems = ReadGroupItems(connection, transaction);
        var machines = ReadMachines(connection, transaction);
        transaction.Commit();
        return new(variableItems, groupItems, machines);
    }

    private static IReadOnlyList<Qc501SpcSetupVariableItemDto> ReadVariableItems(
        NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var result = new List<Qc501SpcSetupVariableItemDto>();
        using var command = new NpgsqlCommand("""
            SELECT qcitemno,qcitemname,COALESCE(caltype,0)::int
            FROM "YMES".tblqcitemforvariables
            WHERE collecttype=1 AND issuestate=2
            ORDER BY qcitemno
            LIMIT @limit;
            """, connection, transaction);
        command.Parameters.AddWithValue("limit", MaxLookupRows + 1);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (result.Count == MaxLookupRows) throw TooMany("計量品管項目");
            result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
        }
        return result;
    }

    private static IReadOnlyList<Qc501SpcSetupGroupItemDto> ReadGroupItems(
        NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var result = new List<Qc501SpcSetupGroupItemDto>();
        using var command = new NpgsqlCommand("""
            SELECT d.groupno,g.groupname,d.qcitemno,q.qcitemname,d.showorder::int
            FROM "YMES".tblqcitemgroupdetail d
            LEFT JOIN "YMES".tblqcitemgroupbasis g ON g.groupno=d.groupno
            LEFT JOIN "YMES".tblqcitemforvariables q ON q.qcitemno=d.qcitemno
            ORDER BY d.groupno,d.showorder,d.qcitemno
            LIMIT @limit;
            """, connection, transaction);
        command.Parameters.AddWithValue("limit", MaxLookupRows + 1);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (result.Count == MaxLookupRows) throw TooMany("品管項目群組明細");
            result.Add(new(reader.GetString(0), NullableText(reader, 1), reader.GetString(2),
                NullableText(reader, 3), reader.GetInt32(4)));
        }
        return result;
    }

    private static IReadOnlyList<Qc501SpcSetupInspectionMachineDto> ReadMachines(
        NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var result = new List<Qc501SpcSetupInspectionMachineDto>();
        using var command = new NpgsqlCommand("""
            SELECT inspmachineno,inspmachinename,COALESCE(insptype,0)::int,baudrate,paritybit,databit,stopbit,
                   endcode,format1,format2,chno
            FROM "YMES".tblqcinspmachineparameter
            WHERE COALESCE(issuestate,0)=2 AND inspmachineno IS NOT NULL
            ORDER BY inspmachineno
            LIMIT @limit;
            """, connection, transaction);
        command.Parameters.AddWithValue("limit", MaxLookupRows + 1);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (result.Count == MaxLookupRows) throw TooMany("已核准檢測儀器");
            result.Add(new(
                reader.GetString(0), NullableText(reader, 1), reader.GetInt32(2), NullableDecimal(reader, 3),
                NullableText(reader, 4), NullableInt(reader, 5), NullableInt(reader, 6), NullableText(reader, 7),
                NullableText(reader, 8), NullableText(reader, 9), NullableText(reader, 10)));
        }
        return result;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT to_regclass(format('%I.%I','YMES',@table)) IS NOT NULL;", connection, transaction);
        command.Parameters.AddWithValue("table", table);
        if (!(bool)command.ExecuteScalar()!)
            throw new QcInspectionRuleException(StatusCodes.Status503ServiceUnavailable, $"YMES.{table} 尚未部署。");
    }

    private static QcInspectionRuleException TooMany(string source) =>
        new(StatusCodes.Status413PayloadTooLarge, $"{source}超過 {MaxLookupRows} 筆；為避免不完整選項，請先縮小正式主檔範圍。");

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static int? NullableInt(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
}
