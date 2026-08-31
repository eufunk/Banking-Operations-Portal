namespace Banking.Infrastructure.Sap;

// Rohes SAP-Antwortformat (Feldnamen angelehnt an typische SAP-BAPI-Strukturen, z. B.
// BAPI_CUSTOMER_GETDETAIL2: STCD1 = Steuernummer, STRAS = Straße, LAND1 = Länderschlüssel).
// Bewusst internal und nur SapRfcCustomerService bekannt - der Rest der Anwendung sieht
// ausschließlich die bereits gemappten Typen aus Banking.Application.Sap.
internal sealed record SapCustomerResponse(
    string StCd1,
    string Stras,
    string PostCode1,
    string City1,
    string Land1,
    int RiskClass);

internal sealed record SapAccountResponse(string AccountStatus, bool IbanValidated);
