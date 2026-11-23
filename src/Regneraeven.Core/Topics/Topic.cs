namespace Regneraeven.Core.Topics;

/// <summary>
/// The kinds of maths in Regneræven. Used both as tags on saved problems and as task types in the game.
/// The numeric values are stored in the database – never renumber them.
/// </summary>
public enum Topic
{
    Plus = 1,
    Minus = 2,
    Gange = 3,
    Division = 4,
    Regnehierarki = 5,
    NegativeTal = 6,
    Decimaltal = 7,
    Enheder = 8,
    Broeker = 9,
    Procent = 10,
    Ligninger = 11,
    Geometri = 12,
    Koordinatsystem = 13,
    Tekststykke = 14,
}

public sealed record TopicInfo(
    Topic Topic,
    string Slug,
    string Name,
    string Short,
    string Icon,
    string Color,
    int BaseXp,
    bool InGame,
    string Description);

public static class TopicCatalog
{
    /// <summary>Simple arithmetic gives a little XP; decimals, fractions, geometry and coordinates give more.</summary>
    public static readonly IReadOnlyList<TopicInfo> All =
    [
        new(Topic.Plus, "plus", "Plus", "Plus", "+", "#2e9e5b", 5, true, "Læg tal sammen – også store tal med mente."),
        new(Topic.Minus, "minus", "Minus", "Minus", "−", "#1f8fbf", 5, true, "Træk fra – også når du skal låne."),
        new(Topic.Gange, "gange", "Gange", "Gange", "×", "#7a5af8", 8, true, "Tabellerne og store gangestykker."),
        new(Topic.Division, "division", "Division", "Division", "÷", "#c4458f", 8, true, "Del op – med og uden rest."),
        new(Topic.Regnehierarki, "regnehierarki", "Regnearternes rækkefølge", "Rækkefølge", "( )", "#d97706", 12, true, "Parenteser først, så gange og division, så plus og minus."),
        new(Topic.NegativeTal, "negative-tal", "Negative tal", "Negative tal", "−3", "#0e7490", 12, true, "Tal under nul på tallinjen."),
        new(Topic.Decimaltal, "decimaltal", "Decimaltal", "Decimaltal", "2,5", "#0891b2", 15, true, "Tal med komma: plus, minus, gange og division."),
        new(Topic.Enheder, "enheder", "Enheder", "Enheder", "cm", "#65a30d", 15, true, "Omregn mellem km, m, cm, kg, g, liter og dl."),
        new(Topic.Broeker, "broeker", "Brøker", "Brøker", "¾", "#e8590c", 20, true, "Forkort, forlæng, læg sammen og find brøkdelen af et tal."),
        new(Topic.Procent, "procent", "Procent", "Procent", "%", "#db2777", 20, true, "Hundrededele: find procent af et tal og rabatter."),
        new(Topic.Ligninger, "ligninger", "Ligninger", "Ligninger", "x=?", "#4f46e5", 20, true, "Find det ukendte tal x."),
        new(Topic.Geometri, "geometri", "Geometri", "Geometri", "△", "#b45309", 25, true, "Omkreds, areal og vinkler."),
        new(Topic.Koordinatsystem, "koordinatsystem", "Koordinatsystem", "Koordinater", "⌖", "#0f766e", 25, true, "Find og afsæt punkter (x, y)."),
        new(Topic.Tekststykke, "tekststykke", "Tekststykke", "Tekststykke", "Aa", "#475467", 0, false, "Regnehistorier, hvor du selv skal finde regnestykket."),
    ];

    private static readonly Dictionary<Topic, TopicInfo> ByTopic = All.ToDictionary(t => t.Topic);
    private static readonly Dictionary<string, TopicInfo> BySlug = All.ToDictionary(t => t.Slug, StringComparer.OrdinalIgnoreCase);

    public static TopicInfo Info(this Topic topic) => ByTopic[topic];

    public static TopicInfo? FromSlug(string? slug) => slug is not null && BySlug.TryGetValue(slug, out var t) ? t : null;

    public static IEnumerable<TopicInfo> Game => All.Where(t => t.InGame);
}
