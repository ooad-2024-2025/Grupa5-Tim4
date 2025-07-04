﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NaPoso.Models;

namespace NaPoso.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<Korisnik> _signInManager;
        private readonly UserManager<Korisnik> _userManager;
        private readonly IUserStore<Korisnik> _userStore;
        private readonly IUserEmailStore<Korisnik> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<Korisnik> userManager,
            IUserStore<Korisnik> userStore,
            SignInManager<Korisnik> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Ovo polje je obavezno.")]
            [StringLength(50, ErrorMessage = "Ime mora sadržavati maksimalno {1} znakova i minimalno {2} znakova.", MinimumLength = 2)]
            [RegularExpression(@"^[A-Za-z]*$", ErrorMessage="Dozvoljena su samo slova.")]
            public string Ime { get; set; }
            [Required(ErrorMessage = "Ovo polje je obavezno.")]
            [StringLength(50, ErrorMessage = "Prezime mora sadržavati maksimalno {1} znakova i minimalno {2} znakova.", MinimumLength = 2)]
            [RegularExpression(@"^[A-Za-z]*$", ErrorMessage = "Dozvoljena su samo slova.")]
            public string Prezime { get; set; }
            [Display(Name = "Broj telefona")]
            [Required(ErrorMessage = "Broj telefona je obavezan.")]
            [RegularExpression(@"^\+387\d{6,15}$", ErrorMessage = "Broj telefona mora počinjati sa +387 i sadržavati samo brojeve.")]
            public string BrojTelefona { get; set; }

            [Required(ErrorMessage ="Ovo polje je obavezno.")]
            [RegularExpression(@"^[^@\s]+@[^@\s]+\.[a-zA-Z]+$", ErrorMessage = "Email mora biti u ispravnom formatu (npr. korisnik@example.com).")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Ovo polje je obavezno.")]
            [StringLength(100, ErrorMessage = "Lozinka mora biti barem {2} znakova i maksimalno {1} znakova duga.", MinimumLength = 6)]
            [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora sadržavati barem jedno veliko slovo i jedan broj.")]
            [DataType(DataType.Password)]
            [Display(Name = "Lozinka")]
            public string Password { get; set; }


            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage ="Ovo polje je obavezno.")]
            [DataType(DataType.Password)]
            [Display(Name = "Potvrdi lozinku")]
            [Compare("Password", ErrorMessage = "Lozinke se ne poklapaju.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Uloge")]
            [Required(ErrorMessage = "Ovo polje je obavezno.")]
            public string Uloga { get; set; } 
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var existingUserWithPhone = await _userManager.Users
    .FirstOrDefaultAsync(u => u.PhoneNumber == Input.BrojTelefona);

                if (existingUserWithPhone != null)
                {
                    ModelState.AddModelError("Input.BrojTelefona", "Broj telefona je već u upotrebi.");
                    return Page();
                }
                var user = CreateUser();

                // Postavi email i broj telefona
                user.PhoneNumber = Input.BrojTelefona;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);
                user.Ime = Input.Ime;
                user.Prezime = Input.Prezime;
                user.PhoneNumber = Input.BrojTelefona;
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);
                    await _userManager.AddToRoleAsync(user, Input.Uloga ?? "Klijent");
                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                    {
                        ModelState.AddModelError("Input.Email", "Email adresa je već u upotrebi.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description); // originalni tekst
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private Korisnik CreateUser()


        {
            try
            {
                return Activator.CreateInstance<Korisnik>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(Korisnik)}'. " +
                    $"Ensure that '{nameof(Korisnik)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<Korisnik> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<Korisnik>)_userStore;
        }
    }
}
