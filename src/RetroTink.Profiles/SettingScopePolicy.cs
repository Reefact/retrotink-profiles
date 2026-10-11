namespace RetroTink.Profiles;

/// <summary>Declares which <see cref="SettingScope"/>s are semantically admissible for a setting.</summary>
public enum SettingScopePolicy
{
    /// <summary>Only <see cref="SettingScope.Global"/> is admissible.</summary>
    GlobalOnly = 0,

    /// <summary>The global scope and any video-mode scope are admissible.</summary>
    GlobalOrVideoMode = 1,
}

/// <summary>Helpers for <see cref="SettingScopePolicy"/>.</summary>
public static class SettingScopePolicyExtensions
{
    /// <summary>Returns whether <paramref name="scope"/> is admissible under <paramref name="policy"/>.</summary>
    public static bool Allows(this SettingScopePolicy policy, SettingScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return policy switch
        {
            SettingScopePolicy.GlobalOnly => scope.IsGlobal,
            SettingScopePolicy.GlobalOrVideoMode => true,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown scope policy."),
        };
    }
}
