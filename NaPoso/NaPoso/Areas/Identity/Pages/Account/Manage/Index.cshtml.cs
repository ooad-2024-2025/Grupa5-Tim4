using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NaPoso.Constants;
using NaPoso.Helpers;
using NaPoso.Models;

namespace NaPoso.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<Korisnik> _userManager;
        private readonly SignInManager<Korisnik> _signInManager;
        private readonly IEmailSender _emailSender;

        public IndexModel(UserManager<Korisnik> userManager, SignInManager<Korisnik> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [Display(Name = "Email")]
        public string Username { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        
        public class InputModel
        {
            public IFormFile? Dokument { get; set; }
            [Display(Name = "Ime")]
            public string Ime { get; set; }
            [Display(Name = "Prezime")]
            public string Prezime { get; set; }
            [Phone]
            [Display(Name = "Broj telefona")]
            public string PhoneNumber { get; set; }
            public bool Verified { get; set; } = false;
        }

        private async Task LoadAsync(Korisnik user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            System.Diagnostics.Debug.WriteLine($"Ime: {user.Ime}");
            System.Diagnostics.Debug.WriteLine($"Prezime: {user.Prezime}");
            Username = userName;
            IsEmailConfirmed = isEmailConfirmed;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Ime = user.Ime,
                Prezime = user.Prezime,
                Verified = user.Verified
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound($"Nije moguće učitati korisnika s ID-jem '{_userManager.GetUserId(User)}'.");
            }
           
            
            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nije moguće učitati korisnika s ID-jem '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Update Ime/Prezime
            bool nameChanged = false;
            if (Input.Ime != user.Ime)
            {
                user.Ime = Input.Ime;
                nameChanged = true;
            }
            if (Input.Prezime != user.Prezime)
            {
                user.Prezime = Input.Prezime;
                nameChanged = true;
            }
            if (nameChanged)
            {
                await _userManager.UpdateAsync(user);
            }

            if (Input.Dokument != null)
            {
                var ext = Path.GetExtension(Input.Dokument.FileName).ToLowerInvariant();
                if (!FileValidationHelper.IsAllowedExtension(ext))
                {
                    ModelState.AddModelError("Input.Dokument", "Dozvoljeni su samo JPG, PNG i PDF fajlovi.");
                    await LoadAsync(user);
                    return Page();
                }

                // Verify file signature (magic numbers) to prevent spoofed extensions
                using var signatureStream = Input.Dokument.OpenReadStream();
                if (!await FileValidationHelper.IsValidFileSignatureAsync(signatureStream, ext))
                {
                    ModelState.AddModelError("Input.Dokument", "Sadržaj fajla ne odgovara deklariranoj ekstenziji.");
                    await LoadAsync(user);
                    return Page();
                }

                var documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
                if (!Directory.Exists(documentsPath))
                {
                    Directory.CreateDirectory(documentsPath);
                }

                var filePath = Path.Combine(documentsPath, $"{user.Id}_document{ext}");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.Dokument.CopyToAsync(stream);
                }

                // Notify all admins about the verification request
                var admins = await _userManager.GetUsersInRoleAsync(RoleConstants.Admin);
                foreach (var admin in admins)
                {
                    var context = Request.HttpContext.RequestServices.GetRequiredService<NaPoso.Data.ApplicationDbContext>();
                    context.Obavijest.Add(new NaPoso.Models.Obavijest
                    {
                        KorisnikId = admin.Id,
                        Sadrzaj = $"Korisnik {user.Ime} {user.Prezime} ({user.Email}) je poslao zahtjev za verifikaciju.",
                        VrijemeSlanja = DateTime.UtcNow,
                        Tip = NaPoso.Enums.Enums.Obavjestenje.Email
                    });
                    await context.SaveChangesAsync();
                }
            }
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Došlo je do greške prilikom ažuriranja broja telefona.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            if (Input.Dokument != null)
            {
                StatusMessage = "Zahtjev za verifikaciju je poslan. Administrator će pregledati vaš dokument.";
            }
            else
            {
                StatusMessage = "Vaš profil je ažuriran.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nije moguće učitati korisnika s ID-jem '{_userManager.GetUserId(User)}'.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                email,
                "Potvrda email adrese — NaPos'o",
                $@"<p style=""font-family: Arial, Helvetica, sans-serif; color: #333333; font-size: 16px; line-height: 1.7;"">
                        Zaprimili smo Vaš zahtjev za potvrdu email adrese na platformi <strong>NaPos'o</strong>.
                   </p>
                   <p style=""font-family: Arial, Helvetica, sans-serif; color: #333333; font-size: 16px; line-height: 1.7;"">
                        Molimo Vas da kliknete na dugme ispod kako biste potvrdili svoju email adresu. Link je validan narednih <strong>5 minuta</strong>.
                   </p>
                   <div style=""text-align: center; margin: 30px 0;"">
                       <a href='{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callbackUrl)}' style=""display: inline-block; padding: 14px 32px; background-color: #e63950; color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; font-family: Arial, Helvetica, sans-serif;"">
                           Potvrdi email adresu
                       </a>
                   </div>
                   <p style=""font-family: Arial, Helvetica, sans-serif; color: #888888; font-size: 13px;"">
                        Ukoliko niste Vi zatražili ovu potvrdu, slobodno zanemarite ovu poruku.
                   </p>");

            StatusMessage = "Verifikacioni email je poslan. Provjerite svoj inbox.";
            return RedirectToPage();
        }
    }
}