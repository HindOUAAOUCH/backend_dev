namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IActiveLlmModelProvider
{
    void SelectModel(string modelName);
    string? GetSelectedModel();
    void ResetSelection();
}
