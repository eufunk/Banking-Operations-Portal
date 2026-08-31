using System.Globalization;
using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Imports.Dtos;
using Banking.Application.Transactions;
using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Customers;
using Banking.Domain.Imports;
using Banking.Domain.Transactions;
using CsvHelper;

namespace Banking.Application.Imports.ImportTransactionsCsv;

/// <summary>
/// Bildet die (in Kapitel 12 mangels Azure-Subscription simulierte) Azure-Data-Factory-
/// Pipeline "Blob Storage -> Validation -> Transformation -> Azure SQL" nach - siehe
/// docs/architecture/data-import.md für die vollständige Schritt-für-Schritt-Erklärung.
/// Läuft synchron innerhalb eines Requests statt als Hintergrund-Job: für den
/// Projektumfang ausreichend, eine echte ADF-Pipeline liefe asynchron über eine Queue.
/// </summary>
public sealed class ImportTransactionsCsvHandler : ICommandHandler<ImportTransactionsCsvCommand, ImportJobSummaryDto>
{
    private readonly IImportJobRepository _importJobRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportTransactionsCsvHandler(
        IImportJobRepository importJobRepository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _importJobRepository = importJobRepository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ImportJobSummaryDto>> Handle(ImportTransactionsCsvCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            return Result<ImportJobSummaryDto>.Failure(Error.Validation(
                "Import.FileNameMissing", "Ein Dateiname ist erforderlich."));
        }

        if (string.IsNullOrWhiteSpace(command.CsvContent))
        {
            return Result<ImportJobSummaryDto>.Failure(Error.Validation(
                "Import.EmptyFile", "Die CSV-Datei ist leer."));
        }

        // Pipeline-Schritt "Blob Storage": die Datei liegt bereits vollständig als Text vor
        // (in einem echten ADF-Setup läge sie in einem Blob-Container, von dem die Pipeline
        // getriggert wird - siehe docs/architecture/data-import.md).
        var job = ImportJob.Start(command.FileName);
        await _importJobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        job.BeginProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        List<TransactionImportRow> rows;
        try
        {
            rows = ParseCsv(command.CsvContent);
        }
        catch (Exception ex) when (ex is CsvHelperException or IOException)
        {
            job.FailCompletely($"CSV-Datei konnte nicht gelesen werden: {ex.Message}");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ImportJobSummaryDto>.Success(ToSummaryDto(job));
        }

        var rowNumber = 0;
        foreach (var row in rows)
        {
            rowNumber++;

            // --- Validation-Schritt: rein formale Prüfung der Rohdaten, noch keine
            // Datenbankzugriffe. Genau die Fehler, die eine ADF-"Data Flow Validation"-
            // Aktivität typischerweise abfängt. ---
            var validationError = TryValidate(row, out var parsed);
            if (validationError is not null)
            {
                job.RecordFailure(rowNumber, EmptyToNull(row.TransactionId), validationError);
                continue;
            }

            // Idempotenz: dieselbe externe TransactionId wurde bereits in einem früheren
            // (oder diesem) Lauf erfolgreich verarbeitet.
            if (await _importJobRepository.ExistsSuccessfulRecordAsync(parsed!.TransactionId, cancellationToken))
            {
                job.RecordSkipped(rowNumber, parsed.TransactionId);
                continue;
            }

            // --- Transformation-Schritt: Rohdaten in interne Domain-Objekte übersetzen,
            // inklusive der Nachschlage-Operationen, die eine reine Validierung nicht leisten
            // kann (existiert das Konto wirklich, passt die Währung, passt der Kunde). ---
            var account = await _accountRepository.GetByAccountNumberAsync(parsed.AccountNumber, cancellationToken);
            if (account is null)
            {
                job.RecordFailure(rowNumber, parsed.TransactionId, $"Konto {parsed.AccountNumber} wurde nicht gefunden.");
                continue;
            }

            if (account.CustomerId != parsed.CustomerId)
            {
                job.RecordFailure(rowNumber, parsed.TransactionId, $"Konto {parsed.AccountNumber} gehört nicht zu Kunde {parsed.CustomerId}.");
                continue;
            }

            if (!account.Currency.Equals(parsed.Currency))
            {
                job.RecordFailure(rowNumber, parsed.TransactionId, $"Währung {parsed.Currency} passt nicht zur Kontowährung {account.Currency}.");
                continue;
            }

            // CSV erlaubt negative Beträge (Soll-/Habenkennzeichen wie in Kernbankensystemen
            // üblich); unser Money ist immer positiv, das Vorzeichen wird stattdessen zum
            // TransactionType - das ist genau die Übersetzung, die ein Transformation-Schritt leistet.
            var transactionType = parsed.Amount >= 0 ? TransactionType.Deposit : TransactionType.Withdrawal;
            var money = Money.Positive(Math.Abs(parsed.Amount), parsed.Currency);

            // --- "Azure SQL"-Schritt: über EF Core / Unit of Work, wie überall sonst im Projekt. ---
            var transaction = Transaction.Create(account.Id, money, transactionType, parsed.Description, parsed.BookingDate);
            transaction.MarkBooked(); // importierte Daten sind bereits real gebuchte Historie, kein Pending-Zustand.
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            job.RecordSuccess(rowNumber, parsed.TransactionId);
        }

        job.Complete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ImportJobSummaryDto>.Success(ToSummaryDto(job));
    }

