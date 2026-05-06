namespace AddressCorrection.src.AddressCorrection.Application.Pipeline;

public interface ICorrectionStep
{
    Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context);
}
