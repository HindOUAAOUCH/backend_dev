using AddressCorrection.src.AddressCorrection.Application.DTOs;
using System.Diagnostics;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline;

/// <summary>
/// État partagé entre les étapes du pipeline de correction d'adresses.
/// Chaque étape lit et enrichit ce contexte.
/// </summary>
public class AddressCorrectionContext
{
    public AddressRequest Request { get; set; } = null!;
    public string? NormalizedAddress { get; set; }
    public AddressResponse? Result { get; set; }
    public string? ModelUsed { get; set; }
    public bool FromCache { get; set; }
    public string? Status { get; set; }
    public Exception? Error { get; set; }

    /// <summary>Vrai si une erreur irrécupérable s'est produite dans le pipeline.</summary>
    public bool IsFailed => Error != null;

    /// <summary>Chronomètre démarré à la création du contexte pour mesurer la durée totale.</summary>
    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
}
