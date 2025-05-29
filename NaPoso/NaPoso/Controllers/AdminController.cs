using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Primi statistiku iz baze
            var totalUsers = _context.Users.Count();
            var totalJobs = _context.Oglas.Count(); // ako imaš entitet Posao
            var totalClients = _context.Users.Count(static u => u.UserRoles.Any(r => r.RoleId == "Klijent")); // prilagodi ako koristiš IdentityUserRole

            var model = new Statistika
            {
                UkupnoKorisnika = totalUsers,
                UkupnoPoslova = totalJobs,
                UkupnoKlijenata = totalClients
            };

            return View(model);
        }
    }
}
