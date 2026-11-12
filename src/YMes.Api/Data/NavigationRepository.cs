using MES.Contracts;
using Npgsql;

namespace YMes.Api.Data;

public sealed class NavigationRepository(NpgsqlDataSource dataSource)
{
    public async Task<NavigationMenuTreeResponse> GetMenuTreeAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                module.moduleno,
                module.moduleorder,
                category.menuno,
                category.menuname,
                category.menuorder,
                item.menuno,
                item.menuname,
                fn.functionno,
                fn.functionname,
                COALESCE(fn.functionorder, item.menuorder)::numeric
            FROM "YMES".tblsysmodule AS module
            JOIN "YMES".tblsysmenuitem AS category
              ON category.moduleno = module.moduleno
             AND category.menutype = 1
            JOIN "YMES".tblsysmenuitem AS item
              ON item.moduleno = category.moduleno
             AND item.parentmenuno = category.menuno
             AND item.menutype = 0
            -- The source DDL has no explicit relation column. This provisional key match is
            -- exposed only when the function row exists; verify it against the Oracle rows.
            LEFT JOIN "YMES".tblsysfunction AS fn
              ON fn.moduleno = module.moduleno
             AND fn.functionno = item.menuno
            ORDER BY module.moduleorder, module.moduleno,
                     category.menuorder, category.menuno,
                     COALESCE(fn.functionorder, item.menuorder), item.menuno;
            """;

        var rows = new List<MenuRow>();
        await using (var command = dataSource.CreateCommand(sql))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new MenuRow(
                    reader.GetString(0),
                    reader.GetDecimal(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDecimal(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.GetDecimal(9)));
            }
        }

        var modules = rows
            .GroupBy(row => new { row.ModuleNo, row.ModuleOrder })
            .Select(module => new NavigationModuleDto(
                module.Key.ModuleNo,
                DisplayModuleName(module.Key.ModuleNo),
                module.Key.ModuleOrder,
                module.GroupBy(row => new { row.CategoryMenuNo, row.CategoryName, row.CategoryOrder })
                    .Select(category => new NavigationMenuGroupDto(
                        category.Key.CategoryMenuNo,
                        category.Key.CategoryName,
                        category.Key.CategoryOrder,
                        category.Select(row => new NavigationMenuItemDto(
                            row.ItemMenuNo,
                            row.ItemName,
                            row.FunctionNo,
                            row.FunctionName,
                            row.FunctionOrder)).ToArray()))
                    .ToArray()))
            .ToArray();

        return new NavigationMenuTreeResponse(modules);
    }

    private static string DisplayModuleName(string moduleNo) => moduleNo.ToUpperInvariant() switch
    {
        "EQP" => "設備資料",
        "OP" => "作業站資料",
        "MTL" => "物料資料",
        "PRS" => "流程資料",
        "PRD" => "產品資料",
        _ => moduleNo
    };

    private sealed record MenuRow(
        string ModuleNo,
        decimal ModuleOrder,
        string CategoryMenuNo,
        string CategoryName,
        decimal CategoryOrder,
        string ItemMenuNo,
        string ItemName,
        string? FunctionNo,
        string? FunctionName,
        decimal FunctionOrder);
}
