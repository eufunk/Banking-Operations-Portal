namespace Banking.Domain.Transactions;

/// <summary>
/// Bestimmt implizit das Vorzeichen der Wirkung auf den Kontosaldo
/// (siehe <see cref="Transaction.IncreasesBalance"/>). Amount selbst ist immer
/// eine positive Betragsgröße - Vorzeichen entstehen nicht durch negative Zahlen,
/// sondern durch den Typ (Muster aus realen Kernbankensystemen, z. B. Soll/Haben-Indikator).
/// </summary>
public enum TransactionType
{
    Deposit,
    Withdrawal,
    TransferIn,
    TransferOut,
    Fee,
    Interest,
    Reversal,
}
