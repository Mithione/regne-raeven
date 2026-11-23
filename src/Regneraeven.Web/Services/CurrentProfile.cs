using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Regneraeven.Core.Progress;
using Regneraeven.Web.Data;

namespace Regneraeven.Web.Services;

/// <summary>Shown to the child instead of an error page.</summary>
public class UserFacingException(string message) : Exception(message);

/// <summary>
/// Which child is using the program right now. On the desktop it is remembered in a cookie;
/// a future website would tie profiles to a logged-in parent instead.
/// </summary>
public class CurrentProfile(AppDb db, IHttpContextAccessor http)
{
    public const string CookieName = "rr.profile";
    private Profile? _profile;
    private bool _loaded;

    public async Task<Profile?> GetAsync()
    {
        if (_loaded) return _profile;
        _loaded = true;
        var raw = http.HttpContext?.Request.Cookies[CookieName];
        if (int.TryParse(raw, out var id)) _profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == id);
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

    /// <summary>The streak only counts if the daily tasks were finished today or yesterday.</summary>
    public static int LiveStreak(Profile p, DateOnly today) =>
        p.LastDailyDone is { } d && d >= today.AddDays(-1) ? p.Streak : 0;
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
