using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Homework;
using Regneraeven.Core.Maths;
using Regneraeven.Core.Topics;
using Regneraeven.Web.Data;
using Regneraeven.Web.Models;
using Regneraeven.Web.Services;

namespace Regneraeven.Web.Controllers;

/// <summary>
/// "Mine opgaver": problems the child saves and tags. The calculator and the worked solution
/// only open once the child has written their own answer.
/// </summary>
[Route("opgaver")]
public class ProblemsController(AppDb db, CurrentProfile current, TimeProvider clock) : Controller
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? tag, string? status)
    {
        var p = await current.RequireAsync();
        var all = await db.Problems.Include(x => x.Tags).Where(x => x.ProfileId == p.Id).OrderByDescending(x => x.UpdatedAt).ToListAsync();

        // Tags with counts, topics first.
        var tagCounts = all.SelectMany(x => x.Tags.Select(t => t.Name)).GroupBy(n => n)
            .Select(g => new TagCount(g.Key, g.Count(), TopicCatalog.FromSlug(g.Key)))
            .OrderBy(t => t.Topic is null).ThenBy(t => t.Topic is null ? t.Name : null).ThenBy(t => t.Topic?.Topic)
            .ToList();

        IEnumerable<SavedProblem> list = all;
        if (!string.IsNullOrWhiteSpace(tag)) list = list.Where(x => x.Tags.Any(t => t.Name == tag));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            list = list.Where(x => x.Text.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                                   || (x.Note ?? "").Contains(term, StringComparison.CurrentCultureIgnoreCase)
                                   || x.Tags.Any(t => t.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                                                      || (t.Topic?.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)));
        }
        list = status switch
        {
            "aabne" => list.Where(x => x.Status == ProblemStatus.Open),
            "rigtige" => list.Where(x => x.Status == ProblemStatus.Correct),
            "forkerte" => list.Where(x => x.Status == ProblemStatus.Wrong),
            // Homework: overdue first, then by hand-in date; done ones last.
            "lektier" => HomeworkDue.InOrder(list.Where(x => x.IsHomework), x => x.DueDate, x => x.IsDone),
            _ => list,
        };
        var openHomework = all.Count(x => x.IsHomework && !x.IsDone);
        return View(new ProblemListModel(list.ToList(), tagCounts, q, tag, status, all.Count, openHomework, Today));
    }

    [HttpGet("ny")]
    public IActionResult Create(string? tekst, bool lektie = false) =>
        View("Edit", new ProblemForm { Text = tekst, IsHomework = lektie, DueDate = lektie ? HomeworkDue.NextSchoolDay(Today) : null });

    [HttpPost("ny")]
    public async Task<IActionResult> Create(ProblemForm form)
    {
        var p = await current.RequireAsync();
        var problem = new SavedProblem { ProfileId = p.Id };
        Apply(problem, form);
        db.Problems.Add(problem);
        await db.SaveChangesAsync();
        TempData["Info"] = "Opgaven er gemt. Regn den selv først – så kan du tjekke dit svar.";
        return Redirect($"/opgaver/{problem.Id}");
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, bool udregning = false)
    {
        var problem = await Load(id);
        var clean = HelpController.CleanInput(problem.Text);
        SolveResult? solved = null;
        string? solveError = null;
        var isExpression = Expression.TryParse(clean, out var node, out _);
        if (isExpression && udregning && problem.Status != ProblemStatus.Open)
        {
            try { solved = Expression.Solve(node, Expression.Render(node)); }
            catch (Exception ex) when (ex is FormatException or DivideByZeroException) { solveError = ex.Message; }
        }
        return View(new ProblemDetailsModel(problem, isExpression, solved, solveError, Today));
    }

    [HttpGet("{id:int}/rediger")]
    public async Task<IActionResult> Edit(int id)
    {
        var problem = await Load(id);
        return View(new ProblemForm
        {
            Id = problem.Id,
            Text = problem.Text,
            Note = problem.Note,
            Topics = problem.Tags.Where(t => t.Topic is not null).Select(t => t.Name).ToList(),
            OwnTags = string.Join(", ", problem.Tags.Where(t => t.Topic is null).Select(t => t.Name)),
            IsHomework = problem.IsHomework,
            DueDate = problem.DueDate,
        });
    }

    [HttpPost("{id:int}/rediger")]
    public async Task<IActionResult> Edit(int id, ProblemForm form)
    {
        var problem = await Load(id);
        var textChanged = problem.Text != (form.Text ?? "").Trim();
        Apply(problem, form);
        if (textChanged && problem.Status is ProblemStatus.Correct or ProblemStatus.Wrong) problem.Status = ProblemStatus.Answered;
        await db.SaveChangesAsync();
        return Redirect($"/opgaver/{id}");
    }

    /// <summary>The child writes their own answer. This unlocks the calculator.</summary>
    [HttpPost("{id:int}/svar")]
    public async Task<IActionResult> SaveAnswer(int id, string? myAnswer)
    {
        var problem = await Load(id);
        var answer = (myAnswer ?? "").Trim();
        if (answer.Length == 0) throw new UserFacingException("Skriv dit eget svar først.");
        problem.MyAnswer = answer.Length > 200 ? answer[..200] : answer;
        problem.Status = ProblemStatus.Answered;
        problem.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Info"] = "Dit svar er gemt. Nu kan du tjekke det med lommeregneren. 🧮";
        return Redirect($"/opgaver/{id}#tjek");
    }

    /// <summary>Automatic check – only for plain arithmetic, and only once the child has an answer.</summary>
    [HttpPost("{id:int}/tjek")]
    public async Task<IActionResult> Check(int id)
    {
        var problem = await Load(id);
        if (problem.Status == ProblemStatus.Open || string.IsNullOrWhiteSpace(problem.MyAnswer))
            throw new UserFacingException("Skriv dit eget svar først – så kan du tjekke det.");
        var clean = HelpController.CleanInput(problem.Text);
        if (!Expression.TryParse(clean, out var node, out _))
            throw new UserFacingException("Den her opgave kan jeg ikke tjekke automatisk. Brug lommeregneren.");
        Rational right;
        try { right = Expression.Evaluate(node); }
        catch (DivideByZeroException) { throw new UserFacingException("Regnestykket dividerer med 0 – det kan ikke lade sig gøre."); }
        if (!AnswerParser.TryParse(problem.MyAnswer, out var mine))
            throw new UserFacingException("Jeg kan ikke læse dit svar som et tal. Ret det, og prøv igen.");
        problem.Status = mine.Value == right ? ProblemStatus.Correct : ProblemStatus.Wrong;
        problem.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData[problem.Status == ProblemStatus.Correct ? "Info" : "Warn"] = problem.Status == ProblemStatus.Correct
            ? "Dit svar er rigtigt! 🎉"
            : "Dit svar passer ikke. Kig på udregningen, eller prøv at regne den igen.";
        return Redirect($"/opgaver/{id}#tjek");
    }

    /// <summary>After checking with the calculator, the child marks the answer as right or wrong.</summary>
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var problem = await Load(id);
        if (problem.Status == ProblemStatus.Open) throw new UserFacingException("Skriv dit eget svar først.");
        problem.Status = status == "rigtig" ? ProblemStatus.Correct : status == "forkert" ? ProblemStatus.Wrong : ProblemStatus.Answered;
        problem.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Redirect($"/opgaver/{id}#tjek");
    }

    [HttpPost("{id:int}/slet")]
    public async Task<IActionResult> Delete(int id)
    {
        var problem = await Load(id);
        db.Problems.Remove(problem);
        await db.SaveChangesAsync();
        TempData["Info"] = "Opgaven er slettet.";
        return Redirect("/opgaver");
    }

    /// <summary>Tag suggestions while typing.</summary>
    [HttpGet("forslag")]
    public IActionResult Suggest(string? tekst) =>
        SuggestFor(tekst);

    /// <summary>
    /// Tags for the text, plus – if the text was dictated in words ("tre fjerdedele plus en sjettedel") –
    /// the calculation written with numbers and signs ("3/4 + 1/6").
    /// </summary>
    private JsonResult SuggestFor(string? tekst)
    {
        var text = tekst ?? "";
        var math = SpokenMath.ToExpression(text);
        if (math is not null && Normalize(math) == Normalize(text)) math = null; // already written like that
        var tags = TopicDetector.Suggest(HelpController.CleanInput(math ?? text)).Select(t => t.Info().Slug);
        return Json(new { tags, math });
    }

    private static string Normalize(string s) => new string(s.Where(c => !char.IsWhiteSpace(c) && c != '?').ToArray())
        .Replace('*', '·').Replace('x', '·').Replace('X', '·').Replace('−', '-').TrimEnd('=');

    private async Task<SavedProblem> Load(int id)
    {
        var p = await current.RequireAsync();
        return await db.Problems.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == id && x.ProfileId == p.Id)
               ?? throw new UserFacingException("Opgaven findes ikke.");
    }

    private static void Apply(SavedProblem problem, ProblemForm form)
    {
        var text = (form.Text ?? "").Trim();
        if (text.Length == 0) throw new UserFacingException("Skriv opgaven.");
        if (text.Length > 2000) text = text[..2000];
        problem.Text = text;
        problem.IsHomework = form.IsHomework;
        problem.DueDate = form.IsHomework ? form.DueDate : null;
        var note = form.Note?.Trim();
        problem.Note = string.IsNullOrEmpty(note) ? null : note.Length > 2000 ? note[..2000] : note;
        problem.UpdatedAt = DateTime.UtcNow;

        var names = new List<string>();
        foreach (var slug in form.Topics ?? [])
            if (TopicCatalog.FromSlug(slug) is { } info && !names.Contains(info.Slug)) names.Add(info.Slug);
        foreach (var raw in (form.OwnTags ?? "").Split(new[] { ',', ';', '#' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var name = raw.ToLowerInvariant();
            if (name.Length > 40) name = name[..40];
            if (TopicCatalog.FromSlug(name) is { } t) name = t.Slug;
            else if (TopicCatalog.All.FirstOrDefault(x => x.Name.Equals(raw, StringComparison.CurrentCultureIgnoreCase)) is { } byName) name = byName.Slug;
            if (!names.Contains(name)) names.Add(name);
        }
        if (names.Count == 0)
            names.AddRange(TopicDetector.Suggest(HelpController.CleanInput(text)).Select(t => t.Info().Slug));

        problem.Tags.RemoveAll(t => !names.Contains(t.Name));
        foreach (var n in names.Where(n => problem.Tags.All(t => t.Name != n)))
            problem.Tags.Add(new ProblemTag { Name = n });
    }
}
