using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Services;

/// <summary>
/// Fournit le modèle LLM actif sélectionné par l'utilisateur.
/// Thread-safe via lock : utilisé en singleton dans l'application.
/// </summary>
public class ActiveLlmModelProvider : IActiveLlmModelProvider
{
    private string? _selectedModel;
    private readonly Lock _lock = new();

    public string? GetSelectedModel()
    {
        lock (_lock) { return _selectedModel; }
    }

    public void ResetSelection()
    {
        lock (_lock) { _selectedModel = null; }
    }

    public void SelectModel(string modelName)
    {
        lock (_lock) { _selectedModel = modelName; }
    }
}
