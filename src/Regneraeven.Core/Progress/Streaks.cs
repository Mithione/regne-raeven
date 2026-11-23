using System.Text.Json;

namespace Regneraeven.Core.Progress;

/// <summary>A milestone for days in a row: an XP bonus plus a choice between exclusive items.</summary>
public sealed record StreakMilestone(int Days, int BonusXp, string Icon, IReadOnlyList<CosmeticUnlock> Choices);

/// <summary>Result of finishing today's tasks.</summary>
public sealed record StreakAdvance(int Streak, int Freezes, int FreezesUsed, bool EarnedFreeze);

/// <summary>
/// Days in a row ("dage i træk"). A day counts when all of the day's tasks are done.
/// Milestones come quickly at first (3 and 7 days) and then further apart, so the habit gets going.
/// Every 7th day earns a day off (fridag, max 2) that is used automatically if a day is missed –
/// one sick day shouldn't wipe out a long streak.
/// </summary>
public static class Streaks
{
    public const int MaxFreezes = 2;
    public const int DaysPerFreeze = 7;

    private static CosmeticUnlock S(UnlockKind kind, string id, string name, string description) =>
        new(kind, id, name, int.MaxValue, description);

    public static readonly IReadOnlyList<StreakMilestone> Milestones =
    [
        new(3, 25, "✨", [S(UnlockKind.Frame, "gnist", "Gnistramme", "Små gnister hele vejen rundt."),
                         S(UnlockKind.Background, "baal", "Bålhygge", "Et lunt bål bag din ræv.")]),
        new(7, 50, "🔥", [S(UnlockKind.Frame, "flammer", "Flammeramme", "Ild i alle farver – for en hel uge i træk."),
                         S(UnlockKind.Theme, "gloed", "Glød", "Varme gløder i hele programmet.")]),
        new(14, 100, "⚡", [S(UnlockKind.Background, "nordlys", "Nordlys", "Grønt og lilla nordlys over himlen."),
                           S(UnlockKind.Frame, "lynring", "Lynring", "Elektrisk blå ring med lyn.")]),
        new(30, 200, "🌋", [S(UnlockKind.Theme, "vulkan", "Vulkan", "Mørkt vulkantema med glødende lava."),
                           S(UnlockKind.Background, "fyrvaerkeri", "Fyrværkeri", "Fyrværkeri for en hel måned!")]),
        new(50, 300, "💎", [S(UnlockKind.Frame, "diamant", "Diamantramme", "Glitrende diamantkant."),
                           S(UnlockKind.Background, "galakse", "Galakse", "En hel galakse bag din ræv.")]),
        new(100, 500, "👑", [S(UnlockKind.Frame, "legende", "Legenderamme", "Kun for legender med 100 dage i træk."),
                            S(UnlockKind.Theme, "regnbue", "Regnbuetema", "Alle regnbuens farver i hele programmet.")]),
    ];

    public static IEnumerable<CosmeticUnlock> Items => Milestones.SelectMany(m => m.Choices);

    public static StreakMilestone? MilestoneFor(CosmeticUnlock item) => Milestones.FirstOrDefault(m => m.Choices.Contains(item));

    /// <summary>The streak after finishing today's tasks.</summary>
    public static StreakAdvance Advance(DateOnly? lastDone, DateOnly today, int streak, int freezes)
    {
        if (lastDone == today) return new StreakAdvance(streak, freezes, 0, false);
        var missed = lastDone is { } d ? today.DayNumber - d.DayNumber - 1 : int.MaxValue;
        int newStreak, used = 0;
        if (missed < 0 || missed > freezes) newStreak = 1;
        else
        {
            newStreak = streak + 1;
            used = missed;
            freezes -= missed;
        }
        var earned = newStreak % DaysPerFreeze == 0 && freezes < MaxFreezes;
        if (earned) freezes++;
        return new StreakAdvance(newStreak, freezes, used, earned);
    }

    /// <summary>The streak as it stands now: still alive if today or yesterday counted, or the gap is covered by days off.</summary>
    public static int Live(DateOnly? lastDone, DateOnly today, int streak, int freezes)
    {
        if (lastDone is not { } d) return 0;
        var missed = today.DayNumber - d.DayNumber - 1; // -1 = done today, 0 = done yesterday
        return missed <= freezes ? streak : 0;
    }

    /// <summary>Days off that will be used up if today is finished (missed days so far).</summary>
    public static int FreezesInUse(DateOnly? lastDone, DateOnly today) =>
        lastDone is { } d ? Math.Max(0, today.DayNumber - d.DayNumber - 1) : 0;

    public static StreakMilestone? Next(int streak) => Milestones.FirstOrDefault(m => m.Days > streak);

    /// <summary>Milestones passed when the best streak goes from one value to another (for the XP bonus).</summary>
    public static IEnumerable<StreakMilestone> ReachedBetween(int fromExclusive, int toInclusive) =>
        Milestones.Where(m => m.Days > fromExclusive && m.Days <= toInclusive);

    // ------------------------------------------------------------ stored choices (JSON on the profile)

    /// <summary>Which item was chosen at each milestone: {"3":"gnist","7":"gloed"}.</summary>
    public static Dictionary<int, string> ParseClaims(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<Dictionary<int, string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    public static string FormatClaims(Dictionary<int, string> claims) => JsonSerializer.Serialize(claims);

    public static IReadOnlySet<string> OwnedItems(string? claimsJson) => ParseClaims(claimsJson).Values.ToHashSet();

    /// <summary>Real-life rewards a parent wrote per milestone: {"7":"Is på fredag"}.</summary>
    public static Dictionary<int, string> ParseRewards(string? json) => ParseClaims(json);

    /// <summary>Choosing a reward: only once per milestone, only after reaching it, only one of its choices.</summary>
    public static bool CanClaim(StreakMilestone milestone, string itemId, int bestStreak, IReadOnlyDictionary<int, string> claims) =>
        bestStreak >= milestone.Days && !claims.ContainsKey(milestone.Days) && milestone.Choices.Any(c => c.Id == itemId);
}
