using System.Globalization;
using System.Text;
using Regneraeven.Core.Room;

namespace Regneraeven.Web.Infrastructure;

/// <summary>
/// The fox room as inline SVG (front view, 800 × 500). Every item is drawn in its own box, so the same
/// drawing is used in the room and as a thumbnail in the shop.
/// </summary>
public static class RoomArt
{
    private readonly record struct Box(double X, double Y, double W, double H);

    private static readonly Dictionary<RoomSlot, Box> Boxes = new()
    {
        [RoomSlot.Wall] = new(0, 0, 800, 500),
        [RoomSlot.Poster] = new(80, 40, 130, 150),
        [RoomSlot.Shelf] = new(40, 196, 210, 54),
        [RoomSlot.Light] = new(520, 24, 240, 110),
        [RoomSlot.Bed] = new(480, 250, 300, 190),
        [RoomSlot.Bedding] = new(480, 250, 300, 190),
        [RoomSlot.Desk] = new(40, 270, 260, 170),
        [RoomSlot.Plant] = new(412, 300, 60, 140),
        [RoomSlot.Rug] = new(230, 405, 320, 80),
    };

    private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The whole room with the child's fox standing on the rug.</summary>
    public static string Room(IReadOnlyDictionary<RoomSlot, RoomItem?> shown, string foxSvg)
    {
        var sb = new StringBuilder("<svg class=\"room-svg\" viewBox=\"0 0 800 500\" xmlns=\"http://www.w3.org/2000/svg\" role=\"img\" aria-label=\"Mit værelse\">");
        sb.Append(Wall(shown.GetValueOrDefault(RoomSlot.Wall)?.Id ?? "hvid"));
        sb.Append(Floor());
        sb.Append(Window());
        foreach (var slot in new[] { RoomSlot.Rug, RoomSlot.Poster, RoomSlot.Shelf, RoomSlot.Light, RoomSlot.Desk, RoomSlot.Bed, RoomSlot.Bedding, RoomSlot.Plant })
        {
            if (shown.GetValueOrDefault(slot) is not { } item) continue;
            var b = Boxes[slot];
            sb.Append($"<g transform=\"translate({N(b.X)} {N(b.Y)})\" data-slot=\"{slot}\">{Item(item, shown.GetValueOrDefault(RoomSlot.Bed)?.Id)}</g>");
        }
        // The fox, standing in front of the rug.
        sb.Append("<ellipse cx=\"397\" cy=\"440\" rx=\"44\" ry=\"7\" fill=\"#000\" opacity=\".12\"/>");
        sb.Append(foxSvg.Replace("<svg ", "<svg x=\"338\" y=\"318\" width=\"118\" height=\"118\" ", StringComparison.Ordinal));
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>A small picture of one item for the shop.</summary>
    public static string Thumb(RoomItem item)
    {
        if (item.Slot == RoomSlot.Wall)
            return $"<svg viewBox=\"0 220 300 280\" xmlns=\"http://www.w3.org/2000/svg\">{Wall(item.Id)}{Floor()}</svg>";
        var b = Boxes[item.Slot];
        var pad = Math.Max(b.W, b.H) * 0.08;
        var extraTop = item.Id is "himmelseng" ? 70 : item.Id is "koeje" ? 10 : 0;
        var inner = item.Slot == RoomSlot.Bedding ? Bed("simpel") + Bedding(item.Id) : Item(item, null);
        return $"<svg viewBox=\"{N(-pad)} {N(-pad - extraTop)} {N(b.W + 2 * pad)} {N(b.H + 2 * pad + extraTop)}\" xmlns=\"http://www.w3.org/2000/svg\">{inner}</svg>";
    }

    private static string Item(RoomItem item, string? bedId) => item.Slot switch
    {
        RoomSlot.Bed => Bed(item.Id),
        RoomSlot.Bedding => bedId == "raket" ? "" : Bedding(item.Id),
        RoomSlot.Desk => Desk(item.Id),
        RoomSlot.Light => Light(item.Id),
        RoomSlot.Poster => Poster(item.Id),
        RoomSlot.Shelf => Shelf(item.Id),
        RoomSlot.Plant => Plant(item.Id),
        RoomSlot.Rug => Rug(item.Id),
        _ => "",
    };

    // ------------------------------------------------------------ room shell

    private static string Wall(string id)
    {
        const string area = "x=\"0\" y=\"0\" width=\"800\" height=\"370\"";
        return id switch
        {
            "mint" => $"<rect {area} fill=\"#cdeee0\"/>",
            "himmel" => $"<rect {area} fill=\"#cfe6fb\"/>",
            "rosa" => $"<rect {area} fill=\"#fbdbe6\"/>",
            "stjerner" => $"<defs><pattern id=\"pw-stars\" width=\"60\" height=\"60\" patternUnits=\"userSpaceOnUse\"><rect width=\"60\" height=\"60\" fill=\"#1e2a5a\"/><path d=\"M12 10 l2 4 4 1 -3 3 1 4 -4 -2 -4 2 1 -4 -3 -3 4 -1Z\" fill=\"#fde68a\"/><circle cx=\"44\" cy=\"38\" r=\"2\" fill=\"#fff\"/><circle cx=\"30\" cy=\"52\" r=\"1.2\" fill=\"#fde68a\"/></pattern></defs><rect {area} fill=\"url(#pw-stars)\"/>",
            "taltapet" => $"<defs><pattern id=\"pw-tal\" width=\"70\" height=\"70\" patternUnits=\"userSpaceOnUse\"><rect width=\"70\" height=\"70\" fill=\"#fff7e8\"/><g font-family=\"Arial\" font-weight=\"700\" font-size=\"16\" fill=\"#f6b98d\"><text x=\"6\" y=\"20\">3</text><text x=\"40\" y=\"18\">+</text><text x=\"22\" y=\"42\">7</text><text x=\"52\" y=\"46\">×</text><text x=\"8\" y=\"64\">½</text><text x=\"42\" y=\"66\">=</text></g></pattern></defs><rect {area} fill=\"url(#pw-tal)\"/>",
            "striber" => $"<defs><pattern id=\"pw-stripes\" width=\"180\" height=\"10\" patternUnits=\"userSpaceOnUse\"><rect width=\"30\" height=\"10\" fill=\"#fecaca\"/><rect x=\"30\" width=\"30\" height=\"10\" fill=\"#fed7aa\"/><rect x=\"60\" width=\"30\" height=\"10\" fill=\"#fef08a\"/><rect x=\"90\" width=\"30\" height=\"10\" fill=\"#bbf7d0\"/><rect x=\"120\" width=\"30\" height=\"10\" fill=\"#bfdbfe\"/><rect x=\"150\" width=\"30\" height=\"10\" fill=\"#ddd6fe\"/></pattern></defs><rect {area} fill=\"url(#pw-stripes)\"/>",
            "guldtapet" => $"<defs><pattern id=\"pw-gold\" width=\"40\" height=\"40\" patternUnits=\"userSpaceOnUse\"><rect width=\"40\" height=\"40\" fill=\"#f8e7a8\"/><path d=\"M20 2 L38 20 L20 38 L2 20Z\" fill=\"none\" stroke=\"#d4a017\" stroke-width=\"1.5\"/><circle cx=\"20\" cy=\"20\" r=\"3\" fill=\"#d4a017\"/></pattern></defs><rect {area} fill=\"url(#pw-gold)\"/>",
            _ => $"<rect {area} fill=\"#f5f1ea\"/>",
        };
    }

    private static string Floor() =>
        "<rect x=\"0\" y=\"370\" width=\"800\" height=\"130\" fill=\"#c9986a\"/>" +
        "<path d=\"M0 400 H800 M0 432 H800 M0 466 H800 M120 370 V400 M330 400 V432 M560 370 V400 M210 432 V466 M690 432 V466 M90 466 V500 M460 466 V500\" stroke=\"#b5865a\" stroke-width=\"2\"/>" +
        "<rect x=\"0\" y=\"362\" width=\"800\" height=\"10\" fill=\"#fffaf3\" stroke=\"#e7d8c4\"/>";

    private static string Window() =>
        "<defs><linearGradient id=\"rw-sky\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#93c5fd\"/><stop offset=\"1\" stop-color=\"#e0f2fe\"/></linearGradient></defs>" +
        "<rect x=\"318\" y=\"64\" width=\"156\" height=\"156\" rx=\"6\" fill=\"#fff\" stroke=\"#e2d6c6\" stroke-width=\"3\"/>" +
        "<rect x=\"328\" y=\"74\" width=\"136\" height=\"136\" fill=\"url(#rw-sky)\"/>" +
        "<circle cx=\"430\" cy=\"104\" r=\"14\" fill=\"#fde68a\"/>" +
        "<ellipse cx=\"370\" cy=\"150\" rx=\"26\" ry=\"10\" fill=\"#fff\"/><ellipse cx=\"388\" cy=\"143\" rx=\"16\" ry=\"10\" fill=\"#fff\"/>" +
        "<path d=\"M396 74 V210 M328 142 H464\" stroke=\"#fff\" stroke-width=\"5\"/>" +
        "<rect x=\"310\" y=\"218\" width=\"172\" height=\"10\" rx=\"3\" fill=\"#fff\" stroke=\"#e2d6c6\"/>";

    // ------------------------------------------------------------ items (local coordinates)

    /// <summary>Beds: 300 × 190, the mattress top is at y = 95 for every bed (the bedding sits there).</summary>
    private static string Bed(string id) => id switch
    {
        "trae" => "<rect x=\"0\" y=\"40\" width=\"34\" height=\"150\" rx=\"8\" fill=\"#d8a86b\"/><path d=\"M6 60 H28 M6 80 H28 M6 100 H28\" stroke=\"#b9874c\" stroke-width=\"3\"/>" +
                  "<rect x=\"268\" y=\"90\" width=\"28\" height=\"100\" rx=\"6\" fill=\"#d8a86b\"/>" +
                  "<rect x=\"20\" y=\"112\" width=\"262\" height=\"52\" rx=\"6\" fill=\"#c4904f\"/>" + Mattress(),
        "koeje" => "<rect x=\"8\" y=\"-60\" width=\"14\" height=\"250\" fill=\"#94a3b8\"/><rect x=\"278\" y=\"-60\" width=\"14\" height=\"250\" fill=\"#94a3b8\"/>" +
                   "<rect x=\"14\" y=\"-12\" width=\"272\" height=\"18\" rx=\"4\" fill=\"#64748b\"/><rect x=\"24\" y=\"-32\" width=\"252\" height=\"20\" rx=\"6\" fill=\"#fff\"/>" +
                   "<rect x=\"30\" y=\"-44\" width=\"60\" height=\"16\" rx=\"8\" fill=\"#e0f2fe\"/><rect x=\"96\" y=\"-40\" width=\"174\" height=\"22\" rx=\"6\" fill=\"#60a5fa\"/>" +
                   "<path d=\"M250 6 V112 M276 6 V112 M250 30 H276 M250 56 H276 M250 82 H276\" stroke=\"#475569\" stroke-width=\"4\"/>" +
                   "<rect x=\"14\" y=\"112\" width=\"272\" height=\"52\" rx=\"6\" fill=\"#64748b\"/>" + Mattress(),
        "himmelseng" => "<rect x=\"6\" y=\"-70\" width=\"10\" height=\"260\" fill=\"#a855f7\"/><rect x=\"284\" y=\"-70\" width=\"10\" height=\"260\" fill=\"#a855f7\"/>" +
                        "<path d=\"M0 -70 H300 V-50 Q260 -20 230 -50 Q190 -20 150 -50 Q110 -20 70 -50 Q40 -20 0 -50Z\" fill=\"#f5d0fe\" stroke=\"#c084fc\" stroke-width=\"2\"/>" +
                        "<path d=\"M16 -50 Q40 40 20 150 L6 150 V-50Z M284 -50 Q262 40 280 150 L294 150 V-50Z\" fill=\"#fae8ff\" opacity=\".85\"/>" +
                        "<rect x=\"16\" y=\"112\" width=\"270\" height=\"52\" rx=\"6\" fill=\"#c084fc\"/>" + Mattress(),
        "raket" => "<path d=\"M10 110 H230 Q300 135 230 170 H10Z\" fill=\"#ef4444\"/><path d=\"M230 110 Q300 135 230 170Z\" fill=\"#fff\" opacity=\".25\"/>" +
                   "<circle cx=\"200\" cy=\"140\" r=\"14\" fill=\"#bae6fd\" stroke=\"#fff\" stroke-width=\"4\"/>" +
                   "<path d=\"M10 110 L-20 92 L10 126Z M10 170 L-20 188 L10 154Z\" fill=\"#f59e0b\"/>" +
                   "<path d=\"M-4 132 L-30 140 L-4 148Z\" fill=\"#fb923c\"/>" +
                   "<rect x=\"14\" y=\"92\" width=\"200\" height=\"22\" rx=\"8\" fill=\"#fff\"/><rect x=\"20\" y=\"80\" width=\"56\" height=\"18\" rx=\"9\" fill=\"#e0f2fe\"/>" +
                   "<rect x=\"84\" y=\"86\" width=\"126\" height=\"24\" rx=\"6\" fill=\"#1e3a8a\"/><circle cx=\"110\" cy=\"96\" r=\"2\" fill=\"#fde68a\"/><circle cx=\"150\" cy=\"100\" r=\"2\" fill=\"#fde68a\"/><circle cx=\"185\" cy=\"94\" r=\"2\" fill=\"#fde68a\"/>",
        _ => "<rect x=\"0\" y=\"60\" width=\"30\" height=\"130\" rx=\"6\" fill=\"#8b6b4e\"/>" +
             "<rect x=\"20\" y=\"112\" width=\"270\" height=\"48\" rx=\"4\" fill=\"#a47e5c\"/><rect x=\"280\" y=\"150\" width=\"12\" height=\"40\" fill=\"#8b6b4e\"/>" + Mattress(),
    };

    private static string Mattress() => "<rect x=\"28\" y=\"95\" width=\"254\" height=\"22\" rx=\"8\" fill=\"#fff\" stroke=\"#e5e7eb\"/>";

    /// <summary>Pillow + duvet on the bed's mattress (same 300 × 190 box as the bed).</summary>
    private static string Bedding(string id)
    {
        var (duvet, pillow, defs) = id switch
        {
            "stribet" => ("url(#bd-stripes)", "#dbeafe", "<pattern id=\"bd-stripes\" width=\"16\" height=\"16\" patternUnits=\"userSpaceOnUse\"><rect width=\"16\" height=\"16\" fill=\"#fff\"/><rect width=\"8\" height=\"16\" fill=\"#60a5fa\"/></pattern>"),
            "stjernetoej" => ("url(#bd-stars)", "#1e3a8a", "<pattern id=\"bd-stars\" width=\"28\" height=\"28\" patternUnits=\"userSpaceOnUse\"><rect width=\"28\" height=\"28\" fill=\"#1e3a8a\"/><path d=\"M8 6 l1.5 3 3 .5 -2.2 2.1 .5 3 -2.8 -1.5 -2.8 1.5 .5 -3 -2.2 -2.1 3 -.5Z\" fill=\"#fde047\"/><circle cx=\"21\" cy=\"20\" r=\"1.5\" fill=\"#fde047\"/></pattern>"),
            "raevetoej" => ("#f97316", "#fff7ed", ""),
            "regnbuetoej" => ("url(#bd-rainbow)", "#fef9c3", "<linearGradient id=\"bd-rainbow\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#f87171\"/><stop offset=\".2\" stop-color=\"#fb923c\"/><stop offset=\".4\" stop-color=\"#facc15\"/><stop offset=\".6\" stop-color=\"#4ade80\"/><stop offset=\".8\" stop-color=\"#60a5fa\"/><stop offset=\"1\" stop-color=\"#a78bfa\"/></linearGradient>"),
            _ => ("#f8fafc", "#ffffff", ""),
        };
        var sb = new StringBuilder();
        if (defs.Length > 0) sb.Append("<defs>").Append(defs).Append("</defs>");
        sb.Append($"<ellipse cx=\"66\" cy=\"90\" rx=\"34\" ry=\"14\" fill=\"{pillow}\" stroke=\"#d1d5db\"/>");
        sb.Append($"<path d=\"M104 82 H278 Q288 82 288 92 V128 H104 Q96 128 96 118 V92 Q96 82 104 82Z\" fill=\"{duvet}\" stroke=\"rgba(0,0,0,.12)\"/>");
        if (id == "raevetoej")
            sb.Append("<path d=\"M52 84 l4 -8 4 6 4 -6 4 8 -8 8Z\" fill=\"#f97316\"/><circle cx=\"60\" cy=\"88\" r=\"1.2\" fill=\"#1f2937\"/><path d=\"M180 96 l10 -12 10 10 10 -10 10 12 -20 18Z\" fill=\"#fff7ed\" opacity=\".9\"/>");
        return sb.ToString();
    }

    /// <summary>Desks: 260 × 170 with the floor at y = 170.</summary>
    private static string Desk(string id) => id switch
    {
        "computer" => "<rect x=\"0\" y=\"84\" width=\"190\" height=\"12\" rx=\"3\" fill=\"#a16207\"/><rect x=\"8\" y=\"96\" width=\"10\" height=\"74\" fill=\"#854d0e\"/><rect x=\"172\" y=\"96\" width=\"10\" height=\"74\" fill=\"#854d0e\"/>" +
                      "<rect x=\"100\" y=\"96\" width=\"78\" height=\"50\" fill=\"#ca8a04\"/><rect x=\"108\" y=\"104\" width=\"62\" height=\"14\" rx=\"3\" fill=\"#eab308\"/>" +
                      "<rect x=\"40\" y=\"14\" width=\"110\" height=\"64\" rx=\"5\" fill=\"#1f2937\"/><rect x=\"46\" y=\"20\" width=\"98\" height=\"50\" fill=\"#2563eb\"/>" +
                      "<text x=\"56\" y=\"42\" font-family=\"Consolas,monospace\" font-size=\"12\" fill=\"#fff\">asdf jklæ</text><rect x=\"56\" y=\"50\" width=\"40\" height=\"6\" fill=\"#93c5fd\"/>" +
                      "<rect x=\"88\" y=\"78\" width=\"14\" height=\"8\" fill=\"#1f2937\"/><rect x=\"48\" y=\"78\" width=\"96\" height=\"6\" rx=\"2\" fill=\"#e5e7eb\"/>" + Chair("#2563eb"),
        "gamer" => "<defs><linearGradient id=\"dk-rgb\" x1=\"0\" x2=\"1\"><stop offset=\"0\" stop-color=\"#f43f5e\"/><stop offset=\".5\" stop-color=\"#22d3ee\"/><stop offset=\"1\" stop-color=\"#a855f7\"/></linearGradient></defs>" +
                   "<rect x=\"0\" y=\"84\" width=\"200\" height=\"12\" rx=\"3\" fill=\"#111827\"/><rect x=\"0\" y=\"96\" width=\"200\" height=\"4\" fill=\"url(#dk-rgb)\"/>" +
                   "<path d=\"M10 100 L22 170 M190 100 L178 170\" stroke=\"#111827\" stroke-width=\"8\"/>" +
                   "<rect x=\"30\" y=\"16\" width=\"140\" height=\"62\" rx=\"5\" fill=\"#111827\"/><rect x=\"36\" y=\"22\" width=\"128\" height=\"50\" fill=\"url(#dk-rgb)\" opacity=\".85\"/>" +
                   "<path d=\"M60 60 l16 -20 16 12 20 -24 16 16\" fill=\"none\" stroke=\"#fff\" stroke-width=\"3\"/>" +
                   "<rect x=\"92\" y=\"78\" width=\"16\" height=\"8\" fill=\"#111827\"/>" + Chair("#dc2626", gamer: true),
        "guldbord" => "<rect x=\"0\" y=\"80\" width=\"200\" height=\"16\" rx=\"4\" fill=\"#eab308\" stroke=\"#a16207\" stroke-width=\"2\"/>" +
                      "<path d=\"M14 96 Q4 130 20 170 M186 96 Q196 130 180 170\" stroke=\"#ca8a04\" stroke-width=\"9\" fill=\"none\"/>" +
                      "<path d=\"M40 96 H160 V130 Q100 150 40 130Z\" fill=\"#facc15\" stroke=\"#a16207\" stroke-width=\"2\"/><circle cx=\"100\" cy=\"118\" r=\"7\" fill=\"#a16207\"/>" +
                      "<path d=\"M150 80 V36 M150 36 l-18 -10 h36Z\" stroke=\"#a16207\" stroke-width=\"4\" fill=\"#fde047\"/><circle cx=\"150\" cy=\"32\" r=\"20\" fill=\"#fde047\" opacity=\".25\"/>" +
                      "<rect x=\"40\" y=\"66\" width=\"50\" height=\"14\" rx=\"2\" fill=\"#b91c1c\"/><rect x=\"44\" y=\"58\" width=\"42\" height=\"8\" rx=\"2\" fill=\"#1d4ed8\"/>" + Chair("#ca8a04"),
        _ => "<rect x=\"10\" y=\"88\" width=\"170\" height=\"12\" rx=\"3\" fill=\"#b45309\"/><rect x=\"20\" y=\"100\" width=\"10\" height=\"70\" fill=\"#92400e\"/><rect x=\"160\" y=\"100\" width=\"10\" height=\"70\" fill=\"#92400e\"/>" +
             "<rect x=\"40\" y=\"72\" width=\"44\" height=\"16\" rx=\"2\" fill=\"#22c55e\"/><rect x=\"46\" y=\"64\" width=\"36\" height=\"8\" rx=\"2\" fill=\"#f97316\"/><path d=\"M120 88 l30 -30\" stroke=\"#facc15\" stroke-width=\"5\" stroke-linecap=\"round\"/>" + Chair("#f97316"),
    };

    private static string Chair(string color, bool gamer = false) => gamer
        ? $"<path d=\"M206 40 Q200 20 230 20 Q258 20 252 40 L248 110 H210Z\" fill=\"{color}\"/><rect x=\"214\" y=\"46\" width=\"30\" height=\"6\" fill=\"#111827\"/><rect x=\"200\" y=\"108\" width=\"58\" height=\"14\" rx=\"5\" fill=\"#111827\"/><path d=\"M229 122 V158 M206 168 L229 158 L252 168\" stroke=\"#111827\" stroke-width=\"6\" fill=\"none\"/>"
        : $"<rect x=\"212\" y=\"60\" width=\"40\" height=\"56\" rx=\"8\" fill=\"{color}\"/><rect x=\"204\" y=\"110\" width=\"52\" height=\"12\" rx=\"4\" fill=\"{color}\"/><path d=\"M210 122 V170 M250 122 V170\" stroke=\"#475569\" stroke-width=\"5\"/>";

    /// <summary>Lights: 240 × 110 on the wall above the bed.</summary>
    private static string Light(string id) => id switch
    {
        "lyskaede" => "<path d=\"M0 10 Q120 70 240 10\" fill=\"none\" stroke=\"#334155\" stroke-width=\"2\"/>" +
                      string.Concat(Enumerable.Range(0, 9).Select(i =>
                      {
                          var t = (i + 0.5) / 9.0;
                          var x = 240 * t;
                          var y = 10 + 120 * t * (1 - t); // on the quadratic curve (control point at 70)
                          var c = new[] { "#f87171", "#fbbf24", "#4ade80", "#60a5fa", "#c084fc" }[i % 5];
                          return $"<circle cx=\"{N(x)}\" cy=\"{N(y + 10)}\" r=\"9\" fill=\"{c}\" opacity=\".35\"/><ellipse cx=\"{N(x)}\" cy=\"{N(y + 10)}\" rx=\"4.5\" ry=\"6.5\" fill=\"{c}\"/>";
                      })),
        "stjernelampe" => "<circle cx=\"120\" cy=\"55\" r=\"52\" fill=\"#fde68a\" opacity=\".35\"/><path d=\"M120 12 l12 26 28 3 -21 19 6 28 -25 -14 -25 14 6 -28 -21 -19 28 -3Z\" fill=\"#facc15\" stroke=\"#ca8a04\" stroke-width=\"2\"/>",
        "neon" => "<rect x=\"30\" y=\"14\" width=\"180\" height=\"80\" rx=\"14\" fill=\"#1e1b4b\"/>" +
                  "<text x=\"120\" y=\"68\" text-anchor=\"middle\" font-family=\"Arial Rounded MT Bold,Arial\" font-weight=\"700\" font-size=\"38\" fill=\"none\" stroke=\"#f472b6\" stroke-width=\"8\" opacity=\".35\">1+1=2</text>" +
                  "<text x=\"120\" y=\"68\" text-anchor=\"middle\" font-family=\"Arial Rounded MT Bold,Arial\" font-weight=\"700\" font-size=\"38\" fill=\"#fdf2f8\" stroke=\"#ec4899\" stroke-width=\"2\">1+1=2</text>",
        "lysekrone" => "<path d=\"M120 0 V30\" stroke=\"#a16207\" stroke-width=\"3\"/><circle cx=\"120\" cy=\"60\" r=\"60\" fill=\"#fde68a\" opacity=\".25\"/>" +
                       "<path d=\"M60 56 Q120 96 180 56\" fill=\"none\" stroke=\"#ca8a04\" stroke-width=\"5\"/><path d=\"M84 46 Q120 74 156 46\" fill=\"none\" stroke=\"#ca8a04\" stroke-width=\"4\"/>" +
                       "<circle cx=\"120\" cy=\"34\" r=\"8\" fill=\"#facc15\"/>" +
                       string.Concat(new[] { 60, 90, 120, 150, 180 }.Select(x => $"<rect x=\"{x - 3}\" y=\"36\" width=\"6\" height=\"16\" fill=\"#fff\"/><ellipse cx=\"{x}\" cy=\"32\" rx=\"3\" ry=\"5\" fill=\"#fb923c\"/>")),
        _ => "<path d=\"M120 0 V36\" stroke=\"#334155\" stroke-width=\"2\"/><path d=\"M120 66 m-60 0 l60 50 l60 -50Z\" fill=\"#fef3c7\" opacity=\".55\"/>" +
             "<path d=\"M92 66 L104 36 H136 L148 66Z\" fill=\"#f97316\"/><ellipse cx=\"120\" cy=\"68\" rx=\"10\" ry=\"6\" fill=\"#fde68a\"/>",
    };

    /// <summary>Posters: 130 × 150 with a frame.</summary>
    private static string Poster(string id)
    {
        const string frame = "<rect x=\"0\" y=\"0\" width=\"130\" height=\"150\" rx=\"4\" fill=\"#fff\" stroke=\"#78716c\" stroke-width=\"5\"/>";
        var content = id switch
        {
            "rumplakat" => "<rect x=\"8\" y=\"8\" width=\"114\" height=\"134\" fill=\"#0f172a\"/><circle cx=\"80\" cy=\"52\" r=\"22\" fill=\"#f59e0b\"/><ellipse cx=\"80\" cy=\"52\" rx=\"36\" ry=\"8\" fill=\"none\" stroke=\"#fde68a\" stroke-width=\"3\"/>" +
                           "<path d=\"M30 128 L46 88 L62 128Z\" fill=\"#e5e7eb\"/><circle cx=\"46\" cy=\"108\" r=\"5\" fill=\"#60a5fa\"/><path d=\"M38 128 L46 142 L54 128Z\" fill=\"#f97316\"/>" +
                           "<circle cx=\"20\" cy=\"24\" r=\"1.5\" fill=\"#fff\"/><circle cx=\"108\" cy=\"110\" r=\"1.5\" fill=\"#fff\"/><circle cx=\"30\" cy=\"60\" r=\"1\" fill=\"#fff\"/>",
            "piplakat" => "<rect x=\"8\" y=\"8\" width=\"114\" height=\"134\" fill=\"#eef2ff\"/><text x=\"65\" y=\"92\" text-anchor=\"middle\" font-family=\"Georgia,serif\" font-size=\"84\" fill=\"#4f46e5\">π</text>" +
                          "<text x=\"65\" y=\"126\" text-anchor=\"middle\" font-family=\"Consolas,monospace\" font-size=\"15\" fill=\"#312e81\">3,14159…</text>",
            "tabelplakat" => "<rect x=\"8\" y=\"8\" width=\"114\" height=\"134\" fill=\"#fff7ed\"/><text x=\"65\" y=\"26\" text-anchor=\"middle\" font-family=\"Arial\" font-weight=\"700\" font-size=\"12\" fill=\"#c2410c\">GANGETABEL</text>" +
                             string.Concat(Enumerable.Range(1, 5).SelectMany(r => Enumerable.Range(1, 5).Select(c =>
                                 $"<text x=\"{18 + (c - 1) * 23}\" y=\"{48 + (r - 1) * 21}\" font-family=\"Arial\" font-size=\"11\" fill=\"{(r == c ? "#c2410c" : "#57534e")}\" font-weight=\"{(r == c ? 700 : 400)}\">{r * c}</text>"))),
            _ => "<rect x=\"8\" y=\"8\" width=\"114\" height=\"134\" fill=\"#dcfce7\"/><path d=\"M8 142 V110 L28 80 L44 110 L60 70 L80 110 V142Z\" fill=\"#16a34a\"/>" +
                 AvatarArt.Svg("hvalp").Replace("<svg ", "<svg x=\"56\" y=\"60\" width=\"64\" height=\"64\" ", StringComparison.Ordinal),
        };
        return frame + content;
    }

    /// <summary>Shelves: 210 × 54, the plank at the bottom.</summary>
    private static string Shelf(string id)
    {
        const string plank = "<rect x=\"0\" y=\"44\" width=\"210\" height=\"10\" rx=\"2\" fill=\"#a16207\"/><path d=\"M20 54 l10 14 M190 54 l-10 14\" stroke=\"#854d0e\" stroke-width=\"4\"/>";
        var items = id switch
        {
            "legetoej" => "<circle cx=\"40\" cy=\"24\" r=\"14\" fill=\"#b45309\"/><circle cx=\"30\" cy=\"10\" r=\"6\" fill=\"#b45309\"/><circle cx=\"50\" cy=\"10\" r=\"6\" fill=\"#b45309\"/><circle cx=\"40\" cy=\"26\" r=\"6\" fill=\"#fde68a\"/><circle cx=\"35\" cy=\"20\" r=\"1.8\" fill=\"#1f2937\"/><circle cx=\"45\" cy=\"20\" r=\"1.8\" fill=\"#1f2937\"/>" +
                          "<rect x=\"86\" y=\"22\" width=\"50\" height=\"16\" rx=\"5\" fill=\"#ef4444\"/><rect x=\"96\" y=\"12\" width=\"28\" height=\"12\" rx=\"4\" fill=\"#ef4444\"/><circle cx=\"98\" cy=\"40\" r=\"6\" fill=\"#1f2937\"/><circle cx=\"124\" cy=\"40\" r=\"6\" fill=\"#1f2937\"/>" +
                          "<circle cx=\"170\" cy=\"28\" r=\"16\" fill=\"#3b82f6\"/><path d=\"M154 28 H186 M170 12 Q160 28 170 44 M170 12 Q180 28 170 44\" stroke=\"#fff\" stroke-width=\"2\" fill=\"none\"/>",
            "pokaler" => string.Concat(new[] { (30, 1.0), (105, 1.25), (180, 0.9) }.Select(t =>
                         $"<g transform=\"translate({t.Item1} 44) scale({N(t.Item2)})\"><rect x=\"-12\" y=\"-6\" width=\"24\" height=\"6\" fill=\"#a16207\"/><rect x=\"-3\" y=\"-14\" width=\"6\" height=\"8\" fill=\"#ca8a04\"/><path d=\"M-14 -38 H14 Q14 -14 0 -14 Q-14 -14 -14 -38Z\" fill=\"#facc15\" stroke=\"#ca8a04\"/><path d=\"M-14 -34 Q-24 -30 -14 -22 M14 -34 Q24 -30 14 -22\" stroke=\"#ca8a04\" stroke-width=\"3\" fill=\"none\"/></g>")),
            _ => string.Concat(new[] { ("#ef4444", 34), ("#3b82f6", 40), ("#22c55e", 30), ("#f59e0b", 38), ("#a855f7", 36), ("#14b8a6", 32), ("#f97316", 40), ("#6366f1", 34) }
                    .Select((b, i) => $"<rect x=\"{14 + i * 16}\" y=\"{44 - b.Item2}\" width=\"14\" height=\"{b.Item2}\" rx=\"2\" fill=\"{b.Item1}\"/>")) +
                 "<rect x=\"150\" y=\"14\" width=\"40\" height=\"30\" rx=\"3\" fill=\"#fde68a\" transform=\"rotate(-8 170 30)\"/>",
        };
        return items + plank;
    }

    /// <summary>Plants: 60 × 140, the pot stands on the floor.</summary>
    private static string Plant(string id)
    {
        const string pot = "<path d=\"M10 104 H50 L45 140 H15Z\" fill=\"#c2410c\"/><rect x=\"6\" y=\"98\" width=\"48\" height=\"10\" rx=\"3\" fill=\"#ea580c\"/>";
        var plant = id switch
        {
            "monstera" => "<path d=\"M30 100 Q28 70 14 50 M30 100 Q34 64 48 44 M30 100 Q30 60 30 34\" stroke=\"#15803d\" stroke-width=\"3\" fill=\"none\"/>" +
                          "<ellipse cx=\"12\" cy=\"46\" rx=\"14\" ry=\"10\" fill=\"#16a34a\"/><ellipse cx=\"48\" cy=\"40\" rx=\"14\" ry=\"10\" fill=\"#22c55e\"/><ellipse cx=\"30\" cy=\"28\" rx=\"14\" ry=\"12\" fill=\"#15803d\"/>",
            "solsikke" => "<path d=\"M30 100 V20\" stroke=\"#15803d\" stroke-width=\"4\"/><ellipse cx=\"20\" cy=\"64\" rx=\"10\" ry=\"5\" fill=\"#22c55e\" transform=\"rotate(-30 20 64)\"/>" +
                          string.Concat(Enumerable.Range(0, 12).Select(i => $"<ellipse cx=\"30\" cy=\"4\" rx=\"5\" ry=\"10\" fill=\"#facc15\" transform=\"rotate({i * 30} 30 18)\"/>")) +
                          "<circle cx=\"30\" cy=\"18\" r=\"9\" fill=\"#78350f\"/>",
            "bonsai" => "<path d=\"M30 100 Q24 80 34 66 Q42 56 30 44\" stroke=\"#78350f\" stroke-width=\"6\" fill=\"none\"/><path d=\"M34 66 Q46 62 50 54\" stroke=\"#78350f\" stroke-width=\"4\" fill=\"none\"/>" +
                        "<ellipse cx=\"28\" cy=\"40\" rx=\"20\" ry=\"11\" fill=\"#16a34a\"/><ellipse cx=\"50\" cy=\"52\" rx=\"12\" ry=\"8\" fill=\"#22c55e\"/>",
            _ => "<rect x=\"22\" y=\"48\" width=\"16\" height=\"54\" rx=\"8\" fill=\"#16a34a\"/><path d=\"M22 74 H12 V58\" stroke=\"#16a34a\" stroke-width=\"8\" fill=\"none\" stroke-linecap=\"round\"/><path d=\"M38 66 H48 V52\" stroke=\"#16a34a\" stroke-width=\"8\" fill=\"none\" stroke-linecap=\"round\"/>" +
                 "<circle cx=\"30\" cy=\"46\" r=\"4\" fill=\"#f472b6\"/>",
        };
        return plant + pot;
    }

    /// <summary>Rugs: 320 × 80 on the floor.</summary>
    private static string Rug(string id) => id switch
    {
        "stribettaeppe" => "<defs><clipPath id=\"rg-clip\"><ellipse cx=\"160\" cy=\"40\" rx=\"156\" ry=\"36\"/></clipPath></defs>" +
                           "<g clip-path=\"url(#rg-clip)\">" + string.Concat(Enumerable.Range(0, 10).Select(i => $"<rect x=\"{i * 32}\" y=\"0\" width=\"32\" height=\"80\" fill=\"{(i % 2 == 0 ? "#fb923c" : "#fde68a")}\"/>")) + "</g>" +
                           "<ellipse cx=\"160\" cy=\"40\" rx=\"156\" ry=\"36\" fill=\"none\" stroke=\"#c2410c\" stroke-width=\"3\"/>",
        "raevetaeppe" => "<path d=\"M30 20 L70 40 L250 40 L290 20 L280 60 Q160 96 40 60Z\" fill=\"#f97316\"/><path d=\"M70 46 Q160 82 250 46 Q200 76 160 78 Q120 76 70 46Z\" fill=\"#fff7ed\"/>" +
                         "<circle cx=\"120\" cy=\"50\" r=\"5\" fill=\"#1f2937\"/><circle cx=\"200\" cy=\"50\" r=\"5\" fill=\"#1f2937\"/><ellipse cx=\"160\" cy=\"68\" rx=\"7\" ry=\"4\" fill=\"#1f2937\"/>",
        "regnbuetaeppe" => string.Concat(new[] { "#f87171", "#fb923c", "#facc15", "#4ade80", "#60a5fa", "#a78bfa" }
                               .Select((c, i) => $"<ellipse cx=\"160\" cy=\"40\" rx=\"{156 - i * 22}\" ry=\"{36 - i * 5}\" fill=\"{c}\"/>")),
        _ => "<ellipse cx=\"160\" cy=\"40\" rx=\"156\" ry=\"36\" fill=\"#60a5fa\"/><ellipse cx=\"160\" cy=\"40\" rx=\"136\" ry=\"28\" fill=\"none\" stroke=\"#dbeafe\" stroke-width=\"3\" stroke-dasharray=\"8 6\"/>",
    };
}
