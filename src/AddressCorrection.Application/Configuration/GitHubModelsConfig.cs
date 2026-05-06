namespace AddressCorrection.src.AddressCorrection.Application.Configuration;

public class GitHubModelsConfig
{
    /// <summary>Token GitHub par défaut (utilisé si le modèle n'en définit pas un spécifique).</summary>
    public string Token { get; set; } = string.Empty;

    public List<LlmModelConfig> Models { get; set; } = new();
}

