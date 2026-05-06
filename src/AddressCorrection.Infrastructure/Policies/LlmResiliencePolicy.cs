using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Policies;

public static class LlmResiliencePolicy
{
    private const int TimeoutSeconds = 60;

    public static AsyncPolicyWrap Build(ILogger logger, string modelName)
    {
        var timeoutPolicy = Policy
            .TimeoutAsync(TimeoutSeconds, TimeoutStrategy.Pessimistic,
                (ctx, ts, task) =>
                {
                    logger.LogWarning(
                        "Model {Model} — timeout after {Seconds}s. Moving to next model.",
                        modelName, ts.TotalSeconds);
                    return Task.CompletedTask;
                });

        var retryPolicy = Policy
            .Handle<Exception>(ex =>
                // Ne pas retenter si timeout (inutile), ni si auth/rate-limit
                // (le token ne changera pas et le 429 s'empire avec les retries)
                ex is not TimeoutRejectedException
                && ex is not LlmAuthenticationException
                && ex is not LlmRateLimitException)
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, delay, attempt, _) =>
                {
                    logger.LogWarning(
                        "Model {Model} — attempt {Attempt} failed. Waiting {Delay}s. Error: {Error}",
                        modelName, attempt, delay.TotalSeconds, exception.Message);
                });

        return retryPolicy.WrapAsync(timeoutPolicy);
    }
}
