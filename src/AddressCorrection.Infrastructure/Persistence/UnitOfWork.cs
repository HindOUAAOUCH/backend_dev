using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

/// <summary>
/// Implémentation du pattern Unit of Work.
/// Regroupe les repositories pour coordonner les opérations de persistance.
/// Extension future : ajouter les sessions MongoDB pour les transactions ACID (replica set requis).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    public IAddressRepository Addresses { get; }
    public ICorrectionRequestRepository Corrections { get; }
    public IUsageTrackingRepository Usage { get; }

    public UnitOfWork(
        IAddressRepository addresses,
        ICorrectionRequestRepository corrections,
        IUsageTrackingRepository usage)
    {
        Addresses = addresses;
        Corrections = corrections;
        Usage = usage;
    }

    /// <summary>
    /// Dans cette implémentation sans session MongoDB, CommitAsync est une no-op.
    /// Pour activer les transactions ACID, injecter IClientSessionHandle et appeler
    /// session.CommitTransactionAsync() ici (nécessite un replica set MongoDB).
    /// </summary>
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void Dispose()
    {
        // Libération future des ressources de session MongoDB si nécessaire.
    }

    /// <summary>
    /// Libération asynchrone des ressources — utilisée pour les sessions MongoDB futures.
    /// </summary>
    public ValueTask DisposeAsync() =>
        ValueTask.CompletedTask;
}
