namespace Banking.Domain.Common.Exceptions;

/// <summary>
/// Basisklasse für alle Ausnahmen, die eine Verletzung einer Geschäftsregel signalisieren
/// (im Unterschied zu strukturellen Validierungsfehlern wie einem leeren Pflichtfeld).
/// Die Api-Schicht kann später gezielt auf diesen Typ mappen (z. B. HTTP 409/422 statt 500).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
