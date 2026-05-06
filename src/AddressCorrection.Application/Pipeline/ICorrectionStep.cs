namespace AddressCorrection.src.AddressCorrection.Application.Pipeline;

/// <summary>
/// Contrat d'une étape du pipeline de correction d'adresses.
/// Chaque étape est responsable d'une seule transformation du contexte.
/// </summary>
public interface ICorrectionStep
{
    Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context);
}
