# ETHAN TCM - Codex Handoff

Ce document sert de memoire projet pour un nouveau compte Codex. A lire avant toute nouvelle modification.

## Objectif produit

ETHAN TCM est une application web d'entreprise pour piloter la conformite fiscale :

- obligations fiscales et responsables operationnels ;
- generation et suivi des declarations ;
- preparation, approbations multi-niveaux, soumission, paiement et cloture ;
- preuves documentaires de preparation, soumission et paiement ;
- rappels d'echeances, notifications, audit log et dashboard de pilotage.

## Stack technique

- Solution .NET : `EthanTcm.sln`
- Framework cible : `net10.0`
- Web : ASP.NET Core MVC dans `src/EthanTcm.Web`
- Application : cas d'usage et contrats dans `src/EthanTcm.Application`
- Domain : entites et regles metier dans `src\EthanTcm.Domain`
- Infrastructure : EF Core, SQL Server, services techniques dans `src\EthanTcm.Infrastructure`
- Jobs : worker de notifications/rappels dans `src\EthanTcm.Jobs`
- Tests : `tests\EthanTcm.Tests`
- Base : SQL Server via `EthanTcmDbContext`
- Auth dev : profils locaux simulant les roles
- Auth prod : Windows Authentication / Active Directory via IIS

Commandes de reference :

```powershell
dotnet restore EthanTcm.sln
dotnet build EthanTcm.sln
dotnet test EthanTcm.sln
dotnet run --project src\EthanTcm.Web\EthanTcm.Web.csproj
```

Si le build echoue car des DLL sont verrouillees, verifier d'abord qu'un serveur local n'est pas deja lance.

## Architecture

Le projet suit une Clean Architecture / Modular Monolith :

- `Domain` contient les entites, enums et invariants.
- `Application` contient les interfaces, DTO, permissions et contrats metier.
- `Infrastructure` implemente EF Core et les services applicatifs.
- `Web` contient les controllers, vues Razor, CSS/JS et configuration ASP.NET Core.
- `Jobs` execute les traitements planifies.

Respecter cette separation. Eviter les refactors transverses non demandes.

## Roles et permissions

Roles principaux :

- `Administrator`
- `TaxManager`
- `Preparer`
- `Approver`
- `FinanceManager`
- `Auditor`
- `ReadOnly`

La source d'autorite est :

- `src\EthanTcm.Application\Authentication\ApplicationPermissions.cs`
- `src\EthanTcm.Web\Program.cs`

Les boutons peuvent etre caches en UI, mais les controles serveur sont obligatoires via policies ASP.NET Core.

## Workflow declaration

Statuts principaux dans `TaxDeclarationStatus` :

- `ToPrepare`
- `InPreparation`
- `SubmittedForReview`
- `ApprovedLevel1`
- `ApprovedLevel2`
- `ApprovedLevel3`
- `ReadyForSubmission`
- `Submitted`
- `PaymentPending`
- `Paid`
- `Closed`
- `Late`
- `Cancelled`
- `NotApplicable`
- `Rejected`

Service central :

- `src\EthanTcm.Infrastructure\Services\TaxDeclarationWorkflowService.cs`

Regles importantes :

- seul le preparateur assigne peut preparer/soumettre, sauf `TaxManager` et `Administrator` ;
- le preparateur assigne ne peut pas approuver sa propre declaration, sauf role privilegie ;
- les approbations sont separees par niveau 1, 2, 3 ;
- un meme approbateur ne peut pas approuver plusieurs niveaux dans le meme cycle ;
- si un rejet intervient, un nouveau cycle d'approbation doit redemarrer proprement ;
- une preuve de preparation est obligatoire avant `SubmitForReview` ;
- une preuve de soumission est obligatoire avant `MarkSubmitted` ;
- une preuve de paiement est obligatoire avant `MarkPaid`.

## Assignations et responsables

Les obligations fiscales portent plusieurs responsables :

