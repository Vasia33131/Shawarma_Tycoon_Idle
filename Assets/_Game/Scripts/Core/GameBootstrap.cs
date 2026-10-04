using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Platform;
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

        private void Awake()
        {
            var tickManagerObject = new GameObject(nameof(TickManager));
            DontDestroyOnLoad(tickManagerObject);
            Services.Register(tickManagerObject.AddComponent<TickManager>());
            Services.Register(new Wallet());

            IPlatformService platform = CreatePlatform();
            Services.Register(platform);
            platform.Init(OnPlatformReady);
        }

        private static IPlatformService CreatePlatform()
        {
            // TODO(YANDEX): на этапе 09 в WebGL-билде создавать YandexPlatformService.
            return new EditorPlatformService();
        }

        private void OnPlatformReady()
        {
            Debug.Log("Bootstrap OK");
            SceneManager.LoadScene(_gameSceneName);
        }
    }
}
