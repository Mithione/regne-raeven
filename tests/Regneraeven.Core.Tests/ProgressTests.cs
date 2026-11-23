namespace Regneraeven.Core.Tests;

public class ProgressTests
{
    [Fact]
    public void Levels_start_at_one_and_grow()
    {
        Assert.Equal(1, Leveling.FromXp(0).Level);
        Assert.Equal(1, Leveling.FromXp(99).Level);
        Assert.Equal(2, Leveling.FromXp(100).Level);
        Assert.Equal(3, Leveling.FromXp(225).Level);
        Assert.Equal("Rævehvalp", Leveling.FromXp(0).Title.Name);
    }

    [Fact]
    public void Titles_unlock_every_five_to_ten_levels()
    {
        var levels = Leveling.Titles.Select(t => t.Level).ToList();
        Assert.Equal(1, levels[0]);
        for (var i = 1; i < levels.Count - 1; i++)
            Assert.InRange(levels[i + 1] - levels[i], 5, 15);
        Assert.Equal("Nysgerrig ræv", Leveling.TitleFor(5).Name);
        Assert.Equal("Nysgerrig ræv", Leveling.TitleFor(9).Name);
        Assert.Single(Leveling.TitlesUnlockedBetween(4, 6));
    }

    [Fact]
    public void Harder_topics_give_more_xp()
    {
        Assert.True(Leveling.TaskXp(Topic.Plus, 1) < Leveling.TaskXp(Topic.Gange, 1));
        Assert.True(Leveling.TaskXp(Topic.Gange, 1) < Leveling.TaskXp(Topic.Decimaltal, 1));
        Assert.True(Leveling.TaskXp(Topic.Decimaltal, 1) < Leveling.TaskXp(Topic.Broeker, 1));
        Assert.True(Leveling.TaskXp(Topic.Broeker, 1) < Leveling.TaskXp(Topic.Koordinatsystem, 1));
        Assert.True(Leveling.TaskXp(Topic.Plus, 1) < Leveling.TaskXp(Topic.Plus, 3));
        Assert.Equal(50, Leveling.TaskXp(Topic.Geometri, 3));
    }

    [Theory]
    [InlineData("3/4 + 1/6", new[] { Topic.Plus, Topic.Broeker }, new[] { Topic.Division })]
    [InlineData("4 + 3 · 5", new[] { Topic.Plus, Topic.Gange, Topic.Regnehierarki }, new Topic[0])]
    [InlineData("432 : 6", new[] { Topic.Division }, new[] { Topic.Broeker })]
    [InlineData("2,5 · 4", new[] { Topic.Gange, Topic.Decimaltal }, new Topic[0])]
    [InlineData("Hvad er arealet af et rektangel, der er 4 cm og 6 cm?", new[] { Topic.Tekststykke, Topic.Geometri }, new Topic[0])]
    [InlineData("2 · x + 5 = 21", new[] { Topic.Ligninger }, new[] { Topic.Tekststykke })]
    [InlineData("Hvad er 25 % af 80?", new[] { Topic.Procent }, new Topic[0])]
    [InlineData("(12 - 4) · 3/4", new[] { Topic.Minus, Topic.Regnehierarki }, new[] { Topic.NegativeTal })]
    [InlineData("12 − 4", new[] { Topic.Minus }, new[] { Topic.NegativeTal })]
    [InlineData("-3 + 8", new[] { Topic.NegativeTal }, new Topic[0])]
    [InlineData("5 − (−3)", new[] { Topic.NegativeTal }, new Topic[0])]
    [InlineData("4 · -2", new[] { Topic.NegativeTal }, new Topic[0])]
    public void Suggests_tags(string text, Topic[] expected, Topic[] notExpected)
    {
        var tags = TopicDetector.Suggest(text);
        foreach (var t in expected) Assert.Contains(t, tags);
        foreach (var t in notExpected) Assert.DoesNotContain(t, tags);
    }

    [Fact]
    public void Rational_formats_the_danish_way()
    {
        Assert.Equal("2,5", new Rational(5, 2).ToDecimalString());
        Assert.Equal("1 3/4", new Rational(7, 4).ToMixedString());
        Assert.Equal("12.500", Rational.FormatInteger(12500));
        Assert.Equal("1250", Rational.FormatInteger(1250));
        Assert.Equal("0,333", new Rational(1, 3).ToDecimalString(3));
        Assert.Equal("-0,5", new Rational(-1, 2).ToDecimalString());
    }
}

public class UnlockTests
{
    [Fact]
    public void Every_title_level_unlocks_a_fox_avatar()
    {
        Assert.Equal(Leveling.Titles.Select(t => t.Level), Unlocks.Avatars.Select(a => a.Level));
        Assert.All(Unlocks.Avatars, a => Assert.True(a.Key.Length <= 16, a.Key)); // fits the database column
        Assert.Equal(Unlocks.Avatars.Count, Unlocks.Avatars.Select(a => a.Id).Distinct().Count());
    }

    [Fact]
    public void Only_unlocked_foxes_can_be_used()
    {
        Assert.False(Unlocks.CanUse("🐼", 99)); // no more start animals – foxes only
        Assert.True(Unlocks.CanUse("fox:hvalp", 1));
        Assert.False(Unlocks.CanUse("fox:briller", 9));
        Assert.True(Unlocks.CanUse("fox:briller", 10));
        Assert.False(Unlocks.CanUse("fox:findes-ikke", 99));
        Assert.False(Unlocks.CanUse("<script>", 99));
        Assert.Equal(["briller"], Unlocks.UnlockedBetween(9, 12).Select(a => a.Id));
        Assert.Equal("lyn", Unlocks.Next(10)!.Id);
    }

    [Theory]
    [InlineData(UnlockKind.Frame)]
    [InlineData(UnlockKind.Background)]
    [InlineData(UnlockKind.Theme)]
    public void Each_kind_has_a_free_default_and_unique_ids(UnlockKind kind)
    {
        var items = Unlocks.Of(kind);
        Assert.Equal(1, items[0].Level);
        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
        Assert.All(items, i => Assert.True(i.Id.Length <= 24 && i.Id.All(c => c is >= 'a' and <= 'z'), i.Id)); // safe as css class
        Assert.Equal(items.OrderBy(i => i.Level).Select(i => i.Id), items.Select(i => i.Id));
    }

    [Fact]
    public void Something_unlocks_at_almost_every_early_level()
    {
        for (var level = 2; level <= 10; level++)
        {
            var anything = Unlocks.UnlockedBetween(level - 1, level).Any() || Unlocks.CosmeticsUnlockedBetween(level - 1, level).Any();
            Assert.True(anything, $"Intet nyt på level {level}");
        }
    }

    [Fact]
    public void Locked_or_unknown_choices_fall_back_to_the_default()
    {
        Assert.Equal("klassisk", Unlocks.Current(UnlockKind.Frame, "regnbue", 5).Id);
        Assert.Equal("regnbue", Unlocks.Current(UnlockKind.Frame, "regnbue", 27).Id);
        Assert.Equal("raev", Unlocks.Current(UnlockKind.Theme, "findes-ikke", 99).Id);
        Assert.False(Unlocks.CanUse(UnlockKind.Theme, "skov", 3));
        Assert.True(Unlocks.CanUse(UnlockKind.Theme, "skov", 4));
        Assert.Equal((3, "🖼️", "Ramme: Prikker"), Unlocks.NextAnything(2));
        Assert.Contains(Unlocks.CosmeticsUnlockedBetween(7, 8), n => n.Name == "Neon");
    }
}
