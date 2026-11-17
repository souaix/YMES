using Microsoft.AspNetCore.Http;

namespace YMes.Api.Quality;

public sealed record Qc112StoredFile(string DisplayPath);

public interface IYmesQc112FileStorage
{
    Task<Qc112StoredFile> SaveAsync(IFormFile upload, string displayFileName, CancellationToken cancellationToken);
    string? MoveToPendingDelete(string displayPath);
    void RestorePendingDelete(string token, string displayPath);
    void FinalizePendingDelete(string token);
    void Delete(string displayPath);
}
