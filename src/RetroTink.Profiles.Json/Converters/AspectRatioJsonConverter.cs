using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Json.Converters;

/// <summary>Persists an <see cref="AspectRatio"/> as <c>{ "numerator": n, "denominator": d }</c> (reduced terms).</summary>
public sealed class AspectRatioJsonConverter : IJsonSettingValueConverter<AspectRatio>
{
    /// <summary>Property holding the numerator.</summary>
    public const string Numerator = "numerator";

    /// <summary>Property holding the denominator.</summary>
    public const string Denominator = "denominator";

    /// <inheritdoc />
    public void Write(Utf8JsonWriter writer, AspectRatio value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();
        writer.WriteNumber(Numerator, value.Numerator);
        writer.WriteNumber(Denominator, value.Denominator);
        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public bool TryRead(JsonElement element, [MaybeNullWhen(false)] out AspectRatio value, [NotNullWhen(false)] out string? error)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            error = $"Expected an object with '{Numerator}' and '{Denominator}', got {element.ValueKind}.";
            return false;
        }

        int? numerator = null;
        int? denominator = null;
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals(Numerator) && numerator is null)
            {
                if (!property.Value.TryGetPositiveInt32(out var n))
                {
                    error = $"'{Numerator}' must be a strictly positive integer.";
                    return false;
                }

                numerator = n;
            }
            else if (property.NameEquals(Denominator) && denominator is null)
            {
                if (!property.Value.TryGetPositiveInt32(out var d))
                {
                    error = $"'{Denominator}' must be a strictly positive integer.";
                    return false;
                }

                denominator = d;
            }
            else
            {
                error = $"Unexpected or duplicated property '{property.Name}' in aspect ratio.";
                return false;
            }
        }

        if (numerator is null || denominator is null)
        {
            error = $"Aspect ratio requires both '{Numerator}' and '{Denominator}'.";
            return false;
        }

        value = AspectRatio.From(numerator.Value, denominator.Value);
        error = null;
        return true;
    }
}
