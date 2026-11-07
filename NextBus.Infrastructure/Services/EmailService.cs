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

        public async Task SendBookingAlertEmailAsync(
            string toEmail,
            string driverName,
            string passengerName,
            string passengerPhone,
            string origin,
            string destination,
            string pickup,
            string dropoff,
            int seats,
            decimal fare,
            DateTime departureTime)
        {
            var subject = $"NextBus - New Booking Request: {passengerName} reserved {seats} seat(s) ({origin} → {destination})";
            var year = DateTime.UtcNow.Year;
            var safeDriverName = string.IsNullOrWhiteSpace(driverName) || driverName.Contains("@") ? "Driver" : driverName;
            var formattedTime = departureTime.ToString("dddd, MMMM d, yyyy @ h:mm tt UTC");

            var body = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{subject}</title>
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
      max-width: 580px;
      margin: 0 auto;
      background-color: #ffffff;
      border-radius: 16px;
      overflow: hidden;
      border: 1px solid rgba(169, 124, 63, 0.25);
      box-shadow: 0 10px 30px rgba(27, 42, 74, 0.08);
    }}
    .email-header {{
      background: linear-gradient(135deg, #1B2A4A 0%, #101B33 100%);
      padding: 32px 30px 26px 30px;
      text-align: center;
      border-bottom: 3px solid #A97C3F;
    }}
    .brand-title {{
      color: #ffffff;
      font-family: Georgia, 'Times New Roman', serif;
      font-size: 26px;
      font-weight: 700;
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
      padding: 32px 36px;
    }}
    .greeting {{
      font-size: 18px;
      font-weight: 700;
      color: #1B2A4A;
      margin: 0 0 12px 0;
    }}
    .intro-text {{
      font-size: 14px;
      line-height: 1.6;
      color: #4A4A4A;
      margin: 0 0 20px 0;
    }}
    .booking-card {{
      background-color: #FDFBF8;
      border: 1px solid rgba(169, 124, 63, 0.3);
      border-radius: 12px;
      padding: 20px;
      margin: 20px 0;
    }}
    .route-header {{
      font-size: 18px;
      font-weight: 800;
      color: #1B2A4A;
      margin-bottom: 14px;
      font-family: Georgia, serif;
    }}
    .detail-row {{
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
      border-bottom: 1px dashed #ECE7DE;
      font-size: 13px;
    }}
    .detail-label {{ color: #7A7265; font-weight: 600; }}
    .detail-val {{ color: #1B2A4A; font-weight: 700; text-align: right; }}
    .fare-highlight {{
      color: #A97C3F;
      font-size: 16px;
      font-weight: 800;
    }}
    .cta-btn {{
      display: inline-block;
      background: linear-gradient(135deg, #1B2A4A 0%, #223762 100%);
      color: #ffffff !important;
      text-decoration: none;
      font-size: 14px;
      font-weight: 700;
      padding: 14px 28px;
      border-radius: 10px;
      margin-top: 22px;
      text-align: center;
      border: 1px solid #A97C3F;
    }}
    .email-footer {{
      background-color: #FAF7F2;
      padding: 24px 30px;
      text-align: center;
      border-top: 1px solid #ECE7DE;
    }}
    .footer-copy {{
      font-size: 11px;
      color: #8C857B;
      line-height: 1.6;
      margin: 0;
    }}
  </style>
</head>
<body>
  <div class=""email-wrapper"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
      <tr>
        <td align=""center"">
          <div class=""email-container"">
            <div class=""email-header"">
              <h1 class=""brand-title"">NextBus</h1>
              <p class=""brand-subtitle"">Rides & Transit Network</p>
            </div>
            <div class=""email-body"">
              <h2 class=""greeting"">Hello {safeDriverName},</h2>
              <p class=""intro-text"">
                Great news! You have a new passenger booking request for your offered ride. The fare has been pre-authorized and held in secure escrow.
              </p>

              <div class=""booking-card"">
                <div class=""route-header"">{origin} &rarr; {destination}</div>
                <div class=""detail-row"">
                  <span class=""detail-label"">Passenger</span>
                  <span class=""detail-val"">{passengerName} ({passengerPhone})</span>
                </div>
                <div class=""detail-row"">
                  <span class=""detail-label"">Pickup Location</span>
                  <span class=""detail-val"">{pickup}</span>
                </div>
                <div class=""detail-row"">
                  <span class=""detail-label"">Dropoff Location</span>
                  <span class=""detail-val"">{dropoff}</span>
                </div>
                <div class=""detail-row"">
                  <span class=""detail-label"">Seats Booked</span>
                  <span class=""detail-val"">{seats} seat(s)</span>
                </div>
                <div class=""detail-row"">
                  <span class=""detail-label"">Escrow Fare Amount</span>
                  <span class=""detail-val fare-highlight"">&#8358;{fare:N2}</span>
                </div>
                <div class=""detail-row"" style=""border-bottom: none;"">
                  <span class=""detail-label"">Departure Time</span>
                  <span class=""detail-val"">{formattedTime}</span>
                </div>
              </div>

              <div style=""text-align: center;"">
                <a href=""https://dev-nextbus.u-check.ng/my-rides"" class=""cta-btn"">
                  Review &amp; Accept Booking in My Rides &rarr;
                </a>
              </div>
            </div>
            <div class=""email-footer"">
              <p class=""footer-copy"">
                &copy; {year} NextBus Transit Platform by ClassicalAitem. All rights reserved.<br>
                Notification sent to {toEmail}.
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
