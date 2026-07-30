namespace NaPoso.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Pošalji email kroz konfigurisanog provajdera (Brevo ili console fallback).
        /// </summary>
        /// <param name="toEmail">Email adresa primaoca (obavezno)</param>
        /// <param name="toName">Ime i prezime / naziv primaoca (opciono, za prikaz u email klijentu)</param>
        /// <param name="subject">Naslov emaila (subject line)</param>
        /// <param name="htmlContent">HTML tijelo emaila</param>
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent);

        /// <summary>Backwards-compatible overload — primaoca bez imena.</summary>
        Task SendEmailAsync(string email, string subject, string message);

        Task SendDocumentApprovalEmail(string email, string userName);
        Task SendDocumentRejectionEmail(string email, string userName);
    }

    // Legacy interface — implementations are in BrevoEmailSender / ConsoleEmailSender / BrevoEmailService
}
