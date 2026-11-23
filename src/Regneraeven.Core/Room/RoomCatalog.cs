using System.Text.Json;

namespace Regneraeven.Core.Room;

/// <summary>Where a thing goes in the room. Stored as text in the room layout – don't rename.</summary>
public enum RoomSlot { Wall, Bed, Bedding, Desk, Light, Poster, Shelf, Plant, Rug }

/// <param name="Price">Gold. 0 = free (the starting furniture).</param>
/// <param name="Level">Level needed before it can be bought (1 = from the start).</param>
public sealed record RoomItem(string Id, RoomSlot Slot, string Name, int Price, int Level, string Description)
{
    public bool IsFree => Price == 0;
}

/// <summary>
/// Gold (guld) and the fox room. Gold is earned together with XP and spent in the shop – XP is never spent.
/// Only gold from tasks, never real money.
/// </summary>
public static class RoomCatalog
{
    /// <summary>Gold for an XP award: 1 gold per 4 XP, rounded up (0 XP = 0 gold).</summary>
    public static int GoldFor(int xp) => xp <= 0 ? 0 : (xp + 3) / 4;

    public static readonly IReadOnlyList<(RoomSlot Slot, string Name, string Icon, bool Optional)> Slots =
    [
        (RoomSlot.Wall, "Vægge og tapet", "🎨", false),
        (RoomSlot.Bed, "Senge", "🛏️", false),
        (RoomSlot.Bedding, "Puder og sengetøj", "🧸", false),
        (RoomSlot.Desk, "Skriveborde", "🪑", false),
        (RoomSlot.Light, "Lamper og lys", "💡", true),
        (RoomSlot.Poster, "Plakater", "🖼️", true),
        (RoomSlot.Shelf, "Reoler og legetøj", "📚", true),
        (RoomSlot.Plant, "Planter", "🪴", true),
        (RoomSlot.Rug, "Tæpper", "🟠", true),
    ];

    public static readonly IReadOnlyList<RoomItem> Items =
    [
        // Walls
        new("hvid", RoomSlot.Wall, "Hvid væg", 0, 1, "Lys og rolig."),
        new("mint", RoomSlot.Wall, "Mintgrøn", 40, 1, "Frisk mintgrøn maling."),
        new("himmel", RoomSlot.Wall, "Himmelblå", 40, 1, "Blå som en sommerhimmel."),
        new("rosa", RoomSlot.Wall, "Lyserød", 40, 1, "Blød lyserød maling."),
        new("stjerner", RoomSlot.Wall, "Stjernetapet", 120, 1, "Mørkeblå tapet med stjerner."),
        new("taltapet", RoomSlot.Wall, "Taltapet", 150, 5, "Tapet med tal og regnetegn."),
        new("striber", RoomSlot.Wall, "Regnbuestriber", 200, 10, "Glade striber i alle farver."),
        new("guldtapet", RoomSlot.Wall, "Guldtapet", 400, 50, "Kun for Guldræve."),

        // Beds
        new("simpel", RoomSlot.Bed, "Simpel seng", 0, 1, "En god seng at sove i."),
        new("trae", RoomSlot.Bed, "Træseng", 80, 1, "Lun seng i lyst træ."),
        new("koeje", RoomSlot.Bed, "Køjeseng", 250, 10, "To etager – plads til en ven."),
        new("himmelseng", RoomSlot.Bed, "Himmelseng", 350, 25, "Med stof hængende over."),
        new("raket", RoomSlot.Bed, "Raketseng", 500, 40, "Klar til at sove blandt stjernerne."),

        // Bedding
        new("hvidt", RoomSlot.Bedding, "Hvidt sengetøj", 0, 1, "Rent og hvidt."),
        new("stribet", RoomSlot.Bedding, "Stribet sengetøj", 40, 1, "Blå og hvide striber."),
        new("stjernetoej", RoomSlot.Bedding, "Stjernesengetøj", 80, 1, "Små gule stjerner."),
        new("raevetoej", RoomSlot.Bedding, "Rævesengetøj", 120, 5, "Orange med en ræv på puden."),
        new("regnbuetoej", RoomSlot.Bedding, "Regnbuesengetøj", 150, 10, "Alle regnbuens farver."),

        // Desks
        new("lillebord", RoomSlot.Desk, "Lille bord", 0, 1, "Et lille bord og en stol."),
        new("computer", RoomSlot.Desk, "Skrivebord med computer", 180, 5, "Perfekt til 10-finger-træning."),
        new("gamer", RoomSlot.Desk, "Gamerbord", 350, 20, "Med farvede lys under bordpladen."),
        new("guldbord", RoomSlot.Desk, "Guld-skrivebord", 600, 50, "Et skrivebord for en Guldræv."),

        // Lights
        new("loftslampe", RoomSlot.Light, "Loftslampe", 50, 1, "En hyggelig lampe."),
        new("lyskaede", RoomSlot.Light, "Lyskæde", 90, 1, "Små lys over sengen."),
        new("stjernelampe", RoomSlot.Light, "Stjernelampe", 120, 5, "En lysende stjerne på væggen."),
        new("neon", RoomSlot.Light, "Neonskilt »1+1=2«", 220, 10, "Lysende regnestykke i neon."),
        new("lysekrone", RoomSlot.Light, "Lysekrone", 450, 60, "Til Mesterræve."),

        // Posters
        new("raeveplakat", RoomSlot.Poster, "Ræveplakat", 40, 1, "En ræv i skoven."),
        new("rumplakat", RoomSlot.Poster, "Rumplakat", 70, 1, "Planeter og en raket."),
        new("piplakat", RoomSlot.Poster, "π-plakat", 90, 5, "π = 3,14159 …"),
        new("tabelplakat", RoomSlot.Poster, "Gangetabel-plakat", 110, 5, "Så tabellerne altid er lige ved hånden."),

        // Shelves
        new("bogreol", RoomSlot.Shelf, "Bogreol", 60, 1, "En hylde med bøger."),
        new("legetoej", RoomSlot.Shelf, "Legetøjshylde", 100, 1, "Bamse, bil og bold."),
        new("pokaler", RoomSlot.Shelf, "Pokalhylde", 250, 30, "Pokaler til en Polarræv."),

        // Plants
        new("kaktus", RoomSlot.Plant, "Kaktus", 30, 1, "Kræver næsten ingen vand."),
        new("monstera", RoomSlot.Plant, "Monstera", 70, 1, "Store grønne blade."),
        new("solsikke", RoomSlot.Plant, "Solsikke", 90, 1, "Stor og gul."),
        new("bonsai", RoomSlot.Plant, "Bonsaitræ", 160, 15, "Et lille træ i en skål."),

        // Rugs
        new("rundt", RoomSlot.Rug, "Rundt tæppe", 40, 1, "Blødt og blåt."),
        new("stribettaeppe", RoomSlot.Rug, "Stribet tæppe", 60, 1, "Striber i varme farver."),
        new("raevetaeppe", RoomSlot.Rug, "Rævetæppe", 120, 5, "Formet som et rævehoved."),
        new("regnbuetaeppe", RoomSlot.Rug, "Regnbuetæppe", 180, 10, "En regnbue på gulvet."),
    ];

