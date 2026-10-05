using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.UI
{
    /// <summary>Таймер без аллокаций: «m:ss» или «h:mm:ss».</summary>
    public static class TimerText
    {
        public static NumberText AppendTimer(this NumberText text, long seconds)
        {
            if (seconds < 0)
                seconds = 0;

            long hours = seconds / 3600;
            long minutes = seconds / 60 % 60;
            long secs = seconds % 60;
            if (hours > 0)
            {
                text.AppendInteger(hours).Append(':');
                if (minutes < 10)
                    text.Append('0');
            }

            text.AppendInteger(minutes).Append(':');
            if (secs < 10)
                text.Append('0');
            return text.AppendInteger(secs);
        }
    }
}
