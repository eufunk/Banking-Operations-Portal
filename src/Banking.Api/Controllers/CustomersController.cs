using Banking.Api.Extensions;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Customers.Dtos;
using Banking.Application.Customers.GetCustomerDetails;
using Banking.Application.Customers.SearchCustomers;
using Banking.Domain.Customers;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly IQueryHandler<SearchCustomersQuery, PagedResult<CustomerSummaryDto>> _searchCustomers;
    private readonly IQueryHandler<GetCustomerDetailsQuery, CustomerDetailsDto> _getCustomerDetails;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IQueryHandler<SearchCustomersQuery, PagedResult<CustomerSummaryDto>> searchCustomers,
        IQueryHandler<GetCustomerDetailsQuery, CustomerDetailsDto> getCustomerDetails,
        ILogger<CustomersController> logger)
    {
        _searchCustomers = searchCustomers;
        _getCustomerDetails = getCustomerDetails;
        _logger = logger;
    }

    /// <summary>GET /api/customers - Kunden suchen/filtern, paginiert.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CustomerSummaryDto>>> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] CustomerStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Kundensuche: searchTerm={SearchTerm}, status={Status}, page={Page}, pageSize={PageSize}",
            searchTerm, status, page, pageSize);

        var result = await _searchCustomers.Handle(
            new SearchCustomersQuery(searchTerm, status, page, pageSize), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>GET /api/customers/{id} - Kundendetails.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kundendetails angefragt: {CustomerId}", id);

        var result = await _getCustomerDetails.Handle(
            new GetCustomerDetailsQuery(new CustomerId(id)), cancellationToken);

        return result.ToActionResult();
    }
}
