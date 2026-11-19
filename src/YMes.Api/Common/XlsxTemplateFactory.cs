using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace YMes.Api.Common;

/// <summary>Creates a YMES workbook containing only the verified import column headers.</summary>
public static class XlsxTemplateFactory
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Create(params string[] headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        if (headers.Length is < 1 or > 16384 || headers.Any(header => string.IsNullOrWhiteSpace(header) || header.Length > 32767))
            throw new ArgumentException("At least one valid Excel column header is required.", nameof(headers));

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXml(archive, "[Content_Types].xml", new XElement(ContentTypes + "Types",
                new XElement(ContentTypes + "Default", new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypes + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(ContentTypes + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));
            WriteXml(archive, "_rels/.rels", new XElement(PackageRelationships + "Relationships",
                new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId1"),
                    new XAttribute("Type", OfficeRelationships.NamespaceName + "/officeDocument"), new XAttribute("Target", "xl/workbook.xml"))));
            WriteXml(archive, "xl/workbook.xml", new XElement(Spreadsheet + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", OfficeRelationships.NamespaceName),
                new XElement(Spreadsheet + "sheets", new XElement(Spreadsheet + "sheet", new XAttribute("name", "YMES"),
                    new XAttribute("sheetId", 1), new XAttribute(OfficeRelationships + "id", "rId1")))));
            WriteXml(archive, "xl/_rels/workbook.xml.rels", new XElement(PackageRelationships + "Relationships",
                new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId1"),
                    new XAttribute("Type", OfficeRelationships.NamespaceName + "/worksheet"), new XAttribute("Target", "worksheets/sheet1.xml"))));
            WriteXml(archive, "xl/worksheets/sheet1.xml", new XElement(Spreadsheet + "worksheet",
                new XElement(Spreadsheet + "dimension", new XAttribute("ref", "A1:" + ColumnName(headers.Length) + "1")),
                new XElement(Spreadsheet + "sheetData", new XElement(Spreadsheet + "row", new XAttribute("r", 1),
                    headers.Select((header, index) => new XElement(Spreadsheet + "c",
                        new XAttribute("r", ColumnName(index + 1) + "1"), new XAttribute("t", "inlineStr"),
                        new XElement(Spreadsheet + "is", new XElement(Spreadsheet + "t", header))))))));
        }
        return output.ToArray();
    }

    private static void WriteXml(ZipArchive archive, string name, XElement root)
    {
        using var stream = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
        new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).Save(writer);
    }

    private static string ColumnName(int column)
    {
        var name = "";
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return name;
    }
}
