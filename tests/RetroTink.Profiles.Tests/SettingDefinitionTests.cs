using RetroTink.Profiles.Values;
using Xunit;

namespace RetroTink.Profiles.Tests;

public class SettingDefinitionTests
{
    [Fact]
    public void Definitions_expose_their_characteristics()
    {
        var definition = Settings.OutputResolution;

        Assert.Equal(SettingId.From("output.resolution"), definition.Id);
        Assert.Equal(typeof(OutputResolution), definition.ValueType);
        Assert.Equal(SettingScopePolicy.GlobalOnly, definition.ScopePolicy);
        Assert.Equal(SettingKind.Hardware, definition.Kind);
        Assert.Equal(SettingKind.Intent, Settings.PictureFormat.Kind);
        Assert.Equal("output.resolution", definition.ToString());
    }

    [Fact]
    public void Scope_policy_decides_which_scopes_are_admissible()
    {
        Assert.True(Settings.OutputResolution.AllowsScope(SettingScope.Global));
        Assert.False(Settings.OutputResolution.AllowsScope(SettingScope.For(VideoMode.Pal576i)));
        Assert.True(Settings.ScalingMode.AllowsScope(SettingScope.For(VideoMode.Pal576i)));
        Assert.True(Settings.ScalingMode.AllowsScope(SettingScope.Global));
    }

    [Fact]
    public void Equality_is_structural_over_all_characteristics()
    {
        var a = new SettingDefinition<HdrMode>(SettingId.From("x.hdr"), "HDR");
        var same = new SettingDefinition<HdrMode>(SettingId.From("x.hdr"), "HDR");
        var otherType = new SettingDefinition<Colorimetry>(SettingId.From("x.hdr"), "HDR");
        var otherPolicy = new SettingDefinition<HdrMode>(SettingId.From("x.hdr"), "HDR", SettingScopePolicy.GlobalOrVideoMode);
        var otherLabel = new SettingDefinition<HdrMode>(SettingId.From("x.hdr"), "High dynamic range");

        Assert.Equal(a, same);
        Assert.Equal(a.GetHashCode(), same.GetHashCode());
        Assert.NotEqual<SettingDefinition>(a, otherType);
        Assert.NotEqual(a, otherPolicy);
        Assert.NotEqual(a, otherLabel);
    }

    [Fact]
    public void Visitor_receives_the_statically_typed_definition()
    {
        var typeName = Settings.Input.Accept(new TypeNameVisitor());

        Assert.Equal(nameof(VideoInput), typeName);
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new SettingDefinition<HdrMode>(null!, "HDR"));
        Assert.Throws<ArgumentException>(() => new SettingDefinition<HdrMode>(SettingId.From("x"), " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettingDefinition<HdrMode>(SettingId.From("x"), "x", (SettingScopePolicy)42));
    }

    [Fact]
    public void Standard_catalog_contains_every_standard_definition_once()
    {
        Assert.Equal(Settings.All.Count, Settings.Catalog.Count);
        foreach (var definition in Settings.All)
        {
            Assert.True(Settings.Catalog.Contains(definition));
            Assert.Same(definition, Settings.Catalog.Get(definition.Id));
        }

        Assert.Equal(Settings.All.Count, Settings.All.Select(d => d.Id).Distinct().Count());
    }

    [Fact]
    public void Catalog_registration_is_idempotent_for_equal_definitions()
    {
        var duplicate = new SettingDefinition<HdrMode>(SettingId.From("output.hdr"), "HDR mode");

        var catalog = Settings.Catalog.With(duplicate);

        Assert.Equal(Settings.Catalog.Count, catalog.Count);
    }

    [Fact]
    public void Catalog_rejects_the_same_id_with_different_characteristics()
    {
        var conflicting = new SettingDefinition<Colorimetry>(SettingId.From("output.hdr"), "HDR mode");

        Assert.Throws<ArgumentException>(() => Settings.Catalog.With(conflicting));
        Assert.Throws<ArgumentException>(() => SettingCatalog.Create(Settings.Hdr, conflicting));
    }

    [Fact]
    public void Catalog_can_be_extended_with_consumer_definitions()
    {
        var custom = new SettingDefinition<AspectRatio>(SettingId.From("custom.ratio"), "Custom ratio", SettingScopePolicy.GlobalOrVideoMode);

        var catalog = Settings.Catalog.With(custom);

        Assert.True(catalog.TryGet(custom.Id, out var found));
        Assert.Same(custom, found);
        Assert.False(Settings.Catalog.Contains(custom.Id), "the standard catalog must not be mutated");
        Assert.Throws<KeyNotFoundException>(() => Settings.Catalog.Get(custom.Id));
        Assert.Equal(Settings.Catalog.Count + 1, catalog.Count);
        Assert.Contains(custom, catalog);
    }

    private sealed class TypeNameVisitor : ISettingDefinitionVisitor<string>
    {
        public string Visit<TValue>(SettingDefinition<TValue> definition)
            where TValue : notnull, IEquatable<TValue>
            => typeof(TValue).Name;
    }
}
