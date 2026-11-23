using System.Text;

namespace Regneraeven.Web.Infrastructure;

/// <summary>
/// The drawn fox avatars as inline SVG, in the same style as the Regneræven logo:
/// one fox head with different fur colours and accessories. viewBox 0 0 64 64.
/// </summary>
public static class AvatarArt
{
    private sealed record Fur(string Main, string Ears, string Inner, string Cheeks, string? Outline = null);

    private static readonly Fur Orange = new("#f26b1d", "#e8590c", "#3b1d0e", "#fff4e8");
    private static readonly Fur Red = new("#d9381e", "#b42a16", "#3b1d0e", "#fff4e8");
    private static readonly Fur Silver = new("#9aa4b2", "#7b8594", "#3a4250", "#f8fafc");
    private static readonly Fur Polar = new("#f8fafc", "#e2e8f0", "#94a3b8", "#ffffff", "#cbd5e1");
    private static readonly Fur Gold = new("#f5b301", "#d99a00", "#6b4500", "#fff7d6");

    public static string Svg(string id) => id switch
    {
        "hvalp" => Draw(Orange, ""),
        "lup" => Draw(Orange, Magnifier),
        "briller" => Draw(Orange, Glasses),
        "lyn" => Draw(Orange, Headband),
        "roed" => Draw(Red, Scarf),
        "soelv" => Draw(Silver, BowTie),
        "polar" => Draw(Polar, Beanie),
        "astronaut" => Draw(Orange, Helmet, before: HelmetBack),
        "guld" => Draw(Gold, Medal),
        "krone" => Draw(Orange, Crown),
        "troldmand" => Draw(Orange, WizardHat),
        _ => Draw(Orange, ""),
    };

    private static string Draw(Fur f, string accessory, string before = "")
    {
        var stroke = f.Outline is null ? "" : $" stroke=\"{f.Outline}\" stroke-width=\"1.2\" stroke-linejoin=\"round\"";
        var sb = new StringBuilder();
        sb.Append("<svg viewBox=\"0 0 64 64\" xmlns=\"http://www.w3.org/2000/svg\" aria-hidden=\"true\">");
        sb.Append(before);
        // ears
        sb.Append($"<path d=\"M10 7 L26 20 L12 28 Z\" fill=\"{f.Ears}\"{stroke}/>");
        sb.Append($"<path d=\"M54 7 L38 20 L52 28 Z\" fill=\"{f.Ears}\"{stroke}/>");
        sb.Append($"<path d=\"M13 12.5 L22 20 L15 24 Z\" fill=\"{f.Inner}\"/>");
        sb.Append($"<path d=\"M51 12.5 L42 20 L49 24 Z\" fill=\"{f.Inner}\"/>");
        // head + cheeks
        sb.Append($"<path d=\"M8 26 Q10 16 32 16 Q54 16 56 26 L50 42 Q44 54 32 58 Q20 54 14 42 Z\" fill=\"{f.Main}\"{stroke}/>");
        sb.Append($"<path d=\"M8 26 Q14 40 32 44 Q24 52 32 58 Q20 54 14 42 Z\" fill=\"{f.Cheeks}\"/>");
        sb.Append($"<path d=\"M56 26 Q50 40 32 44 Q40 52 32 58 Q44 54 50 42 Z\" fill=\"{f.Cheeks}\"/>");
        // eyes + nose
        sb.Append("<ellipse cx=\"23\" cy=\"32\" rx=\"3.2\" ry=\"4\" fill=\"#1f2937\"/><ellipse cx=\"41\" cy=\"32\" rx=\"3.2\" ry=\"4\" fill=\"#1f2937\"/>");
        sb.Append("<circle cx=\"24.2\" cy=\"30.6\" r=\"1.1\" fill=\"#fff\"/><circle cx=\"42.2\" cy=\"30.6\" r=\"1.1\" fill=\"#fff\"/>");
        sb.Append("<path d=\"M28 50 Q32 47 36 50 Q34 54 32 54 Q30 54 28 50 Z\" fill=\"#1f2937\"/>");
        sb.Append(accessory);
        sb.Append("</svg>");
        return sb.ToString();
    }

    private const string Magnifier =
        "<line x1=\"52\" y1=\"52\" x2=\"61\" y2=\"61\" stroke=\"#7c4a1e\" stroke-width=\"4\" stroke-linecap=\"round\"/>" +
        "<circle cx=\"47\" cy=\"47\" r=\"8\" fill=\"#dbeafe\" fill-opacity=\".55\" stroke=\"#475467\" stroke-width=\"3\"/>" +
        "<path d=\"M43 44 Q45 41 48 41\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.6\" stroke-linecap=\"round\"/>";

    private const string Glasses =
        "<circle cx=\"23\" cy=\"32\" r=\"6.5\" fill=\"#ffffff\" fill-opacity=\".25\" stroke=\"#1f2937\" stroke-width=\"2.2\"/>" +
        "<circle cx=\"41\" cy=\"32\" r=\"6.5\" fill=\"#ffffff\" fill-opacity=\".25\" stroke=\"#1f2937\" stroke-width=\"2.2\"/>" +
        "<path d=\"M29.5 31 Q32 29 34.5 31\" fill=\"none\" stroke=\"#1f2937\" stroke-width=\"2.2\"/>" +
        "<path d=\"M16.5 30 L10 27 M47.5 30 L54 27\" stroke=\"#1f2937\" stroke-width=\"2\"/>";

