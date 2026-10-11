# RetroTink.Profiles — Spécifications fonctionnelles et techniques

> **Statut : proposition de référence / base de travail évolutive**  
> **Version du document : 0.1 (11 octobre 2026)**  
> **Projet GitHub :** `retrotink-profiles`  
> **Langue du document :** français ; noms de types, d'API, identifiants de paramètres et documentation publique de la bibliothèque en anglais.

## 0. Nature et utilisation du document

Ce document rassemble le contexte, les objectifs, les décisions de conception prises à ce jour, les contrats fonctionnels, les contraintes techniques et les critères d'acceptation nécessaires pour qu'un développeur — humain ou IA, y compris dans une autre conversation — puisse reprendre le projet **sans connaître les échanges qui ont précédé**.

**Il ne s'agit ni d'un cahier des charges intangible, ni d'un ordre de tout implémenter immédiatement.** C'est une **base de travail** : dans une démarche agile, les modèles, interfaces, noms, responsabilités, tests et priorités **pourront et devront être remis en question** lorsque l'implémentation, l'observation de fichiers RT4 réels ou l'expérimentation sur le matériel révèleront un meilleur choix. Les décisions déjà actées constituent la direction de départ, pas une interdiction de refactorer.

Règles d'utilisation :

- Ne pas transformer une hypothèse, un exemple d'API ou une représentation JSON illustrative en contrainte définitive sans validation.
- Ne pas inventer de correspondance binaire RT4, de capacité matérielle, d'offset ou de valeur admissible à partir des seuls exemples de ce document.
- Préférer de **petites vertical slices testables** à une implémentation théorique complète de tous les paramètres RetroTINK.
- Signaler explicitement les divergences proposées, leur motivation et les tests impactés ; mettre le document à jour au fil des décisions.
- **Ce document seul n'autorise pas à commencer à coder** dans la conversation où il est produit : attendre une instruction explicite. Lorsqu'il sera utilisé ultérieurement comme consigne d'implémentation, convenir d'abord du périmètre du premier incrément.

Les mentions **Décidé**, **Proposé** et **À vérifier** indiquent le degré d'engagement :

- **Décidé** : direction validée dans les échanges, révisable de façon argumentée.
- **Proposé** : choix de conception assez précis pour permettre le travail, mais non encore arbitré explicitement.
- **À vérifier** : dépend d'un comportement réel du matériel, d'une version de firmware ou d'une recherche complémentaire.

---

## 1. Contexte et problème à résoudre

Le projet est né de l'usage de LaserDiscs sur un lecteur **Pioneer CLD-S315** (identification encore probable, non définitivement vérifiée), relié par vidéo composite à un **RetroTINK 4K Pro**, puis à un amplificateur **Onkyo TX-RZ3100** et à un vidéoprojecteur **Epson EH-TW4400**, limité au Full HD. Le propriétaire possède des LaserDiscs PAL et pourrait utiliser des éditions NTSC ; il souhaite affiner les paramètres de décodage, de désentrelacement, de mise à l'échelle et de cadrage en fonction des sources et des films.

Exemple concret : un profil « LaserDisc PAL » contient des paramètres communs ; un film 4:3 et un film Cinémascope *letterbox* utilisent ces paramètres communs tout en adaptant leur géométrie. Corriger ensuite le profil de base doit bénéficier aux deux variantes **lors de leur prochaine résolution/exportation**, sans recopier ni maintenir manuellement tous les paramètres.

Le RetroTINK sait enregistrer et recharger des profils matériels sur carte SD, dans un format binaire couramment identifié par l'extension `.rt4`. Ses fichiers ne représentent pas l'héritage métier que nous souhaitons créer. Le projet a donc deux représentations **distinctes** :

1. **Notre modèle et son format natif JSON** : source de vérité éditable qui préserve définitions, références, composition, surcharges, métadonnées et intentions utilisateur.
2. **Les profils matériels RT4** : représentation d'une configuration effective, exportée vers les appareils compatibles ; l'import RT4 fournit une configuration matérielle exploitable mais **ne peut pas reconstituer** l'héritage natif d'origine.

« Propriétaire » qualifie ici le **schéma du modèle natif**, pas un encodage secret ou nécessairement binaire : **JSON est le format natif retenu**.

### 1.1 Résultat recherché

Produire une bibliothèque .NET open source, réutilisable, fortement typée et indépendante des fichiers, permettant de **définir, manipuler, composer, résoudre, valider et persister** des profils RetroTINK, puis de les **importer/exporter en RT4** via un adaptateur dédié. Une future interface graphique, dite *Studio*, s'appuiera dessus, sans imposer ses choix techniques au modèle métier.

### 1.2 Non-objectifs initiaux

- Écrire dès maintenant une interface graphique ; le Studio est une étape ultérieure.
- Fournir une API d'accès à la carte SD, de gestion de fichiers ou de commande à distance du boîtier.
- Modéliser exhaustivement tous les menus et firmwares dans la première itération.
- Reproduire à l'identique les classes/menus internes du fabricant.
- Faire de RT4 le modèle de domaine ou faire dépendre le Core du format binaire.
- Garantir une compatibilité RT4 universelle sans corpus de fichiers réels et essais matériels.
- Prendre en charge le RetroTINK 5X Pro et les générations 2X dans la V1 : leur gestion des profils diffère.
- Développer immédiatement un *Fluent Builder* avec état mutable et `Build()` : **non retenu**.

---

## 2. Nommage et découpage en assemblies

**Décidé :**

| Élément | Nom | Responsabilité |
| --- | --- | --- |
| Dépôt GitHub | `retrotink-profiles` | Monorepo de la solution et de sa documentation |
| Package/assembly Core | `RetroTink.Profiles` | Domaine, définitions de profils, réglages, Value Objects, composition, résolution et diagnostics |
| Package/assembly JSON | `RetroTink.Profiles.Json` | Persistance du modèle natif JSON versionné |
| Package/assembly RT4 | `RetroTink.Profiles.Rt4` | Import/export binaire RT4 et compatibilités techniques |
| Application future | `RetroTink Studio` (nom provisoire) | Édition et gestion graphique de profils, en dehors du Core |

Les **namespaces** doivent refléter les concepts publics, pas seulement les dossiers. Candidats : `RetroTink.Profiles`, `RetroTink.Profiles.Settings`, `RetroTink.Profiles.Composition`, `RetroTink.Profiles.Formats.Rt4`, `RetroTink.Profiles.Json`. Le suffixe d'assembly `.Rt4` n'oblige pas à mettre tous les types sous exactement ce namespace. **Ne pas utiliser `.4K` comme identifiant C#** (commence par un chiffre).

Graphe de dépendances autorisé :

```text
RetroTink.Profiles              (Core : aucune dépendance aux codecs)
       ▲                ▲
       │                │
RetroTink.Profiles.Json    RetroTink.Profiles.Rt4
       ▲                ▲
       └────────┬───────┘
             Studio / CLI / applications consommatrices
```

