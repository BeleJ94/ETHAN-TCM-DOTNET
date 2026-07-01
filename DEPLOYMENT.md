# ETHAN TCM - Guide de deploiement IIS Production

Ce document decrit la procedure de publication de ETHAN TCM sur IIS avec SQL Server, Windows Authentication / Active Directory, stockage documentaire securise, SMTP, sauvegarde et migrations de base de donnees.

## 1. Architecture cible

```text
Utilisateurs AD
  -> Navigateur intranet HTTPS
  -> IIS / ASP.NET Core Hosting Bundle
  -> ETHAN TCM Web
  -> SQL Server ou SQL Server Always On / Failover Cluster
  -> Dossier securise documents
  -> SMTP entreprise
```

Composants applicatifs :

- `EthanTcm.Web` : application ASP.NET Core MVC publiee dans IIS.
- `EthanTcm.Infrastructure` : acces SQL Server via EF Core.
- `EthanTcm.Jobs` : worker de rappels et notifications, a executer separement comme Windows Service si les jobs ne sont pas heberges dans le site web.

## 2. Prerequis serveur

- Windows Server supporte par IIS.
- IIS avec ASP.NET Core Hosting Bundle compatible `.NET 10`.
- Module `AspNetCoreModuleV2` installe.
- Windows Authentication activee dans IIS.
- Anonymous Authentication desactivee pour le site ETHAN TCM.
- Acces reseau au SQL Server ou au listener SQL Server Cluster.
- Acces reseau au serveur SMTP.
- Certificat TLS installe dans IIS.
- Compte de pool applicatif dedie, par exemple `DOMAIN\svc-ethan-tcm-web`.

## 3. Publication applicative

Depuis la racine de la solution :

```powershell
dotnet restore EthanTcm.sln
dotnet test EthanTcm.sln
dotnet publish src\EthanTcm.Web\EthanTcm.Web.csproj -c Release -o .\artifacts\publish\EthanTcm.Web
```

Copier ensuite le contenu de `.\artifacts\publish\EthanTcm.Web` vers le repertoire IIS, par exemple :

```text
D:\Sites\ETHAN-TCM
```

Le fichier `src\EthanTcm.Web\web.config` est fourni pour IIS. Il configure :

- le handler `AspNetCoreModuleV2` ;
- l'environnement `ASPNETCORE_ENVIRONMENT=Production` ;
- le hosting model `inprocess` ;
- des en-tetes de securite ;
- le filtrage des requetes ;
- la limite d'upload a 10 MB ;
- le blocage des segments `App_Data` et `Documents`.

## 4. Configuration IIS

Creer un site IIS :

- Site name : `ETHAN TCM`
- Physical path : `D:\Sites\ETHAN-TCM`
- Binding : `https://ethan-tcm.your-domain.example`
- Application pool : `ETHAN-TCM-AppPool`
- .NET CLR version : `No Managed Code`
- Pipeline mode : `Integrated`
- Identity : compte dedie, par exemple `DOMAIN\svc-ethan-tcm-web`

Authentification IIS :

- `Windows Authentication` : Enabled
- `Anonymous Authentication` : Disabled
- Providers Windows Authentication : privilegier `Negotiate`, puis `NTLM` si necessaire.

Verifier aussi que le hostname configure dans IIS correspond a `AllowedHosts` dans `appsettings.Production.json`.

## 5. appsettings.Production.json

Le fichier de production se trouve ici :

```text
src\EthanTcm.Web\appsettings.Production.json
```

Exemple attendu :

```json
{
  "ConnectionStrings": {
    "EthanTcmDatabase": "Server=YOUR-SQL-CLUSTER-LISTENER;Database=EthanTcm;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Application Name=ETHAN TCM Web"
  },
  "Authentication": {
    "Mode": "WindowsAuth"
  },
  "DocumentStorage": {
    "RootPath": "D:\\ETHAN-TCM\\Documents",
    "MaxFileSizeBytes": 10485760
  },
  "Notifications": {
    "DryRun": false
  },
  "AllowedHosts": "ethan-tcm.your-domain.example"
}
```

Ne pas stocker de secrets SMTP ou SQL dans le depot source pour une vraie production. Utiliser de preference :

- variables d'environnement machine ;
- coffre de secrets d'entreprise ;
- configuration IIS chiffree ;
- compte de service avec Integrated Security pour SQL Server.

## 6. SQL Server production

Creer la base :

```sql
CREATE DATABASE EthanTcm;
```

Creer le login Windows du pool applicatif :

