namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IModelSelectionService
{
    void SelectModel(string modelName);
    string? GetSelectedModel();
    void ResetSelection();
}
