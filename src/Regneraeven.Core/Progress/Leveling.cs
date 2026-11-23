using Regneraeven.Core.Topics;

namespace Regneraeven.Core.Progress;

public sealed record FoxTitle(int Level, string Name, string Icon, string Description);

public sealed record LevelInfo(int Level, int XpIntoLevel, int XpForNextLevel, FoxTitle Title, FoxTitle? NextTitle)
{
    public int Percent => XpForNextLevel == 0 ? 100 : (int)Math.Round(100.0 * XpIntoLevel / XpForNextLevel);
}

/// <summary>XP, levels and the fox titles that unlock along the way.</summary>
public static class Leveling
{
    /// <summary>Titles unlock every 5–10 levels.</summary>
    public static readonly IReadOnlyList<FoxTitle> Titles =
    [
        new(1, "Rævehvalp", "🦊", "Du er lige begyndt. Velkommen i rævegraven!"),
        new(5, "Nysgerrig ræv", "🔍", "Du snuser til alle slags regnestykker."),
        new(10, "Snu ræv", "🧠", "Du finder smarte veje til svaret."),
        new(15, "Lynræv", "⚡", "Tabellerne sidder lige i poten."),
        new(20, "Rødræv", "🔥", "En rigtig regneræv med flot rød pels."),
        new(25, "Sølvræv", "🥈", "Brøker og decimaltal er ingen sag."),
        new(30, "Polarræv", "❄️", "Kold og rolig – også ved de svære opgaver."),
        new(40, "Stjerneræv", "⭐", "Du lyser op i koordinatsystemet."),
        new(50, "Guldræv", "🥇", "Kun de flittigste ræve når hertil."),
        new(60, "Mesterræv", "👑", "En sand mester i regnerævenes rige."),
        new(75, "Legendarisk regneræv", "🌟", "Der fortælles historier om dig i rævegraven."),
    ];

    /// <summary>XP needed to go from <paramref name="level"/> to the next level: 100, 125, 150, …</summary>
    public static int XpForNext(int level) => 100 + 25 * (level - 1);

    public static LevelInfo FromXp(int totalXp)
    {
        var level = 1;
        var rest = Math.Max(0, totalXp);
        while (rest >= XpForNext(level))
        {
            rest -= XpForNext(level);
            level++;
        }
        return new LevelInfo(level, rest, XpForNext(level), TitleFor(level), Titles.FirstOrDefault(t => t.Level > level));
    }

    public static FoxTitle TitleFor(int level) => Titles.Last(t => t.Level <= level);

    public static IEnumerable<FoxTitle> TitlesUnlockedBetween(int fromLevelExclusive, int toLevelInclusive) =>
        Titles.Where(t => t.Level > fromLevelExclusive && t.Level <= toLevelInclusive);

    /// <summary>XP for solving a task: the topic's base XP, raised for harder tasks (×1, ×1,5, ×2).</summary>
    public static int TaskXp(Topic topic, int difficulty) =>
        (int)Math.Round(topic.Info().BaseXp * (1 + 0.5 * (Math.Clamp(difficulty, 1, 3) - 1)), MidpointRounding.AwayFromZero);

    /// <summary>First try: full XP. Second try: half. After that: no XP, but the solution is shown.</summary>
    public const int MaxAttempts = 2;

    public static int XpForAttempt(int fullXp, int attempt) => attempt switch
    {
        1 => fullXp,
        2 => (fullXp + 1) / 2,
        _ => 0,
    };

    /// <summary>Training outside the daily tasks gives half XP, so the daily tasks stay the main thing.</summary>
    public static int TrainingXp(int xp) => (xp + 1) / 2;

    /// <summary>Bonus for finishing all daily tasks; grows with the streak (days in a row).</summary>
    public static int DailyBonus(int streak) => 40 + 10 * Math.Min(Math.Max(streak, 1) - 1, 6);
}
