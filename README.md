# ETHAN TCM

ETHAN TCM est une application web d'entreprise pour la gestion des obligations fiscales, des declarations, des echeances, des rappels et des preuves de soumission ou de paiement.

## Architecture

La solution suit une approche Clean Architecture / Modular Monolith.

Pour reprendre le contexte fonctionnel et technique complet avec un nouveau compte Codex, lire d'abord `CODEX_HANDOFF.md`.

```text
src/
  EthanTcm.Web             ASP.NET Core MVC, interface utilisateur, IIS, authentification
  EthanTcm.Application     Cas d'usage, contrats applicatifs, orchestration
  EthanTcm.Domain          Entites et regles metier fiscales
  EthanTcm.Infrastructure  EF Core, SQL Server, services techniques
  EthanTcm.Jobs            Worker pour rappels, echeances et traitements planifies

tests/
  EthanTcm.Tests           Tests unitaires et tests d'architecture
```

## Prerequis

- .NET SDK 10.0 ou superieur compatible avec `net10.0`
- SQL Server local ou distant
- Visual Studio recent, Rider ou VS Code
- IIS Hosting Bundle .NET pour le deploiement IIS

## Base de donnees

La solution est preparee pour SQL Server via Entity Framework Core.

Connection string de developpement :

```json
"EthanTcmDatabase": "Server=localhost;Database=EthanTcm;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

En production, remplacer `YOUR-SQL-CLUSTER-LISTENER` dans `appsettings.Production.json` par le listener SQL Server Cluster.

## Lancer le projet web

Depuis la racine :

```powershell
dotnet restore EthanTcm.sln
dotnet build EthanTcm.sln
dotnet run --project src\EthanTcm.Web\EthanTcm.Web.csproj
```

Puis ouvrir l'URL affichee par `dotnet run`.

Le seed initial n'est plus execute a chaque demarrage. Pour le lancer
explicitement sur une nouvelle base :

```powershell
dotnet run --project src\EthanTcm.Web\EthanTcm.Web.csproj -- seed-initial-tax-obligations
```

## Lancer le worker de jobs

```powershell
dotnet run --project src\EthanTcm.Jobs\EthanTcm.Jobs.csproj
```

Le worker est prepare pour fonctionner comme Windows Service via `AddWindowsService`.

## Migrations EF Core

Le `DbContext` principal est `EthanTcm.Infrastructure.Persistence.EthanTcmDbContext`.

Exemple de creation de migration :

```powershell
dotnet ef migrations add InitialCreate --project src\EthanTcm.Infrastructure --startup-project src\EthanTcm.Web --context EthanTcmDbContext
dotnet ef database update --project src\EthanTcm.Infrastructure --startup-project src\EthanTcm.Web --context EthanTcmDbContext
```

Installer `dotnet-ef` si necessaire :

```powershell
dotnet tool install --global dotnet-ef
```

## Authentification

- Developpement : utilisateur local simule par `DevelopmentCurrentUser`.
- Production : Windows Authentication / Active Directory via IIS.

Pour IIS :

- activer Windows Authentication ;
- desactiver Anonymous Authentication ;
- publier `EthanTcm.Web` ;
- configurer la connection string de production ;
- verifier les droits du pool applicatif sur les ressources necessaires.

## Tests

```powershell
dotnet test EthanTcm.sln
```

## Performance

- la liste des declarations utilise une recherche, un tri et une pagination SQL ;
- le tableau de bord projette uniquement les donnees necessaires et dispose d'un
  cache par utilisateur et par vue de 20 secondes ;
- les KPI du tableau de bord sont regroupes par priorite et disposent de
  modales de detail paginees, securisees et chargees a la demande ;
- les reponses HTML sont compressees avec Brotli ou Gzip ;
- les commandes SQL EF Core ne sont plus journalisees au niveau `Information` ;
- la migration `AddPerformanceIndexes` ajoute les index composites utilises par
  les declarations et les preuves documentaires.
