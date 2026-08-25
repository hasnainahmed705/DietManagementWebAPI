public static class TimeZoneHelper
{
    public static DateTime GetUserLocalDate(string timeZoneId)
    {
        try
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzInfo);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow;
        }
    }
}
