# MultiSharp

Extension Visual Studio 2022 d'analyse, correction et refactoring de code C#, inspirée de ReSharper.

> **Zéro dépendance NuGet dans vos projets** — tout s'exécute dans le process Visual Studio.

---

## Fonctionnalités prévues

| Catégorie | Exemples |
|-----------|---------|
| **Analyse de code** | Variables inutilisées, null reference, code smells, using inutilisés |
| **Quick Fixes** | Correction automatique via Alt+Enter |
| **Refactorings** | Rename, Extract Method/Interface, Inline, Change Signature… |
| **Génération de code** | Constructeurs, Equals/GetHashCode, ToString, propriétés |
| **Navigation** | Go to Symbol, Find Usages, Navigate to Implementation |
| **Formatage & Style** | EditorConfig, naming conventions, modernisation C# |

Voir [ROADMAP.md](ROADMAP.md) pour le backlog complet avec les 33 user stories.

---

## Architecture

```
MultiSharp.Core      (net472 + net8)   — logique pure, zéro dépendance VS
MultiSharp.VSIX      (net472)          — intégration Visual Studio SDK
MultiSharp.Tests     (net472 + net8)   — tests xUnit
```

- **Roslyn** (`Microsoft.CodeAnalysis.CSharp`) pour l'analyse syntaxique et sémantique
- **VS SDK 17.x** + MEF pour l'intégration dans Visual Studio
- **Aucun package NuGet** n'est ajouté aux projets analysés

---

## Prérequis

- Visual Studio 2022 (17.x)
- .NET SDK 8.0+
- .NET Framework 4.7.2

---

## Build

```bash
dotnet restore
dotnet build MultiSharp.sln
```

## Tests

```bash
dotnet test tests/MultiSharp.Tests/MultiSharp.Tests.csproj
```

---

## Déboguer avec l'instance expérimentale de Visual Studio

L'instance expérimentale est une installation VS isolée dédiée aux développeurs d'extensions. Elle charge le `.vsix` sans toucher à votre VS principal.

### 1. Configurer le projet de démarrage

Dans Visual Studio, faites un clic droit sur **MultiSharp.VSIX** → **Définir comme projet de démarrage**.

Vérifiez ensuite les propriétés de débogage du projet VSIX :

- Clic droit sur **MultiSharp.VSIX** → **Propriétés** → onglet **Déboguer**
- **Action de démarrage** : `Démarrer le programme externe`
- **Programme** :
  ```
  C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe
  ```
  *(adapter selon votre édition : Community / Professional / Enterprise)*
- **Arguments de la ligne de commande** :
  ```
  /rootsuffix Exp
  ```

### 2. Lancer le débogage

Appuyez sur **F5**. Visual Studio :

1. Compile le projet VSIX
2. Installe l'extension dans l'instance expérimentale (`%LOCALAPPDATA%\Microsoft\VisualStudio\17.0_<id>Exp`)
3. Lance une nouvelle fenêtre VS avec le titre **"Microsoft Visual Studio (Experimental Instance)"**

Vous pouvez placer des points d'arrêt dans `MultiSharp.Core` ou `MultiSharp.VSIX` — ils s'activeront dès que VS exécutera le code de l'extension.

### 3. Réinitialiser l'instance expérimentale

Si l'instance expérimentale est dans un état incohérent :

```bash
# Via le menu Démarrer → "Reset the Visual Studio 2022 Experimental Instance"
# Ou en ligne de commande :
"C:\Program Files\Microsoft Visual Studio\2022\Professional\VSSDK\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe" /Reset /VSInstance=17.0 /RootSuffix=Exp
```

### 4. Afficher les traces de débogage

Pour voir les messages `Debug.WriteLine` de l'extension dans la fenêtre **Sortie** :

```csharp
// Dans votre code VSIX
System.Diagnostics.Debug.WriteLine("[MultiSharp] Message de diagnostic");
```

Ouvrez **Affichage → Sortie** et sélectionnez **Débogage** dans la liste déroulante.

### 5. Journal d'activité VS

VS écrit un journal XML utile pour diagnostiquer les erreurs MEF ou de chargement d'extension :

```
%APPDATA%\Microsoft\VisualStudio\17.0_<id>Exp\ActivityLog.xml
```

Ouvrir ce fichier dans un navigateur affiche les erreurs de chargement de packages et d'extensions.

### 6. Inspecter les erreurs MEF

Si un `[Export]` ou `[Import]` MEF ne fonctionne pas :

**Outils → Options → Environnement → Général** → cocher **"Afficher les erreurs MEF dans le journal d'activité"**

Ou via le menu **Extensions → MultiSharp** → vérifier que les commandes apparaissent.

---

## Avancement

| Phase | Contenu | Statut |
|-------|---------|--------|
| P0 | Fondations VSIX + Roslyn | ✅ Terminé |
| P1 | Analyseurs + Quick Fixes | ✅ Terminé |
| P2 | Refactorings | ✅ Terminé |
| P3 | Génération de code | ✅ Terminé |
| P4 | Navigation & Recherche | ✅ Terminé |
| P5 | Formatage & Style | ✅ Terminé |
| P6 | Fonctionnalités avancées | ✅ Terminé |

---

## Licence

MIT
