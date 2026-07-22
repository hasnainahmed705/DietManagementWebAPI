using DietManagementWebAPI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DietManagementWebAPI.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage)
        {
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail));

            email.To.Add(
                MailboxAddress.Parse(toEmail));

            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = htmlMessage
            };

            using var smtp = new SmtpClient();
            smtp.Timeout = 10000;
            Console.WriteLine("Connecting to Gmail SMTP...");

            await smtp.ConnectAsync(
                _emailSettings.SmtpServer,
                _emailSettings.Port,
                SecureSocketOptions.StartTls);
            Console.WriteLine("SMTP Connected");

            await smtp.AuthenticateAsync(
                _emailSettings.Username,
                _emailSettings.Password);

            Console.WriteLine("SMTP Authenticated");

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }

        public async Task SendOtpEmailAsync(
            string toEmail,
            string otp)
        {
            string subject = "Your Login Verification Code";

            string body = $@"
                <h2>Diet Management App</h2>

                <p>Your One-Time Password (OTP) is:</p>

                <h1 style='color:blue'>{otp}</h1>

                <p>This OTP will expire in <b>5 minutes</b>.</p>

                <p>If you didn't request this login, please ignore this email.</p>";

            await SendEmailAsync(
                toEmail,
                subject,
                body);
        }
    }
}