# Native JSON schema, version 1

`RetroTink.Profiles.Json` persists **profile definitions**: identities, names, descriptions, ordered sources and local assignments. It never stores resolved values or diagnostics, which are recomputed. The file is the source of truth for the model; it is not a copy of an RT4 device profile.

## Document

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
        { "id": "output.hdr", "value": "off" },
        {
          "id": "processing.telecine",
          "scope": { "kind": "videoMode", "value": "pal576i" },
          "value": "cadence-22"
        }
      ]
    },
    {
      "id": "ld-pal-cinema",
      "name": "PAL CinemaScope",
      "sources": ["ld-pal"],
      "settings": [
        {
          "id": "geometry.pictureFormat",
          "value": { "kind": "letterboxed", "aspectRatio": { "numerator": 47, "denominator": 20 } }
        }
      ]
    }
  ]
}
```

| Property | Type | Rules |
| --- | --- | --- |
| `schemaVersion` | integer | Required. Must be `1`. Any other value is refused (`json.unsupportedSchemaVersion`); a future version is never interpreted as version 1. |
| `profiles` | array of profile | Required. Order is preserved. Identifiers must be unique in the document (case-insensitive). |
| profile `id` | string | Required. 1–64 ASCII letters, digits, `-`, `_`, `.`; starts and ends with a letter or digit; normalised to lowercase. |
| profile `name` | string | Required. 1–128 characters after trimming, no control characters. |
| profile `description` | string or null | Optional; omitted when absent. |
| profile `sources` | array of string | Optional (default empty). Ordered; the last source has precedence. No duplicates, no self-reference. Referenced profiles need not be in the same document: their existence is checked by the resolver. |
| profile `settings` | array of assignment | Optional (default empty). The pair `(id, scope)` must be unique within a profile. |
| assignment `id` | string | Required. Setting identifier declared in the catalog (`json.unknownSetting` otherwise). |
| assignment `scope` | object or null | Optional. Absent or `null` means global. Otherwise `{ "kind": "videoMode", "value": "<video mode key>" }`; the setting must allow video-mode scopes (`json.scopeNotAllowed`). |
| assignment `value` | depends on the setting | Required. See below. |

Unknown properties, duplicated properties, comments and trailing commas are errors. Reading is all-or-nothing: any error fails the document and every detected error is reported with a JSON path (for example `$.profiles[1].settings[0].value`).

## Value representations

| Value type | Representation | Examples |
| --- | --- | --- |
| Smart Enums (`VideoInput`, `VideoStandard`, `OutputResolution`, `HdrMode`, `Colorimetry`, `RgbRange`, `DeinterlacingMode`, `TelecineMode`, `ScalingMode`) | string: the member key | `"front-composite"`, `"1080p50"`, `"off"`, `"rec709"`, `"motion-adaptive"`, `"cadence-22"`, `"auto-fill"` |
| `AspectRatio` | object with strictly positive integers; written in reduced form | `{ "numerator": 4, "denominator": 3 }`, `{ "numerator": 47, "denominator": 20 }` |
| `PictureFormat` | object with a `kind` | `{ "kind": "full-frame" }`, `{ "kind": "letterboxed", "aspectRatio": { "numerator": 47, "denominator": 20 } }` |
| Video mode (scope value) | string: the member key | `"ntsc480i"`, `"pal576i"`, `"edtv480p"`, `"edtv576p"` |

Consumers can persist their own value types by registering an `IJsonSettingValueConverter<TValue>` through `ProfileJsonOptions.Converters`.

## Standard setting identifiers

| Identifier | Value type | Scopes | Kind |
| --- | --- | --- | --- |
| `input.source` | `VideoInput` | global | hardware |
| `decode.videoStandard` | `VideoStandard` | global | hardware |
| `output.resolution` | `OutputResolution` | global | hardware |
| `output.hdr` | `HdrMode` | global | hardware |
| `output.colorimetry` | `Colorimetry` | global | hardware |
| `output.rgbRange` | `RgbRange` | global | hardware |
| `processing.deinterlacing` | `DeinterlacingMode` | global or video mode | hardware |
| `processing.telecine` | `TelecineMode` | global or video mode | hardware |
| `scaling.mode` | `ScalingMode` | global or video mode | hardware |
| `geometry.aspectRatio` | `AspectRatio` | global or video mode | hardware |
| `geometry.pictureFormat` | `PictureFormat` | global or video mode | intent |

"Hardware" means a concrete device setting whose encoding an adapter may map once confirmed; "intent" means a high-level intention an adapter must translate or refuse explicitly. Neither implies that an RT4 encoding is known today.

## Writing

Output is UTF-8 without BOM, indented with two spaces (or compact with `WriteIndented = false`), non-ASCII characters kept as is, terminated by a line feed. Property order is fixed, profiles and sources keep their order, assignments keep their insertion order. The same model always produces the same bytes, which keeps Git diffs readable.

## Diagnostic codes

| Code | Meaning |
| --- | --- |
| `json.malformed` | Not well-formed JSON (or too deeply nested, or invalid UTF-8). |
| `json.documentTooLarge` | Input exceeds `ProfileJsonOptions.MaxDocumentSizeInBytes` (16 MiB by default). |
| `json.invalidDocument` | Wrong JSON type or missing required property. |
| `json.unknownProperty` | Property not part of the schema. |
| `json.duplicateProperty` | Property repeated in one object. |
| `json.unsupportedSchemaVersion` | Missing, non-integer or unsupported `schemaVersion`. |
| `json.invalidProfileId` / `json.invalidProfileName` | Identifier or name outside the rules above. |
| `json.duplicateProfileId` | Identifier repeated in the document. |
| `json.invalidSources` | Duplicated source or self-reference. |
| `json.invalidSettingId` / `json.unknownSetting` | Malformed identifier, or identifier absent from the catalog. |
| `json.invalidScope` / `json.scopeNotAllowed` | Malformed scope or unknown video mode / scope forbidden by the setting. |
| `json.duplicateSetting` | Same setting and scope assigned twice in one profile. |
| `json.invalidValue` | Value not convertible to the setting's value type. |

## Versioning

`schemaVersion` is mandatory. When the schema changes incompatibly, the version is incremented and an explicit, tested migration is added to the JSON assembly; migrations must preserve the distinction between an absent assignment and explicit `auto`/`off` values.
