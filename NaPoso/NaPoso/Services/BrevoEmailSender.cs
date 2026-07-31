using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace NaPoso.Services
{
    public class BrevoEmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BrevoEmailSender> _logger;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly IServiceProvider _serviceProvider;

        public BrevoEmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<BrevoEmailSender> logger, IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serviceProvider = serviceProvider;

            var envBrevoSender = Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL");
            var envEmailFrom = Environment.GetEnvironmentVariable("EMAIL_FROM");
            var confSender = configuration["Email:SenderEmail"];
            var confFrom = configuration["Email:From"];
            
            _senderEmail =
                !string.IsNullOrWhiteSpace(envBrevoSender) ? envBrevoSender :
                !string.IsNullOrWhiteSpace(envEmailFrom) ? envEmailFrom :
                !string.IsNullOrWhiteSpace(confSender) ? confSender :
                !string.IsNullOrWhiteSpace(confFrom) ? confFrom :
                "noreply@naposo.example.com";

            var envBrevoName = Environment.GetEnvironmentVariable("BREVO_SENDER_NAME");
            var confName = configuration["Email:SenderName"];
            var confBrevoName = configuration["Email:Brevo:SenderName"];

            _senderName =
                !string.IsNullOrWhiteSpace(envBrevoName) ? envBrevoName :
                !string.IsNullOrWhiteSpace(confName) ? confName :
                !string.IsNullOrWhiteSpace(confBrevoName) ? confBrevoName :
                "NaPos'o Platforma";
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Primaoc emaila je obavezan.", nameof(email));
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Naslov emaila je obavezan.", nameof(subject));

            var apiKeyHeader = _httpClient.DefaultRequestHeaders.TryGetValues("api-key", out var apiKeys)
                ? apiKeys.FirstOrDefault()
                : null;
            if (string.IsNullOrWhiteSpace(apiKeyHeader))
            {
                throw new InvalidOperationException(
                    "BREVO_API_KEY nije konfigurisan u BrevoEmailSender (IEmailSender za Identity). " +
                    "Postavi BREVO_API_KEY u .env ili appsettings.Email.Brevo.ApiKey.");
            }

            try
            {
                string userName = "Korisniče";

                using (var scope = _serviceProvider.CreateScope())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<NaPoso.Models.Korisnik>>();
                    var user = await userManager.FindByEmailAsync(email);
                    if (user != null && !string.IsNullOrEmpty(user.Ime))
                    {
                        userName = user.Ime;
                    }
                }
                // TODO: Nakon Render deploymenta, promijeniti na wwwroot/images/naposo_logo.png servirano direktno iz .NET aplikacije, ukloniti zavisnost od GitHub Pages
                string logoImgTag = "<img src=\"https://idzafic1.github.io/images/naposo_logo.png\" alt=\"NaPos'o Logo\" style=\"width: 60px; height: auto; margin-bottom: 12px;\" /><br/>";

                string styledMessage = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                </head>
                <body style=""margin: 0; padding: 20px; font-family: Arial, Helvetica, sans-serif; background-color: #f4f5f7; color: #333333;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #f4f5f7;"">
                        <tr>
                            <td align=""center"">
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 600px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,0.08);"">
                                    <tr>
                                        <td style=""padding: 36px 30px 20px 30px; text-align: center; border-bottom: 3px solid #e63950;"">
                                            {logoImgTag}
                                            <h1 style=""color: #e63950; margin: 0; font-size: 28px; font-family: Arial, Helvetica, sans-serif;"">NaPos'o</h1>
                                            <p style=""margin: 8px 0 0 0; font-size: 13px; color: #999999;"">Platforma za povezivanje klijenata i radnika</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 28px 30px 20px 30px; font-size: 16px; line-height: 1.7; color: #333333; font-family: Arial, Helvetica, sans-serif;"">
                                            <p style=""margin: 0 0 16px 0; color: #333333;"">Poštovani/a {userName},</p>
                                            {htmlMessage}
                                            <p style=""margin: 20px 0 0 0; color: #333333; font-size: 15px;"">S poštovanjem,<br/>Ekipa <strong>NaPos'o</strong></p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 20px 30px; text-align: center; font-size: 11px; color: #999999; border-top: 1px solid #eee; background-color: #fafafa; border-radius: 0 0 12px 12px; font-family: Arial, Helvetica, sans-serif;"">
                                            <p style=""margin: 0 0 8px 0; color: #b0b0b0; font-size: 11px;"">
                                                <strong>Napomena:</strong> Ovaj sistem je razvijen isključivo u edukativne svrhe, u sklopu studentskog projekta. Plaćanja se obrađuju u test okruženju — ne vrši se nikakva stvarna finansijska transakcija niti se tereti bilo koja kartica.
                                            </p>
                                            <p style=""margin: 0; color: #b0b0b0;"">&copy; {DateTime.Now.Year} NaPos'o Platforma. Sva prava zadržana.</p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

                var payload = new
                {
                    sender = new { name = _senderName, email = _senderEmail },
                    to = new[] { new { email, name = userName } },
                    subject = subject,
                    htmlContent = styledMessage
                };

                _logger.LogInformation("IEmailSender (BrevoEmailSender): Šaljem email → {ToEmail} (subject={Subject})", email, subject);

                var response = await _httpClient.PostAsJsonAsync("smtp/email", payload)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("IEmailSender: Email poslan na {Email}. Status={Status}", email, (int)response.StatusCode);
                    return;
                }

                // === BREVO API ERROR HANDLING ===
                string rawBody = string.Empty;
                try
                {
                    rawBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception readEx)
                {
                    rawBody = $"<nije moguce procitati response: {readEx.Message}>";
                }

                string brevoCode = "n/a";
                string brevoMessage = rawBody;
                try
                {
                    using var doc = JsonDocument.Parse(rawBody);
                    if (doc.RootElement.TryGetProperty("code", out var codeProp))
                        brevoCode = codeProp.GetString() ?? brevoCode;
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        brevoMessage = msgProp.GetString() ?? brevoMessage;
                }
                catch { /* raw body ostaje */ }

                var exMsg =
                    $"IEmailSender.Brevo error: HTTP {(int)response.StatusCode} ({response.StatusCode}), " +
                    $"BrevoCode={brevoCode}, BrevoMessage={brevoMessage}, To={email}, Subject={subject}, Sender={_senderEmail}";
                _logger.LogError("{Msg}. Raw={Raw}", exMsg, rawBody);
                throw new HttpRequestException(exMsg, null, response.StatusCode);
            }
            catch (HttpRequestException) { throw; }
            catch (Exception ex)
            {
                var msg = $"IEmailSender: Neuspješno slanje emaila na {email}: {ex.Message}";
                _logger.LogError(ex, "{Msg}", msg);
                throw new InvalidOperationException(msg, ex);
            }
        }
    }
}
