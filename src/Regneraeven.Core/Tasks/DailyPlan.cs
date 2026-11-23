using Regneraeven.Core.Topics;

namespace Regneraeven.Core.Tasks;

/// <summary>
/// Today's tasks: a fixed mix across the curriculum, the same for a child all day.
/// Easy arithmetic first, the topics worth more XP after.
/// </summary>
public static class DailyPlan
{
    public const int TaskCount = 8;

    private static readonly Topic[][] Slots =
    [
        [Topic.Plus, Topic.Minus],
        [Topic.Gange, Topic.Division],
        [Topic.Regnehierarki, Topic.NegativeTal],
        [Topic.Decimaltal, Topic.Enheder],
        [Topic.Broeker],
        [Topic.Procent, Topic.Ligninger],
        [Topic.Geometri],
        [Topic.Koordinatsystem],
    ];

    /// <summary>Stable seed (string.GetHashCode differs per process).</summary>
    public static int Seed(int profileId, DateOnly date) => unchecked(profileId * 7919 + date.DayNumber * 31);

    /// <summary>
    /// Grade 5 mostly gets "let" and "middel", grade 6 mostly "middel" and "svær".
    /// Higher levels nudge the difficulty up.
    /// </summary>
    public static int PickDifficulty(Random rng, int grade, int level)
    {
        var baseDifficulty = grade <= 5 ? 1 : 2;
        var upChance = 0.4 + Math.Min(level, 30) * 0.01;
        var d = baseDifficulty + (rng.NextDouble() < upChance ? 1 : 0);
        return Math.Clamp(d, 1, 3);
    }

    public static IReadOnlyList<MathTask> Create(int profileId, DateOnly date, int grade, int level)
    {
        var rng = new Random(Seed(profileId, date));
        var generator = new TaskGenerator(rng);
        return Slots.Select(slot => generator.Create(slot[rng.Next(slot.Length)], PickDifficulty(rng, grade, level))).ToList();
    }
}
