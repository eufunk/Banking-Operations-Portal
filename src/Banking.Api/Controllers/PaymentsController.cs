using Banking.Api.Contracts.Payments;
using Banking.Api.Extensions;
using Banking.Api.Security;
using Banking.Application.Common.Messaging;
using Banking.Application.Common.Pagination;
using Banking.Application.Payments.ApprovePayment;
using Banking.Application.Payments.CreatePayment;
using Banking.Application.Payments.Dtos;
using Banking.Application.Payments.GetPayment;
using Banking.Application.Payments.RejectPayment;
using Banking.Application.Payments.SearchPayments;
using Banking.Domain.Accounts;
using Banking.Domain.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

/// <summary>
/// "Payments erstellen" ist ein BankEmployee-Recht (Klassenebene). "Payments bearbeiten"
/// (Approve/Reject) ist strenger und wird pro Action auf OperationsManager angehoben.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize(Roles = Roles.BankEmployee)]
public sealed class PaymentsController : ControllerBase
{
    private readonly ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto> _createPayment;
    private readonly ICommandHandler<ApprovePaymentCommand, PaymentStatusDto> _approvePayment;
    private readonly ICommandHandler<RejectPaymentCommand, PaymentStatusDto> _rejectPayment;
    private readonly IQueryHandler<GetPaymentQuery, PaymentDetailsDto> _getPayment;
    private readonly IQueryHandler<SearchPaymentsQuery, PagedResult<PaymentDetailsDto>> _searchPayments;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto> createPayment,
        ICommandHandler<ApprovePaymentCommand, PaymentStatusDto> approvePayment,
        ICommandHandler<RejectPaymentCommand, PaymentStatusDto> rejectPayment,
        IQueryHandler<GetPaymentQuery, PaymentDetailsDto> getPayment,
        IQueryHandler<SearchPaymentsQuery, PagedResult<PaymentDetailsDto>> searchPayments,
        ILogger<PaymentsController> logger)
    {
        _createPayment = createPayment;
        _approvePayment = approvePayment;
        _rejectPayment = rejectPayment;
        _getPayment = getPayment;
        _searchPayments = searchPayments;
        _logger = logger;
    }

    /// <summary>GET /api/payments - Zahlungsaufträge suchen/filtern, paginiert.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PaymentDetailsDto>>> Search(
        [FromQuery] Guid? sourceAccountId,
        [FromQuery] PaymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Zahlungssuche: sourceAccountId={SourceAccountId}, status={Status}, page={Page}, pageSize={PageSize}",
            sourceAccountId, status, page, pageSize);

        var query = new SearchPaymentsQuery(
            sourceAccountId.HasValue ? new AccountId(sourceAccountId.Value) : null, status, page, pageSize);

        var result = await _searchPayments.Handle(query, cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>POST /api/payments - legt einen neuen Zahlungsauftrag an und reicht ihn direkt zur Freigabe ein.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatePaymentResultDto>> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Zahlungsauftrag wird angelegt: sourceAccountId={SourceAccountId}, amount={Amount} {Currency}",
            request.SourceAccountId, request.Amount, request.Currency);

        var command = new CreatePaymentCommand(
            new AccountId(request.SourceAccountId),
            request.TargetAccountNumber,
            request.Amount,
            request.Currency,
            request.Reference);

        var result = await _createPayment.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error!.ToProblemResult<CreatePaymentResultDto>();
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id.Value }, result.Value);
    }

    /// <summary>GET /api/payments/{id} - Zahlungsauftragsdetails.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Zahlungsauftragsdetails angefragt: {PaymentId}", id);

        var result = await _getPayment.Handle(new GetPaymentQuery(new PaymentId(id)), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>POST /api/payments/{id}/approve - "Payments bearbeiten": nur OperationsManager (und Administrator).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.OperationsManager)]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentStatusDto>> Approve(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Zahlungsauftrag wird genehmigt: {PaymentId}", id);

        var result = await _approvePayment.Handle(new ApprovePaymentCommand(new PaymentId(id)), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>POST /api/payments/{id}/reject - "Payments bearbeiten": nur OperationsManager (und Administrator).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.OperationsManager)]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentStatusDto>> Reject(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Zahlungsauftrag wird abgelehnt: {PaymentId}", id);

        var result = await _rejectPayment.Handle(new RejectPaymentCommand(new PaymentId(id)), cancellationToken);

        return result.ToActionResult();
    }
}
