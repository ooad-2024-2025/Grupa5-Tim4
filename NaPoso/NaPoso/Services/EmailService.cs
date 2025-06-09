using System.Net;
using System.Net.Mail;

namespace NaPoso.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendDocumentApprovalEmail(string email, string userName);
        Task SendDocumentRejectionEmail(string email, string userName);
    }

    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _password;

        public EmailService(string smtpHost, int smtpPort, string senderEmail, string password)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _senderEmail = senderEmail;
            _password = password;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient(_smtpHost)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_senderEmail, _password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_senderEmail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }

        public async Task SendDocumentApprovalEmail(string email, string userName)
        {
            string subject = "Verifikacija dokumenta prihvaćena 🎉🎉🎉";
            string message = $@"
                <html>
                <body>
                    <h2>Poštovani {userName},</h2>
                    <p>Vaš dokument je uspješno verifikovan! Više niste neverifikovani Stabber sada ste Approved ✅ stabber.</p>
                    <p>Srdačan pozdrav,<br/> Vam želi ekipa iz <i>NaPos'o 💼</i></p>
                </body>
                </html>";

            await SendEmailAsync(email, subject, message);
        }

        public async Task SendDocumentRejectionEmail(string email, string userName)
        {
            string subject = "Verifikacija dokumenta odbijena 🥺";
            string message = $@"
                <html>
                <body>
                    <h2>Poštovani {userName},</h2>
                    <p>Nažalost, vaš dokument nije prihvaćen za verifikaciju.</p>
                    <p>Molimo vas da provjerite da vaši dokumenti ispunjavaju sve naše zahtjeve i pokušate ponovo. Ujedno trebate znati kako ne odobravamo osobe sa kriminalnim dosijeom. <i>𐐘💥╾━╤デ╦︻ඞා</i>. Uisitnu nam je žap što niste verifikovani Stabber.</p>
                    <p>Srdačan pozdrav,<br/>Vam želi ekipa iz <i>NaPos'o 💼<i/></p>
                </body>
                </html>";

            await SendEmailAsync(email, subject, message);
        }
    }
}