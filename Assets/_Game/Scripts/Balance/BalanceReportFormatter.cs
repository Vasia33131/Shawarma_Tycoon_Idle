using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ShaurmaTycoon.Economy;

namespace ShaurmaTycoon.Balance
{
    /// <summary>Отчёт прогона баланса в Markdown — для PROGRESS.md и консоли редактора.</summary>
    public static class BalanceReportFormatter
    {
        public static string ToMarkdown(BalanceReport report, IReadOnlyList<string> stationNames)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var sb = new StringBuilder();
            sb.AppendLine("| # | Станция | Открыта | Менеджер | Уровень в конце |");
            sb.AppendLine("|---|---|---|---|---|");
            for (int i = 0; i < report.StationUnlockedAt.Length; i++)
            {
                string name = stationNames != null && i < stationNames.Count ? stationNames[i] : "—";
                sb.Append("| ").Append(i + 1).Append(" | ").Append(name)
                    .Append(" | ").Append(FormatTime(report.StationUnlockedAt[i]))
                    .Append(" | ").Append(FormatTime(report.ManagerHiredAt[i]))
                    .Append(" | ").Append(report.FinalLevels[i].ToString(CultureInfo.InvariantCulture))
                    .AppendLine(" |");
            }

            sb.AppendLine();
            sb.Append("- Порог переезда: ").AppendLine(FormatTime(report.ThresholdReachedAt));
            sb.Append("- Покупок за первую минуту: ").AppendLine(report.PurchasesBefore(60).ToString(CultureInfo.InvariantCulture));
            sb.Append("- Улучшений куплено: ").AppendLine(CountDone(report.UpgradeBoughtAt).ToString(CultureInfo.InvariantCulture)
                + " из " + report.UpgradeBoughtAt.Length.ToString(CultureInfo.InvariantCulture));
            sb.Append("- Заработано: ").AppendLine(BigNumberFormatter.Format(report.TotalEarned));

            sb.AppendLine();
            sb.AppendLine("| Время | Заработано всего | Доход/с |");
            sb.AppendLine("|---|---|---|");
            double[] checkpoints = { 60, 300, 600, 1800, 3600, 5400, 7200, 9000, 10800 };
            for (int c = 0; c < checkpoints.Length; c++)
            {
                BalanceSample? sample = FindSample(report, checkpoints[c]);
                if (sample == null)
                    continue;
                sb.Append("| ").Append(FormatTime(checkpoints[c]))
                    .Append(" | ").Append(BigNumberFormatter.Format(sample.Value.Earned))
                    .Append(" | ").Append(BigNumberFormatter.Format(sample.Value.IncomePerSecond))
                    .AppendLine(" |");
            }

            return sb.ToString();
        }

        /// <summary>«0:45», «12:05», «1:02:30»; отрицательное — «—».</summary>
        public static string FormatTime(double seconds)
        {
            if (seconds < 0)
                return "—";

            var span = TimeSpan.FromSeconds(Math.Round(seconds));
            return span.TotalHours >= 1
                ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)span.TotalHours, span.Minutes, span.Seconds)
                : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", span.Minutes, span.Seconds);
        }

        private static BalanceSample? FindSample(BalanceReport report, double time)
        {
            for (int i = 0; i < report.Samples.Count; i++)
            {
                if (report.Samples[i].Time >= time)
                    return report.Samples[i];
            }
            return null;
        }

        private static int CountDone(double[] times)
        {
            int count = 0;
            for (int i = 0; i < times.Length; i++)
            {
                if (times[i] >= 0)
                    count++;
            }
            return count;
        }
    }
}
