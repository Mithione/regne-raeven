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

    /// <summary>A profile picture: a start emoji, or a drawn fox ("fox:briller") as inline SVG.</summary>
    public static IHtmlContent Avatar(string? avatar, string? color = null, string size = "")
    {
        var style = color is null ? "" : $" style=\"border-color:{E(color)}\"";
        var cls = "avatar" + (size.Length > 0 ? " " + size : "");
        if (Core.Progress.Unlocks.Find(avatar) is { } fox)
            return new HtmlString($"<span class=\"{cls} drawn\"{style} title=\"{E(fox.Name)}\">{AvatarArt.Svg(fox.Id)}</span>");
        return new HtmlString($"<span class=\"{cls}\"{style}>{E(avatar ?? "🦊")}</span>");
    }

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
