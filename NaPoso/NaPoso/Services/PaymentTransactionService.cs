using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Services
{
    public class PaymentTransactionService
    {
        private readonly ApplicationDbContext _context;

        public PaymentTransactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentTransaction?> GetByStripePaymentIntentIdAsync(string paymentIntentId)
        {
            return await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
        }

        public async Task<List<PaymentTransaction>> GetByUserIdAsync(string userId)
        {
            return await _context.PaymentTransactions
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetByOglasIdAsync(int oglasId)
        {
            return await _context.PaymentTransactions
                .Where(p => p.OglasId == oglasId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsPaidAsync(string userId, int oglasId)
        {
            return await _context.PaymentTransactions
                .AnyAsync(p => p.UserId == userId && p.OglasId == oglasId && p.Status == PaymentStatus.Paid);
        }
    }
}