- preparateur / assignee ;
- approbateurs niveau 1, 2, 3 ;
- responsable soumission ;
- responsable paiement ;
- responsable suivi / cloture.

Les champs de responsibilities peuvent etre partiellement remplis : il ne faut pas imposer que tous soient renseignes en meme temps.

Les responsables sont visibles dans le detail declaration afin que chaque utilisateur comprenne qui doit faire l'action suivante.

## Dashboard

Controller :

- `src\EthanTcm.Web\Controllers\HomeController.cs`

Service :

- `src\EthanTcm.Infrastructure\Services\DashboardService.cs`

Contrats :

- `src\EthanTcm.Application\Abstractions\IDashboardService.cs`

Vues dashboard :

- `MyTasks`
- `TeamTasks`
- `ManagementOverview`
- `LateItems`
- `ComplianceOverview`
- `PaymentPending`
- `LatePayments`
- `MissingPaymentProof`

KPIs critiques :

- dues today ;
- dues in less than 5 days ;
- late ;
- pending approvals ;
- pending payments ;
- missing submission proof ;
- missing payment proof ;
- obligations without responsible ;
- penalty risk.

Les KPIs d'urgence absolue doivent clignoter, notamment :

- declarations echues dans moins de 5 jours ;
- late ;
- penalty risk.

Le dashboard est filtre selon le role et l'utilisateur. Par exemple un `Preparer` ne voit que les declarations/taxes qui le concernent.

## Paiements

Module dedie recemment ajoute :

- controller : `src\EthanTcm.Web\Controllers\PaymentsController.cs`
- vue : `src\EthanTcm.Web\Views\Payments\Index.cshtml`

Routes :

- `/Payments/Pending`
- `/Payments/Late`
- `/Payments/MissingProof`

Le menu `Payments` pointe vers ces routes, pas vers des filtres dashboard generiques.

Regles :

- toutes les declarations suivent le paiement par defaut ;
- preuve de paiement obligatoire avant passage a `Paid` ;
- acces reserve aux roles ayant `Permissions.TaxPayments.Manage`.

## Documents

Service :

- `src\EthanTcm.Infrastructure\Services\TaxDocumentService.cs`

Points sensibles :

- uploads proteges ;
- extensions et tailles controlees ;
- acces direct aux fichiers bloque ;
- telechargement via controller/service avec verification d'autorisation ;
- suppression logique des documents ;
- audit lors des uploads/suppressions.

Types importants :

- preuve de preparation ;
- preuve de soumission ;
- preuve de paiement.

## Notifications et rappels

Entites :

- `NotificationLog`
- `NotificationTemplate`
- `NotificationRule`

Service :

- `src\EthanTcm.Infrastructure\Services\DeadlineReminderService.cs`

Worker :

- `src\EthanTcm.Jobs`

Rappels prevus :

- J-30, J-15, J-10, J-5, J-1, Jour J, apres echeance.

En developpement, un mode DryRun SMTP est prevu pour tester sans envoyer de vrais e-mails.

## Audit log

Entite :

- `AuditLog`

Service :

- `src\EthanTcm.Infrastructure\Services\AuditService.cs`
- contrat : `src\EthanTcm.Application\Abstractions\IAuditService.cs`

Page :

- `src\EthanTcm.Web\Controllers\AuditLogsController.cs`

Acces reserve :

- `Administrator`
- `Auditor`

Actions importantes a tracer :

- creation, modification, suppression logique ;
- changement de statut ;
- approbation, rejet ;
- upload/suppression document ;
- generation declaration ;
- import Excel ;
- envoi notification ;
- changement de responsable.

## UI/UX actuelle

L'interface a ete orientee vers un style finance/tax sobre et operationnel :

- menu haut uniquement ;
- suppression du menu lateral ;
- menus deroulants par domaine ;
- icones professionnelles via sprite SVG local ;
- tableaux avec recherche dynamique et pagination 10 lignes par defaut ;
- detail declaration oriente parcours operationnel ;
- blocs d'approbation alignes horizontalement ;
- actions affichees seulement aux utilisateurs habilites ;
- alertes et loaders type SweetAlert/SweetLoader pour les actions.

