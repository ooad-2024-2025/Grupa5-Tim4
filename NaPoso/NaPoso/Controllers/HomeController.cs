using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<Korisnik> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(UserManager<Korisnik> userManager, ILogger<HomeController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }
            public async Task<IActionResult> Index()
            {
                /*if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Contains("Radnik"))
                        return RedirectToAction("Radnik");
                    else if (roles.Contains("Klijent"))
                        return RedirectToAction("Klijent");
            }
            */

                return View(); // ako nije logovan, pokaži home stranicu
            }
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }
        [Authorize(Roles = "Radnik")]
        public IActionResult Radnik()
        {
            return View();
        }

        [Authorize(Roles = "Klijent")]
        public IActionResult Klijent()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
