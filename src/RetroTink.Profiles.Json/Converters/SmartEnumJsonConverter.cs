using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Json.Converters;

/// <summary>Persists a Smart Enum member as its stable <see cref="SmartEnum{TSelf}.Key"/>, as a JSON string.</summary>
/// <typeparam name="TEnum">The Smart Enum type.</typeparam>
public sealed class SmartEnumJsonConverter<TEnum> : IJsonSettingValueConverter<TEnum>
    where TEnum : SmartEnum<TEnum>, ISmartEnum<TEnum>
{
    /// <inheritdoc />
    public void Write(Utf8JsonWriter writer, TEnum value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Key);
    }

    /// <inheritdoc />
    public bool TryRead(JsonElement element, [MaybeNullWhen(false)] out TEnum value, [NotNullWhen(false)] out string? error)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            value = null;
            error = $"Expected a string naming a {typeof(TEnum).Name}, got {element.ValueKind}.";
            return false;
        }

        var key = element.GetString();
        if (SmartEnum<TEnum>.TryFromKey(key, out value))
        {
            error = null;
            return true;
        }

        error = $"'{key}' is not a known {typeof(TEnum).Name}; expected one of: {string.Join(", ", TEnum.All.Select(m => m.Key))}.";
        return false;
    }
}
