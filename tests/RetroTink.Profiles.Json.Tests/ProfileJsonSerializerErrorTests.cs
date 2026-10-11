using Xunit;

namespace RetroTink.Profiles.Json.Tests;

public class ProfileJsonSerializerErrorTests
{
    private readonly ProfileJsonSerializer _serializer = new(Settings.Catalog);

    private static string Document(string profilesJson) => $$"""{ "schemaVersion": 1, "profiles": [ {{profilesJson}} ] }""";

    private static string Profile(string settingsJson, string id = "p") => $$"""{ "id": "{{id}}", "name": "P", "sources": [], "settings": [ {{settingsJson}} ] }""";

    private JsonDiagnostic Single(string json, string expectedCode)
    {
        var result = _serializer.Deserialize(json);

        Assert.False(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Profiles);
        Assert.False(result.TryGetProfiles(out _));
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        return diagnostic;
    }

    [Fact]
    public void Malformed_json_is_reported_with_its_position()
    {
        var diagnostic = Single("{ \"schemaVersion\": 1, ", JsonDiagnosticCodes.Malformed);

        Assert.Contains("line 1", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Root_must_be_an_object()
    {
        Assert.Equal("$", Single("[]", JsonDiagnosticCodes.InvalidDocument).Path);
    }

    [Theory]
    [InlineData("""{ "profiles": [] }""")]
    [InlineData("""{ "schemaVersion": "1", "profiles": [] }""")]
    [InlineData("""{ "schemaVersion": 1.5, "profiles": [] }""")]
    [InlineData("""{ "schemaVersion": 2, "profiles": [] }""")]
    [InlineData("""{ "schemaVersion": 0, "profiles": [] }""")]
    public void Unsupported_or_missing_schema_versions_are_refused(string json)
    {
        var diagnostic = Single(json, JsonDiagnosticCodes.UnsupportedSchemaVersion);

        Assert.Equal("$.schemaVersion", diagnostic.Path);
    }

    [Fact]
    public void A_future_major_version_is_not_interpreted_as_version_1()
    {
        var diagnostic = Single("""{ "schemaVersion": 7, "profiles": [ { "id": "p", "name": "P" } ] }""", JsonDiagnosticCodes.UnsupportedSchemaVersion);

        Assert.Contains("7", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Profiles_array_is_required()
    {
        Assert.Equal("$.profiles", Single("""{ "schemaVersion": 1 }""", JsonDiagnosticCodes.InvalidDocument).Path);
        Assert.Equal("$.profiles", Single("""{ "schemaVersion": 1, "profiles": {} }""", JsonDiagnosticCodes.InvalidDocument).Path);
    }

    [Fact]
    public void Unknown_properties_are_errors_in_schema_version_1()
    {
        Assert.Equal("$.extra", Single("""{ "schemaVersion": 1, "profiles": [], "extra": true }""", JsonDiagnosticCodes.UnknownProperty).Path);
        Assert.Equal("$.profiles[0].color", Single(Document("""{ "id": "p", "name": "P", "color": "red" }"""), JsonDiagnosticCodes.UnknownProperty).Path);
        Assert.Equal("$.profiles[0].settings[0].comment", Single(Document(Profile("""{ "id": "output.hdr", "value": "off", "comment": "x" }""")), JsonDiagnosticCodes.UnknownProperty).Path);
    }

    [Fact]
    public void Duplicated_properties_are_errors()
    {
        var diagnostic = Single(Document("""{ "id": "p", "name": "P", "name": "Q" }"""), JsonDiagnosticCodes.DuplicateProperty);

        Assert.Equal("$.profiles[0].name", diagnostic.Path);
    }

    [Fact]
    public void Profile_identifiers_and_names_are_validated()
    {
        Assert.Equal("$.profiles[0].id", Single(Document("""{ "id": "not valid!", "name": "P" }"""), JsonDiagnosticCodes.InvalidProfileId).Path);
        Assert.Equal("$.profiles[0].name", Single(Document("""{ "id": "p", "name": "  " }"""), JsonDiagnosticCodes.InvalidProfileName).Path);
        Assert.Equal("$.profiles[0].id", Single(Document("""{ "name": "P" }"""), JsonDiagnosticCodes.InvalidDocument).Path);
        Assert.Equal("$.profiles[0].name", Single(Document("""{ "id": "p" }"""), JsonDiagnosticCodes.InvalidDocument).Path);
        Assert.Equal("$.profiles[0].id", Single(Document("""{ "id": 12, "name": "P" }"""), JsonDiagnosticCodes.InvalidDocument).Path);
    }

    [Fact]
    public void Duplicated_profile_identifiers_are_errors_even_when_their_case_differs()
    {
        var json = $$"""{ "schemaVersion": 1, "profiles": [ {{Profile("")}}, {{Profile("", "P")}} ] }""";

        var diagnostic = Single(json, JsonDiagnosticCodes.DuplicateProfileId);

        Assert.Equal("$.profiles[1].id", diagnostic.Path);
        Assert.Equal("P", diagnostic.ProfileId);
    }

    [Fact]
    public void Sources_are_validated()
    {
        Assert.Equal("$.profiles[0].sources[0]", Single(Document("""{ "id": "p", "name": "P", "sources": ["p"] }"""), JsonDiagnosticCodes.InvalidSources).Path);
        Assert.Equal("$.profiles[0].sources[1]", Single(Document("""{ "id": "p", "name": "P", "sources": ["a", "a"] }"""), JsonDiagnosticCodes.InvalidSources).Path);
        Assert.Equal("$.profiles[0].sources[0]", Single(Document("""{ "id": "p", "name": "P", "sources": ["bad id"] }"""), JsonDiagnosticCodes.InvalidProfileId).Path);
        Assert.Equal("$.profiles[0].sources[0]", Single(Document("""{ "id": "p", "name": "P", "sources": [1] }"""), JsonDiagnosticCodes.InvalidDocument).Path);
        Assert.Equal("$.profiles[0].sources", Single(Document("""{ "id": "p", "name": "P", "sources": "a" }"""), JsonDiagnosticCodes.InvalidDocument).Path);
    }

    [Fact]
    public void Unknown_settings_are_errors_with_context()
    {
        var diagnostic = Single(Document(Profile("""{ "id": "output.brightness", "value": 3 }""")), JsonDiagnosticCodes.UnknownSetting);

        Assert.Equal("$.profiles[0].settings[0].id", diagnostic.Path);
        Assert.Equal("p", diagnostic.ProfileId);
        Assert.Equal("output.brightness", diagnostic.SettingId);
    }

    [Fact]
    public void Invalid_setting_identifiers_are_errors()
    {
        Assert.Equal(JsonDiagnosticCodes.InvalidSettingId, Single(Document(Profile("""{ "id": "output..hdr", "value": "off" }""")), JsonDiagnosticCodes.InvalidSettingId).Code);
        Assert.Equal("$.profiles[0].settings[0].id", Single(Document(Profile("""{ "value": "off" }""")), JsonDiagnosticCodes.InvalidDocument).Path);
    }

    [Theory]
    [InlineData("""{ "id": "output.hdr", "value": "hdr11" }""", "hdr11")]
    [InlineData("""{ "id": "output.hdr", "value": 1 }""", "Number")]
    [InlineData("""{ "id": "output.hdr", "value": null }""", "Null")]
    [InlineData("""{ "id": "geometry.aspectRatio", "value": { "numerator": 4, "denominator": 0 } }""", "denominator")]
    [InlineData("""{ "id": "geometry.aspectRatio", "value": { "numerator": 4 } }""", "denominator")]
    [InlineData("""{ "id": "geometry.aspectRatio", "value": { "numerator": 4, "denominator": 3, "extra": 1 } }""", "extra")]
    [InlineData("""{ "id": "geometry.aspectRatio", "value": "4:3" }""", "object")]
    [InlineData("""{ "id": "geometry.pictureFormat", "value": { "kind": "letterboxed" } }""", "aspectRatio")]
    [InlineData("""{ "id": "geometry.pictureFormat", "value": { "kind": "full-frame", "aspectRatio": { "numerator": 4, "denominator": 3 } } }""", "not allowed")]
    [InlineData("""{ "id": "geometry.pictureFormat", "value": { "kind": "pillarboxed" } }""", "pillarboxed")]
    [InlineData("""{ "id": "geometry.pictureFormat", "value": { "aspectRatio": { "numerator": 4, "denominator": 3 } } }""", "kind")]
    public void Invalid_values_are_errors_with_an_explanation(string assignment, string expectedFragment)
    {
        var diagnostic = Single(Document(Profile(assignment)), JsonDiagnosticCodes.InvalidValue);

        Assert.Equal("$.profiles[0].settings[0].value", diagnostic.Path);
        Assert.Contains(expectedFragment, diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_value_is_an_error()
    {
        Assert.Equal("$.profiles[0].settings[0].value", Single(Document(Profile("""{ "id": "output.hdr" }""")), JsonDiagnosticCodes.InvalidDocument).Path);
    }

    [Fact]
    public void Scopes_are_validated()
    {
        Assert.Equal(JsonDiagnosticCodes.ScopeNotAllowed, Single(Document(Profile("""{ "id": "output.hdr", "scope": { "kind": "videoMode", "value": "pal576i" }, "value": "off" }""")), JsonDiagnosticCodes.ScopeNotAllowed).Code);
        Assert.Equal("$.profiles[0].settings[0].scope.value", Single(Document(Profile("""{ "id": "scaling.mode", "scope": { "kind": "videoMode", "value": "vga" }, "value": "auto-fill" }""")), JsonDiagnosticCodes.InvalidScope).Path);
        Assert.Equal("$.profiles[0].settings[0].scope.kind", Single(Document(Profile("""{ "id": "scaling.mode", "scope": { "kind": "input", "value": "pal576i" }, "value": "auto-fill" }""")), JsonDiagnosticCodes.InvalidScope).Path);
        Assert.Equal("$.profiles[0].settings[0].scope", Single(Document(Profile("""{ "id": "scaling.mode", "scope": "pal576i", "value": "auto-fill" }""")), JsonDiagnosticCodes.InvalidScope).Path);
        Assert.Equal("$.profiles[0].settings[0].scope", Single(Document(Profile("""{ "id": "scaling.mode", "scope": { "kind": "videoMode" }, "value": "auto-fill" }""")), JsonDiagnosticCodes.InvalidScope).Path);
        Assert.Equal("$.profiles[0].settings[0].scope.extra", Single(Document(Profile("""{ "id": "scaling.mode", "scope": { "kind": "videoMode", "value": "pal576i", "extra": 1 }, "value": "auto-fill" }""")), JsonDiagnosticCodes.UnknownProperty).Path);
    }

    [Fact]
    public void A_null_scope_means_global()
    {
        var result = _serializer.Deserialize(Document(Profile("""{ "id": "scaling.mode", "scope": null, "value": "auto-fill" }""")));

        Assert.True(result.IsSuccess, string.Join(" | ", result.Diagnostics));
        Assert.True(result.Profiles[0].Settings.Contains(Settings.ScalingMode));
    }

    [Fact]
    public void The_same_setting_and_scope_cannot_be_assigned_twice()
    {
        var diagnostic = Single(Document(Profile("""{ "id": "output.hdr", "value": "off" }, { "id": "output.hdr", "value": "hlg" }""")), JsonDiagnosticCodes.DuplicateSetting);

        Assert.Equal("$.profiles[0].settings[1]", diagnostic.Path);
        Assert.Equal("output.hdr", diagnostic.SettingId);
    }

    [Fact]
    public void Different_scopes_of_the_same_setting_are_allowed()
    {
        var result = _serializer.Deserialize(Document(Profile("""{ "id": "scaling.mode", "value": "proportional" }, { "id": "scaling.mode", "scope": { "kind": "videoMode", "value": "pal576i" }, "value": "auto-fill" }""")));

        Assert.True(result.IsSuccess, string.Join(" | ", result.Diagnostics));
        Assert.Equal(2, result.Profiles[0].Settings.Count);
    }

    [Fact]
    public void All_errors_of_a_document_are_reported_together()
    {
        var json = $$"""{ "schemaVersion": 1, "profiles": [ {{Profile("""{ "id": "output.hdr", "value": "nope" }""", "a")}}, {{Profile("""{ "id": "output.unknown", "value": 1 }""", "b")}}, {{Profile("", "c")}} ] }""";

        var result = _serializer.Deserialize(json);

        Assert.False(result.IsSuccess);
        Assert.Equal([JsonDiagnosticCodes.InvalidValue, JsonDiagnosticCodes.UnknownSetting], result.Diagnostics.Select(d => d.Code));
        Assert.Equal(["a", "b"], result.Diagnostics.Select(d => d.ProfileId));
    }

    [Fact]
    public void Documents_larger_than_the_limit_are_refused_before_parsing()
    {
        var serializer = new ProfileJsonSerializer(Settings.Catalog, new ProfileJsonOptions { MaxDocumentSizeInBytes = 64 });
        var json = serializer.Serialize(TestLibrary.Profiles());

        Assert.Equal(JsonDiagnosticCodes.DocumentTooLarge, Assert.Single(serializer.Deserialize(json).Diagnostics).Code);
        Assert.Equal(JsonDiagnosticCodes.DocumentTooLarge, Assert.Single(serializer.Deserialize(new ReadOnlyMemory<byte>(serializer.SerializeToUtf8Bytes(TestLibrary.Profiles()))).Diagnostics).Code);
        using var stream = new MemoryStream(serializer.SerializeToUtf8Bytes(TestLibrary.Profiles()));
        Assert.Equal(JsonDiagnosticCodes.DocumentTooLarge, Assert.Single(serializer.Deserialize(stream).Diagnostics).Code);
        using var unseekable = new UnseekableStream(serializer.SerializeToUtf8Bytes(TestLibrary.Profiles()));
        Assert.Equal(JsonDiagnosticCodes.DocumentTooLarge, Assert.Single(serializer.Deserialize(unseekable).Diagnostics).Code);
    }

    [Fact]
    public void Deeply_nested_documents_are_refused()
    {
        var json = "{ \"schemaVersion\": 1, \"profiles\": " + new string('[', 40) + new string(']', 40) + " }";

        Assert.Equal(JsonDiagnosticCodes.Malformed, Assert.Single(_serializer.Deserialize(json).Diagnostics).Code);
    }

    [Fact]
    public void Comments_and_trailing_commas_are_not_accepted()
    {
        Assert.Equal(JsonDiagnosticCodes.Malformed, Single("""{ "schemaVersion": 1, /* c */ "profiles": [] }""", JsonDiagnosticCodes.Malformed).Code);
        Assert.Equal(JsonDiagnosticCodes.Malformed, Single("""{ "schemaVersion": 1, "profiles": [], }""", JsonDiagnosticCodes.Malformed).Code);
    }

    private sealed class UnseekableStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
