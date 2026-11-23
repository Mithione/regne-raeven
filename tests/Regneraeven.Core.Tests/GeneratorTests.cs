namespace Regneraeven.Core.Tests;

public class GeneratorTests
{
    public static IEnumerable<object[]> Cases() =>
        from t in TopicCatalog.Game
        from d in new[] { 1, 2, 3 }
        select new object[] { t.Topic, d };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Every_generated_task_accepts_its_own_answer(Topic topic, int difficulty)
    {
        var gen = new TaskGenerator(new Random((int)topic * 31 + difficulty));
        for (var i = 0; i < 400; i++)
        {
            var task = gen.Create(topic, difficulty);
            Assert.Equal(topic, task.Topic);
            Assert.False(string.IsNullOrWhiteSpace(task.Prompt));
            Assert.NotEmpty(task.Solution);
            Assert.False(string.IsNullOrWhiteSpace(task.Hint));

            CheckResult result;
            if (task.Kind == AnswerKind.Point)
            {
                var xy = task.Answer.Split(';');
                result = AnswerChecker.Check(task, null, xy[0], xy[1]);
                Assert.InRange(int.Parse(xy[0]), task.Visual!.Min, task.Visual.Max);
                Assert.InRange(int.Parse(xy[1]), task.Visual.Min, task.Visual.Max);
            }
            else
            {
                result = AnswerChecker.Check(task, task.AnswerText);
            }
            Assert.True(result.Correct, $"{task.Prompt} {task.Display} – facit »{task.AnswerText}« blev ikke godkendt: {result.Message}");
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Expression_tasks_show_an_expression_that_equals_the_answer(Topic topic, int difficulty)
    {
        var gen = new TaskGenerator(new Random(7 + difficulty));
        for (var i = 0; i < 200; i++)
        {
            var task = gen.Create(topic, difficulty);
            if (task.Display is not { } display || !display.EndsWith(" = ?")) continue;
            var expr = display[..^4];
            if (!Expression.TryParse(expr, out var node, out _)) continue;
            Assert.Equal(MathTask.FromCanonical(task.Answer), Expression.Evaluate(node));
        }
    }

    [Fact]
    public void Simple_arithmetic_answers_are_positive_whole_numbers()
    {
        var gen = new TaskGenerator(new Random(3));
        foreach (var topic in new[] { Topic.Plus, Topic.Minus, Topic.Gange, Topic.Division, Topic.Regnehierarki })
            for (var d = 1; d <= 3; d++)
                for (var i = 0; i < 300; i++)
                {
                    var v = MathTask.FromCanonical(gen.Create(topic, d).Answer);
                    Assert.True(v.IsInteger && v.Sign >= 0, $"{topic} {d}: {v}");
                }
    }

    [Fact]
    public void Daily_plan_is_stable_for_a_day_and_covers_the_curriculum()
    {
        var day = new DateOnly(2026, 11, 23);
        var a = DailyPlan.Create(1, day, 5, 1);
        var b = DailyPlan.Create(1, day, 5, 1);
        Assert.Equal(DailyPlan.TaskCount, a.Count);
        Assert.Equal(a.Select(t => t.Prompt + t.Display), b.Select(t => t.Prompt + t.Display));
        Assert.Contains(a, t => t.Topic == Topic.Broeker);
        Assert.Contains(a, t => t.Topic == Topic.Geometri);
        Assert.Contains(a, t => t.Topic == Topic.Koordinatsystem);
        var other = DailyPlan.Create(1, day.AddDays(1), 5, 1);
        Assert.NotEqual(a.Select(t => t.Prompt + t.Display), other.Select(t => t.Prompt + t.Display));
    }
}
