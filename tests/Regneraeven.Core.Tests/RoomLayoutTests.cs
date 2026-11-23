using Regneraeven.Core.Room;

namespace Regneraeven.Core.Tests;

public class RoomLayoutTests
{
    [Fact]
    public void A_new_room_has_a_bed_a_desk_and_the_fox()
    {
        var room = RoomLayouts.Parse(null);
        Assert.Equal("hvid", room.Wall);
        Assert.Equal(["simpel", "lillebord", RoomLayout.Fox], room.Items.Select(i => i.Id));
    }

    [Fact]
    public void Old_rooms_are_converted_without_losing_anything()
    {
        var room = RoomLayouts.Parse("""{"Wall":"stjerner","Bed":"trae","Bedding":"raevetoej","Desk":"computer","Poster":"piplakat"}""");
        Assert.Equal("stjerner", room.Wall);
        Assert.Equal("raevetoej", room.Bedding);
        Assert.Contains(room.Items, i => i.Id == "trae");
        Assert.Contains(room.Items, i => i.Id == "computer");
        Assert.Contains(room.Items, i => i.Id == "piplakat");
        Assert.Contains(room.Items, i => i.Id == RoomLayout.Fox);
        Assert.DoesNotContain(room.Items, i => i.Id == "simpel");
    }

    [Fact]
    public void Round_trips_as_json()
    {
        var room = new RoomLayout("mint", "stribet", [new PlacedItem("trae", 600.5, 440, true, -15, 3), new PlacedItem(RoomLayout.Fox, 300, 430)]);
        var again = RoomLayouts.Parse(RoomLayouts.Format(room));
        Assert.Equal(room.Wall, again.Wall);
        Assert.Equal(room.Items, again.Items);
    }

    [Fact]
    public void Sanitize_keeps_only_owned_things_inside_the_room()
    {
        var bought = new[] { "trae", "mint" };
        var input = new RoomLayout("guldtapet", "regnbuetoej",
        [
            new PlacedItem("trae", 5000, -40, true, 725, 5000),       // owned – clamped
            new PlacedItem("trae", 100, 100),                          // duplicate – dropped
            new PlacedItem("raket", 300, 400),                         // not owned – dropped
            new PlacedItem("simpel", 200, 440),                        // free – kept
            new PlacedItem("mint", 200, 300),                          // a wall is not placeable – dropped
            new PlacedItem("findes-ikke", 1, 1),                       // unknown – dropped
            new PlacedItem(RoomLayout.Fox, double.NaN, 430),           // fox – kept, NaN fixed
        ]);
        var clean = RoomLayouts.Sanitize(input, bought, RoomLayouts.Starter());
        Assert.Equal("hvid", clean.Wall);     // guldtapet not owned → fallback
        Assert.Equal("hvidt", clean.Bedding);
        Assert.Equal(["trae", "simpel", RoomLayout.Fox], clean.Items.Select(i => i.Id));
        var bed = clean.Items[0];
        Assert.Equal((800d, 20d, 5d, 999), (bed.X, bed.Y, bed.Rotation, bed.Z));
        Assert.Equal(400, clean.Items[2].X);
    }

    [Fact]
    public void Add_and_store()
    {
        var room = RoomLayouts.Starter();
        var withPoster = RoomLayouts.Add(room, RoomCatalog.Find("piplakat")!);
        Assert.True(withPoster.Contains("piplakat"));
        Assert.Equal(withPoster.Items.Max(i => i.Z), withPoster.Items.Single(i => i.Id == "piplakat").Z);
        var withRug = RoomLayouts.Add(withPoster, RoomCatalog.Find("rundt")!);
        Assert.Equal(withRug.Items.Min(i => i.Z), withRug.Items.Single(i => i.Id == "rundt").Z); // rugs go to the back
        Assert.Equal("mint", RoomLayouts.Add(room, RoomCatalog.Find("mint")!).Wall);
        Assert.False(RoomLayouts.Store(withPoster, "piplakat").Contains("piplakat"));
        Assert.True(RoomLayouts.Store(room, RoomLayout.Fox).Contains(RoomLayout.Fox)); // the fox stays
        Assert.Same(withPoster, RoomLayouts.Add(withPoster, RoomCatalog.Find("piplakat")!)); // already there
    }
}
