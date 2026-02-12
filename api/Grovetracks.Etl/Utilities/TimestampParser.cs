using System.Globalization;

namespace Grovetracks.Etl.Utilities;

public static class TimestampParser
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.FFFFF UTC";

    public static DateTime Parse(string timestamp)
    {
        if (DateTime.TryParseExact(
                timestamp.AsSpan(),
                [TimestampFormat, "yyyy-MM-dd HH:mm:ss.FFFFFFF UTC"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var result))
        {
            return result;
        }

        return DateTime.Parse(timestamp, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
