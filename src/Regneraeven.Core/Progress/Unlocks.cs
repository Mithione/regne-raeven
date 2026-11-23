namespace Regneraeven.Core.Progress;

/// <summary>A drawn fox avatar that unlocks at a level (the same levels as the fox titles).</summary>
public sealed record AvatarUnlock(string Id, string Name, int Level, string Description)
{
    /// <summary>The value stored on the profile, e.g. "fox:briller".</summary>
    public string Key => "fox:" + Id;
}

/// <summary>
/// What a child can unlock. Today: drawn fox avatars. Later maybe colour themes or avatar frames –
/// add them here with the level they unlock at.
/// </summary>
public static class Unlocks
{
    /// <summary>Simple animal avatars everyone can pick from the start.</summary>
    public static readonly IReadOnlyList<string> StartEmojis =
        ["🦊", "🐻", "🐼", "🐯", "🦁", "🐸", "🐙", "🦄", "🐲", "🐧", "🦉", "🐬", "🐱", "🐶", "🦖", "🚀"];

    public static readonly IReadOnlyList<AvatarUnlock> Avatars =
    [
        new("hvalp", "Rævehvalpen", 1, "Din allerførste ræv."),
        new("lup", "Opdageren", 5, "Med lup – til nysgerrige ræve."),
        new("briller", "Den snu", 10, "Kloge briller til en snu ræv."),
        new("lyn", "Lynræven", 15, "Pandebånd med lyn – lynhurtig til tabellerne."),
        new("roed", "Rødræven", 20, "Flot rød pels og grønt halstørklæde."),
        new("soelv", "Sølvræven", 25, "Sølvpels og butterfly."),
        new("polar", "Polarræven", 30, "Hvid pels og hue mod kulden."),
        new("astronaut", "Astronauten", 40, "Klar til at regne blandt stjernerne."),
        new("guld", "Guldræven", 50, "Gyldenpels med medalje."),
        new("krone", "Mesterræven", 60, "En krone til en sand mester."),
        new("troldmand", "Regnetroldmanden", 75, "Den legendariske regneræv med troldmandshat."),
    ];

    public static AvatarUnlock? Find(string? key) =>
        key is not null && key.StartsWith("fox:", StringComparison.Ordinal) ? Avatars.FirstOrDefault(a => a.Key == key) : null;

    public static bool IsUnlocked(AvatarUnlock avatar, int level) => level >= avatar.Level;

    /// <summary>May a child at this level use this avatar? Start emojis always; drawn foxes once unlocked.</summary>
    public static bool CanUse(string? avatar, int level) =>
        avatar is not null && (StartEmojis.Contains(avatar) || Find(avatar) is { } a && IsUnlocked(a, level));

    /// <summary>Avatars unlocked by going from one level to another (for the level-up dialog).</summary>
    public static IEnumerable<AvatarUnlock> UnlockedBetween(int fromLevelExclusive, int toLevelInclusive) =>
        Avatars.Where(a => a.Level > fromLevelExclusive && a.Level <= toLevelInclusive);

    public static AvatarUnlock? Next(int level) => Avatars.FirstOrDefault(a => a.Level > level);
}
