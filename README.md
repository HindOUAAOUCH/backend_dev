# Address-Correction-System
Système de correction des adresses clients pour l'optimisation des flux logistiques

## 🚀 Lancer le backend en développement

### Prérequis
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MongoDB en local sur `localhost:27017`

### Démarrage

```bash
# Depuis la racine du projet
dotnet run --launch-profile http
```

Le backend démarre sur **`http://localhost:5000`** avec :
- `Hosting environment: Development`
- Swagger UI accessible sur `http://localhost:5000/swagger`

> ⚠️ Ne pas utiliser `dotnet run` sans profil explicite, ni lancer le `.dll` directement :
> ces méthodes ignorent le `launchSettings.json` et démarrent en mode **Production** sur le port 5000.

---

## 🖥️ Configuration frontend (React + Vite)

Créer un fichier `.env.local` à la racine du projet frontend :

```env
VITE_API_URL=http://localhost:5000
```

> ⚠️ Utiliser **HTTP** (pas HTTPS) pour le développement local.

---

## 📐 Ports de référence

| Profil `launchSettings` | URL backend                              | Usage          |
|-------------------------|------------------------------------------|----------------|
| `http` (défaut dev)     | `http://localhost:5000`                  | Développement  |
| `https`                 | `https://localhost:7157` + `http://5251` | Dev avec HTTPS |

---

## 📝 Variables d'environnement requises

| Variable                        | Description                          |
|---------------------------------|--------------------------------------|
| `ASPNETCORE_ENVIRONMENT`        | `Development` / `Production`         |
| `MongoDB__ConnectionString`     | ex: `mongodb://localhost:27017`      |
| `MongoDB__DatabaseName`         | ex: `AddressCorrectionDb`            |
| `GitHubModels__Token`           | Token GitHub Models (LLM)            |

En développement, ces valeurs peuvent être définies dans `appsettings.Development.json` (non versionné) ou via `dotnet user-secrets`.
