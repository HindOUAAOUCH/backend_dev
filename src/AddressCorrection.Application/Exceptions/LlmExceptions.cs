namespace AddressCorrection.src.AddressCorrection.Application.Exceptions;

/// <summary>
/// Exception de base pour toutes les erreurs LLM.
/// </summary>
public class LlmClientException : Exception
{
    public int? HttpStatusCode { get; }

    public LlmClientException(string message) : base(message) { }

    public LlmClientException(string message, int httpStatusCode) : base(message)
    {
        HttpStatusCode = httpStatusCode;
    }

    public LlmClientException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Le modèle a retourné HTTP 429 — rate limit atteint.
/// Polly ne doit PAS retenter sur cette erreur.
/// </summary>
public class LlmRateLimitException : LlmClientException
{
    public LlmRateLimitException(string modelName)
        : base($"Rate limit reached for model '{modelName}' (HTTP 429).") { }
}

/// <summary>
/// Authentification refusée — token invalide ou expiré.
/// Polly ne doit PAS retenter.
/// </summary>
public class LlmAuthenticationException : LlmClientException
{
    public LlmAuthenticationException(string modelName, int statusCode)
        : base($"Authentication failed for model '{modelName}' (HTTP {statusCode}).") { }
}

/// <summary>
/// Le modèle a retourné une réponse vide ou non désérialisable.
/// </summary>
public class LlmEmptyResponseException : LlmClientException
{
    public LlmEmptyResponseException(string modelName)
        : base($"Empty or invalid response from model '{modelName}'.") { }
}