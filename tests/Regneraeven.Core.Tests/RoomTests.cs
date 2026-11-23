using Regneraeven.Core.Room;

namespace Regneraeven.Core.Tests;

public class RoomTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(50, 13)]
    [InlineData(100, 25)]
    public void Gold_is_a_quarter_of_xp_rounded_up(int xp, int gold) => Assert.Equal(gold, RoomCatalog.GoldFor(xp));

    [Fact]
    public void Catalog_is_consistent()
    {
        var items = RoomCatalog.Items;
        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
        Assert.All(items, i => Assert.True(i.Id.All(c => c is >= 'a' and <= 'z'), i.Id));
        foreach (var (slot, _, _, optional) in RoomCatalog.Slots)
        {
            var inSlot = RoomCatalog.InSlot(slot).ToList();
            Assert.InRange(inSlot.Count, 3, 8);
            // Required slots have exactly one free starting item; optional ones start empty.
            Assert.Equal(optional ? 0 : 1, inSlot.Count(i => i.IsFree));
            Assert.All(inSlot.Where(i => !i.IsFree), i => Assert.InRange(i.Price, 20, 1000));
        }
        Assert.Contains(items, i => i.Id == "guldbord" && i.Level == 50);
    }

    [Fact]
    public void Buying_checks_owned_level_and_gold()
    {
        var bought = new List<string>();
        var mint = RoomCatalog.Find("mint")!;
        Assert.Equal(RoomCatalog.BuyCheck.NotEnoughGold, RoomCatalog.CanBuy(mint, 39, 1, bought));
        Assert.Equal(RoomCatalog.BuyCheck.Ok, RoomCatalog.CanBuy(mint, 40, 1, bought));
        bought.Add("mint");
        Assert.Equal(RoomCatalog.BuyCheck.AlreadyOwned, RoomCatalog.CanBuy(mint, 999, 1, bought));
        Assert.Equal(RoomCatalog.BuyCheck.NeedsLevel, RoomCatalog.CanBuy(RoomCatalog.Find("guldbord")!, 9999, 49, bought));
        Assert.Equal(RoomCatalog.BuyCheck.AlreadyOwned, RoomCatalog.CanBuy(RoomCatalog.Find("hvid")!, 0, 1, bought)); // free = owned
    }

    [Fact]
    public void Layout_only_shows_owned_items_in_their_own_slot()
    {
        var layout = RoomCatalog.ParseLayout("""{"Wall":"stjerner","Bed":"mint","Plant":"kaktus","Nope":"x"}""");
        Assert.Equal(2, layout.Count); // "mint" is not a bed, "Nope" is not a slot
        Assert.Equal("hvid", RoomCatalog.Shown(RoomSlot.Wall, layout, []).Id);          // not bought → default
        Assert.Equal("stjerner", RoomCatalog.Shown(RoomSlot.Wall, layout, ["stjerner"])!.Id);
        Assert.Null(RoomCatalog.Shown(RoomSlot.Poster, layout, []));                    // optional + empty
        Assert.Equal("simpel", RoomCatalog.Shown(RoomSlot.Bed, layout, [])!.Id);
        Assert.Empty(RoomCatalog.ParseLayout("not json"));
        var again = RoomCatalog.ParseLayout(RoomCatalog.FormatLayout(layout));
        Assert.Equal(layout, again);
    }
}
