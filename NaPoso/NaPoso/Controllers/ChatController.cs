using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
namespace NaPoso.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<Korisnik> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var chats = await _context.Chat
                .Include(c => c.Korisnik1)
                .Include(c => c.Korisnik2)
                .Include(c => c.Oglas)
                .Where(c => c.Korisnik1Id == userId || c.Korisnik2Id == userId)
                .OrderByDescending(c => c.Poruke.Any() ? c.Poruke.Max(p => p.PoslanoAt) : c.CreatedAt)
                .ToListAsync();

            return View(chats);
        }

        public async Task<IActionResult> StartChat(int oglasId, string korisnik2Id)
        {
            var korisnik1Id = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(korisnik1Id))
            {
                return Unauthorized("Korisnik nije prijavljen.");
            }

            if (korisnik1Id == korisnik2Id)
            {
                return BadRequest("Ne možeš razgovarati sam sa sobom.");
            }

            var chat = await _context.Chat
                .FirstOrDefaultAsync(c =>
                    (c.OglasId == oglasId) &&
                    ((c.Korisnik1Id == korisnik1Id && c.Korisnik2Id == korisnik2Id) ||
                     (c.Korisnik1Id == korisnik2Id && c.Korisnik2Id == korisnik1Id)));

            if (chat == null)
            {
                chat = new Chat
                {
                    Korisnik1Id = korisnik1Id,
                    Korisnik2Id = korisnik2Id,
                    OglasId = oglasId,
                    CreatedAt = DateTime.Now
                };
                _context.Chat.Add(chat);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Poruke", new { id = chat.Id });
        }

        public async Task<IActionResult> Poruke(int id)
        {
            var userId = _userManager.GetUserId(User);

            var chat = await _context.Chat
                .Include(c => c.Poruke.OrderBy(p => p.PoslanoAt))
                .ThenInclude(p => p.Posiljaoc)
                .Include(c => c.Korisnik1)
                .Include(c => c.Korisnik2)
                .Include(c => c.Oglas)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chat == null)
                return NotFound();

            if (chat.Korisnik1Id != userId && chat.Korisnik2Id != userId)
                return Forbid();

            return View(chat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PosaljiPoruku(int chatId, string tekst)
        {
            var userId = _userManager.GetUserId(User);

            var chat = await _context.Chat.FindAsync(chatId);
            if (chat == null || (chat.Korisnik1Id != userId && chat.Korisnik2Id != userId))
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(tekst))
            {
                TempData["ErrorMessage"] = "Poruka ne može biti prazna.";
                return RedirectToAction("Poruke", new { id = chatId });
            }

            var poruka = new Poruka
            {
                ChatId = chatId,
                PosiljaocId = userId,
                Tekst = tekst.Trim(),
                PoslanoAt = DateTime.Now
            };

            _context.Poruka.Add(poruka);
            await _context.SaveChangesAsync();

            return RedirectToAction("Poruke", new { id = chatId });
        }
    }
}