using MES.Contracts;

namespace MES.Api.Data;

public interface IMasterDataRepository
{
    IReadOnlyList<OperationDto> ListOperations(string? keyword, int? issueState);
    OperationDto GetOperation(string no);
    OperationDto CreateOperation(OperationWriteRequest request);
    OperationDto UpdateOperation(string no, OperationWriteRequest request);
    OperationDto ApproveOperation(string no, int dataStamp);
    void DeleteOperation(string no, int issueState, int dataStamp);

    IReadOnlyList<MaterialDto> ListMaterials(string? keyword, int? issueState, string? materialType);
    MaterialDto GetMaterial(string no);
    MaterialDto CreateMaterial(MaterialWriteRequest request);
    MaterialDto UpdateMaterial(string no, MaterialWriteRequest request);
    MaterialDto ApproveMaterial(string no, int dataStamp);
    void DeleteMaterial(string no, int issueState, int dataStamp);

    IReadOnlyList<ProcessDto> ListProcesses(string? keyword, int? issueState);
    ProcessDto GetProcess(string no, string version);
    ProcessDto CreateProcess(ProcessWriteRequest request);
    ProcessDto UpdateProcess(string no, string version, ProcessWriteRequest request);
    ProcessDto ApproveProcess(string no, string version, int dataStamp);
    ProcessGraphDto GetGraph(string no, string version);
    ProcessGraphDto UpdateGraph(string no, string version, ProcessGraphDto request);
    ProcessDto CreateProcessVersion(string no, string version);
    ProcessDto SetCurrentProcess(string no, string version, int dataStamp);
    void DeleteProcess(string no, string version, int issueState, int dataStamp);

    IReadOnlyList<ProductDto> ListProducts(string? keyword, int? issueState);
    ProductDto GetProduct(string no, string version);
    ProductDto CreateProduct(ProductWriteRequest request);
    ProductDto UpdateProduct(string no, string version, ProductWriteRequest request);
    ProductDto ApproveProduct(string no, string version, int dataStamp);
    ProductDto SetCurrentProduct(string no, string version, int dataStamp);
    ProductDto CreateProductVersion(string no, string version);
    void DeleteProduct(string no, string version, int issueState, int dataStamp);
    IReadOnlyList<ProductProcessDto> GetProductProcesses(string no, string version);
    ProductProcessDto AddProductProcess(string no, string version, ProductProcessWriteRequest request);
    ProductProcessDto UpdateProductProcess(string no, string version, string processNo, string selector, ProductProcessWriteRequest request);
    void DeleteProductProcess(string no, string version, int moTypeNo, string segmentNo, string processNo, string selector);
    IReadOnlyList<ProductBomDto> GetBom(string no, string version);
    ProductBomDto AddBom(string no, string version, ProductBomWriteRequest request);
    void DeleteBom(string no, string version, string id);
}
