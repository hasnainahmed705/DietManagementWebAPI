using DietManagementWebAPI.Models;
using Microsoft.Extensions.Options;
using Resend;

namespace DietManagementWebAPI.Services
{
    public class EmailService
    {
        private readonly ResendSettings _settings;
        private readonly IResend _resend;


        public EmailService(
            IOptions<ResendSettings> settings,
            IResend resend)
        {
            _settings = settings.Value;
            _resend = resend;

            Console.WriteLine(
        $"Resend Key Loaded: {_settings.ApiKey?.Substring(0, 8)}"
    );
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