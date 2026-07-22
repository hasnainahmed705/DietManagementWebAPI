using DietManagementWebAPI.Models;
using Microsoft.Extensions.Options;
using Resend;

namespace DietManagementWebAPI.Services
{
    public class EmailService
    {
        private readonly IResend _resend;
        private readonly ResendSettings _settings;


        public EmailService(
            IOptions<ResendSettings> settings,
            IResend resend)
        {
            _settings = settings.Value;
            _resend = resend;

            Console.WriteLine($"Key length: {_settings.ApiKey.Length}");
            Console.WriteLine($"First chars: {_settings.ApiKey.Substring(0, 5)}");
            Console.WriteLine($"Last chars: {_settings.ApiKey.Substring(_settings.ApiKey.Length - 5)}");
        }



        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlMessage)
        {

            var email = new EmailMessage();

            email.From =
                $"{_settings.SenderName} <{_settings.SenderEmail}>";


            email.To.Add(toEmail);


            email.Subject = subject;


            email.HtmlBody = htmlMessage;



            await _resend.EmailSendAsync(email);
        }





        public async Task SendOtpEmailAsync(
            string toEmail,
            string otp)
        {

            string subject =
                "Your Login Verification Code";


            string body = $@"

            <h2>Diet Management App</h2>


            <p>Your One-Time Password (OTP) is:</p>


            <h1 style='color:blue'>
                {otp}
            </h1>


            <p>
                This OTP will expire in 
                <b>5 minutes</b>.
            </p>


            <p>
                If you didn't request this login,
                please ignore this email.
            </p>

            ";



            await SendEmailAsync(
                toEmail,
                subject,
                body);
        }
    }
}