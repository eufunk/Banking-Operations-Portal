using Banking.Application.Common;

namespace Banking.Application.Common.Messaging;

/// <summary>Marker für einen Use Case, der nur liest.</summary>
public interface IQuery<TResult>
{
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
}
