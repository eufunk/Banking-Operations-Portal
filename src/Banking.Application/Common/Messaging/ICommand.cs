using Banking.Application.Common;

namespace Banking.Application.Common.Messaging;

/// <summary>Marker für einen Use Case, der Zustand ändert.</summary>
public interface ICommand<TResult>
{
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken);
}