    public static RoomItem? Find(string? id) => Items.FirstOrDefault(i => i.Id == id);

    public static IEnumerable<RoomItem> InSlot(RoomSlot slot) => Items.Where(i => i.Slot == slot);

    /// <summary>The free starting item of a slot (null for optional slots, which start empty).</summary>
    public static RoomItem? Default(RoomSlot slot) => Items.FirstOrDefault(i => i.Slot == slot && i.IsFree);

    public static bool Owns(RoomItem item, IReadOnlyCollection<string> bought) => item.IsFree || bought.Contains(item.Id);

    public enum BuyCheck { Ok, AlreadyOwned, NeedsLevel, NotEnoughGold }

    public static BuyCheck CanBuy(RoomItem item, int gold, int level, IReadOnlyCollection<string> bought) =>
        Owns(item, bought) ? BuyCheck.AlreadyOwned
        : level < item.Level ? BuyCheck.NeedsLevel
        : gold < item.Price ? BuyCheck.NotEnoughGold
        : BuyCheck.Ok;

    // ------------------------------------------------------------ layout (JSON on the profile)

    /// <summary>What stands in each slot: {"Wall":"mint","Bed":"trae"}. Missing required slots use the free default.</summary>
    public static Dictionary<RoomSlot, string> ParseLayout(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            var layout = new Dictionary<RoomSlot, string>();
            foreach (var (k, v) in raw)
                if (Enum.TryParse<RoomSlot>(k, out var slot) && Find(v) is { } item && item.Slot == slot) layout[slot] = v;
            return layout;
        }
        catch (JsonException) { return []; }
    }

    public static string FormatLayout(Dictionary<RoomSlot, string> layout) =>
        JsonSerializer.Serialize(layout.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

    /// <summary>The item shown in a slot: the placed one if owned, else the default (or nothing for optional slots).</summary>
    public static RoomItem? Shown(RoomSlot slot, IReadOnlyDictionary<RoomSlot, string> layout, IReadOnlyCollection<string> bought) =>
        layout.TryGetValue(slot, out var id) && Find(id) is { } item && Owns(item, bought) ? item : Default(slot);
}
