using Regneraeven.Core.Topics;

namespace Regneraeven.Web.Data;

/// <summary>A child using the program. Several children can share one computer.</summary>
public class Profile
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>The drawn fox, e.g. "fox:briller" (see Core.Progress.Unlocks).</summary>
    public string Avatar { get; set; } = "fox:hvalp";
    /// <summary>Chosen unlocks: frame around the avatar, background behind it and colour theme (null = default).</summary>
    public string? Frame { get; set; }
    public string? Background { get; set; }
    public string? Theme { get; set; }
    public string Color { get; set; } = "#f26b1d";
    /// <summary>Read-aloud voice: "kvinde" or "mand" – the program picks a Danish voice of that kind if one is installed.</summary>
    public string Voice { get; set; } = "kvinde";
    /// <summary>Read-aloud speed: "rolig" (a bit slower) or "normal".</summary>
    public string SpeechRate { get; set; } = "rolig";
    /// <summary>Klassetrin (5 or 6) – decides how hard the daily tasks are.</summary>
    public int Grade { get; set; } = 5;
    public int Xp { get; set; }
    /// <summary>Gold (guld) – earned with XP (1 per 4 XP) and spent in the room shop.</summary>
    public int Gold { get; set; }
    /// <summary>What stands where in the fox room, as JSON: {"Wall":"mint","Bed":"trae"}.</summary>
    public string? Room { get; set; }
    public int Streak { get; set; }
    public DateOnly? LastDailyDone { get; set; }
    /// <summary>Days off (fridage) that cover a missed day – earned every 7th day in a row, max 2.</summary>
    public int StreakFreezes { get; set; }
    /// <summary>Longest streak ever – milestones count from this, so they can't be lost.</summary>
    public int BestStreak { get; set; }
    /// <summary>Chosen milestone rewards as JSON: {"7":"flammer"}.</summary>
    public string? StreakClaims { get; set; }
    /// <summary>Real-life rewards a parent wrote per milestone as JSON: {"7":"Is på fredag"}.</summary>
    public string? StreakRewards { get; set; }
    /// <summary>The title level the child chose to show (null = newest unlocked).</summary>
    public int? ShownTitleLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The parent account that owns this profile on the website (null in the Windows program).</summary>
    public string? OwnerId { get; set; }
}

public enum ProblemStatus
{
    /// <summary>Saved, no answer yet.</summary>
    Open = 0,
    /// <summary>The child has written an answer; the calculator is unlocked.</summary>
    Answered = 1,
    Correct = 2,
    Wrong = 3,
}

/// <summary>A maths problem the child saved (homework, something from the book …).</summary>
public class SavedProblem
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public Profile? Profile { get; set; }
    public string Text { get; set; } = "";
    public string? Note { get; set; }
    public string? MyAnswer { get; set; }
    public ProblemStatus Status { get; set; }
    /// <summary>Homework (lektie) with an optional hand-in date.</summary>
    public bool IsHomework { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ProblemTag> Tags { get; set; } = [];

    /// <summary>Homework counts as done once the child has written an answer.</summary>
    public bool IsDone => Status != ProblemStatus.Open;
}

/// <summary>A tag: either a topic slug ("broeker") or the child's own word ("lektier uge 47").</summary>
public class ProblemTag
{
    public int Id { get; set; }
    public int SavedProblemId { get; set; }
    public string Name { get; set; } = "";

    public TopicInfo? Topic => TopicCatalog.FromSlug(Name);
}

public enum GameTaskStatus { Open = 0, Solved = 1, Revealed = 2 }

/// <summary>A task in the game: one of today's daily tasks or a training task.</summary>
public class GameTask
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public DateOnly Day { get; set; }
    public bool IsDaily { get; set; }
    public int Slot { get; set; }
    public Topic Topic { get; set; }
    public int Difficulty { get; set; }
    /// <summary>The generated <see cref="Core.Tasks.MathTask"/> as JSON (answer included – never sent to the browser before it is solved).</summary>
    public string TaskJson { get; set; } = "";
    public int Attempts { get; set; }
    public GameTaskStatus Status { get; set; }
    public int XpAwarded { get; set; }
    public string? LastAnswer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
}

/// <summary>Every XP gain, so we can show "XP this week" and rebuild totals.</summary>
public class XpEvent
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int Amount { get; set; }
    public int Gold { get; set; }
    public string Reason { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>One run through a 10-finger lesson.</summary>
public class TypingResult
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    /// <summary>Id from Core.Typing.TypingCourse (stable).</summary>
    public string LessonId { get; set; } = "";
    public int Stars { get; set; }
    public int Accuracy { get; set; }
    public int Wpm { get; set; }
    public int XpAwarded { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>A thing bought for the fox room (gold only – never real money).</summary>
public class RoomPurchase
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    /// <summary>Id from Core.Room.RoomCatalog (stable).</summary>
    public string ItemId { get; set; } = "";
    public int Price { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
