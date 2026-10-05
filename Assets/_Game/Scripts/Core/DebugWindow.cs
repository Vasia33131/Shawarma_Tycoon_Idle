#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ShaurmaTycoon.Ambient;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Onboarding;
using ShaurmaTycoon.Prestige;
using ShaurmaTycoon.Save;
using ShaurmaTycoon.Stations;
using UnityEngine;

namespace ShaurmaTycoon.Core
{
    /// <summary>
    /// Отладочное окно (этап 11): только в редакторе и Development build — в релизной сборке файла нет.
    /// Кнопка «DEV» у левого края (или клавиша F2): монеты, ускорение времени ×10, погода и машина (V5), пропуск туториала, сброс сейва.
    /// IMGUI — без префабов и сборки UI; аллокации OnGUI допустимы, потому что окно только для разработки.
    /// </summary>
    public sealed class DebugWindow : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;
        private const float FastTimeScale = 10f;
        private const float ConfirmSeconds = 3f;

        private bool _open;
        private float _resetConfirmUntil = -1f;
        private string _status = string.Empty;
        private GUIStyle _button;
        private GUIStyle _label;

        public static void Create()
        {
            var go = new GameObject(nameof(DebugWindow));
            DontDestroyOnLoad(go);
            go.AddComponent<DebugWindow>();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F2)
            {
                _open = !_open;
                Event.current.Use();
            }

            float scale = Mathf.Max(0.5f, Mathf.Min(Screen.width, Screen.height) / ReferenceHeight);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            EnsureStyles();

            float height = Screen.height / scale;
            if (GUI.Button(new Rect(0f, height * 0.5f - 40f, 90f, 80f), _open ? "×" : "DEV", _button))
                _open = !_open;

            if (!_open)
                return;

            GUILayout.BeginArea(new Rect(100f, height * 0.5f - 500f, 520f, 1000f), GUI.skin.box);
            DrawContent();
            GUILayout.EndArea();
        }

        private void DrawContent()
        {
            Services.TryGet(out TickManager ticks);
            Services.TryGet(out SaveService save);
            Services.TryGet(out TutorialService tutorial);

            GUILayout.Label("Debug", _label);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1K", _button))
                AddCoins(1e3, scaled: true);
            if (GUILayout.Button("+1M", _button))
                AddCoins(1e6, scaled: true);
            if (GUILayout.Button("+1B", _button))
                AddCoins(1e9, scaled: true);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("+10 мин дохода", _button))
                AddCoins(IncomePerSecond() * 600, scaled: false);

            if (ticks != null)
            {
                bool fast = ticks.TimeScale > 1f;
                if (GUILayout.Button(fast ? "Время ×10 → ×1" : "Время ×1 → ×10", _button))
                    ticks.TimeScale = fast ? 1f : FastTimeScale;
            }

            if (Services.TryGet(out WeatherService weather))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Дождь", _button))
                    weather.Force(WeatherKind.Rain);
                if (GUILayout.Button("Снег", _button))
                    weather.Force(WeatherKind.Snow);
                if (GUILayout.Button("Ясно", _button))
                    weather.Force(WeatherKind.Clear);
                GUILayout.EndHorizontal();
            }
            if (Services.TryGet(out AmbientWorldView ambient) && GUILayout.Button("Машина сейчас", _button))
                ambient.SpawnVehicleNow();

            if (tutorial != null && tutorial.IsActive && GUILayout.Button("Пропустить туториал (" + tutorial.Step + ")", _button))
                tutorial.Skip();

            if (save != null && !save.IsWiped)
            {
                bool confirming = Time.unscaledTime < _resetConfirmUntil;
                if (GUILayout.Button(confirming ? "Точно сбросить? Нажми ещё раз" : "Сбросить сейв", _button))
                {
                    if (confirming)
                        WipeSave(save);
                    else
                        _resetConfirmUntil = Time.unscaledTime + ConfirmSeconds;
                }
            }

            if (ticks != null)
                GUILayout.Label("TimeScale ×" + ticks.TimeScale.ToString("0"), _label);
            if (_status.Length > 0)
                GUILayout.Label(_status, _label);
        }

        /// <summary>Монеты засчитываются и как заработок города — так можно проверить порог переезда.</summary>
        private void AddCoins(double amount, bool scaled)
        {
            if (!Services.TryGet(out Wallet wallet))
                return;

            if (scaled && Services.TryGet(out CityConfig city))
                amount *= city.ValueScale;
            if (!(amount > 0))
                amount = 100;

            wallet.Add(amount);
            if (Services.TryGet(out PrestigeState prestige))
                prestige.AddEarned(amount);
            _status = "+" + BigNumberFormatter.Format(amount);
        }

        private static double IncomePerSecond()
        {
            if (!Services.TryGet(out StationController stations))
                return 0;

            double total = 0;
            for (int i = 0; i < stations.Stations.Count; i++)
                total += stations.Stations[i].IncomePerSecond;
            return total;
        }

        private void WipeSave(SaveService save)
        {
            save.Wipe();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            _status = "Сейв сброшен. Перезагрузи страницу.";
            if (Services.TryGet(out TickManager ticks))
                ticks.IsPaused = true;
#endif
        }

        private void EnsureStyles()
        {
            if (_button != null)
                return;

            _button = new GUIStyle(GUI.skin.button) { fontSize = 30, fixedHeight = 80f };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 28, wordWrap = true };
        }
    }
}
#endif
