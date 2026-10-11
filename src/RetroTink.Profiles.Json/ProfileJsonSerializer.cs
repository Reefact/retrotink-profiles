using System.Buffers;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Json;

/// <summary>
/// Reads and writes sets of <see cref="ProfileDefinition"/>s in the native JSON format (schema version 1), in memory or
/// over streams. Never touches the file system: where bytes live is the host application's concern.
/// </summary>
/// <remarks>
/// <para>Writing is deterministic: properties are emitted in a fixed order, profiles and sources in their given order,
/// assignments in their insertion order. Output is UTF-8 without BOM, indented by default, non-ASCII text kept as is,
/// and terminated by a line feed.</para>
/// <para>Reading validates structure and values against the catalog and reports every error with a JSON path; see
/// <see cref="DeserializationResult"/>. Unknown properties and unknown settings are errors in schema version 1.</para>
/// </remarks>
public sealed class ProfileJsonSerializer
{
    private static readonly byte[] LineFeed = [(byte)'\n'];

    private readonly SettingCatalog _catalog;
    private readonly ProfileJsonOptions _options;
    private readonly JsonWriterOptions _writerOptions;
    private readonly JsonDocumentOptions _documentOptions;

    /// <summary>Creates a serializer resolving setting identifiers against <paramref name="catalog"/>.</summary>
    public ProfileJsonSerializer(SettingCatalog catalog, ProfileJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        _options = options ?? ProfileJsonOptions.Default;
        _writerOptions = new JsonWriterOptions
        {
            Indented = _options.WriteIndented,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        _documentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = _options.MaxDepth,
        };
    }

    /// <summary>The catalog used to resolve setting identifiers.</summary>
    public SettingCatalog Catalog => _catalog;

    /// <summary>The options in use.</summary>
    public ProfileJsonOptions Options => _options;

    /// <summary>Writes <paramref name="profiles"/> as a JSON document.</summary>
    /// <exception cref="ArgumentException">Two profiles share an identifier.</exception>
    /// <exception cref="InvalidOperationException">A value type has no registered converter.</exception>
    public string Serialize(IEnumerable<ProfileDefinition> profiles) => Encoding.UTF8.GetString(SerializeToUtf8Bytes(profiles));

