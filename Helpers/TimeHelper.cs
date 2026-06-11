using System;

namespace EduBridge.Helpers;

public static class TimeHelper
{
    public static DateTime GetVietnamNow()
    {
        try 
        { 
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); 
        } 
        catch 
        { 
            return DateTime.UtcNow.AddHours(7); 
        }
    }
}
