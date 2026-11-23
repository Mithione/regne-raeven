using System.Globalization;

namespace Regneraeven.Core.Homework;

public enum DueState
{
    /// <summary>Answered – nothing to worry about.</summary>
    Done,
    /// <summary>Not answered and the date has passed.</summary>
    Overdue,
    /// <summary>Due today.</summary>
    Today,
    /// <summary>Due tomorrow.</summary>
    Tomorrow,
    /// <summary>Due within the next week.</summary>
    Soon,
    /// <summary>More than a week away, or no date.</summary>
    Later,
}

public sealed record DueInfo(DueState State, string Text);

/// <summary>Homework due dates in words a child understands: "I morgen", "På fredag", "2 dage for sent".</summary>
public static class HomeworkDue
{
    private static readonly CultureInfo Da = CultureInfo.GetCultureInfo("da-DK");

    public static DueInfo Describe(DateOnly? due, DateOnly today, bool done)
    {
        if (due is not { } d) return new DueInfo(done ? DueState.Done : DueState.Later, done ? "Lavet" : "Ingen dato");
        var days = d.DayNumber - today.DayNumber;
        if (done) return new DueInfo(DueState.Done, "Lavet · " + Short(d));
        return days switch
        {
            < -1 => new DueInfo(DueState.Overdue, $"{-days} dage for sent"),
            -1 => new DueInfo(DueState.Overdue, "1 dag for sent"),
            0 => new DueInfo(DueState.Today, "I dag"),
            1 => new DueInfo(DueState.Tomorrow, "I morgen"),
            < 7 => new DueInfo(DueState.Soon, "På " + d.ToString("dddd", Da)),
            _ => new DueInfo(DueState.Later, Short(d)),
        };
    }

    /// <summary>"fre. 28. nov."</summary>
    public static string Short(DateOnly d) => d.ToString("ddd d. MMM", Da);

    /// <summary>"fredag d. 28. november"</summary>
    public static string Long(DateOnly d) => d.ToString("dddd 'd.' d. MMMM", Da);

    /// <summary>Suggested due date: the next school day (Friday → Monday, weekend → Monday).</summary>
    public static DateOnly NextSchoolDay(DateOnly today)
    {
        var next = today.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) next = next.AddDays(1);
        return next;
    }

    /// <summary>Order for a homework list: overdue first, then by date; no date last; done at the very end.</summary>
    public static IOrderedEnumerable<T> InOrder<T>(IEnumerable<T> items, Func<T, DateOnly?> due, Func<T, bool> done) =>
        items.OrderBy(done)
            .ThenBy(i => due(i) is null)
            .ThenBy(i => due(i) ?? DateOnly.MaxValue);
}
