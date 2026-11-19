using MES.Contracts;

namespace MES.Api.Data;

public interface IMasterDataRepository
{
    IReadOnlyList<OperationDto> ListOperations(string? keyword, int? issueState);
    OperationDto GetOperation(string no);
    OperationDto CreateOperation(OperationWriteRequest request, string actorUserNo);
    OperationDto UpdateOperation(string no, OperationWriteRequest request, string actorUserNo);
    OperationDto ApproveOperation(string no, int dataStamp, string actorUserNo);
    void DeleteOperation(string no, int issueState, int dataStamp, string actorUserNo);

    IReadOnlyList<MaterialDto> ListMaterials(string? keyword, int? issueState, string? materialType, string? materialNoPrefix = null);
    MaterialDto GetMaterial(string no);
    MaterialDto CreateMaterial(MaterialWriteRequest request, string actorUserNo);
    MaterialDto UpdateMaterial(string no, MaterialWriteRequest request, string actorUserNo);
    MaterialDto ApproveMaterial(string no, int dataStamp, string actorUserNo);
    void DeleteMaterial(string no, int issueState, int dataStamp, string actorUserNo);

    IReadOnlyList<ProcessDto> ListProcesses(string? keyword, int? issueState);
    ProcessDto GetProcess(string no, string version);
    ProcessDto CreateProcess(ProcessWriteRequest request, string actorUserNo);
    ProcessDto UpdateProcess(string no, string version, ProcessWriteRequest request, string actorUserNo);
    ProcessDto ApproveProcess(string no, string version, int dataStamp, string actorUserNo);
    ProcessGraphDto GetGraph(string no, string version);
    ProcessGraphDto UpdateGraph(string no, string version, ProcessGraphDto request, string actorUserNo);
    ProcessDto CreateProcessVersion(string no, string version, string actorUserNo);
    ProcessDto SetCurrentProcess(string no, string version, int dataStamp, string actorUserNo);
    void DeleteProcess(string no, string version, int issueState, int dataStamp, string actorUserNo);

    IReadOnlyList<ProductDto> ListProducts(string? keyword, int? issueState);
    ProductDto GetProduct(string no, string version);
    ProductDto CreateProduct(ProductWriteRequest request, string actorUserNo);
    ProductDto UpdateProduct(string no, string version, ProductWriteRequest request, string actorUserNo);
    ProductDto ApproveProduct(string no, string version, int? issueState, decimal? dataStamp, string actorUserNo);
    ProductDto SetCurrentProduct(string no, string version, int? issueState, decimal? dataStamp, string actorUserNo);
    ProductDto CreateProductVersion(string no, string version, string actorUserNo);
    void DeleteProduct(string no, string version, int? issueState, decimal? dataStamp, string actorUserNo);
    IReadOnlyList<ProductProcessDto> GetProductProcesses(string no, string version);
    ProductProcessDto AddProductProcess(string no, string version, ProductProcessWriteRequest request, string actorUserNo);
    ProductProcessDto UpdateProductProcess(string no, string version, string processNo, string selector, ProductProcessWriteRequest request, string actorUserNo);
    void DeleteProductProcess(string no, string version, int moTypeNo, string segmentNo, string processNo, string selector, string actorUserNo);
    IReadOnlyList<ProductBomDto> GetBom(string no, string version);
    ProductBomDto AddBom(string no, string version, ProductBomWriteRequest request, string actorUserNo);
    void DeleteBom(string no, string version, string id, string actorUserNo);
}