    /// <summary>Writes <paramref name="profiles"/> as a UTF-8 JSON document.</summary>
    /// <exception cref="ArgumentException">Two profiles share an identifier.</exception>
    /// <exception cref="InvalidOperationException">A value type has no registered converter.</exception>
    public byte[] SerializeToUtf8Bytes(IEnumerable<ProfileDefinition> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, _writerOptions))
        {
            WriteDocument(writer, profiles);
        }

        buffer.Write(LineFeed);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Writes <paramref name="profiles"/> as a UTF-8 JSON document to <paramref name="utf8Json"/>.</summary>
    /// <exception cref="ArgumentException">Two profiles share an identifier.</exception>
    /// <exception cref="InvalidOperationException">A value type has no registered converter.</exception>
    public void Serialize(Stream utf8Json, IEnumerable<ProfileDefinition> profiles)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        ArgumentNullException.ThrowIfNull(profiles);
        var bytes = SerializeToUtf8Bytes(profiles);
        utf8Json.Write(bytes, 0, bytes.Length);
        utf8Json.Flush();
    }

    /// <summary>Writes <paramref name="profiles"/> as a UTF-8 JSON document to <paramref name="utf8Json"/>.</summary>
    /// <exception cref="ArgumentException">Two profiles share an identifier.</exception>
    /// <exception cref="InvalidOperationException">A value type has no registered converter.</exception>
    public async Task SerializeAsync(Stream utf8Json, IEnumerable<ProfileDefinition> profiles, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        ArgumentNullException.ThrowIfNull(profiles);
        var bytes = SerializeToUtf8Bytes(profiles);
        await utf8Json.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await utf8Json.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a JSON document given as text.</summary>
    public DeserializationResult Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > _options.MaxDocumentSizeInBytes)
        {
            return TooLarge();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, _documentOptions);
        }
        catch (JsonException exception)
        {
            return Malformed(exception);
        }

        using (document)
        {
            return new Reader(_catalog, _options.Converters).Read(document.RootElement);
        }
    }

    /// <summary>Reads a UTF-8 JSON document held in memory.</summary>
    public DeserializationResult Deserialize(ReadOnlyMemory<byte> utf8Json)
    {
        if (utf8Json.Length > _options.MaxDocumentSizeInBytes)
        {
            return TooLarge();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8Json, _documentOptions);
        }
        catch (JsonException exception)
        {
            return Malformed(exception);
        }

        using (document)
        {
            return new Reader(_catalog, _options.Converters).Read(document.RootElement);
        }
    }

    /// <summary>Reads a UTF-8 JSON document from <paramref name="utf8Json"/>, reading at most the configured size.</summary>
    public DeserializationResult Deserialize(Stream utf8Json)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        var limit = _options.MaxDocumentSizeInBytes;
        if (utf8Json.CanSeek && utf8Json.Length - utf8Json.Position > limit)
        {
            return TooLarge();
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[Math.Min(81920, limit + 1)];
        int read;
        while ((read = utf8Json.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > limit)
            {
                return TooLarge();
            }

            buffer.Write(chunk, 0, read);
        }

        return Deserialize(new ReadOnlyMemory<byte>(buffer.GetBuffer(), 0, (int)buffer.Length));
    }

    /// <summary>Reads a UTF-8 JSON document from <paramref name="utf8Json"/>, reading at most the configured size.</summary>
    public async Task<DeserializationResult> DeserializeAsync(Stream utf8Json, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        var limit = _options.MaxDocumentSizeInBytes;
        if (utf8Json.CanSeek && utf8Json.Length - utf8Json.Position > limit)
        {
            return TooLarge();
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[Math.Min(81920, limit + 1)];
        int read;
        while ((read = await utf8Json.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > limit)
            {
                return TooLarge();
            }

            buffer.Write(chunk, 0, read);
        }

        return Deserialize(new ReadOnlyMemory<byte>(buffer.GetBuffer(), 0, (int)buffer.Length));
    }

    private DeserializationResult TooLarge() =>
        new(null, [new JsonDiagnostic(JsonDiagnosticCodes.DocumentTooLarge, DiagnosticSeverity.Error,
            string.Create(CultureInfo.InvariantCulture, $"The document exceeds the maximum accepted size of {_options.MaxDocumentSizeInBytes} bytes."), "$", null, null)]);

    private static DeserializationResult Malformed(JsonException exception)
    {
        var position = exception.LineNumber is null
            ? string.Empty
            : string.Create(CultureInfo.InvariantCulture, $" (line {exception.LineNumber + 1}, position {exception.BytePositionInLine + 1})");
        return new DeserializationResult(null, [new JsonDiagnostic(JsonDiagnosticCodes.Malformed, DiagnosticSeverity.Error,
            $"The input is not well-formed JSON{position}: {exception.Message}", exception.Path ?? "$", null, null)]);
    }

    private void WriteDocument(Utf8JsonWriter writer, IEnumerable<ProfileDefinition> profiles)
    {
        var valueWriter = new ValueWriter(writer, _options.Converters);
        var ids = new HashSet<ProfileId>();

        writer.WriteStartObject();
        writer.WriteNumber(ProfileJsonSchema.SchemaVersion, ProfileJsonSchema.CurrentVersion);
        writer.WriteStartArray(ProfileJsonSchema.Profiles);
        foreach (var profile in profiles)
        {
            ArgumentNullException.ThrowIfNull(profile, nameof(profiles));
            if (!ids.Add(profile.Id))
            {
                throw new ArgumentException($"Two profiles share the id '{profile.Id}'; a document cannot be read back with duplicated identifiers.", nameof(profiles));
            }

            writer.WriteStartObject();
            writer.WriteString(ProfileJsonSchema.Id, profile.Id.Value);
            writer.WriteString(ProfileJsonSchema.Name, profile.Name.Value);
            if (profile.Description is not null)
            {
                writer.WriteString(ProfileJsonSchema.Description, profile.Description);
            }

            writer.WriteStartArray(ProfileJsonSchema.Sources);
            foreach (var source in profile.Sources)
            {
                writer.WriteStringValue(source.Value);
            }

            writer.WriteEndArray();

            writer.WriteStartArray(ProfileJsonSchema.Settings);
            foreach (var assignment in profile.Settings)
            {
                writer.WriteStartObject();
                writer.WriteString(ProfileJsonSchema.Id, assignment.SettingId.Value);
                if (!assignment.Scope.IsGlobal)
                {
                    writer.WriteStartObject(ProfileJsonSchema.Scope);
                    writer.WriteString(ProfileJsonSchema.ScopeKind, ProfileJsonSchema.VideoModeScopeKind);
                    writer.WriteString(ProfileJsonSchema.ScopeValue, assignment.Scope.VideoMode!.Key);
                    writer.WriteEndObject();
                }

                writer.WritePropertyName(ProfileJsonSchema.Value);
                assignment.Accept(valueWriter);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>Writes a value through the converter of its static type.</summary>
    private sealed class ValueWriter(Utf8JsonWriter writer, JsonSettingValueConverters converters) : ISettingAssignmentVisitor<bool>
    {
        public bool Visit<TValue>(SettingAssignment<TValue> assignment)
            where TValue : notnull, IEquatable<TValue>
        {
            converters.Get<TValue>().Write(writer, assignment.Value);
            return true;
        }
    }

    /// <summary>Reads a value through the converter of the definition's static type.</summary>
    private sealed class ValueReader(JsonElement element, JsonSettingValueConverters converters, SettingScope scope) : ISettingDefinitionVisitor<ValueReader.Outcome>
    {
        public Outcome Visit<TValue>(SettingDefinition<TValue> definition)
            where TValue : notnull, IEquatable<TValue>
        {
            var converter = converters.Get<TValue>();
            return converter.TryRead(element, out var value, out var error)
                ? new Outcome(new SettingAssignment<TValue>(definition, value, scope), null)
                : new Outcome(null, error);
        }

        internal readonly record struct Outcome(SettingAssignment? Assignment, string? Error);
    }

    private sealed class Reader(SettingCatalog catalog, JsonSettingValueConverters converters)
    {
        private readonly List<JsonDiagnostic> _diagnostics = [];

        public DeserializationResult Read(JsonElement root)
        {
            var profiles = ReadRoot(root);
            var failed = profiles is null || _diagnostics.Exists(d => d.Severity == DiagnosticSeverity.Error);
            return new DeserializationResult(failed ? null : profiles, [.. _diagnostics]);
        }

        private ImmutableArray<ProfileDefinition>? ReadRoot(JsonElement root)
        {
            const string path = "$";
            if (root.ValueKind != JsonValueKind.Object)
            {
                Error(JsonDiagnosticCodes.InvalidDocument, path, $"The document must be an object, got {root.ValueKind}.");
                return null;
            }

            JsonElement? version = null;
            JsonElement? profiles = null;
            foreach (var property in Properties(root, path))
            {
                switch (property.Name)
                {
                    case ProfileJsonSchema.SchemaVersion:
                        version = property.Value;
                        break;
                    case ProfileJsonSchema.Profiles:
                        profiles = property.Value;
                        break;
                    default:
                        UnknownProperty(path, property.Name);
                        break;
                }
            }

            if (version is null)
            {
                Error(JsonDiagnosticCodes.UnsupportedSchemaVersion, $"{path}.{ProfileJsonSchema.SchemaVersion}", $"'{ProfileJsonSchema.SchemaVersion}' is required.");
                return null;
            }

            if (version.Value.ValueKind != JsonValueKind.Number || !version.Value.TryGetInt32(out var schemaVersion))
            {
                Error(JsonDiagnosticCodes.UnsupportedSchemaVersion, $"{path}.{ProfileJsonSchema.SchemaVersion}", $"'{ProfileJsonSchema.SchemaVersion}' must be an integer.");
                return null;
            }

            if (schemaVersion != ProfileJsonSchema.CurrentVersion)
            {
                Error(JsonDiagnosticCodes.UnsupportedSchemaVersion, $"{path}.{ProfileJsonSchema.SchemaVersion}",
                    string.Create(CultureInfo.InvariantCulture, $"Schema version {schemaVersion} is not supported; this build reads version {ProfileJsonSchema.CurrentVersion}."));
                return null;
            }

            if (profiles is null)
            {
                Error(JsonDiagnosticCodes.InvalidDocument, $"{path}.{ProfileJsonSchema.Profiles}", $"'{ProfileJsonSchema.Profiles}' is required.");
                return null;
            }

            if (profiles.Value.ValueKind != JsonValueKind.Array)
            {
                Error(JsonDiagnosticCodes.InvalidDocument, $"{path}.{ProfileJsonSchema.Profiles}", $"'{ProfileJsonSchema.Profiles}' must be an array, got {profiles.Value.ValueKind}.");
                return null;
            }

            var result = ImmutableArray.CreateBuilder<ProfileDefinition>();
            var ids = new HashSet<ProfileId>();
            var index = 0;
            foreach (var element in profiles.Value.EnumerateArray())
            {
                var definition = ReadProfile(element, string.Create(CultureInfo.InvariantCulture, $"{path}.{ProfileJsonSchema.Profiles}[{index}]"), ids);
                if (definition is not null)
                {
                    result.Add(definition);
                }

                index++;
            }

            return result.ToImmutable();
        }

        private ProfileDefinition? ReadProfile(JsonElement element, string path, HashSet<ProfileId> ids)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                Error(JsonDiagnosticCodes.InvalidDocument, path, $"A profile must be an object, got {element.ValueKind}.");
                return null;
            }

            var errorsBefore = _diagnostics.Count;
            string? idText = null;
            string? nameText = null;
            string? description = null;
            JsonElement? sources = null;
            JsonElement? settings = null;

            foreach (var property in Properties(element, path))
            {
                var propertyPath = $"{path}.{property.Name}";
                switch (property.Name)
                {
                    case ProfileJsonSchema.Id:
                        idText = ExpectString(property.Value, propertyPath, null);
                        break;
                    case ProfileJsonSchema.Name:
                        nameText = ExpectString(property.Value, propertyPath, idText);
                        break;
                    case ProfileJsonSchema.Description:
                        if (property.Value.ValueKind != JsonValueKind.Null)
                        {
                            description = ExpectString(property.Value, propertyPath, idText);
                        }

                        break;
                    case ProfileJsonSchema.Sources:
                    case ProfileJsonSchema.Settings:
                        if (property.Value.ValueKind != JsonValueKind.Array)
                        {
                            Error(JsonDiagnosticCodes.InvalidDocument, propertyPath, $"'{property.Name}' must be an array, got {property.Value.ValueKind}.", idText);
                        }
                        else if (property.Name == ProfileJsonSchema.Sources)
                        {
                            sources = property.Value;
                        }
                        else
                        {
                            settings = property.Value;
                        }

                        break;
                    default:
                        UnknownProperty(path, property.Name, idText);
                        break;
                }
            }

            ProfileId? id = null;
            if (idText is null)
            {
                if (!_diagnostics.Exists(d => d.Path == $"{path}.{ProfileJsonSchema.Id}"))
                {
                    Error(JsonDiagnosticCodes.InvalidDocument, $"{path}.{ProfileJsonSchema.Id}", $"'{ProfileJsonSchema.Id}' is required.");
                }
            }
            else if (!ProfileId.TryFrom(idText, out id))
            {
                Error(JsonDiagnosticCodes.InvalidProfileId, $"{path}.{ProfileJsonSchema.Id}", $"'{idText}' is not a valid profile id.", idText);
            }
            else if (!ids.Add(id))
            {
                Error(JsonDiagnosticCodes.DuplicateProfileId, $"{path}.{ProfileJsonSchema.Id}", $"Profile id '{id}' is declared more than once in the document.", idText);
            }

            ProfileName? name = null;
            if (nameText is null)
            {
                if (!_diagnostics.Exists(d => d.Path == $"{path}.{ProfileJsonSchema.Name}"))
                {
                    Error(JsonDiagnosticCodes.InvalidDocument, $"{path}.{ProfileJsonSchema.Name}", $"'{ProfileJsonSchema.Name}' is required.", idText);
                }
            }
            else if (!ProfileName.TryFrom(nameText, out name))
            {
                Error(JsonDiagnosticCodes.InvalidProfileName, $"{path}.{ProfileJsonSchema.Name}", $"'{nameText}' is not a valid profile name.", idText);
            }

            var sourceIds = sources is null ? [] : ReadSources(sources.Value, $"{path}.{ProfileJsonSchema.Sources}", id, idText);
            var assignments = settings is null ? [] : ReadSettings(settings.Value, $"{path}.{ProfileJsonSchema.Settings}", idText);

            if (_diagnostics.Count != errorsBefore || id is null || name is null)
            {
                return null;
            }

            return ProfileDefinition.Create(id, name, description, sourceIds, ProfileSettings.From(assignments));
        }

        private ImmutableArray<ProfileId> ReadSources(JsonElement array, string path, ProfileId? self, string? profileId)
        {
            var result = ImmutableArray.CreateBuilder<ProfileId>();
            var index = 0;
            foreach (var element in array.EnumerateArray())
            {
                var itemPath = string.Create(CultureInfo.InvariantCulture, $"{path}[{index++}]");
                var text = ExpectString(element, itemPath, profileId);
                if (text is null)
                {
                    continue;
                }

                if (!ProfileId.TryFrom(text, out var source))
                {
                    Error(JsonDiagnosticCodes.InvalidProfileId, itemPath, $"'{text}' is not a valid profile id.", profileId);
                }
                else if (self is not null && source.Equals(self))
                {
                    Error(JsonDiagnosticCodes.InvalidSources, itemPath, $"Profile '{self}' lists itself as a source.", profileId);
                }
                else if (result.Contains(source))
                {
                    Error(JsonDiagnosticCodes.InvalidSources, itemPath, $"Source '{source}' is listed more than once.", profileId);
                }
                else
                {
                    result.Add(source);
                }
            }

            return result.ToImmutable();
        }

        private ImmutableArray<SettingAssignment> ReadSettings(JsonElement array, string path, string? profileId)
        {
            var result = ImmutableArray.CreateBuilder<SettingAssignment>();
            var keys = new HashSet<(SettingId, SettingScope)>();
            var index = 0;
            foreach (var element in array.EnumerateArray())
            {
                var itemPath = string.Create(CultureInfo.InvariantCulture, $"{path}[{index++}]");
                var assignment = ReadAssignment(element, itemPath, profileId);
                if (assignment is null)
                {
                    continue;
                }

                if (!keys.Add((assignment.SettingId, assignment.Scope)))
                {
                    Error(JsonDiagnosticCodes.DuplicateSetting, itemPath, $"Setting '{assignment.SettingId}' is assigned more than once in scope '{assignment.Scope}'.", profileId, assignment.SettingId.Value);
                    continue;
                }

                result.Add(assignment);
            }

            return result.ToImmutable();
        }

        private SettingAssignment? ReadAssignment(JsonElement element, string path, string? profileId)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                Error(JsonDiagnosticCodes.InvalidDocument, path, $"An assignment must be an object, got {element.ValueKind}.", profileId);
                return null;
            }

            string? idText = null;
            JsonElement? scopeElement = null;
            JsonElement? valueElement = null;
            var hasScope = false;
            foreach (var property in Properties(element, path, profileId))
            {
                switch (property.Name)
                {
                    case ProfileJsonSchema.Id:
                        idText = ExpectString(property.Value, $"{path}.{ProfileJsonSchema.Id}", profileId);
                        break;
                    case ProfileJsonSchema.Scope:
                        hasScope = true;
                        scopeElement = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value;
                        break;
                    case ProfileJsonSchema.Value:
                        valueElement = property.Value;
                        break;
                    default:
                        UnknownProperty(path, property.Name, profileId);
                        break;
                }
            }

            if (idText is null)
            {
                if (!_diagnostics.Exists(d => d.Path == $"{path}.{ProfileJsonSchema.Id}"))
                {
                    Error(JsonDiagnosticCodes.InvalidDocument, $"{path}.{ProfileJsonSchema.Id}", $"'{ProfileJsonSchema.Id}' is required.", profileId);
                }

                return null;
            }

            if (!SettingId.TryFrom(idText, out var settingId))
            {
                Error(JsonDiagnosticCodes.InvalidSettingId, $"{path}.{ProfileJsonSchema.Id}", $"'{idText}' is not a valid setting id.", profileId, idText);
                return null;
            }

            if (!catalog.TryGet(settingId, out var definition))
            {
                Error(JsonDiagnosticCodes.UnknownSetting, $"{path}.{ProfileJsonSchema.Id}", $"Setting '{settingId}' is not declared in the catalog.", profileId, idText);
                return null;
            }

            var scope = SettingScope.Global;
            if (hasScope && scopeElement is not null && !TryReadScope(scopeElement.Value, $"{path}.{ProfileJsonSchema.Scope}", profileId, idText, out scope))
            {
                return null;
            }

            if (!definition.AllowsScope(scope))
            {
                Error(JsonDiagnosticCodes.ScopeNotAllowed, $"{path}.{ProfileJsonSchema.Scope}", $"Scope '{scope}' is not admissible for setting '{settingId}' (policy: {definition.ScopePolicy}).", profileId, idText);
                return null;
            }

            if (valueElement is null)
            {
                Error(JsonDiagnosticCodes.InvalidDocument, $"{path}.{ProfileJsonSchema.Value}", $"'{ProfileJsonSchema.Value}' is required.", profileId, idText);
                return null;
            }

            var outcome = definition.Accept(new ValueReader(valueElement.Value, converters, scope));
            if (outcome.Assignment is null)
            {
                Error(JsonDiagnosticCodes.InvalidValue, $"{path}.{ProfileJsonSchema.Value}", $"Invalid value for setting '{settingId}': {outcome.Error}", profileId, idText);
                return null;
            }

            return outcome.Assignment;
        }

        private bool TryReadScope(JsonElement element, string path, string? profileId, string? settingId, out SettingScope scope)
        {
            scope = SettingScope.Global;
            if (element.ValueKind != JsonValueKind.Object)
            {
                Error(JsonDiagnosticCodes.InvalidScope, path, $"A scope must be an object with '{ProfileJsonSchema.ScopeKind}' and '{ProfileJsonSchema.ScopeValue}', got {element.ValueKind}.", profileId, settingId);
                return false;
            }

            string? kind = null;
            string? value = null;
            foreach (var property in Properties(element, path, profileId))
            {
                switch (property.Name)
                {
                    case ProfileJsonSchema.ScopeKind:
                        kind = ExpectString(property.Value, $"{path}.{ProfileJsonSchema.ScopeKind}", profileId);
                        break;
                    case ProfileJsonSchema.ScopeValue:
                        value = ExpectString(property.Value, $"{path}.{ProfileJsonSchema.ScopeValue}", profileId);
                        break;
                    default:
                        UnknownProperty(path, property.Name, profileId);
                        break;
                }
            }

            if (kind is null || value is null)
            {
                if (!_diagnostics.Exists(d => d.Path.StartsWith(path, StringComparison.Ordinal)))
                {
                    Error(JsonDiagnosticCodes.InvalidScope, path, $"A scope requires both '{ProfileJsonSchema.ScopeKind}' and '{ProfileJsonSchema.ScopeValue}'.", profileId, settingId);
                }

                return false;
            }

            if (kind != ProfileJsonSchema.VideoModeScopeKind)
            {
                Error(JsonDiagnosticCodes.InvalidScope, $"{path}.{ProfileJsonSchema.ScopeKind}", $"'{kind}' is not a known scope kind; expected '{ProfileJsonSchema.VideoModeScopeKind}'.", profileId, settingId);
                return false;
            }

            if (!VideoMode.TryFromKey(value, out var mode))
            {
                Error(JsonDiagnosticCodes.InvalidScope, $"{path}.{ProfileJsonSchema.ScopeValue}", $"'{value}' is not a known video mode; expected one of: {string.Join(", ", VideoMode.All.Select(m => m.Key))}.", profileId, settingId);
                return false;
            }

            scope = SettingScope.For(mode);
            return true;
        }

        /// <summary>Enumerates the properties of an object, reporting duplicated names (all occurrences are still yielded).</summary>
        private IEnumerable<JsonProperty> Properties(JsonElement element, string path, string? profileId = null)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    Error(JsonDiagnosticCodes.DuplicateProperty, $"{path}.{property.Name}", $"Property '{property.Name}' appears more than once.", profileId);
                }

                yield return property;
            }
        }

        private string? ExpectString(JsonElement element, string path, string? profileId)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            Error(JsonDiagnosticCodes.InvalidDocument, path, $"Expected a string, got {element.ValueKind}.", profileId);
            return null;
        }

        private void UnknownProperty(string path, string name, string? profileId = null)
            => Error(JsonDiagnosticCodes.UnknownProperty, $"{path}.{name}", $"Property '{name}' is not part of schema version {ProfileJsonSchema.CurrentVersion}.", profileId);

        private void Error(string code, string path, string message, string? profileId = null, string? settingId = null)
            => _diagnostics.Add(new JsonDiagnostic(code, DiagnosticSeverity.Error, message, path, profileId, settingId));
    }
}