- `Json` et `Rt4` dépendent du Core ; ils **ne dépendent pas l'un de l'autre**.
- Le Core ne référence pas `System.IO` dans ses contrats métiers, ni des bibliothèques JSON, UI, stockage ou matériel. L'usage interne éventuel d'API de la BCL n'est pas en soi interdit ; c'est **l'absence de dépendance conceptuelle au stockage** qui compte.
- Le Core ne connaît pas les offsets, en-têtes, CRC, types binaires, chemins de carte SD ou noms d'extensions.
- L'interface graphique et ses ViewModels restent dans un projet distinct.
- Les assemblies peuvent être distribués séparément en NuGet.

**Proposé :** une solution .NET moderne avec projets de tests séparés ; target framework exact, packaging et stratégie de compatibilité (.NET 8/.NET 10, etc.) à décider lors de l'initialisation. Ne pas imposer un framework sans tenir compte des utilisateurs potentiels de la bibliothèque.

---

## 3. Principes transversaux

1. **Modèle métier indépendant des représentations.** Une valeur `OutputResolution.FullHd50` ne connaît pas son octet RT4 ni son nom JSON.
2. **Value Objects riches et immuables.** Invariants à la création, égalité par valeur, absence de setters publics, comportements pertinents sur le type.
3. **Smart Enums** : remplacer les `enum` C# lorsque la sémantique ou l'extensibilité le justifie. Exemples `HdrMode.Off`, `VideoInput.FrontComposite`, `VideoStandard.Pal`. Instances statiques sur leur propre classe, à la manière des enums Java, avec méthodes/propriétés métier possibles.
4. **Ne pas systématiser les Smart Enums.** Pour une valeur paramétrable comme `AspectRatio`, préférer un VO avec constantes usuelles (`FourThree`, `SixteenNine`) **et factory** (`From(235, 100)`), invariants et normalisation. Certains paramètres quantitatifs sont des VO bornés, pas des listes closes.
5. **API Fluent immuable, sans Builder distinct** : les méthodes `With(...)`, `Without(...)`, `BasedOn(...)`, etc., retournent des instances nouvelles. Les consommateurs et le Studio utilisent les mêmes opérations.
6. **Type-safety** à la compilation : une définition `SettingDefinition<VideoStandard>` n'accepte pas un `OutputResolution`.
7. **Aucune valeur `null` utilisée pour exprimer un héritage.** Une affectation absente n'est pas une affectation `Auto`, `Off` ou une valeur inconnue.
8. **Pas de mutation cachée.** Même un `ResolvedProfile` déjà calculé ne change pas lorsque ses sources sont remplacées.
9. **Résolution déterministe et explicable**, avec conflits observables et provenance accessible.
10. **Échecs explicites, pas de pertes silencieuses.** Une conversion non prise en charge n'est pas une conversion réussie.
11. **Aucune dépendance au système de fichiers pour les bibliothèques.** Entrées/sorties en mémoire (`ReadOnlyMemory<byte>`, `byte[]` via wrappers) ou `Stream` pour les codecs ; fichiers, dossiers et carte SD gérés par l'application hôte.
12. **Simplicité avant généralisation.** Pas de hiérarchie `PalProfile`, `LaserDiscProfile`, `NtscProfile` ni de framework de plug-ins inventé en V1.

### 3.1 Identité et égalité

- `ProfileId`, `ProfileName`, `SettingId` : VO validant leur entrée. Politique de casse, Unicode, normalisation et caractères autorisés **à fixer puis tester**, sans supposer qu'un nom affiché est un identifiant stable.
- `ProfileDefinition` possède une **identité métier (`ProfileId`)** même s'il est immuable ; ne pas assimiler son égalité métier à une égalité aveugle de tous ses champs. Définir distinctement identité, comparaison de contenu et éventuelle empreinte de version.
- `ProfileSettings`, `AspectRatio`, `OutputResolution`, les Smart Enums : égalité sémantique/structurelle (pas identité des objets en mémoire).
- Deux définitions de réglage partageant le même `SettingId` ne peuvent pas déclarer des types ou contraintes incompatibles.

---

## 4. Modèle métier du Core

### 4.1 Vocabulaire et responsabilités

| Concept | Rôle | Précisions |
| --- | --- | --- |
| `ProfileDefinition` | Définition déclarative potentiellement partielle | Id, nom, description, sources ordonnées, affectations locales |
| `SettingDefinition<TValue>` | Définition d'un réglage connu et typé | Id stable, type, libellé, contraintes, politique de portée |
| `SettingId` | Identifiant d'un réglage | Décorrélé de la position dans les menus et du binaire |
| `ProfileSettings` | Ensemble immuable d'affectations **locales** | Clé logique `(SettingId, SettingScope)` et valeur fortement typée |
| `SettingScope` | Contexte d'application facultatif d'un réglage | Global par défaut ; mode vidéo lorsque pertinent |
| `SettingCatalog` | Découverte/validation des définitions | Extensible de façon contrôlée pour Studio et adapters |
| `ProfileResolver` | Composition, détection de cycles, recherche des valeurs effectives | Aucun accès I/O ni repository obligatoire |
| `ResolvedProfile` | Configuration effective autonome | Interface typée de lecture, pas nécessairement complète pour RT4 |
| `ResolutionResult` | Résultat de la résolution | Succès/échec, profil, diagnostics, provenance |
| `ResolutionDiagnostic` | Conflit/avertissement/erreur | Codes stables, contexte, valeurs impliquées si elles sont sûres à exposer |

**Décidé :** la présence d'un `Setting<TValue>` public n'est **pas requise** pour la V1. On n'en introduira un que s'il apporte une vraie valeur à l'API ; le couple `SettingDefinition<TValue>` + `ProfileSettings` peut suffire.

### 4.2 `ProfileDefinition`

Propriétés conceptuelles :

| Membre | Type candidat | Règle |
| --- | --- | --- |
| `Id` | `ProfileId` | Obligatoire, unique dans un ensemble à résoudre |
| `Name` | `ProfileName` | Obligatoire, pour affichage |
| `Description` | `string?` ou `ProfileDescription` | Facultatif, sans sémantique de résolution |
| `Sources` | `IReadOnlyList<ProfileId>` / collection immuable | Ordonnée ; zéro, une ou plusieurs références |
| `Settings` | `ProfileSettings` | Uniquement les affectations **locales**, non les héritées |

Une seule classe suffit quel que soit le type de source : PAL, NTSC, vidéoprojecteur, console, LaserDisc, etc.

**Exemple d'API publique candidate** (pseudo-code C# explicatif, **pas** une implémentation à copier aveuglément) :

```csharp
var pal = ProfileDefinition
    .Create(ProfileId.From("ld-pal"), ProfileName.From("LaserDisc PAL"))
    .With(Settings.Input, VideoInput.FrontComposite)
    .With(Settings.OutputResolution, OutputResolution.FullHd50)
    .With(Settings.Hdr, HdrMode.Off);

var cinema = ProfileDefinition
    .Create(ProfileId.From("ld-pal-cinema"), ProfileName.From("PAL CinemaScope"))
    .BasedOn(pal.Id)
    .With(Settings.PictureFormat,
          PictureFormat.Letterboxed(AspectRatio.From(235, 100)));
```

