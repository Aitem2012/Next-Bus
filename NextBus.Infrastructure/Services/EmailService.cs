using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextBus.Application.Common.Interfaces;

namespace NextBus.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Support both Email: and ZeptoMail: configurations
            var host = _configuration["Email:Host"] ?? _configuration["ZeptoMail:SmtpHost"];
            var portString = _configuration["Email:Port"] ?? _configuration["ZeptoMail:SmtpPort"];
            var username = _configuration["Email:Username"] ?? _configuration["ZeptoMail:SmtpUser"];
            var password = _configuration["Email:Password"] ?? _configuration["ZeptoMail:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? _configuration["ZeptoMail:FromAddress"] ?? "hello@classicalaitem.com";
            var fromName = _configuration["Email:FromName"] ?? _configuration["ZeptoMail:FromName"] ?? "NextBus Transit";

            // If SMTP is not fully configured, log email to console/logger for local/test simulation
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                _logger.LogInformation(
                    "[EMAIL SIMULATION] To: {ToEmail} | Subject: {Subject}\nBody:\n{Body}",
                    toEmail, subject, body);
                return;
            }

            try
            {
                var port = int.TryParse(portString, out var p) ? p : 587;
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                // In development / test environments without outbound SMTP access, log and do not break caller
                _logger.LogWarning("Continuing flow despite SMTP send failure: {Message}", ex.Message);
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string userName, string otpCode, string purpose)
        {
            var subject = $"NextBus - {otpCode} is your verification code for {purpose}";
            var year = DateTime.UtcNow.Year;
            var safeUserName = string.IsNullOrWhiteSpace(userName) || userName.Contains("@") ? "Valued Member" : userName;

            var body = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{subject}</title>
  <!--[if mso]>
  <noscript>
    <xml>
      <o:OfficeDocumentSettings>
        <o:PixelsPerInch>96</o:PixelsPerInch>
      </o:OfficeDocumentSettings>
    </xml>
  </noscript>
  <![endif]-->
  <style>
    body {{
      margin: 0;
      padding: 0;
      background-color: #F6F1E7;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
      color: #1D1811;
      -webkit-font-smoothing: antialiased;
    }}
    table {{ border-collapse: separate; }}
    .email-wrapper {{ width: 100%; background-color: #F6F1E7; padding: 40px 15px; }}
    .email-container {{
      max-width: 560px;
      margin: 0 auto;
      background-color: #ffffff;
      border-radius: 16px;
      overflow: hidden;
      border: 1px solid rgba(169, 124, 63, 0.25);
      box-shadow: 0 10px 30px rgba(27, 42, 74, 0.08);
    }}
    .email-header {{
      background: linear-gradient(135deg, #1B2A4A 0%, #101B33 100%);
      padding: 36px 30px 30px 30px;
      text-align: center;
      border-bottom: 3px solid #A97C3F;
    }}
    .brand-title {{
      color: #ffffff;
      font-family: Georgia, 'Times New Roman', serif;
      font-size: 28px;
      font-weight: 700;
      letter-spacing: -0.5px;
      margin: 0 0 4px 0;
    }}
    .brand-subtitle {{
      color: #A97C3F;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 3px;
      margin: 0;
    }}
    .email-body {{
      padding: 36px 36px 28px 36px;
    }}
    .greeting {{
      font-size: 18px;
      font-weight: 700;
      color: #1B2A4A;
      margin: 0 0 14px 0;
    }}
    .intro-text {{
      font-size: 15px;
      line-height: 1.6;
      color: #4A4A4A;
      margin: 0 0 24px 0;
    }}
    .purpose-badge {{
      display: inline-block;
      background-color: rgba(169, 124, 63, 0.12);
      color: #8C642E;
      font-size: 12px;
      font-weight: 700;
      padding: 6px 14px;
      border-radius: 20px;
      border: 1px solid rgba(169, 124, 63, 0.3);
      margin-bottom: 20px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }}
    .otp-card {{
      background: linear-gradient(180deg, #FDFBF8 0%, #F6F1E7 100%);
      border: 2px dashed #A97C3F;
      border-radius: 12px;
      padding: 24px 20px;
      text-align: center;
      margin: 20px 0 24px 0;
    }}
    .otp-code {{
      font-family: 'Courier New', Courier, monospace;
      font-size: 38px;
      font-weight: 800;
      letter-spacing: 10px;
      color: #1B2A4A;
      margin: 0;
      line-height: 1.2;
    }}
    .otp-caption {{
      font-size: 12px;
      color: #7A7265;
      margin-top: 10px;
      font-weight: 500;
    }}
    .expiry-box {{
      background-color: #FFF9ED;
      border-left: 4px solid #A97C3F;
      padding: 12px 16px;
      border-radius: 4px;
      margin: 20px 0;
      font-size: 13px;
      color: #6C5528;
      line-height: 1.5;
    }}
    .security-notice {{
      font-size: 13px;
      color: #7A7A7A;
      line-height: 1.5;
      padding-top: 16px;
      border-top: 1px solid #ECE7DE;
      margin-top: 24px;
    }}
    .email-footer {{
      background-color: #F8F5EE;
      padding: 24px 30px;
      text-align: center;
      font-size: 12px;
      color: #8C8476;
      border-top: 1px solid #EFEAE0;
    }}
    .footer-links {{
      margin-bottom: 12px;
    }}
    .footer-links a {{
      color: #1B2A4A;
      text-decoration: none;
      margin: 0 8px;
      font-weight: 600;
    }}
    .footer-copy {{
      margin: 0;
      font-size: 11px;
      color: #A0988A;
      line-height: 1.5;
    }}
  </style>
</head>
<body>
  <div class=""email-wrapper"">
    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
      <tr>
        <td align=""center"">
          <div class=""email-container"">
            <!-- Header -->
            <div class=""email-header"">
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                <tr>
                  <td align=""center"">
                    <div style=""display: inline-block; width: 44px; height: 44px; background-color: #A97C3F; border-radius: 10px; line-height: 44px; text-align: center; color: #1B2A4A; font-weight: bold; font-size: 20px; margin-bottom: 10px;"">
                      NB
                    </div>
                    <h1 class=""brand-title"">NextBus</h1>
                    <p class=""brand-subtitle"">Transit & Ride-Sharing Platform</p>
                  </td>
                </tr>
              </table>
            </div>

            <!-- Body -->
            <div class=""email-body"">
              <div class=""purpose-badge"">{purpose}</div>
              <h2 class=""greeting"">Hello {safeUserName},</h2>
              <p class=""intro-text"">
                Please use the single-use verification code below to authorize your <strong>{purpose}</strong> request on NextBus.
              </p>

              <!-- OTP Code Display Card -->
              <div class=""otp-card"">
                <div class=""otp-code"">{otpCode}</div>
                <div class=""otp-caption"">One-Time Verification Code</div>
              </div>

              <!-- Expiry Alert -->
              <div class=""expiry-box"">
                <strong>Important:</strong> This verification code expires in <strong>15 minutes</strong> and can only be used once.
              </div>

              <div class=""security-notice"">
                <strong>Security Reminder:</strong> Never share this code with anyone. NextBus staff and drivers will never ask for your verification code. If you did not initiate this action, please disregard this email or secure your account.
              </div>
            </div>

            <!-- Footer -->
            <div class=""email-footer"">
              <div class=""footer-links"">
                <a href=""https://dev-nextbus.u-check.ng"">NextBus Portal</a> &bull;
                <a href=""https://dev-nextbus.u-check.ng/search"">Find Rides</a> &bull;
                <a href=""mailto:support@classicalaitem.com"">Support</a>
              </div>
              <p class=""footer-copy"">
                &copy; {year} NextBus Transit Platform by ClassicalAitem. All rights reserved.<br>
                Automated security notification sent to {toEmail}.
              </p>
            </div>
          </div>
        </td>
      </tr>
    </table>
  </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
