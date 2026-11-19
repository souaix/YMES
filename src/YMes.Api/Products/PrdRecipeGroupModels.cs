using MES.Api.Data;

namespace YMes.Api.Products;

public sealed record PrdRecipeGroupAssignmentDto(string ProductNo, string ProductVersion, string RecipeGroup);
public sealed record PrdRecipeGroupOptionDto(string RecipeGroup, int RecipeVersion, string? Description, int IssueState, int CurVersion);

public sealed class PrdRecipeGroupWriteRequest
{
    public string RecipeGroup { get; set; } = "";
}

public sealed class PrdRecipeGroupRuleException(int statusCode, string message) : MasterDataRuleException(statusCode, message) { }
