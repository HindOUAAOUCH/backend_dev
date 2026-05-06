namespace AddressCorrection.src.AddressCorrection.Application.Exceptions;

public class AllModelsFailedException : Exception
{
    public AllModelsFailedException()
        : base("All LLM models failed to process the request.") { }
}