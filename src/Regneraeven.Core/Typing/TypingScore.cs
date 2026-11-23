namespace Regneraeven.Core.Typing;

/// <summary>The result of one run through an exercise.</summary>
public sealed record TypingRun(int Accuracy, int Wpm, int Stars, string Message);

/// <summary>
/// Stars and XP for typing. Precision counts most: below 90 % there are no stars, however fast you are.
/// Speed only decides between 2 and 3 stars.
/// </summary>
public static class TypingScore
{
    public const int MinAccuracyForStar = 90;

    /// <summary>Words per minute needed for ★★ and ★★★ – kid-friendly, a bit higher for harder lessons.</summary>
    public static (int TwoStars, int ThreeStars) SpeedTargets(int difficulty) => difficulty switch
    {
        1 => (8, 14),
        2 => (10, 18),
        _ => (8, 14), // numbers and signs are slower to type
    };

    /// <param name="chars">Characters in the text (all typed correctly in the end).</param>
    /// <param name="errors">Wrong key presses along the way.</param>
    public static TypingRun Evaluate(int difficulty, int chars, int errors, double seconds)
    {
        chars = Math.Max(0, chars);
        errors = Math.Max(0, errors);
        seconds = Math.Max(1, seconds);
        var accuracy = chars == 0 ? 0 : (int)Math.Floor(100.0 * chars / (chars + errors));
        // A "word" is 5 characters, as in all typing tests.
        var wpm = (int)Math.Round(chars / 5.0 / (seconds / 60.0));
        var (two, three) = SpeedTargets(difficulty);

        var stars = accuracy < MinAccuracyForStar ? 0
            : accuracy >= 98 && wpm >= three ? 3
            : accuracy >= 95 && wpm >= two ? 2
            : 1;

        var message = stars switch
        {
            0 => "Prøv igen – skriv lidt langsommere, så du rammer de rigtige taster. Præcision er vigtigere end fart.",
            1 => accuracy < 95 ? "Godt! Prøv at ramme endnu flere taster rigtigt for at få flere stjerner." : "Godt! Når fingrene finder tasterne af sig selv, går det hurtigere.",
            2 => "Flot! Lidt mere fart og næsten ingen fejl giver tre stjerner.",
            _ => "Fantastisk – tre stjerner! 🌟",
        };
        return new TypingRun(accuracy, wpm, stars, message);
    }

    /// <summary>XP for each new star in a lesson (first time you reach it).</summary>
    public static int XpPerStar(int difficulty) => difficulty switch { 1 => 8, 2 => 12, _ => 16 };

    /// <summary>Small XP for practising again without beating your best – limited per day.</summary>
    public const int PracticeXp = 2;
    public const int PracticeRunsPerDay = 10;

    /// <summary>
    /// XP for a run: full XP for every star above your best so far; otherwise a little practice XP
    /// (only with at least one star, and only the first <see cref="PracticeRunsPerDay"/> times a day).
    /// </summary>
    public static int Xp(int difficulty, int bestStarsBefore, int stars, int practiceRunsToday)
    {
        if (stars > bestStarsBefore) return (stars - bestStarsBefore) * XpPerStar(difficulty);
        return stars >= 1 && practiceRunsToday < PracticeRunsPerDay ? PracticeXp : 0;
    }

    /// <summary>A lesson is open when the one before it has at least one star (the first is always open).</summary>
    public static bool IsOpen(TypingLesson lesson, IReadOnlyDictionary<string, int> bestStars)
    {
        var i = TypingCourse.IndexOf(lesson);
        return i <= 0 || bestStars.TryGetValue(TypingCourse.Lessons[i - 1].Id, out var s) && s >= 1;
    }
}
