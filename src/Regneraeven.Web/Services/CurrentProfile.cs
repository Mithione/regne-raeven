using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Web.Data;

namespace Regneraeven.Web.Services;

/// <summary>Shown to the child instead of an error page.</summary>
public class UserFacingException(string message) : Exception(message);

/// <summary>
/// Which child is using the program right now (remembered in a cookie). On the website the
/// profiles also belong to the logged-in parent, and only their own children can be chosen.
/// </summary>
public class CurrentProfile(AppDb db, IHttpContextAccessor http, Regneraeven.Web.RegneraevenOptions options)
{
    public const string CookieName = "rr.profile";
    private Profile? _profile;
    private bool _loaded;

    /// <summary>The logged-in parent's id on the website; null in the Windows program.</summary>
    public string? OwnerId => options.ProfilesPerAccount
        ? http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "(ingen)"
        : null;

    /// <summary>The profiles this user may see: all of them on the desktop, the parent's own on the website.</summary>
    public IQueryable<Profile> Mine()
    {
        if (!options.ProfilesPerAccount) return db.Profiles;
        var owner = OwnerId;
        return db.Profiles.Where(p => p.OwnerId == owner);
    }

    public async Task<Profile?> GetAsync()
    {
        if (_loaded) return _profile;
        _loaded = true;
        var raw = http.HttpContext?.Request.Cookies[CookieName];
        if (int.TryParse(raw, out var id)) _profile = await Mine().FirstOrDefaultAsync(p => p.Id == id);
        return _profile;
    }

    public async Task<Profile> RequireAsync() => await GetAsync() ?? throw new UserFacingException("Vælg først, hvem der regner.");

    public void Select(int id)
    {
        http.HttpContext!.Response.Cookies.Append(CookieName, id.ToString(System.Globalization.CultureInfo.InvariantCulture), new CookieOptions
        {
            HttpOnly = true, SameSite = SameSiteMode.Strict, IsEssential = true, Expires = DateTimeOffset.UtcNow.AddYears(1),
        });
        _loaded = false;
    }

    public void Clear() => http.HttpContext!.Response.Cookies.Delete(CookieName);

    public static FoxTitle ShownTitle(Profile p)
    {
        var info = Leveling.FromXp(p.Xp);
        return p.ShownTitleLevel is { } l && l <= info.Level ? Leveling.TitleFor(l) : info.Title;
    }

    /// <summary>The streak counts if the daily tasks were finished today or yesterday – or the gap is covered by days off.</summary>
    public static int LiveStreak(Profile p, DateOnly today) => Streaks.Live(p.LastDailyDone, today, p.Streak, p.StreakFreezes);

    /// <summary>Streak items (frames, backgrounds, themes) the child has chosen at milestones.</summary>
    public static IReadOnlySet<string> OwnedStreakItems(Profile p) => Streaks.OwnedItems(p.StreakClaims);
}

/// <summary>Pages that work without a chosen child (the profile picker itself).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowWithoutProfileAttribute : Attribute;

/// <summary>Sends the user to the profile picker if no child is chosen, and makes the profile available to views.</summary>
public sealed class RequireProfileFilter(CurrentProfile current) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var profile = await current.GetAsync();
        context.HttpContext.Items["Profile"] = profile;
        var allowed = context.ActionDescriptor.EndpointMetadata.OfType<AllowWithoutProfileAttribute>().Any();
        if (profile is null && !allowed)
        {
            context.Result = new RedirectToActionResult("Index", "Profiles", null);
            return;
        }
        await next();
    }
}

/// <summary>Turns <see cref="UserFacingException"/> into a friendly message.</summary>
public sealed class UserFacingExceptionFilter(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory tempData) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not UserFacingException ex) return;
        var req = context.HttpContext.Request;
        var wantsJson = req.Headers.Accept.ToString().Contains("json", StringComparison.OrdinalIgnoreCase) || req.ContentType?.Contains("json") == true;
        if (wantsJson)
        {
            context.Result = new ObjectResult(new { error = ex.Message }) { StatusCode = 400 };
        }
        else
        {
            tempData.GetTempData(context.HttpContext)["Error"] = ex.Message;
            var back = req.Headers.Referer.ToString();
            context.Result = new RedirectResult(Uri.TryCreate(back, UriKind.Absolute, out var u) && u.Host == req.Host.Host ? u.PathAndQuery : "/");
        }
        context.ExceptionHandled = true;
    }
}
