using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Homework;
using Regneraeven.Core.Progress;
using Regneraeven.Web.Data;
using Regneraeven.Web.Infrastructure;
using Regneraeven.Web.Models;
using Regneraeven.Web.Services;

namespace Regneraeven.Web.Controllers;

public class HomeController(AppDb db, CurrentProfile current, GameService game) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var p = await current.RequireAsync();
        var daily = await game.GetDailyAsync(p);
        var problems = await db.Problems.Include(x => x.Tags).Where(x => x.ProfileId == p.Id)
            .OrderByDescending(x => x.UpdatedAt).Take(4).ToListAsync();
        var homework = await db.Problems.Include(x => x.Tags)
            .Where(x => x.ProfileId == p.Id && x.IsHomework && x.Status == ProblemStatus.Open).ToListAsync();
        homework = HomeworkDue.InOrder(homework, x => x.DueDate, x => x.IsDone).Take(5).ToList();
        return View(new HomeModel(p, daily, problems, await game.XpThisWeekAsync(p), CurrentProfile.LiveStreak(p, game.Today), homework, game.Today));
    }

    [HttpGet("fejl"), AllowWithoutProfile]
    public IActionResult Error() => View();
}

[Route("profiler")]
public class ProfilesController(AppDb db, CurrentProfile current) : Controller
{
    public static readonly string[] Colors = ["#f26b1d", "#e03e5c", "#7a5af8", "#2563eb", "#0e9384", "#2e9e5b", "#ca8a04", "#475467"];

    [HttpGet(""), AllowWithoutProfile]
    public async Task<IActionResult> Index()
    {
        var profiles = await db.Profiles.OrderBy(p => p.Name).ToListAsync();
        return View(new ProfilesModel(profiles, await current.GetAsync()));
    }

    [HttpPost("ny"), AllowWithoutProfile]
    public async Task<IActionResult> Create(ProfileForm form)
    {
        var name = (form.Name ?? "").Trim();
        if (name.Length == 0) throw new UserFacingException("Skriv et navn.");
        if (name.Length > 40) name = name[..40];
        var p = new Profile
        {
            Name = name,
            Avatar = Unlocks.Avatars[0].Key, // everyone starts as Rævehvalpen
            Color = Colors.Contains(form.Color) ? form.Color! : Colors[0],
            Grade = form.Grade is 5 or 6 ? form.Grade : 5,
        };
        db.Profiles.Add(p);
        await db.SaveChangesAsync();
        current.Select(p.Id);
        TempData["Info"] = $"Velkommen, {p.Name}! Du er nu en {Leveling.TitleFor(1).Name}. 🦊";
        return Redirect("/");
    }

    [HttpPost("{id:int}/vaelg"), AllowWithoutProfile]
    public async Task<IActionResult> Choose(int id)
    {
        if (!await db.Profiles.AnyAsync(p => p.Id == id)) throw new UserFacingException("Profilen findes ikke.");
        current.Select(id);
        return Redirect("/");
    }

    [HttpGet("rediger")]
    public async Task<IActionResult> Edit() => View(await current.RequireAsync());

    [HttpPost("rediger")]
    public async Task<IActionResult> Edit(ProfileForm form)
    {
        var p = await current.RequireAsync();
        var name = (form.Name ?? "").Trim();
        if (name.Length == 0) throw new UserFacingException("Skriv et navn.");
        p.Name = name.Length > 40 ? name[..40] : name;
        if (Colors.Contains(form.Color)) p.Color = form.Color!;
        if (form.Grade is 5 or 6) p.Grade = form.Grade;
        await db.SaveChangesAsync();
        TempData["Info"] = "Profilen er gemt.";
        return Redirect("/profiler/rediger");
    }

    [HttpPost("titel")]
    public async Task<IActionResult> ChooseTitle(int level)
    {
        var p = await current.RequireAsync();
        var info = Leveling.FromXp(p.Xp);
        p.ShownTitleLevel = Leveling.Titles.Any(t => t.Level == level) && level <= info.Level ? level : null;
        await db.SaveChangesAsync();
        return Redirect("/profiler/rediger#titler");
    }

