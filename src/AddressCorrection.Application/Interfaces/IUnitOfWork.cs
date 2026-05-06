namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Pattern Unit of Work : regroupe les repositories pour coordonner les opérations
/// et offrir un point d'extension pour les transactions futures.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IAddressRepository Addresses { get; }
    ICorrectionRequestRepository Corrections { get; }
    IUsageTrackingRepository Usage { get; }

    /// <summary>
    /// Valide et finalise toutes les opérations en attente.
    /// Dans une implémentation avec session MongoDB (replica set), cela committerait la transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
