using Microsoft.AspNetCore.Identity.UI.Services;

namespace NaPoso.Services
{
    public class ConsoleEmailSender : IEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> _logger;

        public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation("[ConsoleEmail] To: {Email}, Subject: {Subject}", email, subject);
            Console.WriteLine($"[ConsoleEmail] To: {email}, Subject: {subject}");
            return Task.CompletedTask;
        }
    }
}
