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
