namespace AddressCorrection.src.AddressCorrection.Application.Configuration;

public class LlmModelConfig
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }

    /// <summary>
    /// Token GitHub propre à ce modèle (optionnel).
    /// Si null ou vide, le token global GitHubModelsConfig.Token est utilisé.
    /// Permet d'associer un compte GitHub différent à chaque modèle
    /// pour contourner les rate-limits.
    /// </summary>
    public string? Token { get; set; }
}
