namespace Banking.Application.Common;

/// <summary>
/// Abstraktion über "alle bisherigen Repository-Änderungen jetzt committen". Entkoppelt
/// Application von EF Core - Infrastructure implementiert dies als dünnen Wrapper um
/// BankingDbContext.SaveChangesAsync.
/// </summary>
public interface IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
