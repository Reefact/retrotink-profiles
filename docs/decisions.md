# Journal des décisions d'implémentation

> Complète `spécifications.md`. Chaque entrée indique la question ouverte traitée (référence au § de la spec), la décision, sa motivation et les tests qui la protègent. Les décisions sont révisables de façon argumentée.

## D01 — Périmètre du premier incrément (§13, §16, §17.3)

**Décision.** Le premier incrément couvre les étapes 1 à 4 du §13 : socle Core (Value Objects, Smart Enums, `SettingDefinition<T>`, `ProfileSettings`, catalogue), modèle de profils (`ProfileDefinition` fluent immuable), résolution (sources ordonnées, portées, cycles, conflits, provenance, diagnostics) et persistance JSON v1. **Rien n'est implémenté côté RT4** : aucun projet `RetroTink.Profiles.Rt4` n'est créé.

**Motivation.** La spec interdit d'inventer un mapping binaire sans fixtures authentiques (§0, §8.4, §12.4). L'incrément Core/JSON est le seul qui soit « prêt à implémenter » au sens du §16 sans accès au boîtier.

**Prochaine étape RT4.** Collecter des exports `.rt4` réels (modèle + firmware), idéalement des paires ne différant que d'un réglage, puis documenter le format avant tout code.

## D02 — Framework cible et outillage (§2, §14)

**Décision.** Bibliothèques multi-ciblées `net8.0;net10.0` ; projets de tests sur `net10.0` ; SDK épinglé par `global.json` (`10.0.100`, `rollForward: latestFeature`) ; solution au format `.slnx` ; gestion centralisée des versions de paquets (`Directory.Packages.props`) ; `TreatWarningsAsErrors`, analyseurs .NET en mode `latest-recommended`, `Nullable` activé, documentation XML obligatoire sur l'API publique ; xUnit 2.9 pour les tests.

**Motivation.** `net8.0` (LTS) élargit l'audience des paquets NuGet sans coût ; `net10.0` offre le runtime courant. Le code reste compatible C# 12 (version implicite du TFM `net8.0`).

**Écart.** Suppressions ciblées dans `.editorconfig` : CA1000 (membres statiques sur `SmartEnum<TSelf>`, c'est le principe du type), CA1710/CA1711/CA1716/CA1724 (nommage métier : `ProfileSettings`, `SmartEnum`, `Global`, `Settings`).

## D03 — Namespaces (§2)

**Décision.** `RetroTink.Profiles` (identités, définitions de réglages, `ProfileSettings`, `ProfileDefinition`, `SettingScope`, catalogue, classe statique `Settings`), `RetroTink.Profiles.Values` (Smart Enums et Value Objects métier), `RetroTink.Profiles.Composition` (résolution), `RetroTink.Profiles.Json`.

**Écart.** Le candidat `RetroTink.Profiles.Settings` n'est pas retenu : il entrerait en conflit avec la classe `Settings` (un type et un namespace ne peuvent partager le même nom complet, et l'écriture `Settings.Input` deviendrait ambiguë). `.Values` le remplace.

## D04 — Contrat d'erreur (§11.1, §14)

**Décision.** Trois régimes cohérents :

1. **Usage manifestement invalide par le développeur** → exception (`ArgumentException`, `ArgumentOutOfRangeException`, `InvalidOperationException`, `KeyNotFoundException`) : `From` sur un identifiant invalide, portée interdite par la définition, même `SettingId` utilisé avec deux définitions, accès à `Result.Profile` après échec, sérialisation d'un type sans convertisseur.
2. **Situation métier attendue** → résultat explicite avec diagnostics à codes stables : `ResolutionResult` (`resolution.*`) et `DeserializationResult` (`json.*`). Aucune bibliothèque externe de type `Outcome`.
3. **Entrée non fiable** → `TryFrom` / `TryFromKey` / `TryGet` retournant `bool`.

**Tests.** `ProfileIdTests`, `ProfileSettingsTests`, `ProfileResolverTests`, `ProfileJsonSerializerErrorTests`.

## D05 — Identifiants et noms (§3.1, §14)

**Décision.**

- `ProfileId` : 1 à 64 caractères ASCII parmi minuscules, chiffres, `-`, `_`, `.` ; commence et finit par une lettre ou un chiffre ; **insensible à la casse, normalisé en minuscules** (`LD-PAL` ≡ `ld-pal`). Pas d'Unicode : l'identifiant n'est pas un nom d'affichage.
- `SettingId` : segments `[A-Za-z][A-Za-z0-9]*` séparés par des points, 64 caractères maximum, **sensible à la casse** (`output.hdr` ≠ `output.HDR`).
- `ProfileName` : Unicode libre, espaces de bord supprimés, 1 à 128 caractères, aucun caractère de contrôle ; comparaison ordinale.
- Clé de Smart Enum : minuscules ASCII et chiffres, tirets simples optionnels (`front-composite`, `1080p50`).

