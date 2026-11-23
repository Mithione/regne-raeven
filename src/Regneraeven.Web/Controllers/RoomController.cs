using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Core.Room;
using Regneraeven.Web.Data;
using Regneraeven.Web.Models;
using Regneraeven.Web.Services;

namespace Regneraeven.Web.Controllers;

/// <summary>"Mit værelse": the fox room and the shop. Paid with gold from tasks – never real money.</summary>
[Route("vaerelse")]
public class RoomController(AppDb db, CurrentProfile current) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var p = await current.RequireAsync();
        return View(new RoomModel(p, Leveling.FromXp(p.Xp).Level, await BoughtAsync(p), RoomCatalog.ParseLayout(p.Room)));
    }

    [HttpPost("koeb")]
    public async Task<IActionResult> Buy(string? id)
    {
        var p = await current.RequireAsync();
        var item = RoomCatalog.Find(id) ?? throw new UserFacingException("Den ting findes ikke i butikken.");
        var bought = await BoughtAsync(p);
        switch (RoomCatalog.CanBuy(item, p.Gold, Leveling.FromXp(p.Xp).Level, bought))
        {
            case RoomCatalog.BuyCheck.AlreadyOwned: throw new UserFacingException("Den har du allerede.");
            case RoomCatalog.BuyCheck.NeedsLevel: throw new UserFacingException($"{item.Name} kan først købes på level {item.Level}.");
            case RoomCatalog.BuyCheck.NotEnoughGold: throw new UserFacingException($"Du mangler {item.Price - p.Gold} guld. Løs flere opgaver! 🦊");
        }
        p.Gold -= item.Price;
        db.RoomPurchases.Add(new RoomPurchase { ProfileId = p.Id, ItemId = item.Id, Price = item.Price });
        Place(p, item);
        await db.SaveChangesAsync();
        TempData["Info"] = $"Du har købt {item.Name} for {item.Price} guld – den står nu i dit værelse! 🎉";
        return Redirect($"/vaerelse#{item.Slot}");
    }

    /// <summary>Put something you own in its place.</summary>
    [HttpPost("placer")]
    public async Task<IActionResult> Use(string? id)
    {
        var p = await current.RequireAsync();
        var item = RoomCatalog.Find(id) ?? throw new UserFacingException("Den ting findes ikke.");
        if (!RoomCatalog.Owns(item, await BoughtAsync(p))) throw new UserFacingException("Den skal du købe først.");
        Place(p, item);
        await db.SaveChangesAsync();
        return Redirect($"/vaerelse#{item.Slot}");
    }

    /// <summary>Take a lamp, poster, shelf, plant or rug out of the room (you still own it).</summary>
    [HttpPost("fjern")]
    public async Task<IActionResult> Remove(RoomSlot slot)
    {
        var p = await current.RequireAsync();
        if (!RoomCatalog.Slots.Any(s => s.Slot == slot && s.Optional)) throw new UserFacingException("Den plads skal have noget.");
        var layout = RoomCatalog.ParseLayout(p.Room);
        layout.Remove(slot);
        p.Room = RoomCatalog.FormatLayout(layout);
        await db.SaveChangesAsync();
        return Redirect($"/vaerelse#{slot}");
    }

    private static void Place(Profile p, RoomItem item)
    {
        var layout = RoomCatalog.ParseLayout(p.Room);
        layout[item.Slot] = item.Id;
        p.Room = RoomCatalog.FormatLayout(layout);
    }

    private async Task<HashSet<string>> BoughtAsync(Profile p) =>
        (await db.RoomPurchases.Where(r => r.ProfileId == p.Id).Select(r => r.ItemId).ToListAsync()).ToHashSet();
}
