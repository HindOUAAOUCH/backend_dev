namespace AddressCorrection.src.AddressCorrection.Application.Pipeline;

public class AddressCorrectionPipeline
{
    private readonly IEnumerable<ICorrectionStep> _steps;

    public AddressCorrectionPipeline(IEnumerable<ICorrectionStep> steps)
    {
        _steps = steps;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        foreach (var step in _steps)
        {
            // Short-circuit on failure, but always-run steps (e.g. TrackingStep) still execute
            if (context.IsFailed && !step.RunAlways) continue;

            context = await step.ExecuteAsync(context);
        }

        return context;
    }
}
