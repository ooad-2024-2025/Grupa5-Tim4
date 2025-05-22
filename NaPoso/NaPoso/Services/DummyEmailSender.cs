using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace NaPoso.Services
{
    public class DummyEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Ne šalje email, samo logira u konzolu
            Console.WriteLine($"Email to: {email}, subject: {subject}");
            return Task.CompletedTask;
        }
    }
}