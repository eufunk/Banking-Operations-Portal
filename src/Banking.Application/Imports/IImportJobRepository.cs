using Banking.Application.Common.Pagination;
using Banking.Domain.Imports;

namespace Banking.Application.Imports;

public interface IImportJobRepository
{
    public Task AddAsync(ImportJob importJob, CancellationToken cancellationToken);

    public Task<ImportJob?> GetByIdAsync(ImportJobId id, CancellationToken cancellationToken);

    public Task<PagedResult<ImportJob>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Idempotenz-Prüfung über alle bisherigen Import-Läufe hinweg (nicht nur den aktuellen
    /// Job) - so wird eine TransactionId auch dann als bereits importiert erkannt, wenn sie
    /// in einem früheren, separaten Lauf erfolgreich verarbeitet wurde.
    /// </summary>
    public Task<bool> ExistsSuccessfulRecordAsync(string externalTransactionId, CancellationToken cancellationToken);
}
