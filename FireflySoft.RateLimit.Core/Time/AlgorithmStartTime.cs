using System;

namespace FireflySoft.RateLimit.Core.Time
{
    /// <summary>
    /// Defines some methods for the start time of algorithm
    /// </summary>
    public class AlgorithmStartTime
    {
        /// <summary>
        /// Convert to the time of specified type
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="statWindow"></param>
        /// <param name="startTimeType"></param>
        /// <returns></returns>
        public static DateTimeOffset ToSpecifiedTypeTime(DateTimeOffset startTime, TimeSpan statWindow, StartTimeType startTimeType)
        {
            if (startTimeType == StartTimeType.FromNaturalPeriodBeign)
            {
                startTime = AlgorithmStartTime.ToNaturalPeriodBeignTime(startTime, statWindow);
            }

            return startTime;
        }

        /// <summary>
        /// Convert to the time of specified type
        /// </summary>
        /// <param name="startTimeMilliseonds"></param>
        /// <param name="statWindow"></param>
        /// <param name="startTimeType"></param>
        /// <returns></returns>
        public static long ToSpecifiedTypeTime(long startTimeMilliseonds, TimeSpan statWindow, StartTimeType startTimeType)
        {
            if (startTimeType == StartTimeType.FromNaturalPeriodBeign)
            {
                DateTimeOffset startTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMilliseonds);
                DateTimeOffset startTime = AlgorithmStartTime.ToNaturalPeriodBeignTime(startTimeUtc, statWindow);
                return startTime.ToUnixTimeMilliseconds();
            }

            return startTimeMilliseonds;
        }

        /// <summary>
        /// Convert to the natural period begin of the start time
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="statWindow"></param>
        /// <returns></returns>
        public static DateTimeOffset ToNaturalPeriodBeignTime(DateTimeOffset startTime, TimeSpan statWindow)
        {
            TimeSpan offset = startTime.Offset;

            if (statWindow.Days > 0)
            {
                startTime = new DateTimeOffset(startTime.Year, startTime.Month, startTime.Day, 0, 0, 0, offset);
            }
            else if (statWindow.Hours > 0)
            {
                startTime = new DateTimeOffset(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0, offset);
            }
            else if (statWindow.Minutes > 0)
            {
                startTime = new DateTimeOffset(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, 0, offset);
            }
            else if (statWindow.Seconds > 0)
            {
                startTime = new DateTimeOffset(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second, offset);
            }

            return startTime;
        }
    }
}