using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using RetroTink.Profiles.Values;

namespace RetroTink.Profiles.Json.Converters;

/// <summary>
/// Persists a <see cref="PictureFormat"/> as <c>{ "kind": "full-frame" }</c> or
/// <c>{ "kind": "letterboxed", "aspectRatio": { "numerator": n, "denominator": d } }</c>.
/// </summary>
public sealed class PictureFormatJsonConverter : IJsonSettingValueConverter<PictureFormat>
{
    /// <summary>Discriminator property.</summary>
    public const string Kind = "kind";

    /// <summary>Content aspect ratio property (letterboxed only).</summary>
    public const string AspectRatio = "aspectRatio";

    /// <summary>Discriminator value for <see cref="PictureFormat.FullFrame"/>.</summary>
    public const string FullFrameKind = "full-frame";

    /// <summary>Discriminator value for <see cref="PictureFormat.Letterboxed"/>.</summary>
    public const string LetterboxedKind = "letterboxed";

    private static readonly AspectRatioJsonConverter RatioConverter = new();

    /// <inheritdoc />
    public void Write(Utf8JsonWriter writer, PictureFormat value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();
        switch (value.Kind)
        {
            case PictureFormatKind.FullFrame:
                writer.WriteString(Kind, FullFrameKind);
                break;
            case PictureFormatKind.Letterboxed:
                writer.WriteString(Kind, LetterboxedKind);
                writer.WritePropertyName(AspectRatio);
                RatioConverter.Write(writer, value.ContentAspectRatio!);
                break;
            default:
                throw new InvalidOperationException($"Unsupported picture format kind {value.Kind}.");
        }

        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public bool TryRead(JsonElement element, [MaybeNullWhen(false)] out PictureFormat value, [NotNullWhen(false)] out string? error)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            error = $"Expected an object with a '{Kind}' property, got {element.ValueKind}.";
            return false;
        }

        string? kind = null;
        JsonElement? ratioElement = null;
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals(Kind) && kind is null)
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    error = $"'{Kind}' must be a string.";
                    return false;
                }

                kind = property.Value.GetString();
            }
            else if (property.NameEquals(AspectRatio) && ratioElement is null)
            {
                ratioElement = property.Value;
            }
            else
            {
                error = $"Unexpected or duplicated property '{property.Name}' in picture format.";
                return false;
            }
        }

        switch (kind)
        {
            case FullFrameKind:
                if (ratioElement is not null)
                {
                    error = $"'{AspectRatio}' is not allowed for kind '{FullFrameKind}'.";
                    return false;
                }

                value = PictureFormat.FullFrame;
                error = null;
                return true;

            case LetterboxedKind:
                if (ratioElement is null)
                {
                    error = $"Kind '{LetterboxedKind}' requires an '{AspectRatio}' object.";
                    return false;
                }

                if (!RatioConverter.TryRead(ratioElement.Value, out var ratio, out error))
                {
                    return false;
                }

                value = PictureFormat.Letterboxed(ratio);
                return true;

            case null:
                error = $"Picture format requires a '{Kind}' property.";
                return false;

            default:
                error = $"'{kind}' is not a known picture format kind; expected '{FullFrameKind}' or '{LetterboxedKind}'.";
                return false;
        }
    }
}
