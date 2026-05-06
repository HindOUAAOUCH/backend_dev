namespace AddressCorrection.src.AddressCorrection.Application.Pipeline;

/// <summary>
/// Orchestre l'exécution séquentielle des étapes de correction d'adresses.
/// Toutes les étapes s'exécutent dans l'ordre ; chaque étape décide elle-même
/// si elle doit agir en fonction de l'état du contexte.
/// </summary>
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
            context = await step.ExecuteAsync(context);
        }
        return context;
    }
}
