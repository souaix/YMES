using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/wip/erfs")]
public sealed class WipErfInformationController(
    NpgsqlDataSource dataSource,
    ILogger<WipErfInformationController> logger) : ControllerBase
{
    private readonly WipErfInformationRepository repository = new(dataSource);

    [HttpGet("{erfNo}/info")]
    public IActionResult GetInfo(string erfNo, [FromQuery] int? source = null)
    {
        var normalizedErfNo = NormalizeIdentifier(erfNo, 20, "ERF No");
        if (normalizedErfNo is null || source is not null and not (0 or 1 or 2))
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "ERF 詳細資料查詢失敗", detail: "ERF No 或來源條件不正確。");
        try
        {
            var result = repository.GetInformation(normalizedErfNo, source);
            return result is null ? NotFound() : Ok(result);
        }
        catch (WipErfInformationException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "ERF 詳細資料查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(ex, "WIP ERF information query failed for {ErfNo}", normalizedErfNo);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "ERF 詳細資料查詢失敗", detail: "ERF 詳細資料結構或唯讀權限尚未部署。");
        }
    }

    [HttpGet("{erfNo}/attachments/content")]
    public IActionResult GetAttachment(string erfNo, [FromQuery] int erfSource, [FromQuery] string lotNo,
        [FromQuery] string source, [FromQuery] string name, [FromQuery] string? ocapNo = null)
    {
        var normalizedErfNo = NormalizeIdentifier(erfNo, 20, "ERF No");
        var normalizedLotNo = NormalizeIdentifier(lotNo, 50, "Lot No");
        var normalizedName = NormalizeIdentifier(name, 50, "附件名稱");
        var normalizedSource = source.Trim().ToUpperInvariant();
        if (normalizedErfNo is null || normalizedLotNo is null || normalizedName is null ||
            normalizedSource is not ("ERF" or "OCAP") || erfSource is not (0 or 1 or 2))
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "附件下載失敗", detail: "附件識別條件不正確。");
        var normalizedOcapNo = string.IsNullOrWhiteSpace(ocapNo) ? null : NormalizeIdentifier(ocapNo, 50, "OCAP No");
        if (normalizedSource == "OCAP" && normalizedOcapNo is null)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "附件下載失敗", detail: "OCAP 附件需要 OCAP No。");
        try
        {
            var result = repository.GetAttachment(normalizedErfNo, normalizedLotNo, erfSource, normalizedSource,
                normalizedName, normalizedOcapNo);
            return result is null
                ? NotFound()
                : File(result.Bytes, "application/octet-stream", result.FileName, enableRangeProcessing: false);
        }
        catch (WipErfInformationException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "附件下載失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(ex, "WIP ERF attachment download failed for {ErfNo}", normalizedErfNo);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "附件下載失敗", detail: "附件結構或唯讀權限尚未部署。");
        }
    }

    private static string? NormalizeIdentifier(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length > maxLength || normalized.Any(char.IsControl) ? null : normalized;
    }
}
