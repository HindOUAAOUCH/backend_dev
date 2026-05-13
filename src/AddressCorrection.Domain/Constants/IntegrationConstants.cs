namespace AddressCorrection.src.AddressCorrection.Domain.Constants;

/// <summary>
/// Constantes métier pour les intégrations et les clés API.
/// Centralise les magic strings pour éviter les typos et faciliter les refactorings.
/// </summary>
public static class IntegrationConstants
{
	/// <summary>Plateformes supportées.</summary>
	public static class Platform
	{
		public const string Shopify = "shopify";
		public const string WooCommerce = "woocommerce";
		public const string Magento = "magento";
		public const string Custom = "custom";
	}

	/// <summary>Scopes d'autorisation disponibles pour les clés API.</summary>
	public static class Scope
	{
		/// <summary>Permet de soumettre des adresses à corriger.</summary>
		public const string Correct = "correct";

		/// <summary>Permet de consulter l'historique des corrections.</summary>
		public const string Read = "read";

		/// <summary>Accès complet (correct + read).</summary>
		public const string FullAccess = "full_access";

		public static readonly IReadOnlyList<string> All = [Correct, Read, FullAccess];
	}

	/// <summary>Préfixes de clé API selon l'environnement.</summary>
	public static class KeyPrefix
	{
		public const string Live = "sk_live_";
		public const string Test = "sk_test_";
	}

	/// <summary>Limites de configuration.</summary>
	public static class Limits
	{
		/// <summary>Nombre maximum de clés API actives par intégration.</summary>
		public const int MaxActiveKeysPerIntegration = 5;

		/// <summary>Nombre maximum d'intégrations par client.</summary>
		public const int MaxIntegrationsPerClient = 10;

		/// <summary>Longueur de la partie aléatoire de la clé API (en octets).</summary>
		public const int KeyRandomByteLength = 32;

		/// <summary>Nombre d'itérations PBKDF2 pour le hachage des clés.</summary>
		public const int Pbkdf2Iterations = 100_000;
	}

	/// <summary>Noms de collections MongoDB pour cette feature.</summary>
	public static class Collections
	{
		public const string Integrations = "integrations";
		public const string ApiKeys = "api_keys";
	}
}