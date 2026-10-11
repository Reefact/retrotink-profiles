using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using RetroTink.Profiles.Json.Converters;
using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Json;

/// <summary>Immutable registry of <see cref="IJsonSettingValueConverter{TValue}"/>, keyed by value type.</summary>
public sealed class JsonSettingValueConverters
{
    // The only place where a converter loses its static type: entries are stored as object and recovered through the
    // exact typeof(TValue) key, so the cast in TryGet is always sound.
    private readonly ImmutableDictionary<Type, object> _converters;

    private JsonSettingValueConverters(ImmutableDictionary<Type, object> converters) => _converters = converters;

    /// <summary>A registry without any converter.</summary>
    public static JsonSettingValueConverters Empty { get; } = new(ImmutableDictionary<Type, object>.Empty);

    /// <summary>Converters for every value type used by <see cref="Settings"/>.</summary>
    public static JsonSettingValueConverters Default { get; } = Empty
        .With(new SmartEnumJsonConverter<VideoInput>())
        .With(new SmartEnumJsonConverter<VideoStandard>())
        .With(new SmartEnumJsonConverter<OutputResolution>())
        .With(new SmartEnumJsonConverter<HdrMode>())
        .With(new SmartEnumJsonConverter<Colorimetry>())
        .With(new SmartEnumJsonConverter<RgbRange>())
        .With(new SmartEnumJsonConverter<DeinterlacingMode>())
        .With(new SmartEnumJsonConverter<TelecineMode>())
        .With(new SmartEnumJsonConverter<ScalingMode>())
        .With(new AspectRatioJsonConverter())
        .With(new PictureFormatJsonConverter());

    /// <summary>Number of registered converters.</summary>
    public int Count => _converters.Count;

    /// <summary>Returns a registry where <typeparamref name="TValue"/> uses <paramref name="converter"/> (replacing any previous one).</summary>
    public JsonSettingValueConverters With<TValue>(IJsonSettingValueConverter<TValue> converter)
        where TValue : notnull, IEquatable<TValue>
    {
        ArgumentNullException.ThrowIfNull(converter);
        return new JsonSettingValueConverters(_converters.SetItem(typeof(TValue), converter));
    }

    /// <summary>Returns whether a converter is registered for <paramref name="valueType"/>.</summary>
    public bool Contains(Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return _converters.ContainsKey(valueType);
    }

    /// <summary>Attempts to get the converter registered for <typeparamref name="TValue"/>.</summary>
    public bool TryGet<TValue>([MaybeNullWhen(false)] out IJsonSettingValueConverter<TValue> converter)
        where TValue : notnull, IEquatable<TValue>
    {
        if (_converters.TryGetValue(typeof(TValue), out var stored))
        {
            converter = (IJsonSettingValueConverter<TValue>)stored;
            return true;
        }

        converter = null;
        return false;
    }

    /// <summary>Gets the converter registered for <typeparamref name="TValue"/>.</summary>
    /// <exception cref="InvalidOperationException">No converter is registered for the type.</exception>
    public IJsonSettingValueConverter<TValue> Get<TValue>()
        where TValue : notnull, IEquatable<TValue>
        => TryGet<TValue>(out var converter)
            ? converter
            : throw new InvalidOperationException($"No JSON converter is registered for value type {typeof(TValue).Name}. Register one with {nameof(JsonSettingValueConverters)}.{nameof(With)}.");
}
