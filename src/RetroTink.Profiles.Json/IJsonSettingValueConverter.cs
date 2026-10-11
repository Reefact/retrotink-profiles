using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace RetroTink.Profiles.Json;

/// <summary>Converts values of a setting value type to and from their JSON representation.</summary>
/// <typeparam name="TValue">The value type (a Smart Enum or a Value Object of the Core).</typeparam>
/// <remarks>
/// The Core types know nothing about JSON: converters own the persisted representation, which lets the schema evolve
/// independently from the model. Implementations must be stateless and thread-safe.
/// </remarks>
public interface IJsonSettingValueConverter<TValue>
    where TValue : notnull, IEquatable<TValue>
{
    /// <summary>Writes <paramref name="value"/> as a complete JSON value.</summary>
    void Write(Utf8JsonWriter writer, TValue value);

    /// <summary>Attempts to read a value; on failure, <paramref name="error"/> explains why in one sentence.</summary>
    bool TryRead(JsonElement element, [MaybeNullWhen(false)] out TValue value, [NotNullWhen(false)] out string? error);
}
