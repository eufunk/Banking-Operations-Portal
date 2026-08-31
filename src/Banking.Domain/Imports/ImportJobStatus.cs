namespace Banking.Domain.Imports;

/// <summary>Entspricht den in Kapitel 12 geforderten Statuswerten der Azure-Data-Factory-Pipeline.</summary>
public enum ImportJobStatus
{
    Started,
    Processing,
    Completed,
    Failed,
}
