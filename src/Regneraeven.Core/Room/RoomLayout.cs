using System.Text.Json;
using System.Text.Json.Serialization;

namespace Regneraeven.Core.Room;

/// <summary>
/// One thing standing in the room. (X, Y) is the item's bottom centre in room coordinates (800 × 500),
/// so things "stand" where you put them. Flip mirrors it left/right, Rotation tilts it (degrees), Z is the layer.
/// </summary>
public sealed record PlacedItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("f")] bool Flip = false,
    [property: JsonPropertyName("r")] double Rotation = 0,
    [property: JsonPropertyName("z")] int Z = 0);

/// <summary>
/// The whole room: wallpaper and bedding are styles for the whole room; everything else is placed freely.
/// The fox itself is a placed item with the id <see cref="Fox"/>.
/// </summary>
public sealed record RoomLayout(
    [property: JsonPropertyName("wall")] string Wall,
    [property: JsonPropertyName("bedding")] string Bedding,
    [property: JsonPropertyName("items")] IReadOnlyList<PlacedItem> Items)
{
    public const string Fox = "fox";
    public const int MaxItems = 60;

    [JsonPropertyName("v")] public int Version { get; init; } = 2;

    public bool Contains(string id) => Items.Any(i => i.Id == id);
}

/// <summary>Sizes and starting places. The room is 800 × 500; the floor starts at y = 370.</summary>
public static class RoomGeometry
{
    public const double Width = 800, Height = 500, FloorTop = 370;

    /// <summary>The drawing box of an item (local coordinates 0..W, 0..H).</summary>
    public static (double W, double H) Size(RoomSlot slot) => slot switch
    {
        RoomSlot.Poster => (130, 150),
        RoomSlot.Shelf => (210, 70),
        RoomSlot.Light => (240, 110),
        RoomSlot.Bed => (300, 190),
        RoomSlot.Desk => (260, 170),
        RoomSlot.Plant => (60, 140),
        RoomSlot.Rug => (320, 80),
        _ => (100, 100),
    };

    public static (double W, double H) FoxSize => (118, 118);

    /// <summary>Where a new thing is put (bottom centre).</summary>
    public static (double X, double Y) DefaultPosition(RoomSlot slot) => slot switch
    {
        RoomSlot.Poster => (145, 190),
        RoomSlot.Shelf => (145, 266),
        RoomSlot.Light => (640, 134),
        RoomSlot.Bed => (630, 440),
        RoomSlot.Desk => (170, 440),
        RoomSlot.Plant => (442, 440),
        RoomSlot.Rug => (390, 485),
        _ => (400, 440),
    };

    public static (double X, double Y) FoxDefault => (397, 436);

    /// <summary>Rugs and posters go behind furniture by default; lights and plants in front.</summary>
    public static int DefaultLayer(RoomSlot slot) => slot switch
    {
        RoomSlot.Rug => 0,
        RoomSlot.Poster => 1,
        RoomSlot.Shelf => 2,
        RoomSlot.Light => 3,
        RoomSlot.Desk => 4,
        RoomSlot.Bed => 5,
        RoomSlot.Plant => 7,
        _ => 6,
    };
}