`Create`, `With`, `Without`, `BasedOn`, `WithSources`, `Rename` et éventuellement `WithDescription` sont des méthodes **immuables**, sans état intermédiaire ni `Build()`. Le sérialiseur pourra reconstruire directement les composants validés du modèle, **sans devoir jouer une séquence artificielle d'appels Fluent**.

### 4.3 `SettingDefinition<TValue>` et valeurs

- Le type `TValue` doit être non nul et adapté à une valeur immuable et valide.
- La définition expose au minimum un `SettingId` stable, les métadonnées utiles à la découverte et une `ScopePolicy`.
- Ne pas y incorporer un offset RT4, un code binaire, un chemin de fichier ou la liste de toutes les versions firmware.
- Les contraintes **intrinsèques** d'un VO relèvent du VO (dénominateur strictement positif, dimensions non nulles, plage numérique métier, etc.). Les contraintes de portée d'une définition relèvent du catalogue. La **compatibilité avec le boîtier** relève de l'adaptateur et de sa politique de compatibilité.
- Le catalogue ne doit pas être implicitement extensible en enregistrant n'importe quel couple id/type sans validation. Il doit détecter les duplications incompatibles, sans empêcher à terme les consommateurs de définir de nouveaux paramètres typés.
- Des identifiants textuels peuvent servir à la persistance, mais **pas** de `Dictionary<string, object>` comme API métier publique.

Exemples conceptuels :

```csharp
SettingDefinition<VideoInput> input = Settings.Input;
SettingDefinition<OutputResolution> output = Settings.OutputResolution;

ProfileSettings values = ProfileSettings.Empty
    .With(input, VideoInput.FrontComposite)
    .With(output, OutputResolution.FullHd50);

// Rejeté par le compilateur : type de valeur incompatible.
// values.With(Settings.Input, OutputResolution.FullHd50);
```

### 4.4 `ProfileSettings`

Collection immuable de couples `(SettingDefinition<TValue>, SettingScope, TValue)`, avec opérations candidates :

- `With<TValue>(definition, value)` et `With<TValue>(definition, value, scope)` : ajoute ou remplace exactement l'affectation locale correspondante.
- `Without<TValue>(definition)` et `Without<TValue>(definition, scope)` : supprime **cette affectation locale**, sans poser une valeur `Off` ni interdire l'héritage.
- `Get<TValue>(definition[, scope])` et `TryGet<TValue>(...)` pour un ensemble de valeurs locales ; `TryGet` sera préféré pour une valeur optionnelle.
- `Contains`, énumération en lecture seule pour les sérialiseurs et le Studio, comparaison structurelle.
- Aucun doublon `(SettingId, Scope)` à un niveau local. Un doublon JSON doit provoquer un échec contrôlé plutôt qu'une victoire accidentelle du dernier élément.

**Important :** la collection locale (`ProfileSettings`) et la **vue effective** des paramètres (`ResolvedSettings` ou équivalent) n'ont pas forcément les mêmes structures internes. La seconde devra restituer les règles de priorité sans les perdre lors d'un aplatissement naïf.

### 4.5 Smart Enums et Value Objects

Les types ci-dessous sont des **candidats**, pas une affirmation que toutes leurs valeurs sont encodables dans RT4 :

| Catégorie | Types possibles | Exemples |
| --- | --- | --- |
| Entrées/standards | `VideoInput`, `VideoStandard` | `FrontComposite`, `Pal`, `Ntsc`, `Auto` |
| Sortie | `OutputResolution`, `RefreshRate`, `HdrMode`, `Colorimetry`, `RgbRange`, `SyncMode` | `FullHd50`, `FullHd60`, `Off`, `Rec709` |
| Traitement | `DeinterlacingMode`, `TelecineMode`, `CombFilterMode`, `SharpnessLevel` | `MotionAdaptive`, `Cadence22`, `Cadence32` |
| Géométrie | `AspectRatio`, `PictureFormat`, `ScalingMode`, `Crop` | `FourThree`, `Letterboxed(2.35)`, `Proportional` |
| Métadonnées/portée | `ProfileId`, `ProfileName`, `SettingId`, `VideoMode` | `ld-pal`, `576i50` |

Règles :

- Les instances prédéfinies sont exposées depuis **leur propre classe** (`HdrMode.Off`, etc.).
- Les Smart Enums portent leurs comportements utiles, leur identité stable et leur égalité sémantique ; ils ne portent ni les codes RT4 ni les capacités du firmware.
- Leur politique d'extension (`FromCode`/`TryFromCode`, extensibilité publique ou catalogues contrôlés) est **à décider au cas par cas**. Ne pas permettre qu'une chaîne arbitraire devienne silencieusement une valeur reconnue et exportable.
- Un code inconnu lu dans un RT4 n'est **pas** automatiquement un Smart Enum valide : il peut être conservé dans une représentation opaque côté RT4.
- Pour `AspectRatio`, préférer une fraction canonique (numérateur/dénominateur, normalisés) à une comparaison approximative de `double` ; `2.35` et `2.39` ne doivent pas être confondus.
- Une valeur `Auto` ou `Off` est **explicitement définie**. L'absence d'affectation seule représente « non défini / héritable ».

### 4.6 Concepts natifs et paramètres matériels

Le Core peut exprimer des **intentions** (ex. `PictureFormat.Letterboxed(2.35)`) qui se traduisent en plusieurs réglages matériels ou qui ne disposent pas encore d'un encodage RT4 connu. Ne pas faire croire qu'il existe nécessairement un champ RT4 `pictureFormat` unique.

Distinguer, dans le catalogue et le mapping :

- **Paramètre matériel mappable** : une valeur du Core correspond à un réglage concret RT4 documenté/validé.
- **Intention de haut niveau** : requiert une traduction contrôlée (ex. géométrie et zoom).
- **Métadonnée native** : concerne seulement notre modèle (sources, nom logique, provenance) et ne doit pas être exportée comme un réglage matériel.

L'écriture RT4 doit expliquer/diagnostiquer le cas où une intention n'a **pas** de traduction sûre. Elle ne doit jamais l'ignorer silencieusement.

---

## 5. Portées (`SettingScope`) et sémantique des valeurs

### 5.1 Pourquoi des portées ?

Certains comportements du RetroTINK peuvent dépendre du mode vidéo reçu. Le modèle doit pouvoir distinguer une affectation globale et une affectation propre à une entrée telle que `Pal576i`. **À vérifier** : la liste exacte des paramètres sauvegardés par mode et leur traduction RT4, variable selon firmware.

Pour la première itération :

- `SettingScope.Global` (implicite si omis).
- `SettingScope.For(VideoMode.Pal576i)` et autres modes explicitement reconnus.
- Chaque `SettingDefinition` déclare si une portée autre que globale est sémantiquement admissible.
- Ne pas inventer dès la V1 des prédicats arbitraires et combinables de modes, qui compliqueraient la priorité et l'export binaire.

### 5.2 Règle de résolution **validée** : profil d'abord, portée ensuite

