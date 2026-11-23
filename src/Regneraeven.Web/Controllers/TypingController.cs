using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Core.Typing;
using Regneraeven.Web.Data;
using Regneraeven.Web.Models;
using Regneraeven.Web.Services;

namespace Regneraeven.Web.Controllers;

/// <summary>"Skriv": learn the 10-finger system on a Danish keyboard and earn XP.</summary>
[Route("skriv")]
public class TypingController(AppDb db, CurrentProfile current, GameService game) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var p = await current.RequireAsync();
        return View(new TypingIndexModel(await BestAsync(p.Id), await BestWpmAsync(p.Id)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Lesson(string id)
    {
        var p = await current.RequireAsync();
        var lesson = TypingCourse.Find(id);
        if (lesson is null) return NotFound();
        var best = await BestAsync(p.Id);
        if (!TypingScore.IsOpen(lesson, best))
            throw new UserFacingException("Den lektion låses op, når du har mindst én stjerne i lektionen før.");
        best.TryGetValue(lesson.Id, out var stars);
        return View(new TypingLessonModel(lesson, TypingCourse.MakeText(lesson, Random.Shared), stars));
    }

    public sealed record ResultBody(int Chars, int Errors, double Seconds);

    [HttpPost("{id}/resultat")]
    public async Task<IActionResult> Result(string id, [FromBody] ResultBody body)
    {
        var p = await current.RequireAsync();
        var lesson = TypingCourse.Find(id) ?? throw new UserFacingException("Ukendt lektion.");
        var best = await BestAsync(p.Id);
        if (!TypingScore.IsOpen(lesson, best)) throw new UserFacingException("Lektionen er ikke låst op endnu.");
        if (body.Chars is < 10 or > 500 || body.Errors < 0 || body.Seconds < 3)
            throw new UserFacingException("Resultatet ser forkert ud – prøv lektionen igen.");

        var run = TypingScore.Evaluate(lesson.Difficulty, body.Chars, body.Errors, body.Seconds);
        if (run.Wpm > 150) throw new UserFacingException("Det gik vist lidt for hurtigt – prøv lektionen igen.");
        best.TryGetValue(lesson.Id, out var bestBefore);
        var startOfToday = DateTime.Today.ToUniversalTime();
        var practiceToday = await db.TypingResults.CountAsync(r => r.ProfileId == p.Id && r.At >= startOfToday && r.XpAwarded == TypingScore.PracticeXp);
        var xp = TypingScore.Xp(lesson.Difficulty, bestBefore, run.Stars, practiceToday);

        var levelBefore = Leveling.FromXp(p.Xp).Level;
        var goldBefore = p.Gold;
        game.Award(p, xp, $"10-finger: {lesson.Title}");
        db.TypingResults.Add(new TypingResult { ProfileId = p.Id, LessonId = lesson.Id, Stars = run.Stars, Accuracy = run.Accuracy, Wpm = run.Wpm, XpAwarded = xp });
        await db.SaveChangesAsync();

        var info = Leveling.FromXp(p.Xp);
        var next = TypingCourse.Next(lesson);
        best[lesson.Id] = Math.Max(bestBefore, run.Stars);
        return Json(new
        {
            run.Stars,
            run.Accuracy,
            run.Wpm,
            run.Message,
            xp,
            practice = xp == TypingScore.PracticeXp && run.Stars <= bestBefore,
            bestStars = best[lesson.Id],
            newBest = run.Stars > bestBefore,
            next = next is null ? null : new { next.Id, next.Title, open = TypingScore.IsOpen(next, best) },
            level = info.Level,
            levelPercent = info.Percent,
            titleName = CurrentProfile.ShownTitle(p).Name,
            gold = p.Gold,
            goldAwarded = p.Gold - goldBefore,
            levelUp = GameService.LevelUpBetween(levelBefore, info.Level),
        });
    }

    private async Task<Dictionary<string, int>> BestAsync(int profileId) =>
        await db.TypingResults.Where(r => r.ProfileId == profileId)
            .GroupBy(r => r.LessonId).Select(g => new { g.Key, Stars = g.Max(r => r.Stars) })
            .ToDictionaryAsync(x => x.Key, x => x.Stars);

    private async Task<Dictionary<string, int>> BestWpmAsync(int profileId) =>
        await db.TypingResults.Where(r => r.ProfileId == profileId && r.Stars > 0)
            .GroupBy(r => r.LessonId).Select(g => new { g.Key, Wpm = g.Max(r => r.Wpm) })
            .ToDictionaryAsync(x => x.Key, x => x.Wpm);
}