```sql
CREATE LOGIN [DOMAIN\svc-ethan-tcm-web] FROM WINDOWS;
USE [EthanTcm];
CREATE USER [DOMAIN\svc-ethan-tcm-web] FOR LOGIN [DOMAIN\svc-ethan-tcm-web];
ALTER ROLE db_datareader ADD MEMBER [DOMAIN\svc-ethan-tcm-web];
ALTER ROLE db_datawriter ADD MEMBER [DOMAIN\svc-ethan-tcm-web];
```

Pour appliquer les migrations en production, utiliser un compte de deploiement separe avec droits DDL temporaires, ou generer un script SQL valide par DBA.

## 7. SQL Server Cluster / Always On

La connection string de production doit cibler le listener du cluster, pas un noeud physique :

```text
Server=YOUR-SQL-CLUSTER-LISTENER;Database=EthanTcm;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Application Name=ETHAN TCM Web
```

Recommandations :

- utiliser le DNS/listener Always On ou Failover Cluster ;
- activer le chiffrement SQL (`Encrypt=True`) ;
- installer un certificat SQL Server valide pour eviter `TrustServerCertificate=True` ;
- tester un failover planifie avant mise en production ;
- verifier que le compte de pool IIS existe et a les droits sur tous les replicas necessaires.

## 8. Windows Authentication / Active Directory

L'application est configuree pour `WindowsAuth` en production. Les groupes AD sont mappes vers les roles applicatifs dans `Authentication:ActiveDirectory:GroupRoleMappings`.

Roles applicatifs :

- `Administrator`
- `TaxManager`
- `Preparer`
- `Approver`
- `FinanceManager`
- `Auditor`
- `ReadOnly`

Exemple :

```json
"GroupRoleMappings": {
  "DOMAIN\\ETHAN-TCM-Administrators": [ "Administrator" ],
  "DOMAIN\\ETHAN-TCM-TaxManagers": [ "TaxManager" ],
  "DOMAIN\\ETHAN-TCM-Preparers": [ "Preparer" ],
  "DOMAIN\\ETHAN-TCM-Approvers": [ "Approver" ],
  "DOMAIN\\ETHAN-TCM-FinanceManagers": [ "FinanceManager" ],
  "DOMAIN\\ETHAN-TCM-Auditors": [ "Auditor" ],
  "DOMAIN\\ETHAN-TCM-Readers": [ "ReadOnly" ]
}
```

Si un utilisateur authentifie n'appartient a aucun groupe mappe, le role par defaut doit rester `ReadOnly`.

## 9. Dossier securise des documents

Les documents ne doivent pas etre places dans `wwwroot`.

Chemin recommande :

```text
D:\ETHAN-TCM\Documents
```

Droits NTFS recommandes :

- `DOMAIN\svc-ethan-tcm-web` : Modify
- Administrateurs serveur : Full Control
- Utilisateurs finaux : aucun acces direct

L'application valide les extensions, types MIME, tailles et droits d'acces avant de servir un fichier. Le telechargement doit toujours passer par l'action MVC autorisee, jamais par un chemin statique IIS.

## 10. SMTP production

Configurer `Notifications:Smtp` :

```json
"Notifications": {
  "DryRun": false,
  "DailyCron": "0 0 7 ? * *",
  "Smtp": {
    "Host": "smtp.your-domain.example",
    "Port": 587,
    "EnableSsl": true,
    "UserName": "smtp-user",
    "Password": "smtp-password",
    "From": "no-reply@your-domain.example"
  }
}
```

Verifier :

- port SMTP autorise depuis le serveur IIS ou le serveur jobs ;
- TLS active si requis ;
- compte SMTP autorise a emettre avec l'adresse `From` ;
- `DryRun=false` uniquement apres validation en recette.

## 11. Worker de notifications

Si les jobs sont executes separement :

```powershell
dotnet publish src\EthanTcm.Jobs\EthanTcm.Jobs.csproj -c Release -o .\artifacts\publish\EthanTcm.Jobs
```

Installer le worker comme Windows Service avec un compte dedie ayant :

- acces SQL Server ;
- acces SMTP ;
- acces aux memes settings de production ;
- droits minimaux necessaires.

## 12. Migrations database

Option recommandee en production : generer un script SQL idempotent et le faire valider par DBA.

```powershell
dotnet ef migrations script --idempotent --project src\EthanTcm.Infrastructure --startup-project src\EthanTcm.Web --context EthanTcmDbContext -o .\artifacts\db\EthanTcm-migrations.sql
```

Application directe possible en environnement controle :

```powershell
dotnet ef database update --project src\EthanTcm.Infrastructure --startup-project src\EthanTcm.Web --context EthanTcmDbContext
```

Avant toute migration :

- sauvegarder la base ;
- verifier la migration sur une copie de production ;
- conserver le script applique ;
- documenter l'heure, l'operateur et la version applicative.

