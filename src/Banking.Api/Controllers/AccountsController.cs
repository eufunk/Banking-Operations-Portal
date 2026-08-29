using Banking.Api.Extensions;
using Banking.Api.Security;
using Banking.Application.Accounts.Dtos;
using Banking.Application.Accounts.GetAccountDetails;
using Banking.Application.Accounts.SearchAccounts;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Domain.Accounts;
using Banking.Domain.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

/// <summary>"Konten lesen" - BankEmployee-Recht.</summary>
[ApiController]
[Route("api/accounts")]
[Authorize(Roles = Roles.BankEmployee)]
public sealed class AccountsController : ControllerBase
{
    private readonly IQueryHandler<SearchAccountsQuery, PagedResult<AccountDetailsDto>> _searchAccounts;
    private readonly IQueryHandler<GetAccountDetailsQuery, AccountDetailsDto> _getAccountDetails;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IQueryHandler<SearchAccountsQuery, PagedResult<AccountDetailsDto>> searchAccounts,
        IQueryHandler<GetAccountDetailsQuery, AccountDetailsDto> getAccountDetails,
        ILogger<AccountsController> logger)
    {
        _searchAccounts = searchAccounts;
        _getAccountDetails = getAccountDetails;
        _logger = logger;
    }

    /// <summary>GET /api/accounts - Konten suchen/filtern, paginiert.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AccountDetailsDto>>> Search(
        [FromQuery] Guid? customerId,
        [FromQuery] AccountStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Kontosuche: customerId={CustomerId}, status={Status}, page={Page}, pageSize={PageSize}",
            customerId, status, page, pageSize);

        var query = new SearchAccountsQuery(
            customerId.HasValue ? new CustomerId(customerId.Value) : null, status, page, pageSize);

        var result = await _searchAccounts.Handle(query, cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>GET /api/accounts/{id} - Kontodetails inkl. aktuellem Saldo.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kontodetails angefragt: {AccountId}", id);

        var result = await _getAccountDetails.Handle(
            new GetAccountDetailsQuery(new AccountId(id)), cancellationToken);

        return result.ToActionResult();
    }
}
