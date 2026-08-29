namespace Banking.Domain.Common.Exceptions;

public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(string entityName, string fromStatus, string toStatus)
        : base($"{entityName} kann nicht von Status '{fromStatus}' zu '{toStatus}' wechseln.")
    {
    }
}
