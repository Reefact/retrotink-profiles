using System.Diagnostics.CodeAnalysis;
using System.Text;
using RetroTink.Profiles.Composition;
using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Json.Tests;

public class ProfileJsonSerializerTests
{
    private readonly ProfileJsonSerializer _serializer = new(Settings.Catalog);

    [Fact]
    public void Round_trip_preserves_identities_names_descriptions_sources_scopes_and_values()
    {
        var original = TestLibrary.Profiles();

        var json = _serializer.Serialize(original);
        var result = _serializer.Deserialize(json);

        Assert.True(result.IsSuccess, string.Join(" | ", result.Diagnostics));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(original.Count, result.Profiles.Count);
        for (var i = 0; i < original.Count; i++)
        {
            Assert.True(original[i].ContentEquals(result.Profiles[i]), $"profile {original[i].Id} differs after round trip");
        }

        var pal = result.Profiles[0];
        Assert.Equal(TelecineMode.Cadence22, pal.Settings.Get(Settings.Telecine, SettingScope.For(VideoMode.Pal576i)));
        Assert.False(pal.Settings.Contains(Settings.Telecine));
        Assert.Equal("Common settings for PAL LaserDiscs", pal.Description);
    }

    [Fact]
    public void The_fixture_document_reads_and_writes_back_identically()
    {
        var fixture = TestLibrary.Fixture("laserdisc-v1.json");

        var result = _serializer.Deserialize(fixture);

        Assert.True(result.IsSuccess, string.Join(" | ", result.Diagnostics));
        Assert.Equal(fixture.ReplaceLineEndings("\n"), _serializer.Serialize(result.Profiles));
        Assert.Equal(fixture.ReplaceLineEndings("\n"), _serializer.Serialize(TestLibrary.Profiles()));
    }

    [Fact]
    public void Serialization_is_deterministic_and_idempotent()
    {
        var first = _serializer.Serialize(TestLibrary.Profiles());
        var second = _serializer.Serialize(TestLibrary.Profiles());
        var reserialized = _serializer.Serialize(_serializer.Deserialize(first).Profiles);

        Assert.Equal(first, second);
        Assert.Equal(first, reserialized);
    }

    [Fact]
    public void Output_uses_a_fixed_property_order_and_omits_the_description_when_absent()
    {
        var profile = ProfileDefinition.Create(ProfileId.From("p"), ProfileName.From("P"))
            .With(Settings.ScalingMode, ScalingMode.AutoFill, SettingScope.For(VideoMode.Pal576i))
            .With(Settings.Hdr, HdrMode.Off);

        var json = _serializer.Serialize([profile]);

        const string expected = """
            {
              "schemaVersion": 1,
              "profiles": [
                {
                  "id": "p",
                  "name": "P",
                  "sources": [],
                  "settings": [
                    {
                      "id": "scaling.mode",
                      "scope": {
                        "kind": "videoMode",
                        "value": "pal576i"
                      },
                      "value": "auto-fill"
                    },
                    {
                      "id": "output.hdr",
                      "value": "off"
                    }
                  ]
                }
              ]
            }

            """;
        Assert.Equal(expected.ReplaceLineEndings("\n"), json);
    }

