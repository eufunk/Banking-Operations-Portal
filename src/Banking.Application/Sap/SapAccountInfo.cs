namespace Banking.Application.Sap;

/// <summary>Kontostatus, wie SAP ihn führt - eigener Wertebereich, unabhängig von unserem <c>AccountStatus</c>.</summary>
public sealed record SapAccountInfo(
    string SapAccountStatus,
    bool IbanVerified,
    DateTimeOffset SyncedAt);
