using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Regneraeven.Core.Topics;
using Regneraeven.Web.Data;

namespace Regneraeven.Web.Infrastructure;

public static class Ui
{
    /// <summary>Where the library's css/js/images are served (Razor class library static assets).</summary>
    public const string Assets = "/_content/Regneraeven.Web";

    private static string E(string s) => HtmlEncoder.Default.Encode(s);

    public static IHtmlContent Chip(TopicInfo t, string? href = null, bool on = false, string? extra = null)
    {
        var inner = $"<span class=\"sym\">{E(t.Icon)}</span>{E(t.Short)}{(extra is null ? "" : " " + E(extra))}";
        var cls = "chip topic" + (on ? " on" : "");
        return new HtmlString(href is null
            ? $"<span class=\"{cls}\" style=\"--c:{t.Color}\">{inner}</span>"
            : $"<a class=\"{cls}\" style=\"--c:{t.Color}\" href=\"{E(href)}\">{inner}</a>");
    }

    public static IHtmlContent Tag(string name, string? href = null, bool on = false, int? count = null)
    {
        var extra = count is null ? null : $"· {count}";
        if (TopicCatalog.FromSlug(name) is { } t) return Chip(t, href, on, extra);
        var inner = $"#{E(name)}{(extra is null ? "" : " " + E(extra))}";
        var cls = "chip" + (on ? " on" : "");
        return new HtmlString(href is null ? $"<span class=\"{cls}\">{inner}</span>" : $"<a class=\"{cls}\" href=\"{E(href)}\">{inner}</a>");
    }

    public static IHtmlContent Status(ProblemStatus s) => new HtmlString(s switch
    {
        ProblemStatus.Open => "<span class=\"badge gray\">Ikke regnet</span>",
        ProblemStatus.Answered => "<span class=\"badge orange\">Svar skrevet</span>",
        ProblemStatus.Correct => "<span class=\"badge green\">✓ Rigtigt</span>",
        ProblemStatus.Wrong => "<span class=\"badge red\">Prøv igen</span>",
        _ => "",
    });

    /// <summary>
    /// A drawn fox avatar as inline SVG, optionally with a frame and background (css classes frame-x / bg-x).
    /// Old emoji avatars from before foxes-only are shown as the starter fox.
    /// </summary>
    public static IHtmlContent Avatar(string? avatar, string? color = null, string size = "", string? frame = null, string? background = null)
    {
        var fox = Core.Progress.Unlocks.Find(avatar) ?? Core.Progress.Unlocks.Avatars[0];
        var style = color is null ? "" : $" style=\"--ring:{E(color)}\"";
        var cls = "avatar drawn"
                  + (size.Length > 0 ? " " + size : "")
                  + $" frame-{E(frame ?? "klassisk")} bg-{E(background ?? "hvid")}";
        return new HtmlString($"<span class=\"{cls}\"{style} title=\"{E(fox.Name)}\">{AvatarArt.Svg(fox.Id)}</span>");
    }

    /// <summary>The profile's own avatar with its chosen (and still unlocked) frame and background.</summary>
    public static IHtmlContent Avatar(Profile p, string size = "")
    {
        var level = Core.Progress.Leveling.FromXp(p.Xp).Level;
        var fox = Core.Progress.Unlocks.CanUse(p.Avatar, level) ? p.Avatar : Core.Progress.Unlocks.Avatars[0].Key;
        return Avatar(fox, p.Color, size,
            Core.Progress.Unlocks.Current(Core.Progress.UnlockKind.Frame, p.Frame, level).Id,
            Core.Progress.Unlocks.Current(Core.Progress.UnlockKind.Background, p.Background, level).Id);
    }

    /// <summary>The css class for the profile's colour theme (on &lt;body&gt;).</summary>
    public static string ThemeClass(Profile? p) => p is null
        ? "theme-raev"
        : "theme-" + Core.Progress.Unlocks.Current(Core.Progress.UnlockKind.Theme, p.Theme, Core.Progress.Leveling.FromXp(p.Xp).Level).Id;

    public static string ToHtml(this IHtmlContent content)
    {
        using var w = new StringWriter();
        content.WriteTo(w, HtmlEncoder.Default);
        return w.ToString();
    }

    public static string Difficulty(int d) => d switch { 1 => "Let", 2 => "Middel", _ => "Svær" };

    public static string Stars(int d) => new string('★', d) + new string('☆', 3 - d);

    public static string When(DateTime utc)
    {
        var local = utc.ToLocalTime();
        var days = (DateTime.Today - local.Date).Days;
        return days switch
        {
            0 => "i dag " + local.ToString("HH:mm"),
            1 => "i går",
            < 7 => $"for {days} dage siden",
            _ => local.ToString("d. MMM yyyy"),
        };
    }
}