    private static List<TransactionImportRow> ParseCsv(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<TransactionImportRow>().ToList();
    }

    private sealed record ParsedRow(
        string TransactionId,
        CustomerId CustomerId,
        AccountNumber AccountNumber,
        decimal Amount,
        CurrencyCode Currency,
        DateTime BookingDate,
        string? Description);

    private static string? TryValidate(TransactionImportRow row, out ParsedRow? parsed)
    {
        parsed = null;

        if (string.IsNullOrWhiteSpace(row.TransactionId))
        {
            return "TransactionId fehlt.";
        }

        if (!Guid.TryParse(row.CustomerId, out var customerGuid))
        {
            return $"CustomerId '{row.CustomerId}' ist keine gültige Guid.";
        }

        AccountNumber accountNumber;
        try
        {
            accountNumber = new AccountNumber(row.AccountNumber);
        }
        catch (ArgumentException ex)
        {
            return $"AccountNumber ungültig: {ex.Message}";
        }

        if (!decimal.TryParse(row.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount == 0)
        {
            return $"Amount '{row.Amount}' ist keine gültige, von 0 verschiedene Zahl.";
        }

        CurrencyCode currency;
        try
        {
            currency = new CurrencyCode(row.Currency);
        }
        catch (ArgumentException ex)
        {
            return $"Currency ungültig: {ex.Message}";
        }

        if (!DateTime.TryParse(row.BookingDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var bookingDate))
        {
            return $"BookingDate '{row.BookingDate}' ist kein gültiges Datum.";
        }

        if (bookingDate > DateTime.UtcNow.AddDays(1))
        {
            return $"BookingDate {bookingDate:yyyy-MM-dd} liegt in der Zukunft.";
        }

        if (row.Description is { Length: > Transaction.MaxDescriptionLength })
        {
            return $"Description darf maximal {Transaction.MaxDescriptionLength} Zeichen lang sein.";
        }

        parsed = new ParsedRow(row.TransactionId.Trim(), new CustomerId(customerGuid), accountNumber, amount, currency, bookingDate, row.Description);
        return null;
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static ImportJobSummaryDto ToSummaryDto(ImportJob job) => new(
        job.Id,
        job.FileName,
        job.StartedAt,
        job.CompletedAt,
        job.TotalRecords,
        job.SuccessfulRecords,
        job.FailedRecords,
        job.SkippedRecords,
        job.Status);
}
