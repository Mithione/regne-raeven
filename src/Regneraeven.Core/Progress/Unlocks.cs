namespace Regneraeven.Core.Progress;

/// <summary>A drawn fox avatar that unlocks at a level (the same levels as the fox titles).</summary>
public sealed record AvatarUnlock(string Id, string Name, int Level, string Description)
{
    /// <summary>The value stored on the profile, e.g. "fox:briller".</summary>
    public string Key => "fox:" + Id;
}

public enum UnlockKind { Frame, Background, Theme }

/// <summary>A frame around the avatar, a background behind it, or a colour theme for the whole program.</summary>
public sealed record CosmeticUnlock(UnlockKind Kind, string Id, string Name, int Level, string Description);

/// <summary>Something new for the level-up dialog: "🖼️ Ramme: Neon".</summary>
public sealed record UnlockNews(string Icon, string KindName, string Name);

/// <summary>
/// Everything a child can unlock by levelling up: fox avatars at the title levels, and frames,
/// backgrounds and colour themes in between – so the first levels give something new almost every time.
/// The first item of each list is the free default.
/// </summary>
public static class Unlocks
{
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

    public static readonly IReadOnlyList<CosmeticUnlock> Frames =
    [
        new(UnlockKind.Frame, "klassisk", "Klassisk", 1, "En ring i din profilfarve."),
        new(UnlockKind.Frame, "prikker", "Prikker", 3, "Prikket ring – sjov og enkel."),
        new(UnlockKind.Frame, "neon", "Neon", 8, "Lysende neonring."),
        new(UnlockKind.Frame, "guld", "Guldramme", 13, "Dobbelt guldkant."),
        new(UnlockKind.Frame, "regnbue", "Regnbue", 27, "Alle regnbuens farver hele vejen rundt."),
        new(UnlockKind.Frame, "stjerner", "Stjerneramme", 45, "Med stjerner, der stråler."),
    ];

    public static readonly IReadOnlyList<CosmeticUnlock> Backgrounds =
    [
        new(UnlockKind.Background, "hvid", "Hvid", 1, "Enkel og rolig."),
        new(UnlockKind.Background, "himmel", "Himmel", 2, "Blå himmel bag din ræv."),
        new(UnlockKind.Background, "solnedgang", "Solnedgang", 7, "Varme farver fra en sommeraften."),
        new(UnlockKind.Background, "tal", "Talmønster", 12, "Tal og regnetegn – for rigtige regneræve."),
        new(UnlockKind.Background, "skov", "Skov", 22, "Grøn skov med grantræer."),
        new(UnlockKind.Background, "stjernehimmel", "Stjernehimmel", 35, "Mørk nat fuld af stjerner."),
        new(UnlockKind.Background, "regnbue", "Regnbue", 55, "En regnbue bag ræven."),
    ];

    public static readonly IReadOnlyList<CosmeticUnlock> Themes =
    [
        new(UnlockKind.Theme, "raev", "Ræv", 1, "Ræveorange – det klassiske tema."),
        new(UnlockKind.Theme, "skov", "Skov", 4, "Grønne farver fra skoven."),
        new(UnlockKind.Theme, "hav", "Hav", 6, "Blå som havet."),
        new(UnlockKind.Theme, "lilla", "Lilla", 9, "Lilla og lavendel."),
        new(UnlockKind.Theme, "jordbaer", "Jordbær", 14, "Lyserødt og jordbærrødt."),
        new(UnlockKind.Theme, "is", "Is", 18, "Kølig turkis."),
        new(UnlockKind.Theme, "nat", "Nat", 33, "Mørkeblåt nattema."),
        new(UnlockKind.Theme, "guld", "Guld", 65, "Gyldent tema for de flittigste."),
    ];

    public static IReadOnlyList<CosmeticUnlock> Of(UnlockKind kind) => kind switch
    {
        UnlockKind.Frame => Frames,
        UnlockKind.Background => Backgrounds,
        _ => Themes,
    };

    public static (string Icon, string Name, string Plural) Describe(UnlockKind kind) => kind switch
    {
        UnlockKind.Frame => ("🖼️", "Ramme", "Rammer"),
        UnlockKind.Background => ("🌄", "Baggrund", "Baggrunde"),
        _ => ("🎨", "Farvetema", "Farvetemaer"),
    };

    public static AvatarUnlock? Find(string? key) =>
        key is not null && key.StartsWith("fox:", StringComparison.Ordinal) ? Avatars.FirstOrDefault(a => a.Key == key) : null;

    public static CosmeticUnlock? Find(UnlockKind kind, string? id) => id is null ? null : Of(kind).FirstOrDefault(c => c.Id == id);

    public static bool IsUnlocked(AvatarUnlock avatar, int level) => level >= avatar.Level;
    public static bool IsUnlocked(CosmeticUnlock item, int level) => level >= item.Level;

    /// <summary>May a child at this level use this fox?</summary>
    public static bool CanUse(string? avatar, int level) => Find(avatar) is { } a && IsUnlocked(a, level);

    public static bool CanUse(UnlockKind kind, string? id, int level) => Find(kind, id) is { } c && IsUnlocked(c, level);

    /// <summary>The item to show: the chosen one if it is still valid, otherwise the free default.</summary>
    public static CosmeticUnlock Current(UnlockKind kind, string? id, int level) =>
        Find(kind, id) is { } c && IsUnlocked(c, level) ? c : Of(kind)[0];

    /// <summary>Avatars unlocked by going from one level to another (for the level-up dialog).</summary>
    public static IEnumerable<AvatarUnlock> UnlockedBetween(int fromLevelExclusive, int toLevelInclusive) =>
        Avatars.Where(a => a.Level > fromLevelExclusive && a.Level <= toLevelInclusive);

    /// <summary>Frames, backgrounds and themes unlocked between two levels.</summary>
    public static IEnumerable<UnlockNews> CosmeticsUnlockedBetween(int fromLevelExclusive, int toLevelInclusive) =>
        Frames.Concat(Backgrounds).Concat(Themes)
            .Where(c => c.Level > fromLevelExclusive && c.Level <= toLevelInclusive)
            .OrderBy(c => c.Level)
            .Select(c => { var d = Describe(c.Kind); return new UnlockNews(d.Icon, d.Name, c.Name); });

    public static AvatarUnlock? Next(int level) => Avatars.FirstOrDefault(a => a.Level > level);

    /// <summary>The next thing of any kind to unlock: (level, icon, description).</summary>
    public static (int Level, string Icon, string Text)? NextAnything(int level)
    {
        var candidates = Avatars.Where(a => a.Level > level).Select(a => (a.Level, "🦊", $"Ny ræv: {a.Name}"))
            .Concat(Frames.Concat(Backgrounds).Concat(Themes).Where(c => c.Level > level)
                .Select(c => (c.Level, Describe(c.Kind).Icon, $"{Describe(c.Kind).Name}: {c.Name}")))
            .OrderBy(x => x.Level)
            .ToList();
        return candidates.Count == 0 ? null : candidates[0];
    }
}
