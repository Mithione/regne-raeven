using Microsoft.AspNetCore.Mvc;
using Regneraeven.Core.Help;
using Regneraeven.Core.Maths;
using Regneraeven.Core.Topics;
using Regneraeven.Web.Models;
using Regneraeven.Web.Services;

namespace Regneraeven.Web.Controllers;

public class HelpController : Controller
{
    [HttpGet("regler")]
    public IActionResult Index() => View(RuleBook.All);

    [HttpGet("regler/{slug}")]
    public IActionResult Topic(string slug)
    {
        var info = TopicCatalog.FromSlug(slug);
        var rule = info is null ? null : RuleBook.For(info.Topic);
        return rule is null ? NotFound() : View(rule);
    }

    /// <summary>
    /// Step-by-step help. It always solves a SIMILAR example (other numbers, same method), so the
    /// child learns how without getting the answer to their own task served.
    /// </summary>
    [HttpGet("regler/trin")]
    public IActionResult Steps(string? udtryk, int? seed)
    {
        return View(BuildSimilar(udtryk, seed));
    }

    public static StepsModel BuildSimilar(string? input, int? seed)
    {
        if (string.IsNullOrWhiteSpace(input)) return new StepsModel(input, null, null, null, seed ?? 0);
        var clean = CleanInput(input);
        if (!Expression.TryParse(clean, out var node, out var error))
            return new StepsModel(input, error, null, null, seed ?? 0);
        var s = seed ?? Random.Shared.Next(1, 100_000);
        var similar = Expression.MakeSimilar(node, new Random(s));
        try
        {
            var solved = Expression.Solve(similar, Expression.Render(similar));
            return new StepsModel(input, null, solved, TopicDetector.Suggest(clean), s);
        }
        catch (Exception ex) when (ex is FormatException or DivideByZeroException)
        {
            return new StepsModel(input, ex.Message, null, null, s);
        }
    }

    /// <summary>"3/4 + 1/6 = ?" → "3/4 + 1/6".</summary>
    public static string CleanInput(string text)
    {
        var s = text.Replace("?", "").Trim();
        while (s.EndsWith('=')) s = s[..^1].TrimEnd();
        return s;
    }
}

public class TablesController : Controller
{
    [HttpGet("tabeller")]
    public IActionResult Index() => View();
}