**Tests.** `ProfileIdTests`, `SettingIdTests`, `ProfileNameTests`, `SmartEnumTests`.

## D06 — Égalité et identité (§3.1)

**Décision.**

- `ProfileDefinition` : égalité de référence conservée ; identité métier = `Id` ; `ContentEquals` compare id, nom, description, sources ordonnées et affectations locales. Pas d'empreinte de version en V1.
- `SettingDefinition` : égalité structurelle complète (id, type de valeur, libellé, politique de portée, nature). Un `SettingId` doit toujours désigner **une** définition : catalogue, `ProfileSettings` et résolveur rejettent une divergence.
- `ProfileSettings` : égalité structurelle **insensible à l'ordre** ; l'énumération conserve l'ordre d'insertion (remplacement sur place).
- Smart Enums : égalité par clé et type ; instances uniques (constructeurs privés).
- `AspectRatio` : fraction **réduite** (`235/100` → `47/20`), égalité exacte ; `2.35` et `2.39` ne se confondent pas. La forme réduite est aussi celle persistée.

## D07 — Smart Enums (§3, §4.5, §14)

**Décision.** Base `SmartEnum<TSelf>` + interface `ISmartEnum<TSelf>` avec membre statique abstrait `All`, ce qui permet `TryFromKey`/`FromKey` génériques et des convertisseurs JSON génériques **sans réflexion**. Ensembles **fermés** en V1 (constructeurs privés) : une chaîne inconnue ne devient jamais une valeur. L'extensibilité publique est reportée au cas par cas.

Valeurs initiales (concepts métier, sans prétention d'encodage RT4) : `VideoInput` (front-composite, front-svideo, scart, component, vga, hdmi), `VideoStandard` (auto, ntsc, pal), `OutputResolution` (480p60, 576p50, 720p50/60, 1080p50/60, 2160p50/60), `HdrMode` (off, hdr10, hlg), `Colorimetry` (auto, rec601, rec709, rec2020), `RgbRange` (auto, limited, full), `DeinterlacingMode` (motion-adaptive, bob, weave), `TelecineMode` (off, cadence-22, cadence-32), `ScalingMode` (proportional, auto-fill, integer), `VideoMode` (ntsc480i, pal576i, edtv480p, edtv576p).

**Non décidé / à vérifier.** La liste exacte des valeurs réellement disponibles par modèle et firmware ; `SharpnessLevel`, `CombFilterMode`, `SyncMode`, `Crop` ne sont pas modélisés.

## D08 — Portées (§5)

**Décision.** `SettingScope.Global` et `SettingScope.For(VideoMode)` uniquement. Chaque définition déclare `SettingScopePolicy.GlobalOnly` ou `GlobalOrVideoMode` ; une affectation hors politique est refusée à la construction (exception) et à la lecture JSON (`json.scopeNotAllowed`). Quatre modes vidéo SD initiaux ; les modes HD et les prédicats combinables sont hors V1.

## D09 — Algorithme de résolution (§5.2, §6)

**Décision.** Un profil = composition des **configurations effectives** de ses sources, appliquées de la première à la dernière déclarée (la dernière gagne), puis ses affectations locales par-dessus. Implémentation : parcours en profondeur produisant une liste ordonnée de **couches** : le profil, puis pour chaque source de la dernière à la première, la source suivie de ses propres couches. Une définition déjà rencontrée (diamant) garde sa première position, la plus forte ; répéter un ancêtre n'est pas un cycle.

Recherche d'une valeur dans un contexte : couche par couche, affectation du mode vidéo demandé puis affectation globale ; la première couche qui possède l'une ou l'autre gagne (**profil d'abord, portée ensuite**). Sans contexte, seules les affectations globales sont consultées.

**Conséquence documentée.** Pour `D ← [B, C]` avec `B ← A` (B surcharge un réglage de A) et `C ← A` (C ne dit rien), la valeur effective vient de **A via C**, parce que la configuration effective de C (qui inclut A) a priorité sur celle de B. Un avertissement `resolution.sourceConflict` signale le désaccord B/A-via-C ; inverser l'ordre des sources (`[C, B]`) donne la valeur de B. Ce choix est plus simple et toujours défini, contrairement à une linéarisation de type C3 qui peut échouer ; il reste révisable si l'usage montre qu'il surprend.

**Conflits.** Détectés pour **chaque** définition de la lignée ayant au moins deux sources : pour chaque clé `(SettingId, portée)`, si deux sources fournissent des valeurs différentes, avertissement non bloquant indiquant les candidats, la source gagnante et le fait que le profil surcharge ou non localement. Des valeurs égales (même ancêtre ou coïncidence) ne produisent rien.

