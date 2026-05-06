namespace AddressCorrection.src.AddressCorrection.Application.Pipeline;

public interface ICorrectionStep
{
    /// <summary>
    /// When true, this step always executes regardless of pipeline failure state.
    /// Use for steps that must run even when a previous step has set <see cref="AddressCorrectionContext.IsFailed"/>.
    /// Defaults to false.
    /// </summary>
    bool RunAlways => false;

    Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context);
}
