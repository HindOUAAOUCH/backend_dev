using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Contrat pour l'envoi de notifications webhook après une correction d'adresse.
/// L'implémentation est fire-and-forget : les erreurs sont loguées mais ne font pas
/// échouer la requête de correction principale.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>
    /// Envoie une notification POST à l'URL de webhook configurée sur l'intégration.
    /// Si l'intégration n'a pas de webhook configuré, l'appel est ignoré silencieusement.
    /// </summary>
    /// <param name="integrationId">Identifiant de l'intégration source.</param>
    /// <param name="payload">Données de l'événement à envoyer.</param>
    /// <param name="ct">Token d'annulation — timeout court recommandé (5s).</param>
    Task DispatchAsync(
        string integrationId,
        WebhookPayload payload,
        CancellationToken ct = default);
}