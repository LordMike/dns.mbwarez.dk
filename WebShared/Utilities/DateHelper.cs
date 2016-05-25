using System;

namespace WebShared.Utilities
{
    public static class DateHelper
    {
        private static DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static string ToString(DateTime time)
        {
            return ((long)(time.ToUniversalTime() - _epoch).TotalSeconds).ToString();
        }

        public static DateTime FromString(string str)
        {
            return _epoch.AddSeconds(long.Parse(str));
        }
    }
}