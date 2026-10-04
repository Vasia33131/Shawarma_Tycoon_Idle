using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
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

        [MenuItem("ShaurmaTycoon/Debug/Add 1e9 Earned", true)]
        [MenuItem("ShaurmaTycoon/Debug/Add 1e10 Earned", true)]
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
