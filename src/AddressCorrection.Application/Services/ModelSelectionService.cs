using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

public class ModelSelectionService : IModelSelectionService
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