Pour rechercher un paramètre dans un contexte `Pal576i` :

1. Affectation du profil courant à la portée `Pal576i`, si présente.
2. Affectation **globale du profil courant**, si présente.
3. Puis, parmi les sources, **la dernière source déclarée en premier**, en appliquant dans chacune la même recherche (portée spécifique avant globale).
4. À défaut : paramètre absent, **jamais** valeur inventée.

Exemple : si le parent définit `ScalingMode.AutoFill` pour `Pal576i` et l'enfant définit `ScalingMode.Proportional` en global, la valeur effective pour `Pal576i` est **`Proportional`**, parce que la valeur locale de l'enfant l'emporte sur la valeur spécifique du parent.

Ne pas calculer cette valeur par un simple `Dictionary<(SettingId, Scope), object>` fusionné puis par une recherche « portée avant origine » : cela changerait la sémantique convenue.

### 5.3 Questions à protéger par les tests

- Lecture d'un paramètre **sans contexte** : elle consulte uniquement l'affectation globale (ou renvoie absent). Elle ne devine pas un mode vidéo.
- La consultation **avec contexte** respecte la priorité ci-dessus.
- `Without(setting, scope)` supprime uniquement le réglage local correspondant ; il fait donc réapparaître une valeur globale locale ou héritée selon les règles.
- Les champs absents, les valeurs explicites, les paramètres incompatibles avec le matériel et les octets RT4 inconnus sont **quatre états distincts** : ne pas les écraser dans un seul `null` ou une seule valeur `Unknown`.
- La liste des modes, leur représentation et leur degré d'intersection doivent rester déterministes. Les portées qui se chevauchent ne sont pas prises en charge tant qu'une règle de précédence dédiée n'a pas été spécifiée.

---

## 6. Composition et résolution des profils

### 6.1 Règles décidées

- Une définition possède une **liste ordonnée** de sources (`Sources`). `BasedOn(parent)` est un raccourci pour un parent unique ; `WithSources(...)` exprime la composition.
- Les sources sont conceptuellement appliquées **de gauche à droite** ; **la dernière est prioritaire** sur la précédente en cas de conflit, sous réserve du mécanisme de portée décrit plus haut.
- Les affectations locales de la définition courante ont **toujours** priorité sur ses sources.
- La résolution est pure, déterministe, et n'accède ni à des fichiers, ni au réseau, ni à une base de données.
- Le résolveur reçoit explicitement un ensemble de définitions ou un catalogue **déjà chargé en mémoire** ; ne pas imposer `IProfileRepository` au Core.
- Une définition partielle sans paramètres matériels peut être parfaitement valide.
- `ResolvedProfile` est autonome, immuable et typé ; il n'est pas nécessairement exhaustif pour exporter un RT4.

### 6.2 Exemple concret

Définitions :

| Profil | Sources | Affectations locales |
| --- | --- | --- |
| `ld-pal` | Aucune | `input=front-composite`, `output=1080p50`, `hdr=off` |
| `epson` | Aucune | `output=1080p60`, `colorimetry=rec709` |
| `ld-pal-cinema` | `ld-pal`, puis `epson` | `output=1080p50`, `pictureFormat=letterbox:2.35` |

Résolution de `ld-pal-cinema` :

| Paramètre | Valeur | Définie à l'origine par |
| --- | --- | --- |
| Input | FrontComposite | `ld-pal` |
| Output | FullHd50 | `ld-pal-cinema` (surcharge) |
| HDR | Off | `ld-pal` |
| Colorimetry | Rec709 | `epson` |
| PictureFormat | Letterboxed(2.35) | `ld-pal-cinema` |

Le désaccord sur `Output` entre les deux parents génère un **diagnostic de conflit non bloquant** par défaut, même si l'enfant le résout explicitement.

### 6.3 Résultat, diagnostics et provenance

Proposition de contrat conceptuel :

```csharp
var resolver = new ProfileResolver(new[] { pal, epson, cinema });
ResolutionResult result = resolver.Resolve(cinema.Id);

if (result.IsSuccess)
{
    ResolvedProfile effective = result.Profile;
    OutputResolution output = effective.Settings.Get(Settings.OutputResolution);
    // result.Diagnostics : conflits éventuels
    // result.GetOrigin(Settings.OutputResolution) : source et chaîne de priorité
}
```