### Synchronisation du referentiel fiscal consolide

La migration `AddVersionedTaxCatalog` est additive : `CanonicalCode` et
`TaxObligationVersionId` restent nullables pendant le rapprochement. Avant toute
execution en production, realiser et verifier une sauvegarde complete :

```sql
BACKUP DATABASE [EthanTcmDatabase]
TO DISK = N'\\backup-share\ETHAN-TCM\BeforeTaxCatalogSync_20260302.bak'
WITH COPY_ONLY, COMPRESSION, CHECKSUM, INIT;

RESTORE VERIFYONLY
FROM DISK = N'\\backup-share\ETHAN-TCM\BeforeTaxCatalogSync_20260302.bak'
WITH CHECKSUM;
```

Ordre obligatoire :

1. appliquer uniquement la migration additive ;
2. effectuer la sauvegarde et `RESTORE VERIFYONLY` ;
3. lancer `dotnet EthanTcm.Web.dll sync-tax-catalog --dry-run` et archiver le rapport ;
4. faire valider les correspondances et conflits par la fiscalite ;
5. lancer explicitement `dotnet EthanTcm.Web.dll sync-tax-catalog` ;
6. controler les GUID, doublons, versions et relations des declarations ;
7. creer dans une migration ulterieure les contraintes `NOT NULL` finales.

La synchronisation n'est jamais executee automatiquement au demarrage IIS.

## 13. Backup / Restore

Sauvegardes minimales :

- base SQL Server `EthanTcm` ;
- dossier documentaire `D:\ETHAN-TCM\Documents` ;
- fichiers de configuration de production ;
- artefact applicatif publie.

Exemple SQL :

```sql
BACKUP DATABASE [EthanTcm]
TO DISK = N'\\backup-share\ETHAN-TCM\EthanTcm_FULL.bak'
WITH COMPRESSION, CHECKSUM, INIT;
```

Restore de base :

```sql
RESTORE DATABASE [EthanTcm]
FROM DISK = N'\\backup-share\ETHAN-TCM\EthanTcm_FULL.bak'
WITH REPLACE, RECOVERY;
```

Apres restauration :

- restaurer le dossier documentaire correspondant au meme point de sauvegarde ;
- verifier les droits NTFS ;
- verifier la connection string ;
- lancer un test de connexion applicatif ;
- ouvrir le dashboard, les declarations et un document existant.

## 14. Validation post-deploiement

Verifier :

- `https://ethan-tcm.your-domain.example` repond en HTTPS ;
- l'utilisateur AD est reconnu ;
- les roles sont correctement resolus ;
- un utilisateur `ReadOnly` ne peut pas executer d'action POST ;
- un `Preparer` ne peut agir que sur ses declarations assignees ;
- un `FinanceManager` peut uploader une preuve de paiement ;
- un `Auditor` peut consulter AuditLog mais ne peut pas modifier ;
- l'upload refuse une extension interdite ;
- le telechargement direct par chemin IIS est impossible ;
- les notifications SMTP fonctionnent en recette puis en production ;
- les logs d'erreur remontent dans la supervision.

## 15. Checklist de deploiement

- [ ] Artefact Release publie avec `dotnet publish`.
- [ ] `web.config` present dans le repertoire IIS.
- [ ] Hosting Bundle ASP.NET Core installe.
- [ ] Site IIS cree avec HTTPS.
- [ ] Windows Authentication activee.
- [ ] Anonymous Authentication desactivee.
- [ ] App pool en `No Managed Code`.
- [ ] Identite du pool configuree.
- [ ] `appsettings.Production.json` renseigne.
- [ ] `AllowedHosts` remplace par le hostname reel.
- [ ] Connection string cible le listener SQL cluster.
- [ ] Certificat SQL valide et `Encrypt=True`.
- [ ] Droits SQL du compte applicatif verifies.
- [ ] Migrations appliquees ou script DBA execute.
- [ ] Backup SQL realise avant migration.
- [ ] Dossier documents cree hors `wwwroot`.
- [ ] Droits NTFS du dossier documents verifies.
- [ ] SMTP configure et teste.
- [ ] Worker jobs publie et installe si applicable.
- [ ] Tests de fumee effectues.
- [ ] Plan de rollback valide.

## 16. Rollback

En cas d'echec :

1. Arreter le site IIS.
2. Restaurer l'artefact applicatif precedent.
3. Restaurer la configuration precedente.
4. Si une migration a ete appliquee et n'est pas compatible, restaurer la sauvegarde SQL.
5. Restaurer le dossier documentaire au meme point temporel si necessaire.
6. Redemarrer IIS.
7. Verifier l'authentification, le dashboard et l'acces documents.
