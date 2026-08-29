using Banking.Api.Extensions;
using Banking.Application.Accounts.Dtos;
using Banking.Application.Accounts.GetAccountDetails;
using Banking.Application.Common.Messaging;
using Banking.Domain.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IQueryHandler<GetAccountDetailsQuery, AccountDetailsDto> _getAccountDetails;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IQueryHandler<GetAccountDetailsQuery, AccountDetailsDto> getAccountDetails,
        ILogger<AccountsController> logger)
    {
        _getAccountDetails = getAccountDetails;
        _logger = logger;
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
