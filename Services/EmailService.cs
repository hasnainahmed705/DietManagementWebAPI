using DietManagementWebAPI.Models.EmailModels;
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
        }

        public async Task SendHelpCenterEmailAsync(string userName, string userEmail, string userSubject, string userMessage)
        {
            var email = new EmailMessage();
            // The sender MUST be your verified Resend domain
            email.From = "Diet Management <no-reply@dietmanagementapp.fit>";

            // The email goes TO YOU (the app admin)
            email.To.Add("macromate9@gmail.com");
            // Important: This sets the "Reply-To" to the user's email, so when you click Reply, it goes to them
            email.ReplyTo.Add(userEmail);
            email.Subject = $"App Help Center: {userSubject}";

            email.HtmlBody = $@"
            <h3>New Support Request</h3>
            <p><strong>Name:</strong> {userName}</p>
            <p><strong>Email:</strong> {userEmail}</p>
            <p><strong>Message:</strong></p>
            <p>{userMessage}</p>";
            await _resend.EmailSendAsync(email);
        }

    public async Task SendEmailAsync(
     string toEmail,
     string subject,
     string htmlMessage)
        {
            var email = new EmailMessage();


            email.From =
                "Diet Management <no-reply@dietmanagementapp.fit>";


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
                <b>3 minutes</b>.
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


