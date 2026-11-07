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
            var subject = $"NextBus - Your OTP for {purpose}: {otpCode}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
  <style>
    body {{ font-family: Arial, sans-serif; background-color: #f7f9fc; margin: 0; padding: 20px; }}
    .container {{ max-width: 500px; background: #ffffff; margin: 0 auto; padding: 30px; border-radius: 8px; border: 1px solid #e1e8ed; }}
    .header {{ text-align: center; color: #1e88e5; font-size: 24px; font-weight: bold; margin-bottom: 20px; }}
    .otp-box {{ background-color: #e3f2fd; border-radius: 6px; padding: 15px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #0d47a1; margin: 25px 0; }}
    .footer {{ font-size: 12px; color: #8899a6; text-align: center; margin-top: 30px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>NextBus Transit</div>
    <p>Hello {userName},</p>
    <p>Your One-Time Password (OTP) for <strong>{purpose}</strong> is:</p>
    <div class='otp-box'>{otpCode}</div>
    <p>This OTP is valid for <strong>15 minutes</strong>. If you did not initiate this request, please disregard this email or contact support immediately.</p>
    <div class='footer'>&copy; {DateTime.UtcNow.Year} NextBus Inc. All rights reserved.</div>
  </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
