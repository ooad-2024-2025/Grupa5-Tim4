using System.Net.Http.Json;
using System.Text.Json;

namespace NaPoso.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BrevoEmailService> _logger;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public BrevoEmailService(HttpClient httpClient, IConfiguration configuration, ILogger<BrevoEmailService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

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

        /// <inheritdoc />
        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Primaoc emaila je obavezan.", nameof(toEmail));
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Naslov emaila je obavezan.", nameof(subject));

            if (string.IsNullOrWhiteSpace(_senderEmail) ||
                _senderEmail.Equals("noreply@naposo.example.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "BrevoEmailSenderEmail nije konfigurisan! Koristi se fallback 'noreply@naposo.example.com' koji će vjerovatno odbaciti Brevo. " +
                    "Postavi BREVO_SENDER_EMAIL u .env ili appsettings.Email.SenderEmail.");
            }

            var apiKeyHeader = _httpClient.DefaultRequestHeaders.TryGetValues("api-key", out var apiKeys)
                ? apiKeys.FirstOrDefault()
                : null;
            if (string.IsNullOrWhiteSpace(apiKeyHeader))
            {
                throw new InvalidOperationException(
                    "BREVO_API_KEY nije konfigurisan. Postavi BREVO_API_KEY u .env ili appsettings.Email.Brevo.ApiKey. " +
                    "Brevo API zahtijeva 'api-key' request header za autentifikaciju.");
            }

            try
            {
                var sender = new { name = _senderName, email = _senderEmail };
                var to = new[]
                {
                    new
                    {
                        email = toEmail,
                        name = string.IsNullOrWhiteSpace(toName) ? toEmail : toName
                    }
                };

                var payload = new
                {
                    sender,
                    to,
                    subject,
                    htmlContent = htmlContent ?? string.Empty
                };

                _logger.LogInformation("Šaljem email Brevo-om → {ToEmail} (sender={SenderName} <{SenderEmail}>, subject={Subject})",
                    toEmail, _senderName, _senderEmail, subject);

                var response = await _httpClient.PostAsJsonAsync("smtp/email", payload)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email uspješno poslan na {ToEmail}. Brevo status={Status}", toEmail, (int)response.StatusCode);
                    return;
                }

                // === BREVO API ERROR HANDLING ===
                // Brevo vraća koristan JSON oblika:
                //   { "code": "invalid_parameter", "message": "...", "timestamp": "..." }
                // ili:
                //   { "message": "Key not found", "code": "unauthorized" }
                string rawBody = string.Empty;
                try
                {
                    rawBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception readEx)
                {
                    rawBody = $"<nije moguće pročitati response body: {readEx.Message}>";
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
                catch
                {
                    // Ako nije JSON, ostavljamo raw body kao poruku
                }

                var statusInt = (int)response.StatusCode;
                var exMessage =
                    $"Brevo API nije vratio 2xx prilikom slanja emaila na '{toEmail}'. " +
                    $"HTTP Status={(int)response.StatusCode} ({response.StatusCode}), " +
                    $"BrevoCode={brevoCode}, BrevoMessage={brevoMessage}, " +
                    $"Sender={_senderEmail}, Subject={subject}, RawBody={rawBody}";

                _logger.LogError("Brevo error: {Msg}", exMessage);
                throw new HttpRequestException(exMessage, null, response.StatusCode);
            }
            catch (HttpRequestException)
            {
                // Propagirajmo detaljne greške iznad koje imaju sve informacije
                throw;
            }
            catch (Exception ex)
            {
                var msg = $"Neuspješno slanje emaila na {toEmail} putem Brevo API-ja: {ex.Message}";
                _logger.LogError(ex, "{Msg}", msg);
                throw new InvalidOperationException(msg, ex);
            }
        }

        /// <inheritdoc />
        public Task SendEmailAsync(string email, string subject, string message)
            => SendEmailAsync(toEmail: email, toName: string.Empty, subject: subject, htmlContent: message);

        public async Task SendDocumentApprovalEmail(string email, string userName)
        {
            string logoImgTag = "<img src=\"https://naposo.me/images/naposo_logo.png\" alt=\"NaPos'o Logo\" style=\"width: 60px; height: auto; margin-bottom: 12px;\" /><br/>";

            string subject = "Verifikacija dokumenta prihvaćena";
            string message = $@"
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
                                            <p style=""color: #333333;"">Vaš dokument je uspješno verifikovan! Sada imate puni pristup svim funkcionalnostima platforme.</p>
                                            <p style=""margin: 20px 0 0 0; color: #333333; font-size: 15px;"">S poštovanjem,<br/>Ekipa <strong>NaPos'o</strong></p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 20px 30px; text-align: center; font-size: 11px; color: #999999; border-top: 1px solid #eee; background-color: #fafafa; border-radius: 0 0 12px 12px; font-family: Arial, Helvetica, sans-serif;"">
                                            <p style=""margin: 0 0 8px 0; color: #b0b0b0; font-size: 11px;""><strong>Napomena:</strong> Ovaj sistem je razvijen isključivo u edukativne svrhe, u sklopu studentskog projekta. Plaćanja se obrađuju u test okruženju — ne vrši se nikakva stvarna finansijska transakcija niti se tereti bilo koja kartica.</p>
                                            <p style=""margin: 0; color: #b0b0b0;"">&copy; {DateTime.Now.Year} NaPos'o Platforma. Sva prava zadržana.</p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await SendEmailAsync(toEmail: email, toName: userName, subject: subject, htmlContent: message);
        }

        public async Task SendDocumentRejectionEmail(string email, string userName)
        {
            string logoImgTag = "<img src=\"https://naposo.me/images/naposo_logo.png\" alt=\"NaPos'o Logo\" style=\"width: 60px; height: auto; margin-bottom: 12px;\" /><br/>";

            string subject = "Verifikacija dokumenta odbijena";
            string message = $@"
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
                                            <p style=""color: #333333;"">Nažalost, vaš dokument nije prihvaćen za verifikaciju.</p>
                                            <p style=""color: #333333;"">Molimo Vas da provjerite da Vaši dokumenti ispunjavaju sve naše zahtjeve i pokušate ponovo.</p>
                                            <p style=""margin: 20px 0 0 0; color: #333333; font-size: 15px;"">S poštovanjem,<br/>Ekipa <strong>NaPos'o</strong></p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 20px 30px; text-align: center; font-size: 11px; color: #999999; border-top: 1px solid #eee; background-color: #fafafa; border-radius: 0 0 12px 12px; font-family: Arial, Helvetica, sans-serif;"">
                                            <p style=""margin: 0 0 8px 0; color: #b0b0b0; font-size: 11px;""><strong>Napomena:</strong> Ovaj sistem je razvijen isključivo u edukativne svrhe, u sklopu studentskog projekta. Plaćanja se obrađuju u test okruženju — ne vrši se nikakva stvarna finansijska transakcija niti se tereti bilo koja kartica.</p>
                                            <p style=""margin: 0; color: #b0b0b0;"">&copy; {DateTime.Now.Year} NaPos'o Platforma. Sva prava zadržana.</p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            await SendEmailAsync(toEmail: email, toName: userName, subject: subject, htmlContent: message);
        }
    }
}
