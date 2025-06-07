using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NaPoso.Models;

namespace NaPoso.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<Korisnik> _userManager;
        private readonly SignInManager<Korisnik> _signInManager;

        public IndexModel(UserManager<Korisnik> userManager, SignInManager<Korisnik> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [Display(Name = "Email")]
        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        
        public class InputModel
        {
            public IFormFile Dokument { get; set; }
            public string Ime { get; set; }
            public string Prezime { get; set; }
            [Phone]
            [Display(Name = "Broj telefona")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(Korisnik user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            System.Diagnostics.Debug.WriteLine($"Ime: {user.Ime}");
            System.Diagnostics.Debug.WriteLine($"Prezime: {user.Prezime}");
            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Ime = user.Ime,
                Prezime = user.Prezime
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
            if (Input.Dokument != null)
            {
                var ext = Path.GetExtension(Input.Dokument.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg")
                {
                    ModelState.AddModelError("Input.Dokument", "Dozvoljeni su samo JPG fajlovi.");
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

                System.Diagnostics.Debug.WriteLine($"Fajl spremljen na: {filePath}");
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
            StatusMessage = "Vaš profil je ažuriran.";
            return RedirectToPage();
        }
    }
}