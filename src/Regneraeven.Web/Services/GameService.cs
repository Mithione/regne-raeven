using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Core.Tasks;
using Regneraeven.Core.Topics;
using Regneraeven.Web.Data;

namespace Regneraeven.Web.Services;

/// <summary>A newly unlocked fox, with its drawing so the level-up dialog can show it.</summary>
public sealed record NewAvatar(string Key, string Name, string Description, string Svg);

public sealed record LevelUp(int From, int To, IReadOnlyList<FoxTitle> NewTitles, IReadOnlyList<NewAvatar> NewAvatars, IReadOnlyList<UnlockNews> NewItems);

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
    string TitleName,
    StreakNews? StreakNews = null,
    int Gold = 0,
    int GoldAwarded = 0);

/// <summary>What happened to the day streak when today's tasks were finished.</summary>
public sealed record StreakNews(int Streak, bool EarnedFreeze, int FreezesUsed, int Freezes, IReadOnlyList<MilestoneNews> Milestones);

/// <summary>A milestone reached for the first time (bonus XP is already added; the reward is chosen on the profile).</summary>
public sealed record MilestoneNews(int Days, string Icon, int BonusXp, string? RealReward);

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
        var goldBefore = p.Gold;

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
        StreakNews? streakNews = null;
        if (t.IsDaily && t.Status != GameTaskStatus.Open && t.Day == Today && p.LastDailyDone != Today)
        {
            var open = await db.GameTasks.CountAsync(o => o.ProfileId == p.Id && o.Day == Today && o.IsDaily && o.Status == GameTaskStatus.Open && o.Id != t.Id);
            if (open == 0)
            {
                streakNews = FinishDay(p);
                bonus = Leveling.DailyBonus(p.Streak) + streakNews.Milestones.Sum(m => m.BonusXp);
            }
        }

        await db.SaveChangesAsync();
        var after = Leveling.FromXp(p.Xp);
        var levelUp = LevelUpBetween(before.Level, after.Level);
        return Outcome(p, t, task, correct, message, xp, bonus, levelUp) with { StreakNews = streakNews, GoldAwarded = p.Gold - goldBefore };
    }

    /// <summary>All of today's tasks are done: move the streak on (using days off if needed) and pay out bonuses.</summary>
    private StreakNews FinishDay(Profile p)
    {
        var advance = Streaks.Advance(p.LastDailyDone, Today, p.Streak, p.StreakFreezes);
        p.Streak = advance.Streak;
        p.StreakFreezes = advance.Freezes;
        p.LastDailyDone = Today;
        Award(p, Leveling.DailyBonus(p.Streak), $"Alle dagens opgaver klaret ({p.Streak} {(p.Streak == 1 ? "dag" : "dage i træk")})");

        var rewards = Streaks.ParseRewards(p.StreakRewards);
        var reached = Streaks.ReachedBetween(p.BestStreak, p.Streak).ToList();
        foreach (var m in reached) Award(p, m.BonusXp, $"{m.Icon} {m.Days} dage i træk");
        p.BestStreak = Math.Max(p.BestStreak, p.Streak);

        return new StreakNews(p.Streak, advance.EarnedFreeze, advance.FreezesUsed, p.StreakFreezes,
            reached.Select(m => new MilestoneNews(m.Days, m.Icon, m.BonusXp, rewards.GetValueOrDefault(m.Days))).ToList());
    }

    private AnswerOutcome Outcome(Profile p, GameTask t, MathTask task, bool correct, string message, int xp, int bonus, LevelUp? levelUp)
    {
        var finished = t.Status != GameTaskStatus.Open;
        var info = Leveling.FromXp(p.Xp);
        return new AnswerOutcome(correct, message, xp, Math.Max(0, Leveling.MaxAttempts - t.Attempts), finished,
            finished ? task.AnswerText : null,
            finished ? task.Solution : null,
            bonus, CurrentProfile.LiveStreak(p, Today), levelUp, p.Xp, info.Level, info.Percent, CurrentProfile.ShownTitle(p).Name, Gold: p.Gold);
    }

    /// <summary>What a level-up unlocked (titles, foxes, frames …), or null if the level didn't change.</summary>
    public static LevelUp? LevelUpBetween(int from, int to) => to > from
        ? new LevelUp(from, to,
            Leveling.TitlesUnlockedBetween(from, to).ToList(),
            Unlocks.UnlockedBetween(from, to)
                .Select(a => new NewAvatar(a.Key, a.Name, a.Description, Infrastructure.AvatarArt.Svg(a.Id))).ToList(),
            Unlocks.CosmeticsUnlockedBetween(from, to).ToList())
        : null;

    /// <summary>Adds XP to the profile and logs it (saved with the next SaveChanges).</summary>
    /// <summary>Adds XP – and the gold that comes with it (1 per 4 XP) – and logs it (saved with the next SaveChanges).</summary>
    public void Award(Profile p, int amount, string reason)
    {
        if (amount <= 0) return;
        var gold = Core.Room.RoomCatalog.GoldFor(amount);
        p.Xp += amount;
        p.Gold += gold;
        db.XpEvents.Add(new XpEvent { ProfileId = p.Id, Amount = amount, Gold = gold, Reason = reason });
    }

    public async Task<int> XpThisWeekAsync(Profile p)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        return await db.XpEvents.Where(x => x.ProfileId == p.Id && x.At >= since).SumAsync(x => x.Amount);
    }

    public async Task<List<XpEvent>> RecentXpAsync(Profile p, int count = 8) =>
        await db.XpEvents.Where(x => x.ProfileId == p.Id).OrderByDescending(x => x.At).Take(count).ToListAsync();
}
