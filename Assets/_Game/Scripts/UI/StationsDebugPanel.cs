using ShaurmaTycoon.Core;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Stations;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// ВРЕМЕННАЯ отладочная панель этапа 02 (IMGUI): станции, тап и покупка без настоящего UI.
    /// Работает только в редакторе и Development build. Удаляется на этапе 03.
    /// </summary>
    public sealed class StationsDebugPanel : MonoBehaviour
    {
        private const float PanelWidth = 520f;
        private const float ReferenceShortSide = 540f;

        private StationController _stations;
        private Wallet _wallet;

        private void Awake()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            Destroy(this);
#endif
        }

        private void OnGUI()
        {
            if (_stations == null && !Services.TryGet(out _stations))
                return;
            if (_wallet == null && !Services.TryGet(out _wallet))
                return;

            float scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / ReferenceShortSide);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            GUILayout.BeginArea(new Rect(10f, 10f, PanelWidth, Screen.height / scale - 20f), GUI.skin.box);
            GUILayout.Label($"DEBUG (этап 02)   Монеты: {BigNumberFormatter.Format(_wallet.Coins)}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1K"))
                _wallet.Add(1e3);
            if (GUILayout.Button("+1M"))
                _wallet.Add(1e6);
            if (GUILayout.Button("+1B"))
                _wallet.Add(1e9);
            GUILayout.EndHorizontal();

            for (int i = 0; i < _stations.Stations.Count; i++)
                DrawStation(i, _stations.Stations[i]);

            GUILayout.EndArea();
        }

        private void DrawStation(int index, StationModel station)
        {
            GUILayout.Space(6f);
            GUILayout.Label(
                $"{index + 1}. {station.Definition.Id}  ур. {station.Level}  " +
                $"цикл {station.CycleTime:0.00} с  доход/цикл {BigNumberFormatter.Format(station.IncomePerCycle)}  " +
                $"{(station.IsRunning ? $"{station.Progress * 100:0}%" : "—")}  след. веха {station.NextMilestone}");

            GUILayout.BeginHorizontal();
            if (!station.IsUnlocked)
            {
                if (GUILayout.Button($"Открыть ({BigNumberFormatter.Format(station.GetCost(1))})"))
                    _stations.TryBuy(index, 1);
            }
            else
            {
                if (GUILayout.Button("Тап"))
                    _stations.Tap(index);
                if (GUILayout.Button($"×1 ({BigNumberFormatter.Format(station.GetCost(1))})"))
                    _stations.TryBuy(index, 1);
                if (GUILayout.Button($"×10 ({BigNumberFormatter.Format(station.GetCost(10))})"))
                    _stations.TryBuy(index, 10);

                int max = station.GetMaxAffordable(_wallet.Coins);
                if (GUILayout.Button($"×Max ({max})") && max > 0)
                    _stations.TryBuy(index, max);
            }
            GUILayout.EndHorizontal();
        }
    }
}
