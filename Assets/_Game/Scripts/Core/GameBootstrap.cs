using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Точка входа (сцена Boot): создаёт и регистрирует сервисы, затем загружает сцену Game.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _gameSceneName = "Game";
        [Tooltip("TSV-таблица строк (Configs/Localization/strings.txt).")]
        [SerializeField] private TextAsset _strings = null;

        private IPlatformService _platform;

        private void Awake()
        {
            var tickManagerObject = new GameObject(nameof(TickManager));
            DontDestroyOnLoad(tickManagerObject);
            var tickManager = tickManagerObject.AddComponent<TickManager>();
            Services.Register(tickManager);
            Services.Register(new Wallet());

            var tweens = new TweenRunner();
            var pulses = new PulseDriver(tweens);
            Services.Register(tweens);
            Services.Register(pulses);
            tickManager.Register(tweens);
            tickManager.Register(pulses);

            _platform = CreatePlatform();
            Services.Register(_platform);
            _platform.Init(OnPlatformReady);
        }

        private static IPlatformService CreatePlatform()
        {
            // TODO(YANDEX): на этапе 09 в WebGL-билде создавать YandexPlatformService.
            return new EditorPlatformService();
        }

        private void OnPlatformReady()
        {
            Services.Register(new Loc(LoadStrings(), _platform.Language));

            Debug.Log("Bootstrap OK");
            SceneManager.LoadScene(_gameSceneName);
        }

        private LocTable LoadStrings()
        {
            if (_strings != null)
                return LocTable.Parse(_strings.text);

            Debug.LogError($"{nameof(GameBootstrap)}: localization table is not assigned.", this);
            return LocTable.Empty;
        }
    }
}
