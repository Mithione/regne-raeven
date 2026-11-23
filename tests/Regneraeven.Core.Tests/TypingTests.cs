using Regneraeven.Core.Typing;

namespace Regneraeven.Core.Tests;

public class TypingTests
{
    public static IEnumerable<object[]> LessonIds() => TypingCourse.Lessons.Select(l => new object[] { l.Id });

    /// <summary>Every lesson may only use keys taught in that lesson or earlier.</summary>
    [Theory]
    [MemberData(nameof(LessonIds))]
    public void Texts_only_use_keys_learned_so_far(string id)
    {
        var lesson = TypingCourse.Find(id)!;
        var index = TypingCourse.IndexOf(lesson);
        var learned = new HashSet<char>(" ");
        var capitals = false;
        foreach (var l in TypingCourse.Lessons.Take(index + 1))
        {
            if (l.NewKeys == "⇧") capitals = true;
            else foreach (var c in l.NewKeys) learned.Add(c);
        }
        learned.UnionWith("å"); // å is taught with p (same finger) – see the tip in "pyw"

        var rng = new Random(1);
        for (var run = 0; run < 30; run++)
        {
            var text = TypingCourse.MakeText(lesson, rng);
            foreach (var c in text)
            {
                var ok = learned.Contains(c) || capitals && char.IsUpper(c) && learned.Contains(char.ToLowerInvariant(c));
                Assert.True(ok, $"Lektion »{lesson.Title}« bruger »{c}« før den er lært: {text}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(LessonIds))]
    public void Texts_have_a_sensible_length(string id)
    {
        var lesson = TypingCourse.Find(id)!;
        var text = TypingCourse.MakeText(lesson, new Random(2));
        Assert.InRange(text.Length, 60, 140);
        Assert.DoesNotContain("  ", text.Replace("   ", "|"));
        Assert.Equal(text.Trim(), text);
    }

    [Fact]
    public void Course_order_and_ids_are_stable()
    {
        Assert.Equal("fj", TypingCourse.Lessons[0].Id);
        Assert.Equal("regnestykker", TypingCourse.Lessons[^1].Id);
        Assert.Equal(TypingCourse.Lessons.Count, TypingCourse.Lessons.Select(l => l.Id).Distinct().Count());
        Assert.Equal("dk", TypingCourse.Next(TypingCourse.Lessons[0])!.Id);
        Assert.Null(TypingCourse.Next(TypingCourse.Lessons[^1]));
    }

    [Theory]
    [InlineData(100, 0, 60, 3)]    // 100 % and 20 wpm
    [InlineData(100, 1, 60, 3)]    // 99 %
    [InlineData(100, 4, 60, 2)]    // 96 %
    [InlineData(100, 8, 60, 1)]    // 92 %
    [InlineData(100, 15, 60, 0)]   // 86 % – too many errors, speed doesn't help
    [InlineData(100, 0, 600, 1)]   // 100 % but very slow (2 wpm) – still one star
    public void Precision_counts_most(int chars, int errors, double seconds, int stars) =>
        Assert.Equal(stars, TypingScore.Evaluate(1, chars, errors, seconds).Stars);

    [Fact]
    public void Wpm_uses_five_characters_per_word()
    {
        var run = TypingScore.Evaluate(1, 100, 0, 60);
        Assert.Equal(20, run.Wpm);
        Assert.Equal(100, run.Accuracy);
    }

    [Fact]
    public void Xp_rewards_new_stars_and_limits_practice()
    {
        Assert.Equal(24, TypingScore.Xp(1, 0, 3, 0));          // 3 new stars × 8
        Assert.Equal(12, TypingScore.Xp(2, 1, 2, 0));          // 1 new star × 12
        Assert.Equal(TypingScore.PracticeXp, TypingScore.Xp(1, 3, 3, 0));
        Assert.Equal(0, TypingScore.Xp(1, 3, 3, TypingScore.PracticeRunsPerDay));
        Assert.Equal(0, TypingScore.Xp(1, 2, 0, 0));           // no stars, no XP
    }

    [Fact]
    public void Lessons_open_one_after_another()
    {
        var best = new Dictionary<string, int>();
        Assert.True(TypingScore.IsOpen(TypingCourse.Lessons[0], best));
        Assert.False(TypingScore.IsOpen(TypingCourse.Lessons[1], best));
        best["fj"] = 1;
        Assert.True(TypingScore.IsOpen(TypingCourse.Lessons[1], best));
        Assert.False(TypingScore.IsOpen(TypingCourse.Lessons[2], best));
    }
}
