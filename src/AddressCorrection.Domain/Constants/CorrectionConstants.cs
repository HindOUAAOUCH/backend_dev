namespace AddressCorrection.src.AddressCorrection.Domain.Constants;

/// <summary>
/// Constantes métier pour les statuts, sources et modèles de correction d'adresses.
/// Centralise toutes les valeurs littérales utilisées dans le domaine.
/// </summary>
public static class CorrectionConstants
{
    /// <summary>Statuts possibles d'une correction.</summary>
    public static class Status
    {
        public const string Success = "success";
        public const string Failed = "failed";
    }

    /// <summary>Sources d'origine d'une requête de correction.</summary>
    public static class Source
    {
        public const string Api = "API";
    }

    /// <summary>Valeurs spéciales pour le modèle LLM utilisé.</summary>
    public static class Model
    {
        /// <summary>Aucun modèle utilisé (résultat du cache ou échec total).</summary>
        public const string None = "none";
    }
}
