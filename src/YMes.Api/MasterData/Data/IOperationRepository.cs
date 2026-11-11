using MES.Contracts;

namespace MES.Api.Data;

public interface IOperationRepository
{
    IReadOnlyList<OperationDto> List(string? keyword, int? issueState);
    OperationDto Get(string operationNo);
    OperationDto Create(OperationWriteRequest request, string actorUserNo);
    OperationDto Update(string operationNo, OperationWriteRequest request, string actorUserNo);
    OperationDto Approve(string operationNo, long dataStamp, string actorUserNo);
    void DeleteOrRetire(string operationNo, int issueState, long dataStamp, string actorUserNo);
}