    [Fact]
    public void Non_ascii_text_is_written_as_is_and_the_document_ends_with_a_line_feed()
    {
        var profile = ProfileDefinition.Create(ProfileId.From("p"), ProfileName.From("Cinémascope — « test »"));

        var json = _serializer.Serialize([profile]);

        Assert.Contains("\"name\": \"Cinémascope — « test »\"", json, StringComparison.Ordinal);
        Assert.EndsWith("}\n", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u", json, StringComparison.Ordinal);
        Assert.Equal(json, Encoding.UTF8.GetString(_serializer.SerializeToUtf8Bytes([profile])));
        Assert.True(_serializer.Deserialize(json).Profiles[0].ContentEquals(profile));
    }

    [Fact]
    public void Compact_output_is_available_through_options()
    {
        var compact = new ProfileJsonSerializer(Settings.Catalog, new ProfileJsonOptions { WriteIndented = false });

        var json = compact.Serialize(TestLibrary.Profiles());

        Assert.DoesNotContain("\n  ", json, StringComparison.Ordinal);
        Assert.True(compact.Deserialize(json).IsSuccess);
    }

    [Fact]
    public async Task Streams_and_memory_overloads_round_trip()
    {
        var original = TestLibrary.Profiles();
        using var stream = new MemoryStream();

        await _serializer.SerializeAsync(stream, original);
        stream.Position = 0;
        var fromStream = await _serializer.DeserializeAsync(stream);

        using var syncStream = new MemoryStream();
        _serializer.Serialize(syncStream, original);
        syncStream.Position = 0;
        var fromSyncStream = _serializer.Deserialize(syncStream);
        var fromMemory = _serializer.Deserialize(new ReadOnlyMemory<byte>(_serializer.SerializeToUtf8Bytes(original)));

        foreach (var result in new[] { fromStream, fromSyncStream, fromMemory })
        {
            Assert.True(result.IsSuccess, string.Join(" | ", result.Diagnostics));
            Assert.Equal(original.Select(p => p.Id), result.Profiles.Select(p => p.Id));
        }
    }

    [Fact]
    public void Missing_sources_are_not_a_parsing_error_but_a_resolution_error()
    {
        const string json = """
            { "schemaVersion": 1, "profiles": [ { "id": "child", "name": "Child", "sources": ["absent"], "settings": [] } ] }
            """;

        var read = _serializer.Deserialize(json);

        Assert.True(read.IsSuccess);
        var resolution = new ProfileResolver(read.Profiles).Resolve(ProfileId.From("child"));
        Assert.False(resolution.IsSuccess);
        Assert.Equal(ResolutionDiagnosticCodes.MissingSource, Assert.Single(resolution.Diagnostics).Code);
    }

    [Fact]
    public void Serializing_profiles_with_duplicated_ids_is_refused()
    {
        var a = ProfileDefinition.Create(ProfileId.From("p"), ProfileName.From("A"));
        var b = ProfileDefinition.Create(ProfileId.From("P"), ProfileName.From("B"));

        Assert.Throws<ArgumentException>(() => _serializer.Serialize([a, b]));
    }

    [Fact]
    public void Custom_value_types_need_a_converter_and_can_register_one()
    {
        var definition = new SettingDefinition<Percentage>(SettingId.From("custom.percentage"), "Percentage");
        var catalog = Settings.Catalog.With(definition);
        var profile = ProfileDefinition.Create(ProfileId.From("p"), ProfileName.From("P")).With(definition, new Percentage(42));

        var withoutConverter = new ProfileJsonSerializer(catalog);
        Assert.Throws<InvalidOperationException>(() => withoutConverter.Serialize([profile]));

        var options = new ProfileJsonOptions { Converters = JsonSettingValueConverters.Default.With(new PercentageConverter()) };
        var withConverter = new ProfileJsonSerializer(catalog, options);
        var json = withConverter.Serialize([profile]);
        var result = withConverter.Deserialize(json);

        Assert.Contains("\"value\": 42", json, StringComparison.Ordinal);
        Assert.True(result.IsSuccess, string.Join(" | ", result.Diagnostics));
        Assert.Equal(new Percentage(42), result.Profiles[0].Settings.Get(definition));
        Assert.False(JsonSettingValueConverters.Default.Contains(typeof(Percentage)));
        Assert.True(options.Converters.TryGet<Percentage>(out _));
    }

    [Fact]
    public void Modifying_a_parent_through_json_benefits_descendants_on_their_next_resolution()
    {
        var library = TestLibrary.Profiles();
        var stored = _serializer.Serialize(library);
        var before = new ProfileResolver(_serializer.Deserialize(stored).Profiles).Resolve(TestLibrary.LdPalCinema).Profile;

        var reloaded = _serializer.Deserialize(stored).Profiles;
        var updated = reloaded.Select(p => p.Id.Equals(TestLibrary.LdPal) ? p.With(Settings.OutputResolution, OutputResolution.Uhd50) : p).ToList();
        var restored = _serializer.Serialize(updated);
        var after = new ProfileResolver(_serializer.Deserialize(restored).Profiles).Resolve(TestLibrary.LdPalCinema).Profile;

        Assert.Equal(OutputResolution.FullHd50, before.Settings.Get(Settings.OutputResolution));
        Assert.Equal(OutputResolution.Uhd50, after.Settings.Get(Settings.OutputResolution));
        Assert.Equal(PictureFormat.Letterboxed(AspectRatio.From(235, 100)), after.Settings.Get(Settings.PictureFormat));
        Assert.Contains("\"2160p50\"", restored, StringComparison.Ordinal);
        Assert.DoesNotContain("\"2160p50\"", stored, StringComparison.Ordinal);
    }

    private readonly record struct Percentage(int Value);

    private sealed class PercentageConverter : IJsonSettingValueConverter<Percentage>
    {
        public void Write(System.Text.Json.Utf8JsonWriter writer, Percentage value) => writer.WriteNumberValue(value.Value);

        public bool TryRead(System.Text.Json.JsonElement element, [MaybeNullWhen(false)] out Percentage value, [NotNullWhen(false)] out string? error)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number && element.TryGetInt32(out var number) && number is >= 0 and <= 100)
            {
                value = new Percentage(number);
                error = null;
                return true;
            }

            value = default;
            error = "Expected an integer between 0 and 100.";
            return false;
        }
    }
}