public static class RoomLayouts
{
    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.Never };

    public static bool IsPlaceable(RoomItem item) => item.Slot is not (RoomSlot.Wall or RoomSlot.Bedding);

    /// <summary>A new room: white walls, a bed, a little desk and the fox.</summary>
    public static RoomLayout Starter() => new("hvid", "hvidt",
    [
        At("simpel", RoomSlot.Bed), At("lillebord", RoomSlot.Desk),
        new PlacedItem(RoomLayout.Fox, RoomGeometry.FoxDefault.X, RoomGeometry.FoxDefault.Y, Z: 8),
    ]);

    public static PlacedItem At(string id, RoomSlot slot, int? z = null)
    {
        var (x, y) = RoomGeometry.DefaultPosition(slot);
        return new PlacedItem(id, x, y, Z: z ?? RoomGeometry.DefaultLayer(slot));
    }

    /// <summary>Reads the stored room. Rooms saved before moving was possible ({"Wall":"mint","Bed":"trae"}) are converted.</summary>
    public static RoomLayout Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Starter();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("v", out _))
                return JsonSerializer.Deserialize<RoomLayout>(json, Json) ?? Starter();
        }
        catch (JsonException) { return Starter(); }

        // Old format: one item per slot.
        var old = RoomCatalog.ParseLayout(json);
        var items = new List<PlacedItem>();
        foreach (var (slot, _, _, _) in RoomCatalog.Slots)
        {
            if (slot is RoomSlot.Wall or RoomSlot.Bedding) continue;
            var id = old.TryGetValue(slot, out var chosen) ? chosen : RoomCatalog.Default(slot)?.Id;
            if (id is not null) items.Add(At(id, slot));
        }
        items.Add(new PlacedItem(RoomLayout.Fox, RoomGeometry.FoxDefault.X, RoomGeometry.FoxDefault.Y, Z: 8));
        return new RoomLayout(
            old.GetValueOrDefault(RoomSlot.Wall) ?? "hvid",
            old.GetValueOrDefault(RoomSlot.Bedding) ?? "hvidt",
            items);
    }

    public static string Format(RoomLayout layout) => JsonSerializer.Serialize(layout, Json);

    /// <summary>
    /// Cleans a layout from the browser: only things the child owns, each at most once, inside the room,
    /// sensible rotation, and the wall/bedding must be owned styles. Unknown things are dropped.
    /// </summary>
    public static RoomLayout Sanitize(RoomLayout? input, IReadOnlyCollection<string> bought, RoomLayout fallback)
    {
        if (input is null) return fallback;
        bool Owned(string id, RoomSlot slot) => RoomCatalog.Find(id) is { } it && it.Slot == slot && RoomCatalog.Owns(it, bought);

        var wall = input.Wall is { } w && Owned(w, RoomSlot.Wall) ? w : fallback.Wall;
        var bedding = input.Bedding is { } b && Owned(b, RoomSlot.Bedding) ? b : fallback.Bedding;
        var seen = new HashSet<string>();
        var items = new List<PlacedItem>();
        foreach (var p in input.Items ?? [])
        {
            if (p?.Id is null || items.Count >= RoomLayout.MaxItems || !seen.Add(p.Id)) continue;
            var isFox = p.Id == RoomLayout.Fox;
            if (!isFox && (RoomCatalog.Find(p.Id) is not { } item || !IsPlaceable(item) || !RoomCatalog.Owns(item, bought))) continue;
            items.Add(p with
            {
                X = Clamp(p.X, 0, RoomGeometry.Width),
                Y = Clamp(p.Y, 20, RoomGeometry.Height),
                Rotation = Math.Round(Normalize(p.Rotation), 1),
                Z = Math.Clamp(p.Z, -999, 999),
            });
        }
        return new RoomLayout(wall, bedding, items);
    }

    private static double Clamp(double v, double min, double max) => double.IsFinite(v) ? Math.Clamp(Math.Round(v, 1), min, max) : (min + max) / 2;

    private static double Normalize(double deg)
    {
        if (!double.IsFinite(deg)) return 0;
        deg %= 360;
        if (deg > 180) deg -= 360;
        if (deg < -180) deg += 360;
        return deg;
    }

    /// <summary>Puts an item in the room (at its usual place, in front), unless it is already there.</summary>
    public static RoomLayout Add(RoomLayout layout, RoomItem item)
    {
        if (!IsPlaceable(item)) return item.Slot == RoomSlot.Wall ? layout with { Wall = item.Id } : layout with { Bedding = item.Id };
        if (layout.Contains(item.Id)) return layout;
        var top = layout.Items.Count == 0 ? 0 : layout.Items.Max(i => i.Z);
        var z = item.Slot == RoomSlot.Rug ? Math.Min(0, layout.Items.Count == 0 ? 0 : layout.Items.Min(i => i.Z) - 1) : top + 1;
        return layout with { Items = [.. layout.Items, At(item.Id, item.Slot, z)] };
    }

    /// <summary>Takes an item out of the room – into the storage box (still owned). The fox stays.</summary>
    public static RoomLayout Store(RoomLayout layout, string id) =>
        id == RoomLayout.Fox ? layout : layout with { Items = layout.Items.Where(i => i.Id != id).ToList() };
}
