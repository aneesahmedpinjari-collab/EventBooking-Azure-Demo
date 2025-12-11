using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EventBookingSecure.Models;
using EventBookingSecure.ViewModels;
using EventBookingSecure.Services;
using System.ComponentModel.DataAnnotations;

namespace EventBookingSecure.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUrlValidator _urlValidator;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUrlValidator urlValidator)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _urlValidator = urlValidator;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign default "User" role
                    await _userManager.AddToRoleAsync(user, "User");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? Url.Action("Index", "Home")!
            };
            return View(model);
        }

        // VULNERABLE VERSION - Open Redirect Attack
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // VULNERABILITY: No validation of returnUrl
                    // An attacker could use: /Account/Login?returnUrl=https://evil.com
                    if (!string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl); // DANGEROUS!
                    }
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View("Login", model);
        }

        // SECURE VERSION - Protected against Open Redirect
//         [HttpPost]
//         [AllowAnonymous]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> Login(LoginViewModel model)
//         {
//             if (ModelState.IsValid)
//             {
//                 var result = await _signInManager.PasswordSignInAsync(
//                     model.Email,
//                     model.Password,
//                     model.RememberMe,
//                     lockoutOnFailure: false);

//                 if (result.Succeeded)
//                 {
//                     // SECURE: Validate returnUrl before redirecting
// #pragma warning disable CS8604 // Possible null reference argument.
//                     var safeUrl = _urlValidator.GetSafeReturnUrl(model.ReturnUrl, HttpContext);
// #pragma warning restore CS8604 // Possible null reference argument.
//                     return Redirect(safeUrl);
//                 }

//                 if (result.IsLockedOut)
//                 {
//                     return View("Lockout");
//                 }

//                 ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//             }

//             return View(model);
//         }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Google External Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in with external login provider
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);

            if (result.Succeeded)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                var safeUrl = _urlValidator.GetSafeReturnUrl(returnUrl, HttpContext);
#pragma warning restore CS8604 // Possible null reference argument.
                return Redirect(safeUrl);
            }

            // User doesn't exist, create new account
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Provider"] = info.LoginProvider;

            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("Error");
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        var safeUrl = _urlValidator.GetSafeReturnUrl(returnUrl, HttpContext);
                        return Redirect(safeUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }
    }

    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;
    }
}
