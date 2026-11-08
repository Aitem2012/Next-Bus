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
            var host = _configuration["Email:Host"] ?? _configuration["ZeptoMail:SmtpHost"];
            var portString = _configuration["Email:Port"] ?? _configuration["ZeptoMail:SmtpPort"];
            var username = _configuration["Email:Username"] ?? _configuration["ZeptoMail:SmtpUser"];
            var password = _configuration["Email:Password"] ?? _configuration["ZeptoMail:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? _configuration["ZeptoMail:FromAddress"] ?? "hello@classicalaitem.com";
            var fromName = _configuration["Email:FromName"] ?? _configuration["ZeptoMail:FromName"] ?? "NextBus Transit";

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
                _logger.LogWarning("Continuing flow despite SMTP send failure: {Message}", ex.Message);
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string userName, string otpCode, string purpose)
        {
            var subject = $"NextBus - {otpCode} is your verification code for {purpose}";
            var year = DateTime.UtcNow.Year;
            var safeUserName = string.IsNullOrWhiteSpace(userName) || userName.Contains("@") ? "Valued Member" : userName;

            var body = $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; padding: 0; background-color: #F6F1E7;"">
<head>
  <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{subject}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #F6F1E7; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #F6F1E7; padding: 40px 15px;"">
    <tr>
      <td align=""center"">
        <!-- Main Container Card -->
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 16px; border: 1px solid #dcd3c5; overflow: hidden; box-shadow: 0 10px 30px rgba(27, 42, 74, 0.08);"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color: #1B2A4A; padding: 36px 30px 30px 30px; border-bottom: 3px solid #A97C3F;"">
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td align=""center"">
                    <div style=""display: inline-block; width: 44px; height: 44px; background-color: #A97C3F; border-radius: 10px; line-height: 44px; text-align: center; color: #1B2A4A; font-weight: bold; font-size: 20px; margin-bottom: 12px;"">
                      NB
                    </div>
                    <h1 style=""color: #ffffff; font-family: Georgia, 'Times New Roman', serif; font-size: 28px; font-weight: 700; letter-spacing: -0.5px; margin: 0 0 4px 0;"">NextBus</h1>
                    <p style=""color: #A97C3F; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 3px; margin: 0;"">Transit &amp; Ride-Sharing Platform</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding: 36px 36px 30px 36px; background-color: #ffffff;"">
              <!-- Purpose Pill -->
              <div style=""display: inline-block; background-color: #F5EFE6; color: #8C642E; font-size: 11px; font-weight: 700; padding: 6px 14px; border-radius: 20px; border: 1px solid #D9C3A5; margin-bottom: 22px; text-transform: uppercase; letter-spacing: 0.5px;"">
                {purpose}
              </div>

              <h2 style=""font-size: 20px; font-weight: 700; color: #1B2A4A; margin: 0 0 14px 0; font-family: Georgia, serif;"">
                Hello {safeUserName},
              </h2>

              <p style=""font-size: 15px; line-height: 1.6; color: #4A4A4A; margin: 0 0 24px 0;"">
                Please use the single-use verification code below to authorize your <strong>{purpose}</strong> request on NextBus.
              </p>

              <!-- OTP Display Card -->
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #FDFBF8; border: 2px dashed #A97C3F; border-radius: 12px; margin: 20px 0 24px 0; text-align: center;"">
                <tr>
                  <td style=""padding: 24px 20px;"">
                    <div style=""font-family: 'Courier New', Courier, monospace; font-size: 40px; font-weight: 800; letter-spacing: 12px; color: #1B2A4A; line-height: 1.2; margin: 0;"">
                      {otpCode}
                    </div>
                    <div style=""font-size: 12px; color: #7A7265; margin-top: 10px; font-weight: 600;"">
                      One-Time Verification Code
                    </div>
                  </td>
                </tr>
              </table>

              <!-- Expiry Alert -->
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #FFF9ED; border-left: 4px solid #A97C3F; border-radius: 4px; margin: 20px 0;"">
                <tr>
                  <td style=""padding: 12px 16px; font-size: 13px; color: #6C5528; line-height: 1.5;"">
                    <strong>Important:</strong> This verification code expires in <strong>15 minutes</strong> and can only be used once.
                  </td>
                </tr>
              </table>

              <!-- Security Notice -->
              <div style=""font-size: 12px; color: #7A7A7A; line-height: 1.6; padding-top: 20px; border-top: 1px solid #ECE7DE; margin-top: 24px;"">
                <strong>Security Reminder:</strong> Never share this code with anyone. NextBus staff and drivers will never ask for your verification code. If you did not initiate this action, please disregard this email or secure your account.
              </div>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td align=""center"" style=""background-color: #FAF7F2; padding: 26px 30px; text-align: center; border-top: 1px solid #ECE7DE;"">
              <div style=""margin-bottom: 12px;"">
                <a href=""https://dev-nextbus.u-check.ng"" style=""color: #1B2A4A; text-decoration: none; font-weight: 600; font-size: 12px; margin: 0 10px;"">NextBus Portal</a> &bull;
                <a href=""https://dev-nextbus.u-check.ng/search"" style=""color: #1B2A4A; text-decoration: none; font-weight: 600; font-size: 12px; margin: 0 10px;"">Find Rides</a> &bull;
                <a href=""mailto:support@classicalaitem.com"" style=""color: #1B2A4A; text-decoration: none; font-weight: 600; font-size: 12px; margin: 0 10px;"">Support</a>
              </div>
              <p style=""font-size: 11px; color: #8C8476; line-height: 1.6; margin: 0;"">
                &copy; {year} NextBus Transit Platform by ClassicalAitem. All rights reserved.<br />
                Automated security notification sent to {toEmail}.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
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

            var body = $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; padding: 0; background-color: #F6F1E7;"">
<head>
  <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{subject}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #F6F1E7; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #F6F1E7; padding: 40px 15px;"">
    <tr>
      <td align=""center"">
        <!-- Main Card -->
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 16px; border: 1px solid #dcd3c5; overflow: hidden; box-shadow: 0 10px 30px rgba(27, 42, 74, 0.08);"">
          <!-- Header -->
          <tr>
            <td align=""center"" style=""background-color: #1B2A4A; padding: 32px 30px 26px 30px; border-bottom: 3px solid #A97C3F;"">
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td align=""center"">
                    <div style=""display: inline-block; width: 44px; height: 44px; background-color: #A97C3F; border-radius: 10px; line-height: 44px; text-align: center; color: #1B2A4A; font-weight: bold; font-size: 20px; margin-bottom: 10px;"">
                      NB
                    </div>
                    <h1 style=""color: #ffffff; font-family: Georgia, 'Times New Roman', serif; font-size: 26px; font-weight: 700; margin: 0 0 4px 0;"">NextBus</h1>
                    <p style=""color: #A97C3F; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 3px; margin: 0;"">Rides &amp; Transit Network</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding: 34px 36px 30px 36px; background-color: #ffffff;"">
              <div style=""display: inline-block; background-color: #EBF8F2; color: #106644; font-size: 11px; font-weight: 700; padding: 6px 14px; border-radius: 20px; border: 1px solid #A6E2C9; margin-bottom: 20px; text-transform: uppercase; letter-spacing: 0.5px;"">
                New Seat Request
              </div>

              <h2 style=""font-size: 20px; font-weight: 700; color: #1B2A4A; margin: 0 0 12px 0; font-family: Georgia, serif;"">
                Hello {safeDriverName},
              </h2>

              <p style=""font-size: 15px; line-height: 1.6; color: #4A4A4A; margin: 0 0 22px 0;"">
                Great news! You have received a new passenger seat booking for your offered ride. The fare has been pre-authorized and secured in digital escrow.
              </p>

              <!-- Route Card Table -->
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #FDFBF8; border: 1px solid #e2d9cb; border-radius: 12px; margin: 18px 0 24px 0;"">
                <tr>
                  <td style=""padding: 20px;"">
                    <div style=""font-family: Georgia, serif; font-size: 18px; font-weight: 800; color: #1B2A4A; padding-bottom: 14px; border-bottom: 2px solid #A97C3F;"">
                      {origin} &rarr; {destination}
                    </div>

                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""font-size: 13px; margin-top: 10px;"">
                      <tr>
                        <td style=""padding: 8px 0; color: #7A7265; font-weight: 600; border-bottom: 1px dashed #ECE7DE;"">Passenger</td>
                        <td align=""right"" style=""padding: 8px 0; color: #1B2A4A; font-weight: 700; border-bottom: 1px dashed #ECE7DE;"">{passengerName} ({passengerPhone})</td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; color: #7A7265; font-weight: 600; border-bottom: 1px dashed #ECE7DE;"">Pickup Landmark</td>
                        <td align=""right"" style=""padding: 8px 0; color: #1B2A4A; font-weight: 700; border-bottom: 1px dashed #ECE7DE;"">{pickup}</td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; color: #7A7265; font-weight: 600; border-bottom: 1px dashed #ECE7DE;"">Dropoff Landmark</td>
                        <td align=""right"" style=""padding: 8px 0; color: #1B2A4A; font-weight: 700; border-bottom: 1px dashed #ECE7DE;"">{dropoff}</td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; color: #7A7265; font-weight: 600; border-bottom: 1px dashed #ECE7DE;"">Seats Reserved</td>
                        <td align=""right"" style=""padding: 8px 0; color: #1B2A4A; font-weight: 700; border-bottom: 1px dashed #ECE7DE;"">{seats} seat(s)</td>
                      </tr>
                      <tr>
                        <td style=""padding: 10px 0; color: #7A7265; font-weight: 600; border-bottom: 1px dashed #ECE7DE;"">Escrow Fare Amount</td>
                        <td align=""right"" style=""padding: 10px 0; color: #A97C3F; font-size: 17px; font-weight: 800; border-bottom: 1px dashed #ECE7DE;"">&#8358;{fare:N2}</td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; color: #7A7265; font-weight: 600;"">Departure Schedule</td>
                        <td align=""right"" style=""padding: 8px 0; color: #1B2A4A; font-weight: 700;"">{formattedTime}</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>

              <!-- CTA Button -->
              <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                <tr>
                  <td align=""center"" style=""padding-top: 10px;"">
                    <a href=""https://dev-nextbus.u-check.ng/my-rides"" target=""_blank"" style=""display: inline-block; background-color: #1B2A4A; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 700; padding: 14px 28px; border-radius: 10px; border: 1px solid #A97C3F; text-align: center; box-shadow: 0 4px 12px rgba(27,42,74,0.18);"">
                      Review &amp; Accept Booking in My Rides &rarr;
                    </a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td align=""center"" style=""background-color: #FAF7F2; padding: 24px 30px; text-align: center; border-top: 1px solid #ECE7DE;"">
              <p style=""font-size: 11px; color: #8C8476; line-height: 1.6; margin: 0;"">
                &copy; {year} NextBus Transit Platform by ClassicalAitem. All rights reserved.<br />
                Escrow booking notification sent to {toEmail}.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
