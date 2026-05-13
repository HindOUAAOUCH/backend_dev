namespace AddressCorrection.src.AddressCorrection.Domain.Enums;

/// <summary>
/// Cycle de vie d'une intégration.
/// </summary>
public enum IntegrationStatus
{
	/// <summary>L'intégration est opérationnelle et accepte les requêtes.</summary>
	Active,

	/// <summary>L'intégration est temporairement suspendue par le client.</summary>
	Paused,

	/// <summary>L'intégration a été supprimée (soft-delete).</summary>
	Deleted
}