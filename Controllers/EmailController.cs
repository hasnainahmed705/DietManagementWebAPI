using DietManagementWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DietManagementWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;

        public EmailController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [AllowAnonymous]
        [HttpGet("SendEmail")]
        public async Task<IActionResult> SendOTPEmail(string email)
        {
            try
            {
                var otp = Random.Shared.Next(100000, 999999).ToString();

                await _emailService.SendOtpEmailAsync(
                    email,
                    otp
                );


                return Ok(new
                {
                    success = true,
                    message = "OTP has been sent successfully. Please check your email inbox."
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }
    }
}