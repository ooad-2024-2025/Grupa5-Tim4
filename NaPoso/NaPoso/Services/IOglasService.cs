using NaPoso.Models;

namespace NaPoso.Services
{
    public interface IOglasService
    {
        Task<Oglas?> GetOglasByIdAsync(int id);
        Task<List<Oglas>> GetAllOglasAsync(int page = 1, int pageSize = 20);
        Task<List<Oglas>> GetOglasByKlijentIdAsync(string klijentId, int page = 1, int pageSize = 20);
        Task<List<Oglas>> GetOglasByAutorIdAsync(string autorId, int page = 1, int pageSize = 20);
        Task<Oglas> CreateOglasAsync(Oglas oglas, string autorId, string autorUloga);
        Task<Oglas?> UpdateOglasAsync(int id, Oglas input);
        Task<bool> DeleteOglasAsync(int id);
        Task<bool> OglasExistsAsync(int id);
        Task<List<VerifikovanView>> SearchOglasiAsync(string? search, string? lokacija, string? tipPosla, string? sort, int? minCijena, int? maxCijena, int page = 1, int pageSize = 20);
        Task<List<int>> GetPrijavljeniOglasiAsync(string korisnikId);
        Task<bool> ApplyToOglasAsync(int oglasId, string korisnikId);
        Task<bool> AcceptApplicationAsync(int prijavaId);
        Task<bool> RejectApplicationAsync(int prijavaId, string oglasOwnerId);
        Task<List<OglasKorisnik>> GetApplicantsForOglasAsync(int oglasId, string requestUserId);
        Task<List<OglasKorisnik>> GetRadnikPrijaveAsync(string radnikId);
    }
}