**Erreurs (échec sans profil).** `resolution.unknownProfile`, `resolution.missingSource`, `resolution.cyclicSources` (chemin fourni), `resolution.duplicateProfileId` (toute résolution échoue tant que l'ensemble contient des doublons), `resolution.incompatibleSettingDefinitions`.

**Provenance.** `SettingProvenance` : définition, contexte, candidat effectif (`Source` = définition qui déclare réellement la valeur, `Via` = source directe du profil résolu par laquelle elle est atteinte), `IsInherited`, candidats écrasés par ordre de force. `ResolvedProfile.Layers` expose la liste des couches.

**Tests.** `ProfileResolverTests` (zéro/un/plusieurs parents, portées, suppression de surcharge, cycles, doublons, diamants, déterminisme, immutabilité des résultats, `Flatten`, `ListEffective`) et `AcceptanceScenarioTests` (§10).

## D10 — Résolveur sans dépôt (§6.1)

**Décision.** `new ProfileResolver(IEnumerable<ProfileDefinition>)` reçoit l'ensemble en mémoire ; pas d'`IProfileRepository`. Les doublons d'identifiant ne lèvent pas d'exception à la construction mais font échouer les résolutions avec un diagnostic, conformément au tableau du §6.3.

## D11 — Catalogue de réglages (§4.3, §14)

**Décision.** `SettingCatalog` immuable, construit par `Create`/`With` ; l'enregistrement d'une définition égale est idempotent, celui d'une définition divergente pour le même id échoue. `Settings.Catalog` regroupe les onze définitions standard ; les consommateurs l'étendent sans registre global mutable. `SettingKind` (`Hardware` / `Intent`) distingue réglage matériel mappable et intention de haut niveau (§4.6) ; les métadonnées natives (sources, nom) ne sont pas des réglages.

## D12 — Schéma JSON v1 (§7, §14)

**Décision.** Voir `docs/json-schema-v1.md`. Points arbitrés :

- Document multi-profils ; `schemaVersion` entier obligatoire, seule la valeur `1` est acceptée.
- Tableau d'affectations `{ id, scope?, value }` ; portée sous la forme `{ "kind": "videoMode", "value": "pal576i" }`, absente ou `null` = globale.
- Valeurs : clé de Smart Enum en chaîne ; `AspectRatio` en objet `{ numerator, denominator }` réduit ; `PictureFormat` en objet discriminé par `kind` (`full-frame`, `letterboxed`). Les jetons sont en kebab-case, les propriétés en camelCase, les identifiants de réglage en camelCase pointé.
- Propriété inconnue, propriété dupliquée, réglage inconnu, doublon `(id, portée)`, version future : **erreur explicite** ; lecture tout-ou-rien avec tous les diagnostics collectés et un chemin JSON.
- **Parsing vs validation sémantique** : la lecture ne vérifie pas l'existence des sources référencées ; c'est le rôle du résolveur sur l'ensemble assemblé par l'application.
- Écriture déterministe : ordre fixe des propriétés, ordre d'insertion des affectations, UTF-8 sans BOM, caractères non ASCII non échappés, saut de ligne final, indentation deux espaces (option compacte disponible).
- Robustesse (§11.2) : limite de taille (16 Mio par défaut, vérifiée avant parsing, y compris sur flux non positionnables), profondeur maximale 16, commentaires et virgules finales refusés.
- Implémentation sur `JsonDocument`/`Utf8JsonWriter` (pas de DTO par réflexion) ; les Smart Enums ne portent aucun attribut JSON : les convertisseurs (`IJsonSettingValueConverter<T>`, registre `JsonSettingValueConverters`) détiennent la représentation persistée et sont extensibles.

**Tests.** `ProfileJsonSerializerTests` (aller-retour, fixture de référence `Fixtures/laserdisc-v1.json` identique octet pour octet, déterminisme, flux, convertisseur personnalisé, modification d'un parent), `ProfileJsonSerializerErrorTests`.

## D13 — Modèle de données du Core (§4)

**Décision.** Pas de `Setting<TValue>` public (§4.1). `SettingAssignment` (abstrait) / `SettingAssignment<TValue>` représentent une affectation `(définition, portée, valeur)` ; les visiteurs `ISettingDefinitionVisitor<T>` et `ISettingAssignmentVisitor<T>` offrent un double dispatch typé qui évite tout `object` dans l'API publique et toute réflexion dans les sérialiseurs. Le seul `object` interne se trouve dans le registre de convertisseurs JSON, indexé par `typeof(TValue)`.

`ProfileDefinition.Description` reste un `string?` (texte libre normalisé : vide → `null`) ; `ProfileSettings.From(...)` et `ProfileDefinition.Create(id, name, description, sources, settings)` permettent au sérialiseur de reconstruire le modèle sans rejouer d'appels fluent.

## D14 — Ce qui n'est pas fait

- Codec RT4 (voir D01) ; aucune hypothèse d'offset n'a été codée.
- Migrations de schéma JSON : prévues, non nécessaires tant que la version 1 est la seule.
- Conservation opaque des extensions JSON inconnues : refusée en V1 (erreur explicite).
- Type `ProfileSet` / bibliothèque de profils avec validation d'ensemble : le résolveur suffit pour l'instant.
- Licence open source : à décider (§14).
