using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class FirebaseNotificationsController : ControllerBase
{
    private readonly FirebaseNotificationService _firebaseService;

    public FirebaseNotificationsController(FirebaseNotificationService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
    {
        try
        {
            var messageId = await _firebaseService.SendToDeviceAsync(
                request.DeviceToken,
                request.Title,
                request.Body
            );

            return Ok(new { success = true, messageId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class NotificationRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}