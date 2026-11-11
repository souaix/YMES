using MES.Contracts;

namespace MES.Api.Data;

public interface IEquipmentRepository
{
    IReadOnlyList<EquipmentDto> List(string? keyword, int? issueState, string? equipmentType);
    EquipmentDto Get(string equipmentNo);
    IReadOnlyList<EquipmentDto> Chambers(string equipmentNo);
    EquipmentDto CreateChamber(string parentEquipmentNo, ChamberWriteRequest request, string actorUserNo);
    EquipmentDto UpdateChamber(string parentEquipmentNo, string chamberNo, ChamberWriteRequest request, string actorUserNo);
    EquipmentDto ApproveChamber(string parentEquipmentNo, string chamberNo, long dataStamp, string actorUserNo);
    EquipmentDto DeleteChamber(string parentEquipmentNo, string chamberNo, int issueState, long dataStamp, string actorUserNo);
    EquipmentDto Create(EquipmentWriteRequest request, string actorUserNo);
    EquipmentDto Update(string equipmentNo, EquipmentWriteRequest request, string actorUserNo);
    EquipmentDto Approve(string equipmentNo, long dataStamp, string actorUserNo);
    EquipmentDto DeleteOrRetire(string equipmentNo, int issueState, long dataStamp, string actorUserNo);
}
