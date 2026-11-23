using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Regneraeven.Web.Services;

namespace Regneraeven.Server.Auth;

public sealed class LoginForm
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class RegisterForm
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? InviteCode { get; set; }
}

/// <summary>Parent accounts: log in, create an account, log out.</summary>
[Route("konto"), AllowAnonymous, AllowWithoutProfile]
public class AccountController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, IConfiguration config, CurrentProfile current) : Controller
{
    private string? InviteCode => config["Registration:InviteCode"] is { Length: > 0 } c ? c : null;

    [HttpGet("log-ind")]
    public IActionResult Login(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost("log-ind")]
    public async Task<IActionResult> Login(LoginForm form)
    {
        var result = await signIn.PasswordSignInAsync(form.Email?.Trim() ?? "", form.Password ?? "", isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/");
        ViewData["Error"] = result.IsLockedOut ? "For mange forkerte forsøg. Prøv igen om lidt." : "Forkert e-mail eller adgangskode.";
        form.Password = null;
        return View(form);
    }

    [HttpGet("opret")]
    public IActionResult Register() => View(new RegisterForm());

    [HttpPost("opret")]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        if (InviteCode is { } code && !string.Equals(form.InviteCode?.Trim(), code, StringComparison.OrdinalIgnoreCase))
        {
            ViewData["Error"] = "Invitationskoden er forkert.";
            return View(form);
        }
        var email = form.Email?.Trim() ?? "";
        var user = new IdentityUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, form.Password ?? "");
        if (!result.Succeeded)
        {
            ViewData["Error"] = string.Join(" ", result.Errors.Select(Danish));
            return View(form);
        }
        await signIn.SignInAsync(user, isPersistent: true);
        return Redirect("/profiler");
    }

    [HttpPost("log-ud"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        current.Clear();
        return Redirect("/konto/log-ind");
    }

    private static string Danish(IdentityError e) => e.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Der findes allerede en konto med den e-mail.",
        "InvalidEmail" or "InvalidUserName" => "Skriv en gyldig e-mailadresse.",
        "PasswordTooShort" => "Adgangskoden skal være mindst 8 tegn.",
        "PasswordRequiresDigit" => "Adgangskoden skal indeholde mindst ét tal.",
        "PasswordRequiresLower" => "Adgangskoden skal indeholde mindst ét lille bogstav.",
        _ => e.Description,
    };
}
