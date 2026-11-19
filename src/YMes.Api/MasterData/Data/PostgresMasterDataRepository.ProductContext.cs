using Npgsql;

namespace MES.Api.Data;

// A projection of the existing product master, not a second product model/table.
// Preserve raw nullable state/stamp instead of the legacy ProductDto coercions.
public sealed record ProductVersionContext(string ProductNo, string ProductVersion, int? IssueState, decimal? DataStamp);

public sealed partial class PostgresMasterDataRepository
{
    public static ProductVersionContext ReadProductVersionContext(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string no, string version, bool forUpdate)
    {
        var productNo = RequiredCode(no, "產品編號", 50);
        var productVersion = RequiredCode(version, "產品版本", 5);
        if (productNo.Any(char.IsControl) || productVersion.Any(char.IsControl)) throw BadRequest("產品編號與版本不可含控制字元。");
        using var command = new NpgsqlCommand($"""
            SELECT productno,productversion,issuestate::int,datastamp
            FROM "YMES".tblprdproductbasis
            WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version)
            ORDER BY productno,productversion
            {(forUpdate ? "FOR UPDATE" : "FOR SHARE")};
            """, connection, transaction);
        AddVarchar(command, "no", productNo); AddVarchar(command, "version", productVersion);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw NotFound("產品", $"{productNo}/{productVersion}");
        var result = new ProductVersionContext(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetDecimal(3));
        if (reader.Read()) throw Conflict("產品編號／版本存在大小寫不同的重複資料，不能任意選取其中一筆。");
        return result;
    }
}