    /// <summary>Choose a start emoji or an unlocked fox. Also called from the level-up dialog ("Brug den nu").</summary>
    [HttpPost("avatar")]
    public async Task<IActionResult> ChooseAvatar(string? avatar)
    {
        var p = await current.RequireAsync();
        if (!Unlocks.CanUse(avatar, Leveling.FromXp(p.Xp).Level))
            throw new UserFacingException("Den avatar er ikke låst op endnu.");
        p.Avatar = avatar!;
        await db.SaveChangesAsync();
        if (Request.Headers.Accept.ToString().Contains("json", StringComparison.OrdinalIgnoreCase))
            return Json(new { ok = true, html = Ui.Avatar(p).ToHtml() });
        TempData["Info"] = Unlocks.Find(avatar) is { } fox ? $"Du er nu {fox.Name}! 🦊" : "Din avatar er skiftet.";
        return Redirect("/profiler/rediger#unlocks");
    }

    /// <summary>Choose an unlocked frame, background or colour theme.</summary>
    [HttpPost("unlock")]
    public async Task<IActionResult> ChooseUnlock(UnlockKind kind, string? id)
    {
        var p = await current.RequireAsync();
        var item = Unlocks.Find(kind, id);
        if (item is null || !Unlocks.CanUse(kind, id, Leveling.FromXp(p.Xp).Level, CurrentProfile.OwnedStreakItems(p)))
            throw new UserFacingException("Det er ikke låst op endnu.");
        switch (kind)
        {
            case UnlockKind.Frame: p.Frame = item.Id; break;
            case UnlockKind.Background: p.Background = item.Id; break;
            case UnlockKind.Theme: p.Theme = item.Id; break;
        }
        await db.SaveChangesAsync();
        TempData["Info"] = $"{Unlocks.Describe(kind).Name}: {item.Name} er valgt.";
        return Redirect($"/profiler/rediger#{kind.ToString().ToLowerInvariant()}");
    }

    /// <summary>Choose the reward for a reached day-streak milestone. It is put to use straight away.</summary>
    [HttpPost("dage/vaelg")]
    public async Task<IActionResult> ClaimStreak(int days, string? id)
    {
        var p = await current.RequireAsync();
        var milestone = Streaks.Milestones.FirstOrDefault(m => m.Days == days) ?? throw new UserFacingException("Ukendt milepæl.");
        var claims = Streaks.ParseClaims(p.StreakClaims);
        if (!Streaks.CanClaim(milestone, id ?? "", p.BestStreak, claims))
            throw new UserFacingException(claims.ContainsKey(days) ? "Du har allerede valgt din belønning for den milepæl." : "Den belønning kan du ikke vælge endnu.");
        var item = milestone.Choices.First(c => c.Id == id);
        claims[days] = item.Id;
        p.StreakClaims = Streaks.FormatClaims(claims);
        switch (item.Kind)
        {
            case UnlockKind.Frame: p.Frame = item.Id; break;
            case UnlockKind.Background: p.Background = item.Id; break;
            case UnlockKind.Theme: p.Theme = item.Id; break;
        }
        await db.SaveChangesAsync();
        TempData["Info"] = $"{milestone.Icon} Du har valgt {item.Name} – og den er taget i brug!";
        return Redirect("/profiler/rediger#dage");
    }

    /// <summary>A parent writes real-life rewards for the milestones ("Is på fredag").</summary>
    [HttpPost("dage/beloenninger")]
    public async Task<IActionResult> SaveStreakRewards(IFormCollection form)
    {
        var p = await current.RequireAsync();
        var rewards = new Dictionary<int, string>();
        foreach (var m in Streaks.Milestones)
        {
            var text = form[$"reward{m.Days}"].ToString().Trim();
            if (text.Length > 0) rewards[m.Days] = text.Length > 80 ? text[..80] : text;
        }
        p.StreakRewards = rewards.Count == 0 ? null : Streaks.FormatClaims(rewards);
        await db.SaveChangesAsync();
        TempData["Info"] = "Belønningerne er gemt.";
        return Redirect("/profiler/rediger#dage");
    }

    [HttpPost("slet")]
    public async Task<IActionResult> Delete(string? confirmName)
    {
        var p = await current.RequireAsync();
        if (!string.Equals(confirmName?.Trim(), p.Name, StringComparison.CurrentCultureIgnoreCase))
            throw new UserFacingException("Skriv profilens navn for at bekræfte, at den skal slettes.");
        db.Profiles.Remove(p);
        await db.SaveChangesAsync();
        current.Clear();
        return Redirect("/profiler");
    }

    [HttpPost("skift"), AllowWithoutProfile]
    public IActionResult Switch()
    {
        current.Clear();
        return Redirect("/profiler");
    }
}
