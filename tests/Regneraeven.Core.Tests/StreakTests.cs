namespace Regneraeven.Core.Tests;

public class StreakTests
{
    private static readonly DateOnly Day = new(2026, 11, 23);

    [Fact]
    public void Consecutive_days_grow_the_streak()
    {
        var a = Streaks.Advance(null, Day, 0, 0);
        Assert.Equal(1, a.Streak);
        var b = Streaks.Advance(Day, Day.AddDays(1), a.Streak, a.Freezes);
        Assert.Equal(2, b.Streak);
        Assert.Equal(b, Streaks.Advance(Day.AddDays(1), Day.AddDays(1), b.Streak, b.Freezes) with { }); // same day twice: unchanged
    }

    [Fact]
    public void Every_seventh_day_earns_a_day_off_max_two()
    {
        Assert.Equal((7, 1, true), (Streaks.Advance(Day, Day.AddDays(1), 6, 0) is var r ? (r.Streak, r.Freezes, r.EarnedFreeze) : default));
        Assert.Equal(2, Streaks.Advance(Day, Day.AddDays(1), 13, 1).Freezes);
        Assert.Equal(2, Streaks.Advance(Day, Day.AddDays(1), 20, 2).Freezes); // already full
    }

    [Fact]
    public void Missed_days_use_days_off_or_reset()
    {
        // Missed 1 day, has 1 day off: streak continues.
        var r = Streaks.Advance(Day, Day.AddDays(2), 10, 1);
        Assert.Equal(11, r.Streak);
        Assert.Equal(0, r.Freezes);
        Assert.Equal(1, r.FreezesUsed);
        // Missed 2 days, only 1 day off: starts over.
        Assert.Equal(1, Streaks.Advance(Day, Day.AddDays(3), 10, 1).Streak);
    }

    [Theory]
    [InlineData(0, 0, 5)]   // done today
    [InlineData(1, 0, 5)]   // done yesterday – still alive
    [InlineData(2, 0, 0)]   // missed a day, no days off
    [InlineData(2, 1, 5)]   // missed a day, covered by a day off
    [InlineData(4, 2, 0)]   // missed 3 days, only 2 days off
    public void Live_streak(int daysSince, int freezes, int expected) =>
        Assert.Equal(expected, Streaks.Live(Day, Day.AddDays(daysSince), 5, freezes));

    [Fact]
    public void Milestones_start_early_and_grow()
    {
        Assert.Equal([3, 7, 14, 30, 50, 100], Streaks.Milestones.Select(m => m.Days));
        Assert.True(Streaks.Milestones.Zip(Streaks.Milestones.Skip(1)).All(p => p.First.BonusXp < p.Second.BonusXp));
        Assert.All(Streaks.Milestones, m => Assert.Equal(2, m.Choices.Count));
        Assert.Equal(7, Streaks.Next(3)!.Days);
        Assert.Null(Streaks.Next(100));
        Assert.Equal([7, 14], Streaks.ReachedBetween(5, 14).Select(m => m.Days));
    }

    [Fact]
    public void Streak_items_have_unique_safe_ids_per_kind()
    {
        foreach (var kind in Enum.GetValues<UnlockKind>())
        {
            var ids = Unlocks.Of(kind).Select(i => i.Id).Concat(Streaks.Items.Where(i => i.Kind == kind).Select(i => i.Id)).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
        Assert.All(Streaks.Items, i => Assert.True(i.IsStreakItem && i.Id.All(c => c is >= 'a' and <= 'z'), i.Id));
    }

    [Fact]
    public void Streak_items_are_only_usable_once_chosen()
    {
        Assert.False(Unlocks.CanUse(UnlockKind.Frame, "flammer", 99));
        var owned = Streaks.OwnedItems(Streaks.FormatClaims(new() { [7] = "flammer" }));
        Assert.True(Unlocks.CanUse(UnlockKind.Frame, "flammer", 1, owned));
        Assert.Equal("flammer", Unlocks.Current(UnlockKind.Frame, "flammer", 1, owned).Id);
        Assert.Equal("klassisk", Unlocks.Current(UnlockKind.Frame, "flammer", 99).Id);
    }

    [Fact]
    public void A_milestone_can_be_claimed_once_with_one_of_its_choices()
    {
        var seven = Streaks.Milestones.Single(m => m.Days == 7);
        var none = new Dictionary<int, string>();
        Assert.False(Streaks.CanClaim(seven, "flammer", 6, none));      // not reached
        Assert.True(Streaks.CanClaim(seven, "flammer", 7, none));
        Assert.True(Streaks.CanClaim(seven, "gloed", 12, none));
        Assert.False(Streaks.CanClaim(seven, "nordlys", 20, none));     // belongs to 14
        Assert.False(Streaks.CanClaim(seven, "gloed", 9, new Dictionary<int, string> { [7] = "flammer" })); // already chosen
        Assert.Empty(Streaks.ParseClaims("not json"));
    }
}
