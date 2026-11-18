namespace YMes.Api.Quality;

public sealed record Qc303FactorDto(
    string FactorNo,
    string ParameterName,
    bool FactorSpec,
    int ParameterType,
    string? ShowName);

public sealed record Qc303QcItemDto(
    string QcItemNo,
    string QcItemName,
    string QcDataType,
    int ChartType,
    int SpecLineType,
    int ComponentCollect,
    string Unit,
    int LotIsValid,
    IReadOnlyList<Qc303FactorDto> Factors);

public sealed record Qc303FactorOptionDto(string Value, string Name);

public sealed record Qc303SpecDetailDto(
    int SpcId,
    string ChartName,
    string Us,
    string Cs,
    string Ls,
    string Ucl,
    string Cl,
    string Lcl,
    string Memo);

public sealed class Qc303SpecBasisDto
{
    public string QcSpecSerialNo { get; set; } = "";
    public string QcItemNo { get; set; } = "";
    public string QcItemName { get; set; } = "";
    public int IssueState { get; set; }
    public decimal DataStamp { get; set; }
    public string Description { get; set; } = "";
    public string Creator { get; set; } = "";
    public DateTime? CreateDate { get; set; }
    public string Reviser { get; set; } = "";
    public DateTime? ReviseDate { get; set; }
    public IReadOnlyDictionary<string, string> FactorValues { get; set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> FactorDisplayValues { get; set; } = new Dictionary<string, string>();
    public IReadOnlyList<Qc303SpecDetailDto> Details { get; set; } = [];
}

public sealed class Qc303SpecWriteRequest
{
    public string QcItemNo { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> FactorValues { get; set; } = [];
    public List<Qc303SpecDetailWriteRequest> Details { get; set; } = [];
    public string Memo { get; set; } = "";
    public int IssueState { get; set; }
    public decimal DataStamp { get; set; }
}

public sealed class Qc303SpecDetailWriteRequest
{
    public int SpcId { get; set; }
    public string Us { get; set; } = "";
    public string Cs { get; set; } = "";
    public string Ls { get; set; } = "";
    public string Ucl { get; set; } = "";
    public string Cl { get; set; } = "";
    public string Lcl { get; set; } = "";
    public string Memo { get; set; } = "";
}

public sealed class Qc303SpecStateRequest
{
    public int IssueState { get; set; }
    public decimal DataStamp { get; set; }
}

public sealed class Qc303CopyRequest
{
    public string FromQcItemNo { get; set; } = "";
    public string ToQcItemNo { get; set; } = "";
    public int CopyType { get; set; }
}

public sealed record Qc303CopyResult(int CopiedCount);

public sealed record Qc303StateResult(string QcSpecSerialNo, int IssueState, decimal DataStamp);

public sealed class Qc303RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