    private const string Headband =
        "<path d=\"M10 23 Q32 15 54 23 L53 28 Q32 20 11 28 Z\" fill=\"#2563eb\"/>" +
        "<path d=\"M33 15 L27 25 L31.5 25 L29 32 L37 21 L32.5 21 L35 15 Z\" fill=\"#facc15\" stroke=\"#a16207\" stroke-width=\".8\" stroke-linejoin=\"round\"/>";

    private const string Scarf =
        "<path d=\"M16 50 Q32 62 48 50 L50 55 Q32 66 14 55 Z\" fill=\"#16a34a\"/>" +
        "<path d=\"M40 56 L46 64 L50 62 L45 54 Z\" fill=\"#15803d\"/>" +
        "<path d=\"M20 54 L22 57 M26 57 L27.5 60 M34 58 L34.5 61\" stroke=\"#bbf7d0\" stroke-width=\"1.4\" stroke-linecap=\"round\"/>";

    private const string BowTie =
        "<path d=\"M32 59 L23 54 L23 64 Z\" fill=\"#7c3aed\"/><path d=\"M32 59 L41 54 L41 64 Z\" fill=\"#7c3aed\"/>" +
        "<circle cx=\"32\" cy=\"59\" r=\"2.6\" fill=\"#5b21b6\"/>" +
        "<path d=\"M52 10 l1.2 2.6 2.6 1.2 -2.6 1.2 -1.2 2.6 -1.2 -2.6 -2.6 -1.2 2.6 -1.2 Z\" fill=\"#cbd5e1\"/>";

    private const string Beanie =
        "<path d=\"M11 24 Q12 4 32 4 Q52 4 53 24 Z\" fill=\"#3b82f6\"/>" +
        "<rect x=\"9\" y=\"20\" width=\"46\" height=\"7\" rx=\"3.5\" fill=\"#1d4ed8\"/>" +
        "<path d=\"M18 9 L18 20 M25 6 L25 20 M32 5 L32 20 M39 6 L39 20 M46 9 L46 20\" stroke=\"#60a5fa\" stroke-width=\"1.4\"/>" +
        "<circle cx=\"32\" cy=\"4\" r=\"4\" fill=\"#f8fafc\" stroke=\"#cbd5e1\"/>";

    private const string HelmetBack = "<circle cx=\"32\" cy=\"34\" r=\"30\" fill=\"#e0f2fe\"/>";

    private const string Helmet =
        "<circle cx=\"32\" cy=\"34\" r=\"29\" fill=\"#bae6fd\" fill-opacity=\".28\" stroke=\"#94a3b8\" stroke-width=\"3\"/>" +
        "<path d=\"M14 18 Q20 10 28 9\" fill=\"none\" stroke=\"#fff\" stroke-width=\"2.4\" stroke-linecap=\"round\"/>" +
        "<rect x=\"24\" y=\"59\" width=\"16\" height=\"5\" rx=\"2\" fill=\"#94a3b8\"/>" +
        "<path d=\"M56 6 l1 2.2 2.2 1 -2.2 1 -1 2.2 -1 -2.2 -2.2 -1 2.2 -1 Z\" fill=\"#facc15\"/>";

    private const string Medal =
        "<path d=\"M25 50 L30 60 L34 60 L29 50 Z\" fill=\"#2563eb\"/><path d=\"M39 50 L34 60 L30 60 L35 50 Z\" fill=\"#dc2626\"/>" +
        "<circle cx=\"32\" cy=\"59\" r=\"5\" fill=\"#facc15\" stroke=\"#a16207\" stroke-width=\"1.2\"/>" +
        "<path d=\"M32 56.3 l.8 1.7 1.9 .2 -1.4 1.3 .4 1.9 -1.7 -1 -1.7 1 .4 -1.9 -1.4 -1.3 1.9 -.2 Z\" fill=\"#a16207\"/>";

    private const string Crown =
        "<path d=\"M19 20 L20 6 L26 13 L32 3 L38 13 L44 6 L45 20 Z\" fill=\"#fbbf24\" stroke=\"#b45309\" stroke-width=\"1.2\" stroke-linejoin=\"round\"/>" +
        "<circle cx=\"32\" cy=\"14\" r=\"2.2\" fill=\"#dc2626\"/><circle cx=\"24\" cy=\"16\" r=\"1.6\" fill=\"#2563eb\"/><circle cx=\"40\" cy=\"16\" r=\"1.6\" fill=\"#16a34a\"/>";

    private const string WizardHat =
        "<path d=\"M12 22 Q32 26 52 22 L50 19 Q32 22 14 19 Z\" fill=\"#5b21b6\"/>" +
        "<path d=\"M16 20 L36 0 L46 20 Q32 23 16 20 Z\" fill=\"#7c3aed\"/>" +
        "<path d=\"M30 9 l1 2 2 .3 -1.5 1.4 .4 2 -1.9 -1 -1.9 1 .4 -2 -1.5 -1.4 2 -.3 Z\" fill=\"#fde047\"/>" +
        "<circle cx=\"39\" cy=\"15\" r=\"1.3\" fill=\"#fde047\"/><circle cx=\"25\" cy=\"16\" r=\"1\" fill=\"#fde047\"/>" +
        "<path d=\"M57 30 l1 2.2 2.2 1 -2.2 1 -1 2.2 -1 -2.2 -2.2 -1 2.2 -1 Z\" fill=\"#a78bfa\"/>";
}
