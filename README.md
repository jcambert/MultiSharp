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

## Avancement

| Phase | Contenu | Statut |
|-------|---------|--------|
| P0 | Fondations VSIX + Roslyn | ✅ Terminé |
| P1 | Analyseurs + Quick Fixes | ✅ Terminé |
| P2 | Refactorings | ✅ Terminé |
| P3 | Génération de code | ✅ Terminé |
| P4 | Navigation & Recherche | ✅ Terminé |
| P5 | Formatage & Style | 🔲 Backlog |
| P6 | Fonctionnalités avancées | 🔲 Backlog |

---

## Licence

MIT
