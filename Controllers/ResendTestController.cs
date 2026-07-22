using DietManagementWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace DietManagementWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResendTestController : ControllerBase
    {
        private readonly ResendSettings _settings;


        public ResendTestController(
            IOptions<ResendSettings> settings)
        {
            _settings = settings.Value;
        }



        [HttpGet("TestApiKey")]
        public async Task<IActionResult> TestApiKey()
        {
            try
            {
                using var client = new HttpClient();


                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        _settings.ApiKey
                    );


                var response = await client.GetAsync(
                    "https://api.resend.com/api-keys"
                );


                var responseBody =
                    await response.Content.ReadAsStringAsync();



                return Ok(new
                {
                    StatusCode = (int)response.StatusCode,

                    IsSuccess = response.IsSuccessStatusCode,

                    Response = responseBody
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message,
                    Detail = ex.InnerException?.Message
                });
            }
        }
    }
}