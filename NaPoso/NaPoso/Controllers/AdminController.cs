using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager; // Added UserManager for user role management  

        public AdminController(ApplicationDbContext context, UserManager<Korisnik> userManager)
        {
            _context = context;
            _userManager = userManager; // Initialize UserManager  
        }
        public IActionResult Documents()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot" ,"documents");
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var files = Directory.GetFiles(path).Select(Path.GetFileName).ToList();

            return View(files);
        }
        public IActionResult Index()
        {
            // Fetch statistics from the database  
            var totalUsers = _context.Users.Count();
            var totalJobs = _context.Oglas.Count(); // Assuming you have an entity for jobs  

            // Fix for the error: Use UserManager to check roles  
            var users = _context.Users.ToList();
            var totalClients = users.Count(u => _userManager.GetRolesAsync((Korisnik)u).Result.Contains("Klijent"));

            var model = new Statistika
            {
                UkupnoKorisnika = totalUsers,
                UkupnoPoslova = totalJobs,
                UkupnoKlijenata = totalClients
            };

            return View("~/Views/Admin/Index.cshtml", model);
        }
    }
}
