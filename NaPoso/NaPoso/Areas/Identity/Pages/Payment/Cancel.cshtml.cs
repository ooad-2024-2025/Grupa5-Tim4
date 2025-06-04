using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NaPoso.Data;

namespace NaPoso.Areas.Identity.Pages.Payment
{
    public class CancelModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CancelModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int? OglasId { get; set; }

        public void OnGet()
        {
            if (TempData["PrihvatiOglasId"] != null)
            {
                OglasId = (int)TempData["PrihvatiOglasId"];

                TempData.Remove("PrihvatiOglasId");
                TempData.Remove("PrihvatiRadnikId");
                TempData.Remove("PrihvatiPrijavaId");
            }
        }
    }
}