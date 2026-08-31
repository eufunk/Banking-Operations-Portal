using Banking.Api.Extensions;
using Banking.Api.Security;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Imports.Dtos;
using Banking.Application.Imports.GetImportJobDetails;
using Banking.Application.Imports.ImportTransactionsCsv;
using Banking.Application.Imports.SearchImportJobs;
using Banking.Domain.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

/// <summary>
/// Simuliert den "Banking Portal"-Endpunkt der in Kapitel 12 beschriebenen Pipeline
/// Blob Storage -&gt; Azure Data Factory -&gt; Validation -&gt; Transformation -&gt; Azure SQL -&gt;
/// Banking Portal. "Imports einsehen" ist ein BankEmployee-Recht (Klassenebene), "einen
/// Import auslösen" ist strenger und wird auf OperationsManager angehoben - das Anlegen
/// tausender Transaktionen auf einen Schlag ist ein deutlich wirkungsvollerer Vorgang als
/// nur einen bestehenden anzusehen.
/// </summary>
[ApiController]
[Route("api/imports")]
[Authorize(Roles = Roles.BankEmployee)]
public sealed class ImportsController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB - ausreichend für eine tägliche CSV-Lieferung in diesem Projektumfang.

    private readonly ICommandHandler<ImportTransactionsCsvCommand, ImportJobSummaryDto> _importTransactionsCsv;
    private readonly IQueryHandler<SearchImportJobsQuery, PagedResult<ImportJobSummaryDto>> _searchImportJobs;
    private readonly IQueryHandler<GetImportJobDetailsQuery, ImportJobDetailsDto> _getImportJobDetails;
    private readonly ILogger<ImportsController> _logger;

    public ImportsController(
        ICommandHandler<ImportTransactionsCsvCommand, ImportJobSummaryDto> importTransactionsCsv,
        IQueryHandler<SearchImportJobsQuery, PagedResult<ImportJobSummaryDto>> searchImportJobs,
        IQueryHandler<GetImportJobDetailsQuery, ImportJobDetailsDto> getImportJobDetails,
        ILogger<ImportsController> logger)
    {
        _importTransactionsCsv = importTransactionsCsv;
        _searchImportJobs = searchImportJobs;
        _getImportJobDetails = getImportJobDetails;
        _logger = logger;
    }

    /// <summary>GET /api/imports - Import-Läufe überwachen, paginiert, neueste zuerst.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ImportJobSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ImportJobSummaryDto>>> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _searchImportJobs.Handle(new SearchImportJobsQuery(page, pageSize), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>GET /api/imports/{id} - Details eines Import-Laufs inkl. Zeile-für-Zeile-Ergebnis.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ImportJobDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportJobDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getImportJobDetails.Handle(new GetImportJobDetailsQuery(new ImportJobId(id)), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// POST /api/imports - simuliert den ADF-Trigger "neue Datei im Blob Storage": statt
    /// eines Blob-Storage-Events löst hier ein Datei-Upload den Import direkt aus.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.OperationsManager)]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(ImportJobSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportJobSummaryDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CSV-Import gestartet: {FileName} ({Length} Bytes)", file.FileName, file.Length);

        using var reader = new StreamReader(file.OpenReadStream());
        var csvContent = await reader.ReadToEndAsync(cancellationToken);

        var result = await _importTransactionsCsv.Handle(
            new ImportTransactionsCsvCommand(file.FileName, csvContent), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error!.ToProblemResult<ImportJobSummaryDto>();
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id.Value }, result.Value);
    }
}
