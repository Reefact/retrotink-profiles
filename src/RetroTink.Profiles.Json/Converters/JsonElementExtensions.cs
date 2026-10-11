using System.Text.Json;

namespace RetroTink.Profiles.Json.Converters;

/// <summary>Small helpers over <see cref="JsonElement"/>.</summary>
internal static class JsonElementExtensions
{
    internal static bool TryGetPositiveInt32(this JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value) && value > 0;
    }
}
