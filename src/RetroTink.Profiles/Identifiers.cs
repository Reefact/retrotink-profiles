namespace RetroTink.Profiles;

/// <summary>Shared character-level validation helpers for identifier value objects.</summary>
internal static class Identifiers
{
    internal static bool IsAsciiLetterOrDigit(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');

    internal static bool IsLowerAsciiLetterOrDigit(char c) => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

    internal static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    internal static bool ContainsControlCharacter(string value)
    {
        foreach (var c in value)
        {
            if (char.IsControl(c))
            {
                return true;
            }
        }

        return false;
    }
}
