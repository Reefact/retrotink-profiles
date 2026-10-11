namespace RetroTink.Profiles.Json;

/// <summary>Constants of the native JSON schema (version 1).</summary>
/// <remarks>
/// <c>schemaVersion</c> designates this JSON schema, not a firmware or RT4 version. The schema persists definitions
/// only: identities, names, descriptions, ordered sources and local assignments with their scope. Derived values and
/// diagnostics are recomputed, never stored.
/// </remarks>
public static class ProfileJsonSchema
{
    /// <summary>The only schema version understood by this build.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Root property holding the schema version.</summary>
    public const string SchemaVersion = "schemaVersion";

    /// <summary>Root property holding the array of profiles.</summary>
    public const string Profiles = "profiles";

    /// <summary>Profile / setting identifier property.</summary>
    public const string Id = "id";

    /// <summary>Profile display name property.</summary>
    public const string Name = "name";

    /// <summary>Profile description property (optional).</summary>
    public const string Description = "description";

    /// <summary>Profile ordered sources property.</summary>
    public const string Sources = "sources";

    /// <summary>Profile local assignments property.</summary>
    public const string Settings = "settings";

    /// <summary>Assignment scope property (optional; absent means global).</summary>
    public const string Scope = "scope";

    /// <summary>Scope discriminator property.</summary>
    public const string ScopeKind = "kind";

    /// <summary>Scope value property.</summary>
    public const string ScopeValue = "value";

    /// <summary>Scope discriminator for a video-mode scope.</summary>
    public const string VideoModeScopeKind = "videoMode";

    /// <summary>Assignment value property.</summary>
    public const string Value = "value";
}
