using System.Text;

namespace Regneraeven.Core.Typing;

/// <summary>One lesson in the 10-finger course.</summary>
/// <param name="Id">Stable id stored in the database – never rename.</param>
/// <param name="NewKeys">The keys this lesson introduces (shown and highlighted).</param>
/// <param name="Patterns">Letter drills (e.g. "fff jjj") – used when there are no words yet.</param>
/// <param name="Words">Words or short sentences/calculations to type.</param>
public sealed record TypingLesson(
    string Id,
    string Group,
    string Title,
    string NewKeys,
    string Tip,
    int Difficulty,
    IReadOnlyList<string> Patterns,
    IReadOnlyList<string> Words);

/// <summary>
/// The course: home row → top row → bottom row → æ ø å → capitals → numbers and maths signs.
/// Layout is the Danish keyboard. Each lesson only uses keys learned so far.
/// </summary>
public static class TypingCourse
{
    public static readonly IReadOnlyList<TypingLesson> Lessons =
    [
        // ---------------- Grundrækken
        new("fj", "Grundrækken", "Pegefingrene: f og j", "fj",
            "Læg pegefingrene på f og j – de har små bump, så du kan mærke dem uden at kigge.", 1,
            ["fff", "jjj", "fj", "jf", "ffj", "jjf", "fjf", "jfj"], []),
        new("dk", "Grundrækken", "Langefingrene: d og k", "dk",
            "Langefingrene hviler på d og k – lige ved siden af pegefingrene.", 1,
            ["ddd", "kkk", "dk", "kd", "fdk", "jkd", "dkf", "kjd", "fdjk"], []),
        new("sl", "Grundrækken", "Ringfingrene: s og l", "sl",
            "Ringfingrene hviler på s og l.", 1,
            ["sss", "lll", "sl", "ls", "sdf", "lkj", "slk", "fds", "jkl", "dsl"], []),
        new("aae", "Grundrækken", "Lillefingrene: a og æ", "aæ",
            "Lillefingrene hviler på a og æ. Nu ligger alle otte fingre på grundrækken!", 1,
            ["aaa", "æææ", "aæ", "asdf", "jklæ", "fdsa", "ælkj"], ["sal", "dal", "fad", "lad", "kals", "skal", "æl"]),
        new("gh", "Grundrækken", "Hele grundrækken: g og h", "gh",
            "Pegefingrene strækker sig ind til g og h – og hopper så tilbage til f og j.", 1,
            ["ggg", "hhh", "fg", "jh", "gh"],
            ["flag", "glas", "hals", "gas", "haj", "dag", "slag", "salg", "falsk", "glad", "skaf"]),

        // ---------------- Øverste række
        new("er", "Øverste række", "e og r", "er",
            "e tages med langefingeren og r med pegefingeren – op og tilbage igen.", 1,
            ["eee", "rrr", "de", "fr", "er", "re"],
            ["er", "der", "her", "ser", "fersk", "sær", "rejse", "sjæl", "fre", "rask", "kære", "hele"]),
        new("tu", "Øverste række", "t og u", "tu",
            "t tages med venstre pegefinger, u med højre pegefinger.", 2,
            ["ttt", "uuu", "ft", "ju"],
            ["tre", "ude", "tur", "hus", "ret", "tæt", "skat", "juletræ", "tegl", "fuld", "lukket", "stue"]),
        new("io", "Øverste række", "i og o", "io",
            "i tages med højre langefinger, o med højre ringfinger.", 2,
            ["iii", "ooo", "ki", "lo"],
            ["is", "sol", "ord", "skole", "otte", "ost", "fisk", "flot", "kiosk", "lille", "rolig", "tid"]),
        new("pyw", "Øverste række", "p, y, w og q", "pywq",
            "p og å tages med højre lillefinger, w med venstre ringfinger og y med højre pegefinger.", 2,
            ["ppp", "yyy", "www", "qqq"],
            ["post", "peger", "type", "hyl", "pige", "spil", "kopi", "tre typer", "sky", "pyt", "wifi"]),

        // ---------------- Nederste række
        new("vn", "Nederste række", "v og n", "vn",
            "Pegefingrene går ned: venstre til v, højre til n.", 2,
            ["vvv", "nnn", "fv", "jn"],
            ["vind", "vand", "ven", "nu", "sne", "avis", "vinter", "ni", "seng", "svin", "nogen", "vente"]),
        new("bm", "Nederste række", "b og m", "bm",
            "b tages med venstre pegefinger, m med højre pegefinger.", 2,
            ["bbb", "mmm", "fb", "jm"],
            ["bil", "bog", "mus", "mand", "bold", "bamse", "mor", "bjerg", "minut", "bord", "mange", "bombe"]),
        new("cx", "Nederste række", "c, x, z, komma og punktum", "cxz,.",
            "Komma tages med højre langefinger og punktum med højre ringfinger.", 2,
            ["ccc", "xxx", "zzz", ",,,", "..."],
            ["zebra", "taxi", "cirkus", "cykel", "ok.", "hej, du.", "vi ses.", "ja, tak.", "en, to, tre.", "pizza"]),

        // ---------------- Æ Ø Å
        new("oeaa", "Æ, Ø og Å", "æ, ø og å", "æøå",
            "ø ligger lige til højre for æ, og å ligger til højre for p – alle tages med højre lillefinger.", 2,
            ["øøø", "ååå", "æøå"],
            ["øl", "sø", "grøn", "blå", "år", "æble", "ærme", "røde", "både", "gå", "hånd", "små", "fødselsdag", "søster", "køre", "måne"]),

        // ---------------- Store bogstaver
        new("shift", "Store bogstaver", "Store bogstaver med Shift", "⇧",
            "Hold Shift nede med den ANDEN hånds lillefinger – stort A: højre Shift, stort J: venstre Shift.", 2,
            [],
            ["Ida", "Mads", "Emma", "Oskar", "Danmark", "Aarhus", "Odense", "Ræven", "Ole har en kat.", "Sofie spiller fodbold.", "Jeg hedder Ali."]),

        // ---------------- Tal og tegn
        new("tal15", "Tal og tegn", "Tallene 1 til 5", "12345",
            "Tallene ligger øverst. 1 og 2 med venstre lille- og ringfinger, 4 og 5 med venstre pegefinger.", 3,
            ["11", "22", "33", "44", "55", "12", "34", "45", "21", "53", "135", "24"], []),
        new("tal60", "Tal og tegn", "Tallene 6 til 0", "67890",
            "6 og 7 med højre pegefinger, 8 langefinger, 9 ringfinger og 0 lillefinger.", 3,
            ["66", "77", "88", "99", "00", "67", "89", "90", "76", "608", "1990", "2026"], []),
        new("regnetegn", "Tal og tegn", "Regnetegn", "+-=/*()",
            "+ ligger til højre for 0. / er Shift + 7, * er Shift + ', = er Shift + 0 og ( ) er Shift + 8 og 9.", 3,
            [],
            ["2 + 3 = 5", "9 - 4 = 5", "6 / 2 = 3", "3 * 4 = 12", "10 - 7 = 3", "8 + 8 = 16", "(2 + 1) * 3 = 9", "20 / 5 = 4"]),
        new("regnestykker", "Tal og tegn", "Regnestykker", ":",
            "Nu skriver du rigtige regnestykker – som dem du gemmer under Mine opgaver.", 3,
            [],
            ["3/4 + 1/6", "12 - 7 = 5", "2,5 + 1,5 = 4", "(4 + 3) * 5 = 35", "48 : 6 = 8", "1/2 + 1/4 = 3/4", "7 * 8 = 56", "100 - 45 = 55", "0,5 * 4 = 2"]),
    ];

