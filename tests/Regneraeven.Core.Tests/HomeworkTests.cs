using Regneraeven.Core.Homework;

namespace Regneraeven.Core.Tests;

public class HomeworkTests
{
    private static readonly DateOnly Monday = new(2026, 11, 23);

    [Theory]
    [InlineData(-3, DueState.Overdue, "3 dage for sent")]
    [InlineData(-1, DueState.Overdue, "1 dag for sent")]
    [InlineData(0, DueState.Today, "I dag")]
    [InlineData(1, DueState.Tomorrow, "I morgen")]
    [InlineData(4, DueState.Soon, "På fredag")]
    [InlineData(10, DueState.Later, "tors. 3. dec.")]
    public void Describes_due_dates(int days, DueState state, string text)
    {
        var info = HomeworkDue.Describe(Monday.AddDays(days), Monday, done: false);
        Assert.Equal(state, info.State);
        Assert.Equal(text, info.Text);
    }

    [Fact]
    public void Done_homework_is_never_late()
    {
        var info = HomeworkDue.Describe(Monday.AddDays(-5), Monday, done: true);
        Assert.Equal(DueState.Done, info.State);
        Assert.StartsWith("Lavet", info.Text);
        Assert.Equal(DueState.Later, HomeworkDue.Describe(null, Monday, false).State);
    }

    [Theory]
    [InlineData(2026, 11, 23, 2026, 11, 24)] // Monday → Tuesday
    [InlineData(2026, 11, 27, 2026, 11, 30)] // Friday → Monday
    [InlineData(2026, 11, 28, 2026, 11, 30)] // Saturday → Monday
    [InlineData(2026, 11, 29, 2026, 11, 30)] // Sunday → Monday
    public void Suggests_next_school_day(int y, int m, int d, int ey, int em, int ed) =>
        Assert.Equal(new DateOnly(ey, em, ed), HomeworkDue.NextSchoolDay(new DateOnly(y, m, d)));

    [Fact]
    public void Orders_by_date_with_done_and_undated_last()
    {
        var items = new (string Name, DateOnly? Due, bool Done)[]
        {
            ("lavet", Monday, true), ("uden dato", null, false), ("fredag", Monday.AddDays(4), false),
            ("for sent", Monday.AddDays(-2), false), ("i morgen", Monday.AddDays(1), false),
        };
        var ordered = HomeworkDue.InOrder(items, i => i.Due, i => i.Done).Select(i => i.Name);
        Assert.Equal(["for sent", "i morgen", "fredag", "uden dato", "lavet"], ordered);
    }
}
