using DietManagementWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DietManagementWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly EmailService _emailService;

        public TestController(EmailService emailService)
        {
            _emailService = emailService;
        }


        [HttpGet("SendEmail")]
        public async Task<IActionResult> SendEmail()
        {
            try
            {
                string testEmail = "hasnainwork705@gmail.com";
                string testOtp = "123456";


                await _emailService.SendOtpEmailAsync(
                    testEmail,
                    testOtp
                );


                return Ok(new
                {
                    success = true,
                    message = "Email sent successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }
    }
}