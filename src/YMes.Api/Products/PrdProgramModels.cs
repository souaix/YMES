using MES.Api.Data;

namespace YMes.Api.Products;

public sealed record PrdProgramOption(string ProgramNo, string? ProgramType, string? Description, int IssueState);
public sealed record PrdProgramAssignments(string ProductNo, string ProductVersion, int? IssueState,
    IReadOnlyList<string> ProgramNos, string Version);

public sealed class PrdProgramWriteRequest
{
    public List<string>? ProgramNos { get; set; }
    public string? Version { get; set; }
}

public sealed class PrdProgramRuleException(int statusCode, string message) : MasterDataRuleException(statusCode, message) { }
