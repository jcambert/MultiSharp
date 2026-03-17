# MultiSharp — Roadmap

> Extension Visual Studio (VSIX) d'analyse, correction et refactoring de code C#
> Inspirée de ReSharper — **zéro dépendance NuGet dans les projets cibles**
> Architecture : Roslyn (Microsoft.CodeAnalysis) embarqué dans l'extension VSIX

---

## Architecture cible

```
MultiSharp.VSIX
├── Analyseurs Roslyn          (DiagnosticAnalyzer)
├── Code Fixes                 (CodeFixProvider)
├── Refactorings               (CodeRefactoringProvider)
├── Navigation & Search        (IVsTextManager, SymbolFinder)
├── Code Generation            (SyntaxGenerator)
├── Formatting                 (Formatter, SyntaxNormalizer)
└── UI (Tool Windows, Options) (ToolWindowPane, DialogPage)
```

Tout s'exécute dans le process Visual Studio. Les projets analysés n'ont aucune dépendance ajoutée.

---

## Phases & Milestones

| Phase | Nom | Priorité |
|-------|-----|----------|
| **P0** | Fondations VSIX + Roslyn | Critique |
| **P1** | Analyse de code & Quick Fixes | Haute |
| **P2** | Refactorings essentiels | Haute |
| **P3** | Génération de code | Moyenne |
| **P4** | Navigation & Recherche | Moyenne |
| **P5** | Formatage & Style | Basse |
| **P6** | Fonctionnalités avancées | Basse |

---

## Phase 0 — Fondations VSIX + Roslyn

**Objectif :** Infrastructure de base, extension installable dans VS 2022.

### US-001 — Squelette VSIX
**En tant que** développeur sur le projet MultiSharp,
**je veux** avoir un projet VSIX compilable et installable dans VS 2022,
**afin de** pouvoir développer les fonctionnalités sur une base saine.

