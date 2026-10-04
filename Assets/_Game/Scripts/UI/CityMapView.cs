using System;
using System.Collections.Generic;
using System.Globalization;
using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Prestige;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Карта филиалов во вкладке «Город»: города стоят по CityConfig.MapPosition и соединены дорогой из точек.
    /// Фургон на дороге к следующему городу показывает прогресс к порогу. Тап по городу — описание в шапке карты.
    /// Всё позиционируется якорями (доли области карты), поэтому раскладка не зависит от размера экрана.
    /// </summary>
    public sealed class CityMapView : MonoBehaviour, ISceneBinding
    {
        // Фургон и закрашенная часть дороги идут между краями кружков, а не между центрами.
        private const float RouteStart = 0.15f;
        private const float RouteEnd = 0.85f;

        [SerializeField] private RectTransform _area = null;
        [SerializeField] private CityMapNodeView _nodePrefab = null;
        [SerializeField] private Image _roadDotPrefab = null;
        [SerializeField] private RectTransform _traveler = null;
        [SerializeField] private TMP_Text _selectedNameText = null;
        [SerializeField] private TMP_Text _selectedInfoText = null;
        [Tooltip("Примерный размер области карты в единицах канваса — по нему считается шаг точек дороги.")]
        [SerializeField] private Vector2 _referenceSize = new Vector2(960f, 640f);
        [SerializeField] private float _dotSpacing = 36f;

        [Header("Road colors")]
        [SerializeField] private Color _roadDoneColor = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color _roadProgressColor = new Color(0.55f, 0.35f, 0.85f);
        [SerializeField] private Color _roadAheadColor = new Color(0.36f, 0.18f, 0.07f, 0.45f);
        [SerializeField] private Color _roadLockedColor = new Color(0.36f, 0.18f, 0.07f, 0.18f);

        private readonly List<Image> _routeDots = new List<Image>();
        private readonly List<float> _routeDotT = new List<float>();
        private readonly List<CityMapNodeView> _nodes = new List<CityMapNodeView>();

        private IReadOnlyList<CityConfig> _cities;
        private int _currentIndex;
        private int _selectedIndex = -1;
        private Loc _loc;
        private float _shownProgress = -1f;

        private bool HasNext => _currentIndex + 1 < _cities.Count;

        public void Bind(IReadOnlyList<CityConfig> cities, int currentIndex, Loc loc)
        {
            SceneBindings.Track(this);
            _cities = cities;
            _currentIndex = Mathf.Clamp(currentIndex, 0, cities.Count - 1);
            _loc = loc;

            BuildRoad();
            BuildNodes();
            _traveler.gameObject.SetActive(HasNext);
            _traveler.SetAsLastSibling();

            _loc.LanguageChanged += RefreshTexts;
            Select(HasNext ? _currentIndex + 1 : _currentIndex);
            SetProgress(0f);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_loc != null)
                _loc.LanguageChanged -= RefreshTexts;
        }

        /// <summary>Доля пути к порогу текущего города, 0..1.</summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (!HasNext || Mathf.Approximately(progress, _shownProgress))
                return;

            _shownProgress = progress;
            float t = Mathf.Lerp(RouteStart, RouteEnd, progress);
            for (int i = 0; i < _routeDots.Count; i++)
                _routeDots[i].color = _routeDotT[i] <= t ? _roadProgressColor : _roadAheadColor;

            Vector2 from = _cities[_currentIndex].MapPosition;
            Vector2 to = _cities[_currentIndex + 1].MapPosition;
            Vector2 anchor = Vector2.Lerp(from, to, t);
            _traveler.anchorMin = anchor;
            _traveler.anchorMax = anchor;
            _traveler.anchoredPosition = Vector2.zero;
            // Фургон смотрит в сторону движения.
            _traveler.localScale = new Vector3(to.x >= from.x ? 1f : -1f, 1f, 1f);
        }

        private void BuildRoad()
        {
            for (int segment = 0; segment + 1 < _cities.Count; segment++)
            {
                Vector2 from = _cities[segment].MapPosition;
                Vector2 to = _cities[segment + 1].MapPosition;
                float length = Vector2.Scale(to - from, _referenceSize).magnitude;
                int steps = Mathf.Max(2, Mathf.RoundToInt(length / _dotSpacing));

                Color color = segment < _currentIndex ? _roadDoneColor : _roadLockedColor;
                for (int k = 1; k < steps; k++)
                {
                    float t = (float)k / steps;
                    Image dot = Instantiate(_roadDotPrefab, _area);
                    dot.name = $"Road_{segment}_{k}";
                    Place(dot.rectTransform, Vector2.Lerp(from, to, t));
                    dot.color = color;

                    if (segment == _currentIndex)
                    {
                        _routeDots.Add(dot);
                        _routeDotT.Add(t);
                    }
                }
            }
        }

        private void BuildNodes()
        {
            for (int i = 0; i < _cities.Count; i++)
            {
                CityMapNodeView node = Instantiate(_nodePrefab, _area);
                node.name = $"City_{i + 1}_{_cities[i].Id}";
                Place((RectTransform)node.transform, _cities[i].MapPosition);
                node.Setup(i + 1, StateOf(i), _cities[i].BoothColor);

                int index = i;
                node.Button.onClick.AddListener(() => Select(index));
                _nodes.Add(node);
            }
        }

        private CityMapNodeState StateOf(int index)
        {
            if (index < _currentIndex)
                return CityMapNodeState.Done;
            if (index == _currentIndex)
                return CityMapNodeState.Current;
            return index == _currentIndex + 1 ? CityMapNodeState.Next : CityMapNodeState.Locked;
        }

        private void Select(int index)
        {
            _selectedIndex = index;
            for (int i = 0; i < _nodes.Count; i++)
                _nodes[i].SetSelected(i == index);
            RefreshTexts();
        }

        private void RefreshTexts()
        {
            for (int i = 0; i < _nodes.Count; i++)
                _nodes[i].SetName(_loc.Get(_cities[i].NameKey));

            if (_selectedIndex < 0)
                return;

            CityConfig city = _cities[_selectedIndex];
            _selectedNameText.text = _loc.Get(city.NameKey);
            string status = _loc.Get(StatusKey(StateOf(_selectedIndex)));
            string details = _loc.Format("map.details", BigNumberFormatter.Format(city.PrestigeThreshold),
                FormatScale(city.ValueScale));
            _selectedInfoText.text = status + " · " + details;
        }

        // Масштаб города — целое «×10 000» (суффиксы K/M здесь читаются хуже).
        private static string FormatScale(double scale)
        {
            return scale < 1e9
                ? Math.Round(scale).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ')
                : BigNumberFormatter.Format(scale);
        }

        private static string StatusKey(CityMapNodeState state)
        {
            switch (state)
            {
                case CityMapNodeState.Done:
                    return "map.status_done";
                case CityMapNodeState.Current:
                    return "map.status_current";
                case CityMapNodeState.Next:
                    return "map.status_next";
                default:
                    return "map.status_locked";
            }
        }

        private static void Place(RectTransform rect, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