    public static TypingLesson? Find(string? id) => Lessons.FirstOrDefault(l => l.Id == id);

    public static int IndexOf(TypingLesson lesson) => Lessons.ToList().FindIndex(l => l.Id == lesson.Id);

    public static TypingLesson? Next(TypingLesson lesson)
    {
        var i = IndexOf(lesson);
        return i >= 0 && i + 1 < Lessons.Count ? Lessons[i + 1] : null;
    }

    /// <summary>
    /// An exercise text of about <paramref name="targetLength"/> characters: letter drills for the first lessons,
    /// words (mixed with a few drills) later. Different every time.
    /// </summary>
    public static string MakeText(TypingLesson lesson, Random rng, int targetLength = 90)
    {
        var parts = new List<string>();
        var length = 0;
        var pool = lesson.Words.Count >= 4 ? lesson.Words : lesson.Patterns.Concat(lesson.Words).ToList();
        string? last = null;
        while (length < targetLength)
        {
            // Now and then a drill with the new keys, so they get extra practice.
            var usePattern = lesson.Patterns.Count > 0 && lesson.Words.Count >= 4 && rng.NextDouble() < 0.2;
            var source = usePattern ? lesson.Patterns : pool;
            var next = source[rng.Next(source.Count)];
            if (next == last && source.Count > 1) continue;
            parts.Add(next);
            last = next;
            length += next.Length + 1;
        }
        // Calculations and sentences are separated a bit more clearly.
        var separator = lesson.Words.Any(w => w.Contains(' ')) ? "   " : " ";
        return string.Join(separator, parts).Trim();
    }
}