Les noms et types précis restent à stabiliser (résultat dédié vs `Outcome<T>`, propriété `Profile` éventuellement optionnelle en cas d'échec). **Ne pas imposer une dépendance externe de gestion d'erreurs** par défaut. Préférer des erreurs explicites et identifiables, sans utiliser les exceptions pour les situations métier attendues.

Diagnostics attendus :

| Situation | Comportement |
| --- | --- |
| Source inexistante | Échec, code stable, profil/parent concernés |
| Cycle de sources | Échec, chemin du cycle fourni |
| `ProfileId` dupliqué dans l'ensemble | Échec |
| Définition d'un même `SettingId` avec types incompatibles | Échec |
| Portée non permise pour ce réglage | Échec de validation |
| Deux sources donnent des valeurs incompatibles | Avertissement, dernier parent gagnant par défaut |
| Surcharge volontaire locale | Normal ; peut être visible dans la trace |
| Paramètre non renseigné | Absence, sans valeur fictive |

La **provenance** n'est pas une propriété du VO valeur : elle accompagne le résultat de résolution. Elle doit indiquer au moins la définition ayant effectivement apporté la valeur, s'il s'agit d'un héritage, la portée choisie et, pour faciliter le diagnostic, les candidats écrasés. Préserver les identités **ultimes** dans les chaînes d'héritage.

### 6.4 Diamants, composition récursive et ordre

Le cas `A <- B`, `A <- C`, puis `B, C <- D` est autorisé : **la répétition du même ancêtre n'est pas un cycle**. Les sources sont résolues dans leur ordre déclaré ; la dernière source effective est prioritaire, même si elle hérite elle-même de A. Les diagnostics de conflit doivent être utiles et éviter les faux doublons bruyants lorsque la même valeur provient effectivement du même ancêtre.

Pour respecter à la fois **priorité du profil** et **portée**, l'implémentation peut conserver une hiérarchie/trace de couches ou produire une structure équivalente ; elle ne doit pas aplatir les seules affectations globales/scopées d'une manière qui inverse les priorités. Documenter précisément l'algorithme retenu et le tester sur plusieurs niveaux, plusieurs parents et plusieurs portées.

### 6.5 Comportement transactionnel et immutabilité

Une résolution échouée ne renvoie pas un `ResolvedProfile` partiellement fiable sous couvert de succès. Aucune opération ne doit modifier les `ProfileDefinition` d'entrée ni un résultat de résolution antérieur. Deux résolutions avec les mêmes entrées, dans le même ordre, donnent les mêmes valeurs et diagnostics (à métadonnées non déterministes exclues).

---

## 7. Persistance native JSON : `RetroTink.Profiles.Json`

### 7.1 Objectifs

**Décidé : JSON.** Ce format sauvegarde notre **modèle natif**, non une copie du binaire RT4. Il préserve :

- identités, noms, descriptions et autres métadonnées de définitions ;
- références ordonnées aux sources (héritage/composition) ;
- seules les affectations **locales**, avec leur portée et leur valeur ;
- version du schéma de persistance.

Il n'a pas à sauvegarder les valeurs dérivées ni les diagnostics de résolution comme sources de vérité. Ces informations se recalculent.

### 7.2 Schéma de départ proposé

Un document peut contenir un **ensemble** de définitions ; il est possible d'ajouter plus tard une persistance un profil par document si un cas d'usage l'exige. Proposition V1 (à valider par tests de round-trip) :

```json
{
  "schemaVersion": 1,
  "profiles": [
    {
      "id": "ld-pal",
      "name": "LaserDisc PAL",
      "description": "Common settings for PAL LaserDiscs",
      "sources": [],
      "settings": [
        { "id": "input.source", "value": "front-composite" },
        { "id": "output.resolution", "value": "1080p50" },
        { "id": "output.hdr", "value": "off" }
      ]
    },
    {
      "id": "ld-pal-cinema",
      "name": "PAL CinemaScope",
      "sources": ["ld-pal"],
      "settings": [
        {
          "id": "geometry.pictureFormat",
          "value": { "kind": "letterboxed", "aspectRatio": { "numerator": 235, "denominator": 100 } }
        }
      ]
    }
  ]
}
```

La forme **tableau d'affectations** permet d'associer une portée explicite si nécessaire :

```json
{
  "id": "scaling.mode",
  "scope": { "kind": "videoMode", "value": "pal576i" },
  "value": "proportional"
}
```

L'absence de `scope` signifie global. `schemaVersion` désigne **notre JSON**, pas le firmware ou la version binaire du RetroTINK. Les noms de clés et les codes de valeurs ci-dessus sont des **propositions de schéma**, pas encore une norme publiée.

### 7.3 Règles de (dé)sérialisation

- Utiliser `System.Text.Json` ou une justification explicite pour une autre solution ; pas de sérialisation par chemins de fichiers.
- API sur `ReadOnlyMemory<byte>`/`Stream`, éventuellement JSON textuel (`string`, `ReadOnlySpan<char>`) si utile ; méthodes synchrones en mémoire et éventuellement asynchrones sur flux.
- Désérialiser vers des VO valides, via un **catalogue de `SettingDefinition<T>`**, et non comme `object` non typé exposé à l'utilisateur.
- `ProfileId`, Smart Enums et ratios utilisent des représentations stables ; les transformations de version du schéma sont isolées dans l'assembly JSON.
- Les sources n'ont pas à être présentes dans **le même appel unitaire** de désérialisation, mais la validation de cohérence d'un ensemble à résoudre devra détecter les sources manquantes. Définir clairement les responsabilités « parsing » vs « validation sémantique ».
- En V1, une valeur invalide, un identifiant dupliqué, un schéma inconnu ou un réglage non reconnu doit entraîner une **erreur explicite**, jamais une suppression silencieuse. Une conservation opaque des extensions JSON inconnues peut être envisagée plus tard, avec une politique documentée.
- Produire un JSON déterministe si l'ordre d'entrée est stable (utile aux diffs Git) ; ne pas reclasser la liste ordonnée des sources.
- L'export JSON et son import doivent reproduire exactement les définitions, surcharges et portées : `Deserialize(Serialize(model))` sémantiquement équivalent.

### 7.4 Gestion des versions

- `schemaVersion` obligatoire dès la V1.
- Refuser proprement une version majeure non prise en charge ; ne pas interpréter arbitrairement un schéma inconnu comme la V1.
- Prévoir des migrations explicites, testées sur des fixtures, **quand** une évolution de schéma les rend nécessaires.
- Une migration doit conserver les relations et distinctions « absent »/« Auto »/« Off ».

---

## 8. Format matériel RT4 : `RetroTink.Profiles.Rt4`

### 8.1 Frontière fonctionnelle

L'assembly RT4 importe et exporte une **configuration effective** à destination des appareils compatibles. Il dépend du Core mais n'a pas connaissance de la façon dont le JSON natif est stocké.

Responsabilités :

1. Reconnaître/valider les données RT4 suffisamment documentées (structure, contrôles d'intégrité, limites, versions connues).
2. Lire les paramètres compris et les traduire en VO/paramètres du Core, sans inventer de définitions parentes.
3. Préserver les parties non comprises du profil **côté RT4** pour permettre une modification non destructive.
4. Appliquer sur une représentation de référence des paramètres du `ResolvedProfile`, seulement si leur encodage et la compatibilité de la cible sont connus.
5. Restituer les octets exportables sur `Stream` ou en mémoire, avec résultat/diagnostics explicites.
6. Décrire les paramètres supportés par version de codec/firmware et les intentions non encodables.

**À ne pas faire :** mettre `byte[]` de provenance RT4, offsets ou CRC directement dans `ProfileDefinition` ou ses Value Objects.

### 8.2 APIs de principe, stockage agnostique

```csharp
// Signatures illustratives : noms exacts et gestion des erreurs à finaliser.
ImportedRt4Profile imported = codec.Import(rt4Bytes);
ResolvedProfile effective = resolver.Resolve(profileId).Profile;

// Tant que le binaire n'est pas complètement maîtrisé :
// export depuis une référence compatible, pour conserver les champs inconnus.
Rt4ExportResult exported = codec.Export(
    effective,
    imported.BinaryTemplate,
    targetCapabilities);

ReadOnlyMemory<byte> bytes = exported.Bytes;
```

Proposer aussi des surcharges sur `Stream` (lecture/écriture asynchrone avec `CancellationToken` si utile). Éviter l'obligation de posséder un fichier physique ou d'exposer `FileInfo`/chemin dans les signatures publiques. Ne pas provoquer une conversion buffer ↔ stream multiple sans bénéfice.

### 8.3 Compatibilité, modèles et firmwares

**Cible principale initiale : RetroTINK 4K Pro**, car c'est l'appareil effectivement acquis pour ce projet. Le **4K CE** et le **6X CE** partagent l'écosystème de profils 4K et constituent des cibles d'extension pertinentes. Le fabricant indique que les profils 6X CE et 4K peuvent être importés entre appareils, avec **adaptations automatiques possibles des paramètres de sortie** ; cela ne doit pas être interprété comme une garantie de compatibilité binaire/semantique parfaite pour chaque réglage.

Un `TargetCapabilities` / `DeviceTarget` (nom à choisir) doit pouvoir prendre en compte au besoin le **modèle**, le **firmware** et les paramètres encodables. Pas de `if (firmware >= X)` disséminés dans les VO du Core. Un paramètre intrinsèquement valide peut être **non pris en charge** par le matériel ciblé.

La chaîne d'export ne doit pas considérer silencieusement un profil matériel comme valide pour toutes les versions. Les compatibilités restent une matrice à enrichir avec les fixtures et les essais.

### 8.4 Ce qui est documenté et ce qui ne l'est pas

La bibliothèque open source [`boatmeme/rt4k-profile`](https://github.com/boatmeme/rt4k-profile) fournit un travail de rétro-ingénierie du format `.rt4` en TypeScript, notamment pour un schéma référencé **1.4.2**. Elle constitue une **source de comparaison**, pas une spécification officielle exhaustive ni une dépendance obligatoire.

Éléments observables dans ce code historique, **à revérifier sur les fichiers et firmwares ciblés** :

- En-tête reconnu `RT4K Profile`.
- Champs documentés pour l'entrée, la résolution de sortie, HDR, colorimétrie, RGB range, synchronisation, VRR, deep color et quelques effets/OSD.
- Vérification/écriture d'un **CRC16-CCITT**, que l'implémentation consultée calcule sur les octets à partir de l'offset 128 et écrit à l'offset 32 (deux octets). **Ce détail n'est pas une garantie universelle pour les futures versions.**
- Une grande partie des paramètres vidéo avancés n'est pas couverte par le schéma public consulté ; **ne pas inventer** leurs offsets.

**Important :** les firmwares officiels évoluent encore, y compris dans leur manière d'appliquer des profils, de préserver l'entrée/la résolution et de migrer certains paramètres. Examiner les *release notes* correspondant aux fichiers testés. **Les exemples de réglages PAL/NTSC et de cadrage de nos échanges ne prouvent pas qu'un encodage RT4 est disponible.**

### 8.5 Politique de non-perte et d'export

- **V1 prudente** : export **basé sur un profil RT4 binaire de référence connu compatible** ; modifier uniquement les champs dont le mapping est confirmé. Recalculer le contrôle d'intégrité adéquat selon la version vérifiée.
- Préserver **octet pour octet** les régions non interprétées dans la représentation binaire, hors ajustements documentés nécessaires à la validité du format.
- Si le Core demande explicitement un réglage non exportable, retourner une **erreur de compatibilité / mapping indisponible**, sans prétendre que l'export a réussi.
- Un éventuel mode « ignore unsupported settings » ne pourrait exister que sous une option **explicite**, désactivée par défaut, avec diagnostics détaillés. **Non requis en V1.**
- N'exporter « à partir de rien », sans template, que si la création de tous les champs indispensables est réellement documentée et testée.
- Test impératif : importer puis réexporter **sans modification** un RT4 connu doit conserver le contenu (idéalement identité binaire), et les imports/exports modifiés doivent préserver les zones inconnues et produire une intégrité valide.
- Ne jamais écraser automatiquement les originaux ; la gestion des backups ou du système de fichiers relève de l'application hôte.

### 8.6 RT4 → modèle natif

L'import d'un `.rt4` ne contient pas nos parents, nos intentions de composition ou nos métadonnées JSON. Il peut créer un **profil autonome** ou une définition initiale avec seulement les valeurs interprétées. La conservation de la **référence binaire** et des champs inconnus reste dans un objet/artefact de l'adaptateur RT4, distinct du modèle de domaine. Documenter le lien entre ce template et une définition native si l'application souhaite les associer.

---

## 9. Catalogue initial de réglages : périmètre contrôlé

Ne pas traiter cette liste comme une spécification d'offsets ni comme une obligation de tout livrer en V1.

| Domaine | Réglages conceptuels | Situation |
| --- | --- | --- |
| Input | Source composite en façade, S-Video, HDMI, etc. | Certaines valeurs mappées dans le schéma communautaire historique |
| HDMI Output | Résolution (1080p50/60...), HDR, colorimétrie, plage RGB, synchronisation | Plusieurs champs historiques documentés ; compatibilité firmware à valider |
| Composite Decode | Standard vidéo, filtres Y/C, peigne 3D NTSC | Fonctionnalités matérielles à investiguer, mapping récent non établi |
| Processing | Désentrelacement, inverse télécinéma, netteté, traitements de bruit | Concepts métier pertinents ; mappings encore à investiguer |
| Geometry | Ratio, cropping, zoom, mode de scaling et ajustement par mode | Mélange de concepts de haut niveau et réglages matériels ; prudence maximale |
| Native metadata | Sources, nom logique, description, provenance, diagnostics | Uniquement dans le modèle/JSON ; pas d'encodage matériel supposé |

Premier **scénario vertical** recommandé : un profil RT4 de référence existant, les champs de sortie **réellement vérifiés** (entrée composite, résolution, HDR, selon firmware), la résolution du parent PAL et l'export non destructif. Les paramètres de cadrage/Cinémascope constituent ensuite un scénario de recherche et d'implémentation prioritaire, **pas un mapping déjà acquis**.

**Ne pas coder `VideoStandard.Pal` en RT4 si le boîtier le détecte automatiquement ou si aucun champ persistant correspondant n'a été identifié.** Le Core peut conserver le contexte de la source sans forcer une écriture matérielle fictive.

---

## 10. Exemples fonctionnels de bout en bout

### 10.1 Définir des profils avec héritage

- `ld-pal` : entrée composite, sortie 1080p50 et HDR off ; paramètres supplémentaires lorsque documentés.
- `ld-pal-43` : source `ld-pal`, intention 4:3.
- `ld-pal-cinema` : source `ld-pal`, intention letterbox 2.35:1.
- `ld-ntsc` : définition distincte, sortie 1080p60 et traitements NTSC quand disponibles.
- `epson-fullhd` : réglages de sortie adaptés au projecteur, composables avec un profil de source.

Ces exemples **ne préjugent pas** du nombre exact de profils optimaux sur le matériel : ils servent d'acceptance tests pour notre composition, notre JSON et, lorsque le mapping existe, notre RT4.

### 10.2 Modifier un parent

1. Enregistrer le modèle JSON avec `ld-pal` et ses descendants.
2. Modifier `ld-pal` (`OutputResolution`, etc.) via `With` ; sauvegarder de nouveau le modèle.
3. Résoudre un descendant : le nouveau paramètre parent apparaît **à moins qu'une surcharge locale ou un autre parent prioritaire n'en décide autrement**.
4. Un `ResolvedProfile` créé **avant** cette modification conserve ses anciennes valeurs.
5. Réexporter le descendant au format RT4 valide (dans la limite des mappings disponibles).

### 10.3 Modifier depuis le Studio futur

Le Studio affiche une valeur et sa provenance, permet « Override », « Reset to inherited », « Add source », « Reorder sources » et peut comparer les valeurs avant/après résolution. Son ViewModel modifiable se synchronise avec les **copies immuables** de `ProfileDefinition` ; il n'est **pas nécessaire** de rendre mutable le Core. L'Undo/Redo pourra conserver des états successifs.

### 10.4 Flux des données

```text
              .json natif versionné
                      │
                      ▼
         RetroTink.Profiles.Json
                      │
                      ▼
             ProfileDefinition(s)
                      │
              ProfileResolver
                      │
                      ▼
            ResolutionResult
            ├─ ResolvedProfile
            ├─ Diagnostics
            └─ Provenance
                      │
              RT4 compatibility
                      │
            + Template RT4 connu
                      │
                      ▼
          RetroTink.Profiles.Rt4
                      │
                      ▼
                  bytes RT4
                      │
            Application hôte / SD
```

Les imports RT4 suivent un chemin distinct : `bytes RT4` → adaptateur RT4 → réglages connus + référence binaire préservée → éventuelle création d'une définition native autonome → sauvegarde JSON.

---

## 11. Erreurs, robustesse et sécurité

### 11.1 Politique d'erreur

- **VO invalides** : stratégie à fixer de façon cohérente (`From` / `TryFrom` / `Parse`, exception réservée aux usages manifestement invalides, ou résultat explicite selon la catégorie). Ne pas multiplier des APIs contradictoires.
- **Résolution** : résultat explicite pour cycles, parents absents, doublons, conflits de définitions, portées invalides.
- **JSON** : erreur de schéma, version non prise en charge, type incompatible, valeurs hors domaine et duplications ; contexte exploitable (`profileId`, `settingId`, index/chemin JSON si possible).
- **RT4** : structure invalide, checksum incompatible, version/capacité non prise en charge, valeur non encodable, template inadéquat ; refuser une sortie prétendument réussie.
- **Diagnostics** : codes stables indépendants des messages, niveaux `Error`, `Warning`, `Info` si nécessaire, textes lisibles mais localisables par le Studio.

### 11.2 Données d'entrée non fiables

Les fichiers JSON et les profils binaires proviennent de l'extérieur : vérifier tailles limites raisonnables, troncature, longueurs annoncées, index/offsets, contrôles d'intégrité, valeurs hors plage et cycles de graphes. Pas d'allocations non bornées ni d'exceptions cryptiques provenant d'un dépassement de tableau. Un fichier corrompu ne doit jamais conduire à un export plausible mais erroné.

### 11.3 Indépendance de l'infrastructure

Aucun `File.ReadAllBytes`, répertoire implicite, état global d'application, dépendance UI ou résolution d'un chemin à l'intérieur du cœur. Le codec peut travailler sur des octets/flux mais **ne décide pas où** ils sont persistés. Tous les états nécessaires à un résultat reproductible sont injectés ou fournis explicitement.

---

## 12. Tests et critères d'acceptation

Les tests **définissent le comportement** ; ils ne doivent pas seulement vérifier des propriétés triviales. Minimum attendu :

### 12.1 Value Objects / définitions

- Construction valide/invalide de `ProfileId`, `SettingId`, ratios et résolutions ; normalisation documentée.
- Égalité sémantique et immutabilité de VO et Smart Enums ; instances statiques stables.
- Typage de `SettingDefinition<T>` et rejet de types/identifiants incompatibles.
- Différence entre valeur absente et valeur explicite `Auto`/`Off`.
- `ProfileSettings.With`/`Without` n'altèrent pas l'instance initiale.

### 12.2 Résolution

- Zéro parent ; un parent ; plusieurs parents ordonnés ; surcharge locale.
- Deux parents contradictoires : dernier parent gagnant, avertissement observable.
- Portée spécifique vs globale au **même niveau** : spécifique gagnante.
- Globale **enfant** vs spécifique **parent** : globale enfant gagnante.
- Retrait d'une surcharge : la valeur héritée réapparaît.
- Résolution sans contexte : ne consulte pas arbitrairement des modes spécifiques.
- Parent absent, cycle simple et cycle indirect, id dupliqué : erreurs explicites.
- Héritage en diamant : absence de faux cycle, priorité déterministe et provenance correcte.
- Source partagée et valeurs égales : diagnostics non bruyants.
- Résolution identique répétée : même résultat et même trace à entrée égale.
- Résultats antérieurs inchangés après création de nouvelles définitions.

### 12.3 JSON

- Aller-retour JSON conservant valeurs locales, types, portées, ordre de composition et descriptions.
- Validation version de schéma ; rejet de versions futures inconnues.
- Valeurs invalides, réglages inconnus, types incompatibles, doublons et graphes incomplets signalés clairement au bon niveau.
- JSON lisible et stable dans Git ; pas de données d'exécution parasites.
- Pas de système de fichiers requis pour exécuter les tests de sérialisation.

### 12.4 RT4

- Fixtures binaires **authentiques et identifiées** par modèle/firmware, conservées sans dépendre de valeurs imaginées.
- Reconnaissance des échantillons valides et rejet des troncatures/CRC incorrects lorsqu'applicables.
- Import/export sans modifications : identité ou équivalence binaire documentée.
- Export modifiant uniquement un champ connu ; contrôle d'intégrité recalculé ; parties inconnues inchangées.
- Valeur ou intention non mappable : échec explicite.
- Template inadapté à la cible : échec explicite.
- Compatibilité confirmée sur **le boîtier physique** pour au moins un profil généré avant toute revendication de fiabilité.

### 12.5 Qualité du code

- Tests unitaires séparés par assembly et tests d'intégration codec avec fixtures.
- Pas de réflexion dangereuse ou conversions arbitraires `object` pour contourner le typage.
- Analyseurs, nullable reference types, warnings maîtrisés ; documentation XML pour l'API publique essentielle.
- Mesurer les performances si les flux/collections deviennent volumineux, sans micro-optimisation prématurée.
- Exemples de consommation ne dépendant ni de la carte SD ni d'une IHM.

---

## 13. Stratégie de livraison itérative

**Ne pas démarrer par toutes les features du Studio ni tous les offsets RT4.** Ordre recommandé, révisable après retour d'expérience :

1. **Socle Core** : VO essentiels (`ProfileId`, `ProfileName`, `SettingId`), Smart Enums représentatifs, `SettingDefinition<T>`, `ProfileSettings`, catalogue minimal et tests d'invariants.
2. **Modèle de profils** : `ProfileDefinition` + API Fluent immuable sans Builder ; tests de reconstruction et de suppression d'une surcharge.
3. **Résolution** : sources ordonnées, scopes, cycles, conflits, `ResolvedProfile`, provenance et diagnostics ; tests en diamant.
4. **Persistance native JSON** : schéma V1 versionné, aller-retour, validation ; exemples `ld-pal`, `ld-pal-43`, `ld-pal-cinema`.
5. **Recherche RT4 / fixtures** : collecte d'exports authentiques avec modèle et firmware, comparaison de profils ne différant que d'un paramètre, format/CRC confirmés ; documenter les champs effectivement décodés.
6. **Premier codec RT4 sûr** : import de champs confirmés, export à partir d'une référence, préservation des octets inconnus, tests binaires ; essai sur l'appareil.
7. **Couverture vidéo prioritaire** : élargir pas à pas aux paramètres utiles à la lecture de LaserDiscs (géométrie, désentrelacement, PAL/NTSC, filtres) uniquement avec preuves suffisantes.
8. **Studio** : découverte automatique des settings, édition, héritage, provenance, conflits, previews de résolution et export ; l'interface est un consommateur, pas un prérequis.

Chaque incrément doit livrer un ensemble cohérent, testé et documenté ; mettre à jour le catalogue des capacités, les limites et le présent document.

---

## 14. Questions ouvertes / décisions à arbitrer avant certaines implémentations

| Question | État initial / recommandation |
| --- | --- |
| Framework cible et politique .NET | **Ouvert.** Choisir selon compatibilité souhaitée et maintenance, pas uniquement selon le dernier SDK disponible. |
| Contrat d'erreur exact | **Ouvert.** Résultat explicite pour les erreurs attendues ; ne pas imposer automatiquement une librairie `Outcome`. |
| `Setting<TValue>` public | **Non requis en V1**, à justifier s'il est introduit. |
| Définition d'une extension de Smart Enum | **Ouvert au cas par cas**, avec invariants et codecs séparés. |
| Catalogue statique vs composable | **Proposé :** catalogue fortement typé et extensible de manière contrôlée, sans registre global mutable. |
| Identité des profils et canonicalisation des ids | **À fixer** avec tests de casse/culture/caractères. |
| JSON tableau vs objet de settings | **Proposé :** tableau d'affectations, pour gérer les scopes sans ambiguïté. Peut évoluer tant que le schéma n'est pas publié. |
| Format des valeurs JSON complexes | **À stabiliser** par converters et tests de round-trip. |
| Portées autres que vidéo | **Hors V1** tant que besoin matériel non confirmé. |
| Export RT4 sans template | **Non garanti** ; exiger un template jusqu'à preuve suffisante. |
| Compatibilité firmware RT4 | **À vérifier expérimentalement**, en enregistrant des fixtures et versions connues. |
| Import RT4 → définition native et association au template | **À préciser** sans introduire de binaire opaque dans le Core. |
| Gestion des paramètres JSON futurs inconnus | **V1 : échec explicite** ; conservation opaque/migration à envisager. |
| Futures architectures de l'UI | **Ouvert** (Avalonia, MAUI, autre), hors Core. |
| Licence open source / marque | **À décider** ; éviter toute présentation comme produit officiel RetroTINK. Vérifier les licences des références réutilisées. |

Toute révision doit privilégier la correction sémantique et la maintenabilité plutôt que la fidélité aveugle au texte initial. Garder un court journal des décisions (ADR léger ou section de changelog) lorsque cela change les contrats publics ou les formats persistés.

---

## 15. Sources techniques de départ et niveau de confiance

Références vérifiées ou consultées lors de la rédaction (11 octobre 2026) :

1. **Documentation / firmwares du fabricant RetroTINK** : <https://retrotink-llc.github.io/firmware/4k.html> ; <https://retrotink-llc.github.io/firmware/4k-experimental.html>. Référence principale pour fonctionnalités, évolutions et compatibilité firmware, **pas** un schéma binaire exhaustif.
2. **Présentation officielle du RetroTINK 6X CE** : <https://www.retrotink.com/blog/introducing-the-retrotink-6x-ce>. Indique l'échange de profils entre 6X CE et famille 4K avec adaptations possibles ; ne dispense pas de tests.
3. **Bibliothèque communautaire `boatmeme/rt4k-profile`** : <https://github.com/boatmeme/rt4k-profile>, notamment `src/settings/Schema.ts`, `src/profile/RetroTinkProfile.ts`, `src/utils/FileUtils.ts`. Rétro-ingénierie sous licence MIT, historiquement basée sur le schéma 1.4.2 ; **couvrir, vérifier et documenter** toute reprise de code en respectant la licence et l'attribution.
4. **Wiki / manuel communautaire RetroTINK 4K** : <https://consolemods.org/wiki/AV:RetroTINK-4K>. Utile pour les menus et comportements visibles ; vérifier leur adéquation avec le firmware effectivement utilisé.

**Hiérarchie de fiabilité pour le projet :** observations et fichiers issus du matériel + tests reproductibles ; documentation et notes officielles correspondant au firmware ; schémas communautaires datés ; hypothèses. Un lien vers du code de rétro-ingénierie **ne suffit pas** à certifier un offset pour toute version.

Les caractéristiques concrètes du lecteur LaserDisc et du vidéoprojecteur servent de **cas d'usage**, pas d'invariants de la bibliothèque. Le logiciel doit fonctionner avec d'autres sources et afficheurs compatibles avec le modèle de paramètres.

---

## 16. Définition de « prêt à implémenter » et de « terminé »

### Prêt à implémenter pour un incrément

- Cas d'usage et exemples d'entrées/sorties identifiés.
- Valeurs et invariants des VO concernés définis.
- Responsabilité de l'assembly concerné clarifiée.
- Règles de résolution et de compatibilité connues ou hypothèses **explicitement marquées**.
- Tests de comportement écrits ou décrits avec des résultats attendus observables.
- Si l'incrément touche RT4 : au moins une preuve binaire/fixture, ou bien limiter le travail à la **recherche documentée**.

### Terminé pour le Core/JSON V1

- Création, modification immuable, composition et résolution démontrées par tests.
- Diagnostics et provenance cohérents sur conflits, scopes, cycles et diamants.
- Persistance JSON versionnée fonctionnelle en mémoire/flux, round-trip vérifié.
- Documentation claire de l'API et de ses limites, avec exemples indépendants d'une IHM.

### Terminé pour le premier support RT4

- Import/export d'un sous-ensemble **annoncé précisément**, testé sur des fixtures réelles et sur un appareil cible.
- Préservation des régions binaires inconnues, aucune perte silencieuse.
- Échec explicite pour les intentions et paramètres non pris en charge.
- Limites de modèles/firmwares clairement publiées ; pas de revendication de prise en charge universelle.

---

## 17. Consignes de reprise pour un développeur ou une autre IA

1. Lire **l'ensemble** de ce document avant d'écrire du code ; relire les décisions et les questions ouvertes.
2. Examiner d'abord le contenu **actuel** du dépôt `retrotink-profiles` : le dépôt peut avoir évolué depuis ce texte. Ne pas remplacer des choix déjà implémentés et validés sans raison.
3. Expliquer le **premier incrément vertical** proposé, ses contrats et ses tests. En cas de divergence avec ce document, identifier explicitement l'écart et la justification.
4. Respecter les frontières : Core sans codecs, JSON sans RT4, RT4 sans dépendance JSON, bibliothèque sans gestion de fichiers, Studio séparé.
5. Écrire des tests démontrant les **sémantiques**, en particulier l'immutabilité, la priorité profil/portée, les conflits et la non-perte binaire.
6. Ne pas extrapoler les formats RT4 manquants ; recueillir des exports réels avant de figer les mappings.
7. Faire évoluer les spécifications et exemples si un meilleur modèle se dégage : **la démarche est volontairement agile et itérative**.
8. Ne pas engager une implémentation non demandée : un accord sur la conception n'est pas automatiquement un feu vert pour modifier le dépôt.

**Synthèse :** `RetroTink.Profiles` modélise les définitions et produit des profils résolus ; `RetroTink.Profiles.Json` conserve notre modèle complet ; `RetroTink.Profiles.Rt4` importe et exporte des configurations matérielles à partir d'un mapping vérifié. **Le JSON natif est la source de vérité pour la composition ; le RT4 est un artefact technique.** Ce document est une base de conception sérieuse, mais **explicitement ouverte à la remise en question au fur et à mesure des apprentissages**.