Fichiers UI principaux :

- `src\EthanTcm.Web\Views\Shared\_Layout.cshtml`
- `src\EthanTcm.Web\wwwroot\css\site.css`
- `src\EthanTcm.Web\wwwroot\js\site.js`
- `src\EthanTcm.Web\Views\TaxDeclarations\Details.cshtml`

## Menus actuels

Navigation principale :

- Dashboard
- Declarations
  - All declarations
  - Generate declarations
  - Manual declaration
- Payments
  - Payments follow-up
  - Late payments
  - Missing payment proof
- Referential
  - Tax obligations
  - Import matrix
- Monitoring
  - Compliance overview
  - Late items
  - Audit log
- Administration
  - Admin seed
- Profile / Dev user switcher

Les menus sont conditionnes par les permissions.

## Deploiement

Document detaille :

- `DEPLOYMENT.md`

Points de production :

- IIS + ASP.NET Core Hosting Bundle ;
- Windows Authentication activee ;
- Anonymous Authentication desactivee ;
- SQL Server production / cluster listener ;
- dossier documents securise hors webroot ;
- SMTP configure ;
- migrations EF Core controlees ;
- backup/restore SQL Server documente.

## Tests et verification

Avant de rendre une modification :

```powershell
dotnet build EthanTcm.sln
dotnet test EthanTcm.sln --no-build
```

Dernier etat connu apres ajout des pages Payment :

- build OK ;
- tests OK ;
- 57 tests passes ;
- serveur local OK sur `http://localhost:5147` ;
- routes Payment verifiees en `200 OK`.

## Points sensibles deja rencontres

- Les migrations EF et la base locale doivent rester synchronisees.
- Les erreurs de concurrence sur `TaxObligations/Edit` ont deja ete traitees : ne pas reintroduire de mise a jour naive des responsibilities.
- Si une modification UI ne semble pas visible, verifier que le serveur local a bien ete redemarre.
- Les actions de workflow doivent toujours respecter le role et l'utilisateur attendu.
- Apres rejet d'une approbation, les anciennes approbations ne doivent pas bloquer un nouveau cycle.
- Les sous-menus Payment doivent rester sur les routes `/Payments/...`.
- Ne pas seulement cacher les boutons : toute action critique doit etre protegee cote serveur.

## Referentiel fiscal consolide et versionne

Ajout du catalogue consolide version `2026.03.02` :

- code canonique nullable et index unique filtre sur `TaxObligation` ;
- versions legales et rattachement nullable depuis `TaxDeclaration` ;
- alias, references de source, autorites, taux, references legales, penalites,
  processus, documents requis, conflits et allocations ;
- catalogue C# embarque dans
  `src\EthanTcm.Application\TaxCatalog\ConsolidatedTaxCatalog.cs` ;
- synchronisation transactionnelle et idempotente via
  `ITaxCatalogSynchronizationService` ;
- commande explicite :
  `dotnet run --project src\EthanTcm.Web -- sync-tax-catalog --dry-run`.

La migration additive `AddVersionedTaxCatalog` et la synchronisation reelle ont
ete appliquees a la base locale le 29 juin 2026. Etat verifie apres execution :
56 obligations avec code canonique (37 actives et 19 inactives), 56 versions,
56 alias, 110 references de source, 504 documents requis, 7 conflits et
5 regles d'allocation. Les 452 declarations existantes ont ete conservees et
sont toutes rattachees a une version fiscale. Aucun code canonique n'est
duplique. Un dry-run post-synchronisation a confirme l'idempotence : zero
creation, zero enrichissement, zero nouvelle version, zero nouvel alias,
zero nouvelle reference et zero conflit.

## Reformes fiscales saisies par les utilisateurs

