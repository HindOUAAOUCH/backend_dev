namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface ILlmClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, string modelName);
}