**Critères d'acceptance :**
- [ ] Projet `.vsixmanifest` configuré (VS 2022, C# workload)
- [ ] Extension chargeable via F5 (Experimental Instance)
- [ ] Entry point `AsyncPackage` initialisé
- [ ] CI build qui produit un `.vsix` signable
- [ ] `.gitignore` et structure de solution définis

---

### US-002 — Intégration Roslyn workspace
**En tant que** développeur MultiSharp,
**je veux** accéder au `Workspace` Roslyn de la solution ouverte dans VS,
**afin de** pouvoir analyser n'importe quel projet/fichier sans y ajouter de dépendance.

**Critères d'acceptance :**
- [ ] Accès à `VisualStudioWorkspace` via MEF
- [ ] Enumération des projets et documents de la solution
- [ ] Récupération du `SemanticModel` et `SyntaxTree` d'un document
- [ ] Invalidation du cache quand la solution change
- [ ] Aucun package NuGet ajouté aux projets analysés

---

### US-003 — Options & Configuration
**En tant qu'** utilisateur de MultiSharp,
**je veux** configurer l'extension via `Tools > Options > MultiSharp`,
**afin de** personnaliser les règles actives et leur sévérité.

**Critères d'acceptance :**
- [ ] Page Options `DialogPage` enregistrée
- [ ] Paramètres persistés entre sessions (VS settings store)
- [ ] Activation/désactivation de chaque catégorie de règle
- [ ] Sévérité configurable (Erreur / Avertissement / Suggestion / Info)
- [ ] Bouton "Restaurer les défauts"

---

### US-004 — Tool Window "MultiSharp Issues"
**En tant qu'** utilisateur de MultiSharp,
**je veux** voir tous les problèmes détectés dans une fenêtre dédiée,
**afin d'** avoir une vue globale sans polluer la liste d'erreurs VS.

**Critères d'acceptance :**
- [ ] Tool Window ancrable dans VS
- [ ] Liste filtrables (projet, fichier, catégorie, sévérité)
- [ ] Double-clic → navigation vers le code concerné
- [ ] Badge avec le nombre de problèmes total
- [ ] Rafraîchissement live à la modification du code

---

## Phase 1 — Analyse de code & Quick Fixes

**Objectif :** Détecter les problèmes courants et proposer des corrections automatiques (Alt+Enter).

### US-101 — Variables inutilisées
**En tant qu'** utilisateur,
**je veux** être averti des variables locales et paramètres non utilisés,
**afin de** nettoyer mon code et éviter la confusion.

**Critères d'acceptance :**
- [ ] Détection des variables locales non référencées
- [ ] Détection des paramètres de méthode non utilisés
- [ ] Quick Fix : supprimer la variable / préfixer par `_`
- [ ] Sévérité configurable (défaut : Suggestion)
- [ ] Respect des suppressions `#pragma warning`

---

### US-102 — Membres privés inutilisés
**En tant qu'** utilisateur,
**je veux** détecter les champs, méthodes et propriétés privés jamais utilisés,
**afin d'** éliminer le code mort.

**Critères d'acceptance :**
- [ ] Analyse inter-méthodes dans le fichier
- [ ] Détection des membres privés sans référence (hors réflexion)
- [ ] Quick Fix : supprimer le membre
- [ ] Exclusion des membres marqués `[UsedImplicitly]` ou équivalent
- [ ] Analyse solution-wide optionnelle (configurable)

---

### US-103 — Null reference analysis
**En tant qu'** utilisateur,
**je veux** être alerté des déréférencements potentiellement nuls,
**afin de** prévenir les `NullReferenceException` en production.

**Critères d'acceptance :**
- [ ] Détection des accès à membres sans vérification null
- [ ] Prise en compte des annotations `[NotNull]`, `[CanBeNull]`, nullable reference types C# 8+
- [ ] Quick Fix : ajouter vérification null, utiliser `?.`, utiliser `??`
- [ ] Pas de faux positifs sur les types non-nullables certifiés

---

### US-104 — Simplifications d'expressions
**En tant qu'** utilisateur,
**je veux** des suggestions pour simplifier les expressions redondantes,
**afin d'** obtenir un code plus lisible.

**Critères d'acceptance :**
- [ ] `if (x == true)` → `if (x)`
- [ ] `x != null ? x.ToString() : null` → `x?.ToString()`
- [ ] Concatenations string → interpolation
- [ ] Cast redondant détecté
- [ ] Each fix applicable via Alt+Enter

---

### US-105 — Détection des using manquants / inutiles
**En tant qu'** utilisateur,
**je veux** voir les `using` non utilisés et les `using` manquants pour les types référencés,
**afin de** garder des fichiers propres.

**Critères d'acceptance :**
- [ ] Soulignement des `using` inutilisés
- [ ] Quick Fix : supprimer le `using` inutilisé
- [ ] Quick Fix : supprimer tous les `using` inutilisés du fichier
- [ ] Suggestion d'ajout de `using` pour un type non résolu
- [ ] Tri automatique des `using` après nettoyage (optionnel, configurable)

---

### US-106 — Code smells SOLID / patterns
**En tant qu'** utilisateur,
**je veux** être alerté des violations courantes (méthodes trop longues, classes trop grandes, etc.),
**afin de** maintenir un design maintenable.

**Critères d'acceptance :**
- [ ] Méthode > N lignes (N configurable, défaut 50)
- [ ] Classe > N membres (N configurable, défaut 20)
- [ ] Paramètres trop nombreux (> N, défaut 5)
- [ ] Nesting trop profond (> N niveaux, défaut 4)
- [ ] Complexité cyclomatique configurable
- [ ] Sévérité configurable par règle

---

## Phase 2 — Refactorings essentiels

**Objectif :** Transformations de code sûres, préservant le comportement.

### US-201 — Rename (renommage sûr)
**En tant qu'** utilisateur,
**je veux** renommer un symbole (variable, méthode, classe, namespace, fichier) partout dans la solution,
**afin de** maintenir la cohérence sans casser le build.

**Critères d'acceptance :**
- [ ] Déclenchement via Alt+Enter ou raccourci dédié
- [ ] Prévisualisation des changements avant application
- [ ] Rename dans commentaires et strings (optionnel, confirmation)
- [ ] Rename du fichier si le type principal est renommé
- [ ] Undo/Redo en une seule action VS

---

### US-202 — Extract Method
**En tant qu'** utilisateur,
**je veux** extraire un bloc de code sélectionné en une nouvelle méthode,
**afin de** réduire la complexité et améliorer la réutilisabilité.

**Critères d'acceptance :**
- [ ] Sélection de code → Alt+Enter → "Extract Method"
- [ ] Inférence automatique des paramètres et du type de retour
- [ ] Nom de méthode suggéré, éditable inline
- [ ] Gestion des variables locales capturées
- [ ] Prévisualisation avant application

---

### US-203 — Extract Interface
**En tant qu'** utilisateur,
**je veux** extraire les membres publics d'une classe en une interface,
**afin de** faciliter l'injection de dépendances et les tests.

**Critères d'acceptance :**
- [ ] Sélection des membres à inclure dans l'interface
- [ ] Création du fichier `IXxx.cs` dans le même dossier
- [ ] Implémentation de l'interface ajoutée à la classe source
- [ ] Namespace correct dans le nouveau fichier
- [ ] Option : remplacer les usages par l'interface

---

### US-204 — Inline Method / Inline Variable
**En tant qu'** utilisateur,
**je veux** inliner une méthode ou variable là où elle est utilisée,
**afin de** simplifier le code quand l'abstraction n'apporte plus de valeur.

**Critères d'acceptance :**
- [ ] Inline variable : remplace toutes les références par l'expression initiale
- [ ] Inline method : substitue l'appel par le corps de la méthode
- [ ] Vérification : pas d'effets de bord multiples
- [ ] Suppression de la déclaration originale après inline

---

### US-205 — Introduce Variable / Field / Parameter
**En tant qu'** utilisateur,
**je veux** extraire une expression en variable locale, champ ou paramètre,
**afin d'** éliminer les répétitions et nommer les intentions.

**Critères d'acceptance :**
- [ ] Introduce Variable : Alt+Enter sur une expression
- [ ] Introduce Field : avec choix readonly/static
- [ ] Introduce Parameter : ajout au signature de la méthode
- [ ] Remplacement de toutes les occurrences identiques (optionnel)
- [ ] Nom suggéré basé sur le type et le contexte

---

### US-206 — Change Signature
**En tant qu'** utilisateur,
**je veux** modifier la signature d'une méthode (ordre, ajout, suppression de paramètres),
**afin de** refactorer les interfaces sans casser les appelants.

**Critères d'acceptance :**
- [ ] Dialog de modification de signature
- [ ] Mise à jour de tous les sites d'appel dans la solution
- [ ] Valeur par défaut pour les nouveaux paramètres
- [ ] Prévisualisation des impacts
- [ ] Undo atomique

---

### US-207 — Move Type to File
**En tant qu'** utilisateur,
**je veux** déplacer un type vers son propre fichier,
**afin de** respecter la convention "un type = un fichier".

**Critères d'acceptance :**
- [ ] Détection automatique des fichiers avec plusieurs types
- [ ] Suggestion / Quick Fix "Move to ItsOwnFile"
- [ ] Création du nouveau fichier avec bon namespace et `using`
- [ ] Suppression du type de l'ancien fichier

---

### US-208 — Convert to Extension Method / Static
**En tant qu'** utilisateur,
**je veux** convertir une méthode d'instance en méthode d'extension ou statique,
**afin de** réorganiser la logique.

**Critères d'acceptance :**
- [ ] Méthode d'instance sans accès à `this` → suggérer `static`
- [ ] Méthode statique avec premier paramètre d'un type → suggérer extension method
- [ ] Mise à jour des sites d'appel

---

## Phase 3 — Génération de code

**Objectif :** Générer automatiquement le boilerplate courant.

### US-301 — Implémenter les membres d'interface / classe abstraite
**En tant qu'** utilisateur,
**je veux** implémenter automatiquement les membres requis d'une interface ou classe abstraite,
**afin de** ne pas écrire le boilerplate manuellement.

**Critères d'acceptance :**
- [ ] Alt+Enter sur la classe → "Implement missing members"
- [ ] Génération avec `throw new NotImplementedException()` par défaut
- [ ] Option : générer des stubs avec corps vide ou logique minimale
- [ ] Support des implémentations explicites

---

### US-302 — Générer constructeur
**En tant qu'** utilisateur,
**je veux** générer un constructeur à partir des champs/propriétés de la classe,
**afin d'** éviter l'écriture répétitive.

**Critères d'acceptance :**
- [ ] Sélection des membres à inclure dans le constructeur
- [ ] Génération des assignations `this.field = field`
- [ ] Détection et appel du constructeur de base si héritage
- [ ] Support des primary constructors C# 12 (optionnel)

---

### US-303 — Générer Equals / GetHashCode
**En tant qu'** utilisateur,
**je veux** générer `Equals` et `GetHashCode` basés sur les membres choisis,
**afin d'** implémenter l'égalité structurelle correctement.

**Critères d'acceptance :**
- [ ] Dialog de sélection des membres participants
- [ ] Pattern null-safe généré
- [ ] `operator ==` et `operator !=` optionnels
- [ ] Compatible avec `IEquatable<T>`
- [ ] Mise à jour si les membres changent (re-génération)

---

### US-304 — Générer ToString
**En tant qu'** utilisateur,
**je veux** générer un `ToString()` lisible basé sur les membres sélectionnés,
**afin d'** améliorer le débogage.

**Critères d'acceptance :**
- [ ] Sélection des membres à inclure
- [ ] Format : `"ClassName { Prop1 = val, Prop2 = val }"`
- [ ] Utilisation de string interpolation
- [ ] Régénération possible si classe modifiée

---

### US-305 — Générer propriétés depuis champs
**En tant qu'** utilisateur,
**je veux** convertir des champs privés en propriétés encapsulées,
**afin de** respecter les bonnes pratiques OOP.

**Critères d'acceptance :**
- [ ] Champ privé → propriété avec getter/setter
- [ ] Option : getter seul (readonly property)
- [ ] Mise à jour des références dans la classe
- [ ] Support des champs `readonly` → propriété get-only

---

## Phase 4 — Navigation & Recherche

**Objectif :** Se déplacer dans le code rapidement.

### US-401 — Go to Symbol (solution-wide)
**En tant qu'** utilisateur,
**je veux** trouver n'importe quel symbole (type, méthode, propriété) par son nom,
**afin de** naviguer rapidement sans connaître la structure du projet.

**Critères d'acceptance :**
- [ ] Raccourci configurable (défaut : Ctrl+T)
- [ ] Recherche fuzzy avec score de pertinence
- [ ] Filtres : Types / Membres / Fichiers
- [ ] Navigation directe à l'emplacement du symbole
- [ ] Historique des symboles récents

---

### US-402 — Find Usages
**En tant qu'** utilisateur,
**je veux** trouver toutes les utilisations d'un symbole dans la solution,
**afin de** comprendre l'impact d'une modification.

**Critères d'acceptance :**
- [ ] Résultats groupés par fichier/projet
- [ ] Distinction : lecture / écriture / appel
- [ ] Filtres : portée (fichier, projet, solution)
- [ ] Aperçu du contexte sans quitter le fichier actuel
- [ ] Export des résultats

---

### US-403 — Navigate to Implementation
**En tant qu'** utilisateur,
**je veux** naviguer vers l'implémentation concrète d'une interface ou méthode virtuelle,
**afin de** comprendre le comportement réel.

**Critères d'acceptance :**
- [ ] Ctrl+Click ou raccourci sur un appel d'interface
- [ ] Liste des implémentations si plusieurs
- [ ] Hiérarchie de classes navigable
- [ ] Support des méthodes d'extension

---

### US-404 — Structural Search & Replace
**En tant qu'** utilisateur,
**je veux** rechercher des patterns de code structurels (pas juste du texte),
**afin de** trouver et remplacer des patterns sémantiques à l'échelle de la solution.

**Critères d'acceptance :**
- [ ] Syntaxe de pattern type ReSharper SSR
- [ ] Variables de pattern `$x$`, `$method$`
- [ ] Aperçu des correspondances
- [ ] Remplacement avec substitution de variables
- [ ] Sauvegarde des patterns personnalisés

---

## Phase 5 — Formatage & Style

**Objectif :** Uniformiser le style de code automatiquement.

### US-501 — Formatage automatique à la sauvegarde
**En tant qu'** utilisateur,
**je veux** que mon code soit formaté automatiquement quand je sauvegarde,
**afin de** ne pas perdre de temps sur l'indentation et le style.

**Critères d'acceptance :**
- [ ] Intégration avec les règles EditorConfig du repo
- [ ] Configurable : activer/désactiver par projet
- [ ] Formatage du document entier ou sélection uniquement
- [ ] Respect des overrides locaux (commentaires `// @formatter:off`)

---

### US-502 — Naming conventions
**En tant qu'** utilisateur,
**je veux** être alerté quand un symbole ne respecte pas les conventions de nommage configurées,
**afin de** maintenir un code cohérent.

**Critères d'acceptance :**
- [ ] Règles configurables : PascalCase, camelCase, `_camelCase`, `I`-prefix interfaces
- [ ] Quick Fix : renommer selon la convention
- [ ] Import/Export des règles de nommage (JSON)
- [ ] Intégration EditorConfig (`dotnet_naming_rule`)

---

### US-503 — Code style enforcement
**En tant qu'** utilisateur,
**je veux** des suggestions de style (var vs type explicite, expression bodies, pattern matching),
**afin de** moderniser le code vers les idiomes C# récents.

**Critères d'acceptance :**
- [ ] `int x = ...` → `var x = ...` (configurable)
- [ ] Méthode une ligne → expression body `=>`
- [ ] `if (x is T t)` préféré au cast
- [ ] `switch` expression vs `switch` statement
- [ ] Chaque règle configurable indépendamment

---

## Phase 6 — Fonctionnalités avancées

### US-601 — Analyse solution-wide (background)
**En tant qu'** utilisateur,
**je veux** lancer une analyse complète de la solution en tâche de fond,
**afin de** voir tous les problèmes même dans les fichiers non ouverts.

**Critères d'acceptance :**
- [ ] Barre de progression dans la status bar VS
- [ ] Résultats consolidés dans la Tool Window MultiSharp Issues
- [ ] Analyse incrémentale (re-analyse seulement les fichiers modifiés)
- [ ] Possibilité d'annuler l'analyse

---

### US-602 — Postfix templates
**En tant qu'** utilisateur,
**je veux** utiliser des templates postfix (`.if`, `.var`, `.foreach`, `.null`),
**afin d'** accélérer l'écriture de code courant.

**Critères d'acceptance :**
- [ ] `expr.if` → `if (expr) { }` avec curseur positionné
- [ ] `expr.var` → `var name = expr;`
- [ ] `expr.foreach` → `foreach (var item in expr) { }`
- [ ] `expr.null` → `if (expr == null) { }`
- [ ] Templates personnalisables
- [ ] Intégration avec IntelliSense VS

---

### US-603 — Live Templates (snippets intelligents)
**En tant qu'** utilisateur,
**je veux** des live templates contextuels (comme `prop`, `ctor`, `foreach` enrichis),
**afin d'** accélérer la saisie de structures récurrentes.

**Critères d'acceptance :**
- [ ] Templates prédéfinis pour patterns courants
- [ ] Variables dans les templates avec navigation Tab
- [ ] Templates spécifiques au contexte (dans classe, dans méthode, etc.)
- [ ] Import/Export des templates personnalisés

---

### US-604 — Détection de problèmes async/await
**En tant qu'** utilisateur,
**je veux** être alerté des mauvaises pratiques async (`.Result`, `.Wait()`, `async void`),
**afin d'** éviter les deadlocks et les exceptions non gérées.

**Critères d'acceptance :**
- [ ] Détection `.Result` / `.Wait()` sur Task (hors contexte légitime)
- [ ] Détection `async void` hors event handlers
- [ ] Détection méthode async sans `await` interne
- [ ] Quick Fix : `await` + async propagation
- [ ] Détection des `ConfigureAwait` manquants (configurable)

---

### US-605 — LINQ optimisations
**En tant qu'** utilisateur,
**je veux** des suggestions pour optimiser ou simplifier mes requêtes LINQ,
**afin d'** avoir un code plus performant et lisible.

**Critères d'acceptance :**
- [ ] `.Where().First()` → `.First(predicate)`
- [ ] `.Count() > 0` → `.Any()`
- [ ] `.OrderBy().First()` → `.Min()` / `.Max()` si applicable
- [ ] Détection des LINQ dans des boucles (N+1)
- [ ] Quick Fix appliquable pour chaque optimisation

---

## Suivi de l'avancement

### Légende des statuts

| Statut | Signification |
|--------|--------------|
| 🔲 Backlog | Non commencé |
| 🔵 En cours | En développement |
| ✅ Terminé | Livré et testé |
| ⏸ Suspendu | Bloqué ou déprioritisé |

### Dashboard

| US | Titre | Phase | Statut |
|----|-------|-------|--------|
| US-001 | Squelette VSIX | P0 | 🔲 |
| US-002 | Roslyn workspace | P0 | 🔲 |
| US-003 | Options & Config | P0 | 🔲 |
| US-004 | Tool Window Issues | P0 | 🔲 |
| US-101 | Variables inutilisées | P1 | ✅ |
| US-102 | Membres privés inutilisés | P1 | ✅ |
| US-103 | Null reference analysis | P1 | ✅ |
| US-104 | Simplifications expressions | P1 | ✅ |
| US-105 | Using manquants/inutiles | P1 | ✅ |
| US-106 | Code smells SOLID | P1 | ✅ |
| US-201 | Rename | P2 | 🔲 |
| US-202 | Extract Method | P2 | 🔲 |
| US-203 | Extract Interface | P2 | 🔲 |
| US-204 | Inline Method/Variable | P2 | 🔲 |
| US-205 | Introduce Variable/Field/Param | P2 | 🔲 |
| US-206 | Change Signature | P2 | 🔲 |
| US-207 | Move Type to File | P2 | 🔲 |
| US-208 | Convert Extension/Static | P2 | 🔲 |
| US-301 | Implémenter membres | P3 | 🔲 |
| US-302 | Générer constructeur | P3 | 🔲 |
| US-303 | Générer Equals/GetHashCode | P3 | 🔲 |
| US-304 | Générer ToString | P3 | 🔲 |
| US-305 | Propriétés depuis champs | P3 | 🔲 |
| US-401 | Go to Symbol | P4 | 🔲 |
| US-402 | Find Usages | P4 | 🔲 |
| US-403 | Navigate to Implementation | P4 | 🔲 |
| US-404 | Structural Search & Replace | P4 | 🔲 |
| US-501 | Formatage auto à la sauvegarde | P5 | 🔲 |
| US-502 | Naming conventions | P5 | 🔲 |
| US-503 | Code style enforcement | P5 | 🔲 |
| US-601 | Analyse solution-wide | P6 | 🔲 |
| US-602 | Postfix templates | P6 | 🔲 |
| US-603 | Live Templates | P6 | 🔲 |
| US-604 | Async/await detection | P6 | 🔲 |
| US-605 | LINQ optimisations | P6 | 🔲 |

---

## Dépendances entre phases

```
P0 (Fondations)
  └── P1 (Analyse + Quick Fixes)
        ├── P2 (Refactorings)
        │     └── P3 (Code Generation)
        └── P4 (Navigation)
  └── P5 (Formatage)       ← peut démarrer en parallèle de P1
P1 + P4 ──► P6 (Avancé)
```

---

## Stack technique recommandée

| Composant | Technologie |
|-----------|-------------|
| Extension host | VSIX / AsyncPackage (VS SDK 17.x) |
| Analyse syntaxique | `Microsoft.CodeAnalysis.CSharp` (Roslyn) |
| Semantic model | `Microsoft.CodeAnalysis.CSharp.Semantics` |
| Code transforms | `Microsoft.CodeAnalysis.CSharp.SyntaxFactory` |
| MEF / Services | `System.ComponentModel.Composition` (VS MEF) |
| Tests unitaires | xUnit + `Microsoft.CodeAnalysis.Testing` |
| Tests intégration | `Microsoft.VSSDK.BuildTools` + test host |
| CI/CD | GitHub Actions + `vsce` packaging |