Les utilisateurs disposant de `ManageTaxObligations` peuvent creer une nouvelle
version depuis la fiche d'une taxe avec le bouton `Nouvelle version fiscale`.
Le formulaire `/TaxObligations/CreateVersion/{id}` permet de renseigner la date
d'effet, l'autorite, les echeances de declaration et de paiement, le cycle
metier, les taux et bases taxables, les references legales, les penalites, le
processus, le statut de validation et le motif de la reforme.

La creation :

- exige une date posterieure a la derniere version ;
- cloture l'ancienne version la veille de la nouvelle date d'effet ;
- conserve toutes les anciennes versions et declarations ;
- met a jour le statut de revue affiche sur la taxe ;
- journalise l'action `CreateVersion` dans le module `Tax Referential` ;
- reste compatible avec la synchronisation : une resynchronisation du
  catalogue ne remplace pas une reforme manuelle plus recente.

Tests dedies :
`tests/EthanTcm.Tests/TaxObligationVersionManagementTests.cs`.

## Optimisations de performance

Optimisations appliquees le 30 juin 2026 :

- pagination, recherche et filtre de statut executes par SQL sur
  `/TaxDeclarations` au lieu de charger 500 lignes dans le navigateur ;
- projection SQL legere du dashboard sans chargement complet des collections de
  documents, paiements et responsables ;
- cache dashboard de 20 secondes, segmente par utilisateur, roles et vue ;
- compression Brotli/Gzip des reponses ;
- logs des commandes EF Core limites au niveau `Warning` ;
- seed initial retire du demarrage normal et disponible par la commande
  `seed-initial-tax-obligations` ;
- migration `AddPerformanceIndexes` appliquee a la base locale avec les index
  `AssignedToUserId/Status/DueDate` et
  `TaxDeclarationId/DocumentType/IsDeleted/UploadedAt`.

Mesures locales a chaud avant/apres :

- dashboard : 1 144 ms -> 142 ms ;
- liste des declarations : 2 747 ms -> 126 ms ;
- fiche declaration : 344 ms -> 173 ms ;
- referentiel fiscal : 398 ms -> 170 ms.

La pagination serveur a ete verifiee avec exactement 10 lignes rendues par page,
et la compression HTTP repond avec `Content-Encoding: gzip`.

## Dashboard decisionnel et modales KPI

Le dashboard a ete restructure en trois groupes :

- priorites operationnelles ;
- conformite et exposition ;
- charge a venir.

Il affiche aussi le perimetre actif, le nombre exact de declarations ouvertes,
le taux de couverture des preuves et l'heure d'actualisation. Les anciens
clignotements permanents ont ete retires.

Les 11 KPI ouvrent une modale Bootstrap reutilisable. Le detail est obtenu via
`/Home/KpiDetails`, respecte le perimetre et les roles de l'utilisateur, limite
les pages a 10-50 lignes et fournit la definition metier du calcul. Les lignes
renvoient vers la declaration ou l'obligation concernee. Les donnees du
dashboard et des modales sont cachees 20 secondes par utilisateur, roles, vue,
KPI et page.

Le KPI complexe `PenaltyRisk` reutilise le snapshot leger du dashboard afin
d'eviter une requete SQL avec de nombreux `OR`. Apres chargement du dashboard,
la modale a ete mesuree a 369 ms au premier affichage puis 157 ms en cache.
Tests dedies :
`tests/EthanTcm.Tests/DashboardDecisionSupportTests.cs`.

Avant production : sauvegarde SQL avec `CHECKSUM`, `RESTORE VERIFYONLY`,
validation du rapport dry-run par la fiscalite, puis execution explicite.

## Conseils pour le prochain Codex

1. Lire ce fichier, puis `README.md`, puis `DEPLOYMENT.md`.
2. Lire les controllers/services concernes avant de modifier.
3. Garder les changements limites au besoin utilisateur.
4. Utiliser `rg` pour explorer.
5. Utiliser `apply_patch` pour les edits manuels.
6. Builder et tester avant de conclure.
7. Repondre en francais a l'utilisateur.
