using FirebaseAdmin.Messaging;

public class FirebaseNotificationService
{
    public async Task<string> SendToDeviceAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
    {
        var message = new Message()
        {
            Token = deviceToken,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data, // optional custom data
            Android = new AndroidConfig
            {
                Priority = Priority.High
            }
        };

        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        return response; // returns message ID
    }

    // Send to multiple devices
    public async Task<BatchResponse> SendToMultipleDevicesAsync(List<string> deviceTokens, string title, string body)
    {
        var message = new MulticastMessage()
        {
            Tokens = deviceTokens,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Android = new AndroidConfig
            {
                Priority = Priority.High
            }
        };

        return await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
    }

    // Send to a Topic
    public async Task<string> SendToTopicAsync(string topic, string title, string body)
    {
        var message = new Message()
        {
            Topic = topic,
            Notification = new Notification
            {
                Title = title,
                Body = body
            }
        };

        return await FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
}