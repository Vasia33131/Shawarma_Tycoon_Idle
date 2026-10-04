using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Managers;
using ShaurmaTycoon.Prestige;
using UnityEditor;
using UnityEngine;

namespace ShaurmaTycoon.EditorTools
{
    /// <summary>
    /// ВРЕМЕННЫЕ пункты для проверки престижа в Play (полноценное отладочное окно — этап 11).
    /// Добавляют монеты и одновременно засчитывают их как заработок города.
    /// </summary>
    public static class DebugMenu
    {
        [MenuItem("ShaurmaTycoon/Debug/Add 1e9 Earned")]
        private static void AddBillion()
        {
            AddEarned(1e9);
        }

        [MenuItem("ShaurmaTycoon/Debug/Add 1e10 Earned")]
        private static void AddTenBillions()
        {
            AddEarned(1e10);
        }

        /// <summary>Ровно порог текущего города — для любого города каталога.</summary>
        [MenuItem("ShaurmaTycoon/Debug/Add City Threshold Earned")]
        private static void AddCityThreshold()
        {
            if (!Services.TryGet(out PrestigeController controller))
            {
                Debug.LogWarning("[Debug] Services are not ready — enter Play first.");
                return;
            }

            AddEarned(controller.Threshold);
        }

        /// <summary>Нанимает менеджера на первую открытую станцию без него (монеты на найм добавляются) — проверка роста ларька.</summary>
        [MenuItem("ShaurmaTycoon/Debug/Hire Next Manager")]
        private static void HireNextManager()
        {
            if (!Services.TryGet(out ManagerController managers) || !Services.TryGet(out Wallet wallet))
            {
                Debug.LogWarning("[Debug] Services are not ready — enter Play first.");
                return;
            }

            for (int i = 0; i < managers.Count; i++)
            {
                if (!managers.CanHire(i))
                    continue;

                wallet.Add(managers.GetCost(i));
                managers.TryHire(i);
                Debug.Log($"[Debug] Hired manager #{i + 1}.");
                return;
            }

            Debug.Log("[Debug] No manager to hire (station locked or all hired).");
        }

        [MenuItem("ShaurmaTycoon/Debug/Add 100 Bills")]
        private static void AddBills()
        {
            if (!Services.TryGet(out BillWallet bills))
            {
                Debug.LogWarning("[Debug] Services are not ready — enter Play first.");
                return;
            }

            bills.Add(100, BillSource.Debug);
            Debug.Log($"[Debug] +100 bills, now {bills.Bills}.");
        }

        [MenuItem("ShaurmaTycoon/Debug/Add 100 Bills", true)]
        [MenuItem("ShaurmaTycoon/Debug/Add 1e9 Earned", true)]
        [MenuItem("ShaurmaTycoon/Debug/Add 1e10 Earned", true)]
        [MenuItem("ShaurmaTycoon/Debug/Add City Threshold Earned", true)]
        [MenuItem("ShaurmaTycoon/Debug/Hire Next Manager", true)]
        private static bool ValidatePlaying()
        {
            return Application.isPlaying;
        }

        private static void AddEarned(double amount)
        {
            if (!Services.TryGet(out Wallet wallet) || !Services.TryGet(out PrestigeState prestige))
            {
                Debug.LogWarning("[Debug] Services are not ready — enter Play first.");
                return;
            }

            wallet.Add(amount);
            prestige.AddEarned(amount);
            Debug.Log($"[Debug] +{BigNumberFormatter.Format(amount)} coins and city earnings.");
        }
    }
}
