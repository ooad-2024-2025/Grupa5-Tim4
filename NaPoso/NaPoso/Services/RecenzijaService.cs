using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Services;

public class RecenzijaService : IRecenzijaService
{
    private readonly ApplicationDbContext _context;

    public RecenzijaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Recenzija?> GetByIdAsync(int id)
    {
        return await _context.Recenzija.FindAsync(id);
    }

    public async Task<List<Recenzija>> GetAllAsync()
    {
        return await _context.Recenzija.ToListAsync();
    }

    public async Task<List<Recenzija>> GetByRadnikIdAsync(string radnikId)
    {
        return await _context.Recenzija
            .Where(r => r.RadnikId == radnikId)
            .ToListAsync();
    }

    public async Task<List<RecenzijaViewModel>> GetByRadnikIdWithEmailAsync(string radnikId)
    {
        return await _context.Recenzija
            .Where(r => r.RadnikId == radnikId)
            .Join(_context.Users,
                r => r.KlijentId,
                k => k.Id,
                (r, k) => new RecenzijaViewModel
                {
                    Id = r.Id,
                    KlijentEmail = k.Email ?? "Nepoznat",
                    Ocjena = r.Ocjena,
                    Sadrzaj = r.Sadrzaj
                })
            .ToListAsync();
    }

    public async Task<Recenzija> CreateAsync(Recenzija recenzija)
    {
        _context.Add(recenzija);
        await _context.SaveChangesAsync();
        return recenzija;
    }

    public async Task<Recenzija?> UpdateAsync(int id, Recenzija input)
    {
        var recenzija = await _context.Recenzija.FindAsync(id);
        if (recenzija == null) return null;

        recenzija.Ocjena = input.Ocjena;
        recenzija.Sadrzaj = input.Sadrzaj;
        recenzija.RadnikId = input.RadnikId;
        recenzija.KlijentId = input.KlijentId;

        await _context.SaveChangesAsync();
        return recenzija;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var recenzija = await _context.Recenzija.FindAsync(id);
        if (recenzija == null) return false;

        _context.Recenzija.Remove(recenzija);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Recenzija.AnyAsync(e => e.Id == id);
    }

    public Task<bool> ValidatePaymentSessionAsync(int? oglasId, string? radnikId, string? paymentVerified)
    {
        bool valid = oglasId.HasValue &&
                     !string.IsNullOrEmpty(radnikId) &&
                     !string.IsNullOrEmpty(paymentVerified);
        return Task.FromResult(valid);
    }
}
