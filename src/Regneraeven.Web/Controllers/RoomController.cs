using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Core.Room;
using Regneraeven.Web.Data;
using Regneraeven.Web.Models;
using Regneraeven.Web.Services;

namespace Regneraeven.Web.Controllers;

/// <summary>
/// "Mit værelse": the fox room and the shop. Things can be moved, flipped, tilted and layered in the browser;
/// the layout is saved here. Paid with gold from tasks – never real money.
/// </summary>
[Route("vaerelse")]
public class RoomController(AppDb db, CurrentProfile current) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var p = await current.RequireAsync();
        var bought = await BoughtAsync(p);
        return View(new RoomModel(p, Leveling.FromXp(p.Xp).Level, bought, LayoutOf(p, bought)));
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
        bought.Add(item.Id);
        p.Room = RoomLayouts.Format(RoomLayouts.Add(LayoutOf(p, bought), item));
        await db.SaveChangesAsync();
        TempData["Info"] = RoomLayouts.IsPlaceable(item)
            ? $"Du har købt {item.Name} for {item.Price} guld! Træk den hen, hvor den skal stå. 🎉"
            : $"Du har købt {item.Name} for {item.Price} guld – den er taget i brug! 🎉";
        return Redirect($"/vaerelse#{item.Slot}");
    }

    /// <summary>Use a wallpaper/bedding you own, or put a thing from the storage box into the room.</summary>
    [HttpPost("placer")]
    public async Task<IActionResult> Use(string? id)
    {
        var p = await current.RequireAsync();
        var item = RoomCatalog.Find(id) ?? throw new UserFacingException("Den ting findes ikke.");
        var bought = await BoughtAsync(p);
        if (!RoomCatalog.Owns(item, bought)) throw new UserFacingException("Den skal du købe først.");
        p.Room = RoomLayouts.Format(RoomLayouts.Add(LayoutOf(p, bought), item));
        await db.SaveChangesAsync();
        return Redirect($"/vaerelse#{item.Slot}");
    }

    /// <summary>Put a thing in the storage box (you still own it).</summary>
    [HttpPost("kasse")]
    public async Task<IActionResult> Store(string? id)
    {
        var p = await current.RequireAsync();
        var bought = await BoughtAsync(p);
        var item = RoomCatalog.Find(id) ?? throw new UserFacingException("Den ting findes ikke.");
        p.Room = RoomLayouts.Format(RoomLayouts.Store(LayoutOf(p, bought), item.Id));
        await db.SaveChangesAsync();
        return Redirect($"/vaerelse#{item.Slot}");
    }

    /// <summary>Saves the room after moving, flipping, tilting or layering things (called by the page while you play).</summary>
    [HttpPost("indret")]
    public async Task<IActionResult> Save([FromBody] RoomLayout? layout)
    {
        var p = await current.RequireAsync();
        var bought = await BoughtAsync(p);
        var clean = RoomLayouts.Sanitize(layout, bought, LayoutOf(p, bought));
        // The page can only rearrange – wallpaper and bedding are changed through the shop.
        var saved = LayoutOf(p, bought);
        p.Room = RoomLayouts.Format(clean with { Wall = saved.Wall, Bedding = saved.Bedding });
        await db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    /// <summary>The room as stored, cleaned (e.g. things that are no longer owned are left out).</summary>
    private static RoomLayout LayoutOf(Profile p, IReadOnlyCollection<string> bought)
    {
        var parsed = RoomLayouts.Parse(p.Room);
        return RoomLayouts.Sanitize(parsed, bought, RoomLayouts.Starter());
    }

    private async Task<HashSet<string>> BoughtAsync(Profile p) =>
        (await db.RoomPurchases.Where(r => r.ProfileId == p.Id).Select(r => r.ItemId).ToListAsync()).ToHashSet();
}
