using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace YMes.Api.Quality;

[ApiController]
[Route("api/qc/reason-product-sop-file")]
public sealed class Qc112Controller(Qc112Repository repository) : ControllerBase
{
    private const long MultipartLimit = 27L * 1024 * 1024;

    [HttpGet("reasons")]
    public IActionResult Reasons([FromQuery] int issueState = 2) => Execute(() =>
    {
        if (issueState != 2) throw new Qc112RuleException(400, "此清單只提供已核准的品質原因（IssueState=2）。");
        return Ok(new { reasons = repository.GetApprovedReasons() });
    });

    [HttpGet("reasons/{reasonNo}/files")]
    public IActionResult Files(string reasonNo) => Execute(() => Ok(new { files = repository.GetFiles(reasonNo) }));

    [HttpGet("products")]
    public IActionResult Products([FromQuery] int issueState = 2, [FromQuery] int currentVersion = 1, [FromQuery] string search = "") => Execute(() =>
    {
        if (issueState != 2 || currentVersion != 1)
            throw new Qc112RuleException(400, "產品查詢只允許已核准且目前版本（IssueState=2、CurVersion=1）。");
        return Ok(new { products = repository.FindApprovedCurrentProducts(search) });
    });

    [HttpPost("reasons/{reasonNo}/products/{productNo}/file")]
    [RequestSizeLimit(MultipartLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartLimit)]
    public Task<IActionResult> AddFile(string reasonNo, string productNo, [FromForm] IFormFile file, [FromForm] string sopFileName, CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await repository.AddFileAsync(reasonNo, productNo, sopFileName, file, cancellationToken)));

    [HttpDelete("reasons/{reasonNo}/products/{productNo}/file")]
    public Task<IActionResult> RemoveFile(string reasonNo, string productNo, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await repository.RemoveFileAsync(reasonNo, productNo, cancellationToken);
            return NoContent();
        });

    [HttpPost("import/preview")]
    [RequestSizeLimit(MultipartLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartLimit)]
    public IActionResult PreviewImport([FromForm] IFormFile file) => Execute(() => Ok(new { rows = repository.PreviewImport(file) }));

    [HttpPost("import/item")]
    [RequestSizeLimit(MultipartLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartLimit)]
    public Task<IActionResult> ImportItem(
        [FromForm] string reasonNo,
        [FromForm] string productNo,
        [FromForm] string sopFileName,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(new
        {
            rowNumber = (int?)null,
            item = await repository.AddFileAsync(reasonNo, productNo, sopFileName, file, cancellationToken),
            result = "成功",
            message = "上傳並完成對應。"
        }));

    [HttpGet("import/template")]
    public IActionResult ImportTemplate()
    {
        return File(CreateXlsxTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReasonPrdSOPFile.xlsx");
    }

    private static byte[] CreateXlsxTemplate()
    {
        XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace officeRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationship = "http://schemas.openxmlformats.org/package/2006/relationships";
        XNamespace contentType = "http://schemas.openxmlformats.org/package/2006/content-types";
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddXml(archive, "[Content_Types].xml", new XDocument(
                new XElement(contentType + "Types",
                    new XElement(contentType + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                    new XElement(contentType + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                    new XElement(contentType + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                    new XElement(contentType + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
            AddXml(archive, "_rels/.rels", new XDocument(
                new XElement(packageRelationship + "Relationships",
                    new XElement(packageRelationship + "Relationship", new XAttribute("Id", "rId1"),
                        new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                        new XAttribute("Target", "xl/workbook.xml")))));
            AddXml(archive, "xl/workbook.xml", new XDocument(
                new XElement(spreadsheet + "workbook",
                    new XAttribute(XNamespace.Xmlns + "r", officeRelationship),
                    new XElement(spreadsheet + "sheets",
                        new XElement(spreadsheet + "sheet", new XAttribute("name", "ReasonPrdSOPFile"), new XAttribute("sheetId", "1"), new XAttribute(officeRelationship + "id", "rId1"))))));
            AddXml(archive, "xl/_rels/workbook.xml.rels", new XDocument(
                new XElement(packageRelationship + "Relationships",
                    new XElement(packageRelationship + "Relationship", new XAttribute("Id", "rId1"),
                        new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                        new XAttribute("Target", "worksheets/sheet1.xml")))));
            var headings = new[] { "ReasonNo", "ProductNo", "FileName", "FilePath" };
            var cells = headings.Select((heading, index) =>
            {
                var column = ((char)('A' + index)).ToString();
                return new XElement(spreadsheet + "c", new XAttribute("r", column + "1"), new XAttribute("t", "inlineStr"),
                    new XElement(spreadsheet + "is", new XElement(spreadsheet + "t", heading)));
            });
            AddXml(archive, "xl/worksheets/sheet1.xml", new XDocument(
                new XElement(spreadsheet + "worksheet", new XElement(spreadsheet + "sheetData", new XElement(spreadsheet + "row", new XAttribute("r", "1"), cells)))));
        }
        return output.ToArray();
    }

    private static void AddXml(ZipArchive archive, string path, XDocument document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        document.Save(writer);
    }

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc112RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "QC112 資料操作失敗", detail: ex.Message); }
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Qc112RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "QC112 資料操作失敗", detail: ex.Message); }
    }
}
