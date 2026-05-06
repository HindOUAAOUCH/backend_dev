namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Fournit le modèle LLM actif sélectionné par l'utilisateur.
/// Thread-safe : utilisé en singleton.
/// </summary>
public interface IActiveLlmModelProvider
{
    void SelectModel(string modelName);
    string? GetSelectedModel();
    void ResetSelection();
}
