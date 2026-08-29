using Banking.Api.Contracts.Payments;
using Banking.Api.Extensions;
using Banking.Application.Common.Messaging;
using Banking.Application.Payments.CreatePayment;
using Banking.Application.Payments.Dtos;
using Banking.Application.Payments.GetPayment;
using Banking.Domain.Accounts;
using Banking.Domain.Payments;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto> _createPayment;
    private readonly IQueryHandler<GetPaymentQuery, PaymentDetailsDto> _getPayment;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ICommandHandler<CreatePaymentCommand, CreatePaymentResultDto> createPayment,
        IQueryHandler<GetPaymentQuery, PaymentDetailsDto> getPayment,
        ILogger<PaymentsController> logger)
    {
        _createPayment = createPayment;
        _getPayment = getPayment;
        _logger = logger;
    }

    /// <summary>POST /api/payments - legt einen neuen Zahlungsauftrag im Status Draft an.</summary>
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
}
