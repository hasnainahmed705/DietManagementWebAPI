using DietManagementWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DietManagementWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private EmailService _emailService;

        public TestController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet("SendEmail")]
        public async Task<IActionResult> SendEmail()
        {
            try
            {
                await _emailService.SendOtpEmailAsync(
                    "hasnainwork705@gmail.com",
                    "123456");

                return Ok(new
                {
                    Success = true,
                    Message = "Email sent successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}