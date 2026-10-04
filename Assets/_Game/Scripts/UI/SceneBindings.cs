using System.Collections.Generic;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>Вью, подписанная на сервисы Boot (Wallet, Loc, PrestigeState, BillWallet, GameSettings), которые переживают сцену.</summary>
    public interface ISceneBinding
    {
        /// <summary>Отписаться от всех событий. Вызывается повторно без вреда.</summary>
        void Unbind();
    }

    /// <summary>
    /// Отписка вью сцены Game при её выгрузке (переезд в новый город).
    /// Unity не вызывает OnDestroy у объектов, которые ни разу не были активны: строки закрытых вкладок,
    /// скрытые попапы. Их подписки на сервисы Boot остались бы жить и падали бы на уничтоженных объектах
    /// уже в новой сцене. Поэтому GameScreen (он всегда активен) в своём OnDestroy отписывает всех.
    /// </summary>
    public static class SceneBindings
    {
        private static readonly List<ISceneBinding> Bindings = new List<ISceneBinding>(64);

        public static void Track(ISceneBinding binding)
        {
            if (binding != null && !Bindings.Contains(binding))
                Bindings.Add(binding);
        }

        public static void UnbindAll()
        {
            for (int i = 0; i < Bindings.Count; i++)
                Bindings[i].Unbind();
            Bindings.Clear();
        }

        // Сброс при старте Play Mode, если в редакторе отключён Domain Reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Bindings.Clear();
        }
    }
}
