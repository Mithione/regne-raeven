using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Core.Tasks;
using Regneraeven.Core.Topics;
using Regneraeven.Web.Data;

namespace Regneraeven.Web.Services;

/// <summary>A newly unlocked fox, with its drawing so the level-up dialog can show it.</summary>
public sealed record NewAvatar(string Key, string Name, string Description, string Svg);

public sealed record LevelUp(int From, int To, IReadOnlyList<FoxTitle> NewTitles, IReadOnlyList<NewAvatar> NewAvatars);

public sealed record AnswerOutcome(
    bool Correct,
    string Message,
    int XpAwarded,
    int AttemptsLeft,
    bool Finished,
    string? AnswerText,
    IReadOnlyList<string>? Solution,
    int DailyBonus,
    int Streak,
    LevelUp? LevelUp,
    int TotalXp,
    int Level,
    int LevelPercent,
    string TitleName);

/// <summary>The game: today's tasks, training, XP, levels and the daily streak.</summary>
public class GameService(AppDb db, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public static MathTask Read(GameTask t) => JsonSerializer.Deserialize<MathTask>(t.TaskJson, Json)!;

    private static GameTask Store(Profile p, MathTask task, DateOnly day, bool daily, int slot) => new()
    {
        ProfileId = p.Id,
        Day = day,
        IsDaily = daily,
        Slot = slot,
        Topic = task.Topic,
        Difficulty = task.Difficulty,
        TaskJson = JsonSerializer.Serialize(task, Json),
    };

    /// <summary>Today's tasks – created the first time the child looks at them today.</summary>
    public async Task<List<GameTask>> GetDailyAsync(Profile p)
    {
        var today = Today;
        var tasks = await db.GameTasks.Where(t => t.ProfileId == p.Id && t.Day == today && t.IsDaily).OrderBy(t => t.Slot).ToListAsync();
        if (tasks.Count > 0) return tasks;

        var level = Leveling.FromXp(p.Xp).Level;
        tasks = DailyPlan.Create(p.Id, today, p.Grade, level).Select((task, i) => Store(p, task, today, true, i)).ToList();
        db.GameTasks.AddRange(tasks);
        await db.SaveChangesAsync();
        return tasks;
    }

    public async Task<GameTask> CreateTrainingAsync(Profile p, Topic topic, int difficulty)
    {
        if (!topic.Info().InGame) throw new UserFacingException("Den slags opgaver kan man ikke træne i spillet.");
        // Throw away unanswered training tasks so the list doesn't grow forever.
        await db.GameTasks.Where(t => t.ProfileId == p.Id && !t.IsDaily && t.Status == GameTaskStatus.Open && t.Attempts == 0).ExecuteDeleteAsync();
        var task = new TaskGenerator(Random.Shared).Create(topic, difficulty);
        var stored = Store(p, task, Today, false, 0);
        db.GameTasks.Add(stored);
        await db.SaveChangesAsync();
        return stored;
    }

    public async Task<GameTask> GetAsync(Profile p, int id) =>
        await db.GameTasks.FirstOrDefaultAsync(t => t.Id == id && t.ProfileId == p.Id)
        ?? throw new UserFacingException("Opgaven findes ikke.");

    public static int FullXp(GameTask t)
    {
        var xp = Leveling.TaskXp(t.Topic, t.Difficulty);
        return t.IsDaily ? xp : Leveling.TrainingXp(xp);
    }

    public async Task<AnswerOutcome> AnswerAsync(Profile p, int id, string? answer, string? x, string? y, bool giveUp = false)
    {
        var t = await GetAsync(p, id);
        var task = Read(t);
        var before = Leveling.FromXp(p.Xp);

        if (t.Status != GameTaskStatus.Open)
            return Outcome(p, t, task, t.Status == GameTaskStatus.Solved, "Opgaven er allerede færdig.", 0, 0, null);

        bool correct;
        string message;
        if (giveUp)
        {
            correct = false;
            message = "Her er løsningen. Kig på den, og prøv en ny opgave!";
            t.Status = GameTaskStatus.Revealed;
        }
        else
        {
            var check = AnswerChecker.Check(task, answer, x, y);
            t.LastAnswer = task.Kind == AnswerKind.Point ? $"({x}, {y})" : answer?.Trim();
            correct = check.Correct;
            message = check.Message;
            t.Attempts++;
            if (correct) t.Status = GameTaskStatus.Solved;
            else if (t.Attempts >= Leveling.MaxAttempts)
            {
                t.Status = GameTaskStatus.Revealed;
                message = "Det var desværre ikke rigtigt. Se, hvordan man regner den.";
            }
            else message += " Du har et forsøg mere (til halv XP).";
        }

        var xp = 0;
        if (correct)
        {
            xp = Leveling.XpForAttempt(FullXp(t), t.Attempts);
            t.XpAwarded = xp;
            Award(p, xp, $"{task.Topic.Info().Name}{(t.IsDaily ? " (dagens opgave)" : " (træning)")}");
        }
        if (t.Status != GameTaskStatus.Open) t.FinishedAt = DateTime.UtcNow;

        var bonus = 0;
        if (t.IsDaily && t.Status != GameTaskStatus.Open && t.Day == Today && p.LastDailyDone != Today)
        {
            var open = await db.GameTasks.CountAsync(o => o.ProfileId == p.Id && o.Day == Today && o.IsDaily && o.Status == GameTaskStatus.Open && o.Id != t.Id);
            if (open == 0)
            {
                p.Streak = p.LastDailyDone == Today.AddDays(-1) ? p.Streak + 1 : 1;
                p.LastDailyDone = Today;
                bonus = Leveling.DailyBonus(p.Streak);
                Award(p, bonus, $"Alle dagens opgaver klaret ({p.Streak} {(p.Streak == 1 ? "dag" : "dage i træk")})");
            }
        }

        await db.SaveChangesAsync();
        var after = Leveling.FromXp(p.Xp);
        var levelUp = after.Level > before.Level
            ? new LevelUp(before.Level, after.Level,
                Leveling.TitlesUnlockedBetween(before.Level, after.Level).ToList(),
                Unlocks.UnlockedBetween(before.Level, after.Level)
                    .Select(a => new NewAvatar(a.Key, a.Name, a.Description, Infrastructure.AvatarArt.Svg(a.Id))).ToList())
            : null;
        return Outcome(p, t, task, correct, message, xp, bonus, levelUp);
    }

    private AnswerOutcome Outcome(Profile p, GameTask t, MathTask task, bool correct, string message, int xp, int bonus, LevelUp? levelUp)
    {
        var finished = t.Status != GameTaskStatus.Open;
        var info = Leveling.FromXp(p.Xp);
        return new AnswerOutcome(correct, message, xp, Math.Max(0, Leveling.MaxAttempts - t.Attempts), finished,
            finished ? task.AnswerText : null,
            finished ? task.Solution : null,
            bonus, CurrentProfile.LiveStreak(p, Today), levelUp, p.Xp, info.Level, info.Percent, CurrentProfile.ShownTitle(p).Name);
    }

    private void Award(Profile p, int amount, string reason)
    {
        if (amount <= 0) return;
        p.Xp += amount;
        db.XpEvents.Add(new XpEvent { ProfileId = p.Id, Amount = amount, Reason = reason });
    }

    public async Task<int> XpThisWeekAsync(Profile p)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        return await db.XpEvents.Where(x => x.ProfileId == p.Id && x.At >= since).SumAsync(x => x.Amount);
    }

    public async Task<List<XpEvent>> RecentXpAsync(Profile p, int count = 8) =>
        await db.XpEvents.Where(x => x.ProfileId == p.Id).OrderByDescending(x => x.At).Take(count).ToListAsync();
}
