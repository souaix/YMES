using MES.Contracts;

namespace MES.Api.Data;

public interface IEquipmentRepository
{
    IReadOnlyList<EquipmentDto> List(string? keyword, int? issueState, string? equipmentType);
    EquipmentDto Get(string equipmentNo);
    IReadOnlyList<EquipmentDto> Chambers(string equipmentNo);
    EquipmentDto CreateChamber(string parentEquipmentNo, ChamberWriteRequest request);
    EquipmentDto UpdateChamber(string parentEquipmentNo, string chamberNo, ChamberWriteRequest request);
    EquipmentDto ApproveChamber(string parentEquipmentNo, string chamberNo, int dataStamp);
    EquipmentDto DeleteChamber(string parentEquipmentNo, string chamberNo, int issueState, int dataStamp);
    EquipmentDto Create(EquipmentWriteRequest request);
    EquipmentDto Update(string equipmentNo, EquipmentWriteRequest request);
    EquipmentDto Approve(string equipmentNo, int dataStamp);
    EquipmentDto DeleteOrRetire(string equipmentNo, int issueState, int dataStamp);
}
