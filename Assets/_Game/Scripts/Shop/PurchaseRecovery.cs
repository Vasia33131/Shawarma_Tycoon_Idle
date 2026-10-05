using System;
using ShaurmaTycoon.Platform;
using UnityEngine;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Обработка неподтверждённых покупок при старте (обязательное требование модерации Яндекса):
    /// оплачено, но игра закрылась до выдачи — выдаём и подтверждаем сейчас. Выполняется один раз за сессию.
    /// </summary>
    public static class PurchaseRecovery
    {
        private static bool _done;

        /// <param name="tryGrant">Выдаёт покупку по id товара; false — товар не знаком этому обработчику.</param>
        /// <returns>true — проверка запущена, false — уже выполнялась или нет платформы.</returns>
        public static bool Run(IPlatformService platform, Func<string, bool> tryGrant, Action<string> log = null)
        {
            if (_done || platform == null || tryGrant == null)
                return false;

            _done = true;
            platform.GetPurchases(ids =>
            {
                if (ids == null)
                    return;

                for (int i = 0; i < ids.Length; i++)
                {
                    if (tryGrant(ids[i]))
                        log?.Invoke($"[Purchases] Recovered unconfirmed purchase '{ids[i]}'.");
                }
            });
            return true;
        }

        /// <summary>Сброс «выполнялось» (старт Play Mode без Domain Reload, тесты).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            _done = false;
        }
    }
}
