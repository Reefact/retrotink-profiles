# retrotink-profiles

A .NET library for reading, writing, manipulating, and composing RetroTINK profiles, featuring profile inheritance and support for multiple profile formats, with no file system dependencies.

> **Status: work in progress.** The first increment (core model, composition/resolution, native JSON persistence) is implemented and tested. The RT4 hardware-format adapter is **not started**: it requires authentic `.rt4` exports from a real device before any binary mapping is written. This is a community project, not an official RetroTINK product.

## Packages

| Package | Purpose | State |
| --- | --- | --- |
| `RetroTink.Profiles` | Core domain: typed settings, immutable profile definitions, composition and deterministic resolution with provenance and diagnostics. No codec, no I/O. | Implemented |
| `RetroTink.Profiles.Json` | Versioned native JSON persistence (schema version 1), in memory or over streams. | Implemented |
| `RetroTink.Profiles.Rt4` | Import/export of RT4 device profiles from a known binary template, preserving unknown regions. | Not started (needs fixtures) |

Libraries target `net8.0` and `net10.0`. Only the Core and JSON assemblies depend on each other (JSON → Core); a future Studio application is a consumer, never a prerequisite.

## Quick start

```csharp
using RetroTink.Profiles;
using RetroTink.Profiles.Composition;
using RetroTink.Profiles.Json;
using RetroTink.Profiles.Values;

// 1. Declare profiles. Every operation returns a new, immutable instance.
var pal = ProfileDefinition
    .Create(ProfileId.From("ld-pal"), ProfileName.From("LaserDisc PAL"))
    .With(Settings.Input, VideoInput.FrontComposite)
    .With(Settings.OutputResolution, OutputResolution.FullHd50)
    .With(Settings.Hdr, HdrMode.Off)
    .With(Settings.Telecine, TelecineMode.Cadence22, SettingScope.For(VideoMode.Pal576i));

var cinema = ProfileDefinition
    .Create(ProfileId.From("ld-pal-cinema"), ProfileName.From("PAL CinemaScope"))
    .BasedOn(pal.Id)
    .With(Settings.PictureFormat, PictureFormat.Letterboxed(AspectRatio.From(235, 100)));

// 2. Resolve: sources are composed in order (last wins), local assignments on top.
var resolver = new ProfileResolver([pal, cinema]);
ResolutionResult result = resolver.Resolve(cinema.Id);

if (result.IsSuccess)
{
    ResolvedProfile effective = result.Profile;
    OutputResolution output = effective.Settings.Get(Settings.OutputResolution);      // FullHd50, inherited
    TelecineMode telecine = effective.Settings.Get(Settings.Telecine, VideoMode.Pal576i); // Cadence22

    if (effective.Settings.TryGetProvenance(Settings.OutputResolution, out var provenance))
    {
        Console.WriteLine($"{provenance.Effective.Source} provides {output}, inherited: {provenance.IsInherited}");
    }

    foreach (var warning in result.Warnings)
    {
        Console.WriteLine(warning); // e.g. two sources disagreeing on a setting
    }
}

// 3. Persist the *definitions* (not the resolved values) as versioned JSON.
var serializer = new ProfileJsonSerializer(Settings.Catalog);
string json = serializer.Serialize([pal, cinema]);

DeserializationResult read = serializer.Deserialize(json);
if (!read.IsSuccess)
{
    foreach (var diagnostic in read.Diagnostics) Console.WriteLine(diagnostic); // code, JSON path, message
}
```

Fixing `pal` later (for example `pal.With(Settings.Hdr, HdrMode.Hlg)`) benefits every descendant on its next resolution. A `ResolvedProfile` computed earlier keeps its old values: nothing is mutated behind your back.

## Concepts

| Type | Role |
| --- | --- |
| `ProfileDefinition` | Declarative, possibly partial definition: id, name, description, ordered `Sources`, local `Settings`. Fluent, immutable API (`With`, `Without`, `BasedOn`, `WithSources`, `Rename`, `WithDescription`). |
| `SettingDefinition<TValue>` / `Settings` | Typed, stable description of a setting (`Settings.OutputResolution` is a `SettingDefinition<OutputResolution>`); the compiler rejects values of the wrong type. |
| `ProfileSettings` | Immutable set of **local** assignments keyed by `(SettingId, SettingScope)`. Absence means "inherit"; it is never a `null` or an `Auto`. |
| `SettingScope` / `VideoMode` | Global scope (default) or a scope restricted to an incoming video mode (`SettingScope.For(VideoMode.Pal576i)`). |
| `SettingCatalog` | Immutable, validated set of definitions for discovery and persistence; extend `Settings.Catalog` with your own typed definitions. |
| Value types (`RetroTink.Profiles.Values`) | Smart Enums (`VideoInput`, `OutputResolution`, `HdrMode`, …) and Value Objects (`AspectRatio`, `PictureFormat`). They know nothing about RT4 bytes or JSON names. |
| `ProfileResolver` → `ResolutionResult` → `ResolvedProfile` | Pure, deterministic resolution with diagnostics (missing source, cycle, duplicate id, conflicts) and provenance (`SettingProvenance`: who provided the value, through which source, what was overridden). |
| `ProfileJsonSerializer` | Native JSON schema v1, deterministic output, all-or-nothing reading with stable diagnostic codes and JSON paths. |

### Resolution rules

1. A profile's local assignment always beats its sources.
2. Sources are composed from the first declared to the last; **the last one wins**. Each source contributes its own *effective* configuration (its ancestors included).
3. **Profile first, scope second**: within one layer an assignment for the requested video mode beats the global one, but a child's global assignment beats a parent's video-mode assignment.
4. A lookup without video-mode context only considers global assignments. Absent settings stay absent; no value is ever invented.
5. Diamond inheritance (the same ancestor reached twice) is not a cycle. Sources that disagree produce a non-blocking `resolution.sourceConflict` warning, even when the profile overrides the setting locally; equal values are not reported.

### Error contract

- Programmer errors (invalid identifier passed to `From`, a scope the setting forbids, the same setting id used with two definitions) throw `ArgumentException`/`InvalidOperationException`.
- Expected business failures (resolution problems, invalid documents) are results with diagnostics carrying stable codes: see `ResolutionDiagnosticCodes` and `JsonDiagnosticCodes`.
- Untrusted input goes through `TryFrom`/`TryFromKey`.

## Building and testing

Requires the .NET SDK 10 (see `global.json`). Libraries multi-target `net8.0;net10.0`; tests run on `net10.0`.

```bash
dotnet build RetroTink.Profiles.slnx
dotnet test RetroTink.Profiles.slnx
```

Warnings are errors and the recommended .NET analyzers are enabled.

## Repository layout

```text
src/RetroTink.Profiles/          Core (namespaces RetroTink.Profiles, .Values, .Composition)
src/RetroTink.Profiles.Json/     JSON persistence (namespace RetroTink.Profiles.Json)
tests/RetroTink.Profiles.Tests/  Core unit and acceptance tests
tests/RetroTink.Profiles.Json.Tests/  JSON tests and the reference fixture (Fixtures/laserdisc-v1.json)
docs/                            Decision log and schema documentation
spécifications.md                Functional and technical specification (French)
```

## Documentation

- [`spécifications.md`](spécifications.md): functional and technical specification, in French.
- [`docs/decisions.md`](docs/decisions.md): implementation decision log, in French.
- [`docs/json-schema-v1.md`](docs/json-schema-v1.md): the native JSON schema, version 1.

## License

Not decided yet (see the specification, §14). The project must never be presented as an official RetroTINK product.
