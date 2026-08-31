using Banking.Application.Accounts;
using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Customers.Dtos;
using Banking.Application.Sap;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Customers.GetCustomerSapProfile;

public sealed class GetCustomerSapProfileHandler : IQueryHandler<GetCustomerSapProfileQuery, CustomerSapProfileDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ISapCustomerService _sapCustomerService;
    private readonly ILogger<GetCustomerSapProfileHandler> _logger;

    public GetCustomerSapProfileHandler(
        ICustomerRepository customerRepository,
        IAccountRepository accountRepository,
        ISapCustomerService sapCustomerService,
        ILogger<GetCustomerSapProfileHandler> logger)
    {
        _customerRepository = customerRepository;
        _accountRepository = accountRepository;
        _sapCustomerService = sapCustomerService;
        _logger = logger;
    }

    public async Task<Result<CustomerSapProfileDto>> Handle(GetCustomerSapProfileQuery query, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result<CustomerSapProfileDto>.Failure(Error.NotFound(
                "Customer.NotFound", $"Kunde {query.CustomerId} wurde nicht gefunden."));
        }

        var accounts = await _accountRepository.SearchAsync(customer.Id, status: null, page: 1, pageSize: 100, cancellationToken);

        // SAP ist eine externe Abhängigkeit außerhalb unserer Kontrolle - ein Ausfall dort
        // darf die eigentliche Kundenansicht nicht mit hinunterreißen. Statt die
        // SapIntegrationException durchzureichen (-> 500 für eine reine Datenanreicherung),
        // degradieren wir bewusst auf die rein lokalen Daten und machen das über
        // SapAvailable=false für die aufrufende Seite sichtbar.
        var sapAvailable = true;
        SapCustomerProfile? sapProfile = null;
        try
        {
            sapProfile = await _sapCustomerService.GetCustomerAsync(customer.CustomerNumber, cancellationToken);
        }
        catch (SapIntegrationException ex)
        {
            sapAvailable = false;
            _logger.LogWarning(ex, "SAP nicht erreichbar bei Kundenprofil-Anreicherung für {CustomerId}", customer.Id);
        }

        var accountDtos = new List<CustomerSapAccountDto>(accounts.Items.Count);
        foreach (var account in accounts.Items)
        {
            string? sapAccountStatus = null;
            bool? sapIbanVerified = null;

            if (sapAvailable)
            {
                try
                {
                    var sapAccountInfo = await _sapCustomerService.GetAccountAsync(account.AccountNumber, cancellationToken);
                    sapAccountStatus = sapAccountInfo?.SapAccountStatus;
                    sapIbanVerified = sapAccountInfo?.IbanVerified;
                }
                catch (SapIntegrationException ex)
                {
                    sapAvailable = false;
                    _logger.LogWarning(ex, "SAP nicht erreichbar bei Konto-Anreicherung für {AccountId}", account.Id);
                }
            }

            accountDtos.Add(new CustomerSapAccountDto(
                account.Id,
                account.AccountNumber.Value,
                account.AccountType,
                account.Currency.Value,
                account.Balance.Amount,
                account.Status,
                sapAccountStatus,
                sapIbanVerified));
        }

        var sapProfileDto = sapProfile is null
            ? null
            : new SapCustomerProfileSectionDto(
                sapProfile.TaxId,
                sapProfile.Street,
                sapProfile.PostalCode,
                sapProfile.City,
                sapProfile.Country,
                sapProfile.RiskRating.ToString(),
                sapProfile.SyncedAt);

        var dto = new CustomerSapProfileDto(
            customer.Id,
            customer.CustomerNumber.Value,
            customer.FirstName,
            customer.LastName,
            customer.Email.Value,
            customer.Status,
            sapAvailable,
            sapProfileDto,
            accountDtos);

        return Result<CustomerSapProfileDto>.Success(dto);
    }
}
