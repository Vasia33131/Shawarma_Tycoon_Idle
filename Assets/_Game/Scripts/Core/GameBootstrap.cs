using ShaurmaTycoon.Core.Audio;
using ShaurmaTycoon.Core.Settings;
using ShaurmaTycoon.Core.Tweening;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Platform;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Точка входа (сцена Boot): создаёт и регистрирует сервисы, загружает сейв, затем загружает сцену Game.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _gameSceneName = "Game";
        [Tooltip("TSV-таблица строк (Configs/Localization/strings.txt).")]
        [SerializeField] private TextAsset _strings = null;

        private TickManager _tickManager;
        private IPlatformService _platform;

        private void Awake()
        {
            var tickManagerObject = new GameObject(nameof(TickManager));
            DontDestroyOnLoad(tickManagerObject);
            _tickManager = tickManagerObject.AddComponent<TickManager>();
            tickManagerObject.AddComponent<ApplicationEvents>();
            Services.Register(_tickManager);
            Services.Register(new Wallet());
            Services.Register(new BillWallet());

            var tweens = new TweenRunner();
            var pulses = new PulseDriver(tweens);
            Services.Register(tweens);
            Services.Register(pulses);
            _tickManager.Register(tweens);
            _tickManager.Register(pulses);

            // Единственный AudioListener игры: в сценах его нет, а этот объект переживает перезагрузку Game.
            var audioObject = new GameObject("Audio");
            DontDestroyOnLoad(audioObject);
            audioObject.AddComponent<AudioListener>();
            var music = new MusicService(audioObject);
            Services.Register(music);
            _tickManager.Register(music);

            var settings = new GameSettings();
            Services.Register(settings);
            settings.Changed += () => music.Muted = !settings.MusicOn;

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

            IPlatformService platform = _platform;
            var save = new SaveService(new PlatformSaveStorage(platform), new JsonSaveSerializer(), () => platform.ServerTimeUtc);
            save.LoadFailed += error => Debug.LogWarning($"[Save] Save is corrupted, starting a new game: {error.Message}");
            save.SaveFailed += () => Debug.LogWarning("[Save] Storage failed to write the save.");
            GameEvents.ApplicationFocusChanged += save.OnApplicationFocusChanged;
            GameEvents.ApplicationPaused += save.OnApplicationPaused;
            GameEvents.ApplicationQuitting += save.OnApplicationQuitting;
            Services.Register(save);
            _tickManager.Register(save);

            var prestige = new PrestigeState();
            Services.Register(prestige);
            save.Load(() =>
            {
                // Прогресс престижа живёт в Boot и переживает перезагрузку сцены Game.
                save.Register(new PrestigeSaveParticipant(prestige));

                // Настройки — сразу из сейва; дальше каждое переключение сохраняется сразу.
                var settings = Services.Get<GameSettings>();
                save.Register(new SettingsSaveParticipant(settings));
                settings.Changed += save.SaveNow;

                save.Register(new BillsSaveParticipant(Services.Get<BillWallet>()));
                OnSaveLoaded();
            });
        }

        private void OnSaveLoaded()
        {
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
