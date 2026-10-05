using System.Collections.Generic;
using System.Text;
using ShaurmaTycoon.Balance;
using ShaurmaTycoon.Core;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Stations;
using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// Прогон баланса (этап 11) по настоящим ассетам: для каждого города каталога — «обычный» и «внимательный» игрок.
    /// Результат (Markdown-таблицы) — в консоль и в буфер обмена, для PROGRESS.md.
    /// </summary>
    public static class BalanceMenu
    {
        private const string CatalogPath = "Assets/_Game/Configs/Cities/CityCatalog.asset";
        private const string BalancePath = "Assets/_Game/Configs/Balance.asset";

        [MenuItem("ShaurmaTycoon/Balance/Simulate Cities")]
        private static void SimulateCities()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CityCatalog>(CatalogPath);
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            if (catalog == null || balance == null)
            {
                Debug.LogError($"[Balance] Not found: {CatalogPath} or {BalancePath}.");
                return;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < catalog.Count; i++)
            {
                CityConfig city = catalog.Get(i);
                sb.Append("### ").Append(i + 1).Append(". ").AppendLine(city.Id);
                AppendRun(sb, city, balance, "Обычный игрок (решает раз в 10 с, тап раз в 1.5 с)",
                    BalanceSimulationSettings.Typical(city.PrestigeThreshold));
                AppendRun(sb, city, balance, "Внимательный игрок (решает раз в 1 с, тап раз в 0.5 с)",
                    BalanceSimulationSettings.Active(city.PrestigeThreshold));
            }

            string report = sb.ToString();
            EditorGUIUtility.systemCopyBuffer = report;
            Debug.Log("[Balance] Copied to clipboard.\n" + report);
        }

        private static void AppendRun(StringBuilder sb, CityConfig city, BalanceConfig balance, string title, BalanceSimulationSettings settings)
        {
            var simulator = new BalanceSimulator(city.Stations, city.Upgrades, balance.CreateMilestoneTable(), balance.ManagerCostMultiplier);
            BalanceReport report = simulator.Run(settings);

            var names = new List<string>(city.Stations.Count);
            foreach (StationConfig station in city.Stations)
                names.Add(station.Id);

            sb.Append("**").Append(title).AppendLine("**").AppendLine();
            sb.AppendLine(BalanceReportFormatter.ToMarkdown(report, names));
        }
    }
}
