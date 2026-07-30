using Microsoft.AspNetCore.Identity.UI.Services;

namespace NaPoso.Services
{
    // Kept for backward compatibility — delegates to ConsoleEmailSender
    public class DummyEmailSender : ConsoleEmailSender
    {
        public DummyEmailSender(ILogger<ConsoleEmailSender> logger) : base(logger) { }
    }
}
