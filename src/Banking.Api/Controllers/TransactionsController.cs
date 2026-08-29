using Banking.Api.Extensions;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Transactions.Dtos;
using Banking.Application.Transactions.GetTransactionDetails;
using Banking.Application.Transactions.SearchTransactions;
using Banking.Domain.Accounts;
using Banking.Domain.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly IQueryHandler<SearchTransactionsQuery, PagedResult<TransactionSummaryDto>> _searchTransactions;
    private readonly IQueryHandler<GetTransactionDetailsQuery, TransactionDetailsDto> _getTransactionDetails;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        IQueryHandler<SearchTransactionsQuery, PagedResult<TransactionSummaryDto>> searchTransactions,
        IQueryHandler<GetTransactionDetailsQuery, TransactionDetailsDto> getTransactionDetails,
        ILogger<TransactionsController> logger)
    {
        _searchTransactions = searchTransactions;
        _getTransactionDetails = getTransactionDetails;
        _logger = logger;
    }

    /// <summary>GET /api/transactions?accountId=... - Transaktionen eines Kontos suchen/filtern.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TransactionSummaryDto>>> Search(
        [FromQuery] Guid accountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] TransactionStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Transaktionssuche: accountId={AccountId}, from={From}, to={To}, status={Status}, page={Page}, pageSize={PageSize}",
            accountId, from, to, status, page, pageSize);

        var query = new SearchTransactionsQuery(new AccountId(accountId), from, to, status, page, pageSize);
        var result = await _searchTransactions.Handle(query, cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>GET /api/transactions/{id} - Transaktionsdetails.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transaktionsdetails angefragt: {TransactionId}", id);

        var result = await _getTransactionDetails.Handle(
            new GetTransactionDetailsQuery(new TransactionId(id)), cancellationToken);

        return result.ToActionResult();
    }
}
