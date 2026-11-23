using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Core.Topics;
using Regneraeven.Web.Data;
using Regneraeven.Web.Models;
using Regneraeven.Web.Services;

namespace Regneraeven.Web.Controllers;

[Route("spil")]
public class GameController(AppDb db, CurrentProfile current, GameService game) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var p = await current.RequireAsync();
        var daily = await game.GetDailyAsync(p);
        var solvedByTopic = await db.GameTasks.Where(t => t.ProfileId == p.Id && t.Status == GameTaskStatus.Solved)
            .GroupBy(t => t.Topic).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return View(new GameHubModel(p, daily, CurrentProfile.LiveStreak(p, game.Today), await game.RecentXpAsync(p), solvedByTopic));
    }

    [HttpGet("opgave/{id:int}")]
    public async Task<IActionResult> Task(int id)
    {
        var p = await current.RequireAsync();
        var t = await game.GetAsync(p, id);
        var daily = t.IsDaily ? await game.GetDailyAsync(p) : null;
        return View(new GameTaskModel(t, GameService.Read(t), GameService.FullXp(t), daily));
    }

    public sealed record AnswerBody(string? Answer, string? X, string? Y);

    [HttpPost("opgave/{id:int}/svar")]
    public async Task<IActionResult> Answer(int id, [FromBody] AnswerBody body)
    {
        var p = await current.RequireAsync();
        return Json(await game.AnswerAsync(p, id, body.Answer, body.X, body.Y));
    }

    [HttpPost("opgave/{id:int}/opgiv")]
    public async Task<IActionResult> GiveUp(int id)
    {
        var p = await current.RequireAsync();
        return Json(await game.AnswerAsync(p, id, null, null, null, giveUp: true));
    }

    /// <summary>Next open daily task, or back to the game page when all are done.</summary>
    [HttpGet("naeste")]
    public async Task<IActionResult> Next()
    {
        var p = await current.RequireAsync();
        var next = (await game.GetDailyAsync(p)).FirstOrDefault(t => t.Status == GameTaskStatus.Open);
        return Redirect(next is null ? "/spil" : $"/spil/opgave/{next.Id}");
    }

    [HttpPost("traen")]
    public async Task<IActionResult> Train(string emne, int svaerhed = 1)
    {
        var p = await current.RequireAsync();
        var info = TopicCatalog.FromSlug(emne) ?? throw new UserFacingException("Ukendt emne.");
        var t = await game.CreateTrainingAsync(p, info.Topic, svaerhed);
        return Redirect($"/spil/opgave/{t.Id}");
    }

    [HttpGet("titler")]
    public IActionResult Titles() => Redirect("/spil#titler");

    public static int SuggestedDifficulty(Profile p) =>
        Math.Clamp((p.Grade <= 5 ? 1 : 2) + (Leveling.FromXp(p.Xp).Level >= 20 ? 1 : 0), 1, 3);
}
