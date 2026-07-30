using NaPoso.Models;

namespace NaPoso.Services;

public interface IRecenzijaService
{
    Task<Recenzija?> GetByIdAsync(int id);
    Task<List<Recenzija>> GetAllAsync();
    Task<List<Recenzija>> GetByRadnikIdAsync(string radnikId);
    Task<List<RecenzijaViewModel>> GetByRadnikIdWithEmailAsync(string radnikId);
    Task<Recenzija> CreateAsync(Recenzija recenzija);
    Task<Recenzija?> UpdateAsync(int id, Recenzija input);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> ValidatePaymentSessionAsync(int? oglasId, string? radnikId, string? paymentVerified);
}
