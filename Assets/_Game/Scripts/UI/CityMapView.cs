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
    /// Карта филиалов во вкладке «Город»: города стоят по CityConfig.MapPosition и соединены извилистой дорогой
    /// из штрихов (квадратичная кривая Безье между соседями, изгиб чередуется). Фургон едет по кривой к следующему
    /// городу и показывает прогресс к порогу. Тап по городу — описание в плашке сверху.
    /// Всё позиционируется якорями (доли области карты), поэтому раскладка не зависит от размера экрана.
    /// </summary>
    public sealed class CityMapView : MonoBehaviour, ISceneBinding
    {
        // Изгиб дороги: сдвиг середины по нормали, доля длины отрезка.
        private const float CurveBend = 0.12f;
        private const float MaxTravelerTilt = 15f;
        private const int LengthSamples = 16;

        [SerializeField] private RectTransform _area = null;
        [SerializeField] private CityMapNodeView _nodePrefab = null;
        [SerializeField] private Image _roadDotPrefab = null;
        [SerializeField] private RectTransform _traveler = null;
        [SerializeField] private TMP_Text _selectedNameText = null;
        [SerializeField] private TMP_Text _selectedInfoText = null;
        [Tooltip("Миниатюра выбранного города в плашке. Пусто или у города нет миниатюры — скрыта.")]
        [SerializeField] private Image _selectedIcon = null;
        [Tooltip("Статус выбранного города отдельной строкой. Пусто — статус в начале строки описания.")]
        [SerializeField] private TMP_Text _selectedStatusText = null;
        [SerializeField] private Color _statusColor = new Color(0.45f, 0.35f, 0.28f);
        [SerializeField] private Color _nextStatusColor = new Color(0.95f, 0.55f, 0.15f);
        [Tooltip("Размер области карты в единицах канваса — по нему считаются шаг штрихов и изгиб дороги.")]
        [SerializeField] private Vector2 _referenceSize = new Vector2(960f, 640f);
        [SerializeField] private float _dotSpacing = 30f;
        [Tooltip("Штрихов ближе этого расстояния (ед. канваса) к центру города нет — дорога начинается у края миниатюры.")]
        [SerializeField] private float _nodeClearance = 72f;

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
        // Отрезок к следующему городу в единицах _referenceSize и доли кривой у краёв миниатюр.
        private Vector2 _routeFrom;
        private Vector2 _routeControl;
        private Vector2 _routeTo;
        private float _routeStart = 0.15f;
        private float _routeEnd = 0.85f;

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
            float t = Mathf.Lerp(_routeStart, _routeEnd, progress);
            for (int i = 0; i < _routeDots.Count; i++)
                _routeDots[i].color = _routeDotT[i] <= t ? _roadProgressColor : _roadAheadColor;

            Place(_traveler, ToAnchor(Bezier(_routeFrom, _routeControl, _routeTo, t)));
            // Фургон смотрит в сторону движения и слегка наклоняется по дороге (на крутых участках — не больше ±15°).
            Vector2 tangent = BezierTangent(_routeFrom, _routeControl, _routeTo, t);
            bool right = _routeTo.x >= _routeFrom.x;
            float tilt = Mathf.Clamp(Mathf.Atan2(tangent.y, Mathf.Abs(tangent.x)) * Mathf.Rad2Deg, -MaxTravelerTilt, MaxTravelerTilt);
            _traveler.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
            _traveler.localRotation = Quaternion.Euler(0f, 0f, right ? tilt : -tilt);
        }

        private void BuildRoad()
        {
            for (int segment = 0; segment + 1 < _cities.Count; segment++)
            {
                Vector2 from = Vector2.Scale(_cities[segment].MapPosition, _referenceSize);
                Vector2 to = Vector2.Scale(_cities[segment + 1].MapPosition, _referenceSize);
                Vector2 chord = to - from;
                float sign = segment % 2 == 0 ? 1f : -1f;
                Vector2 control = (from + to) * 0.5f + new Vector2(-chord.y, chord.x) * (CurveBend * sign);
                float length = CurveLength(from, control, to);
                int steps = Mathf.Max(2, Mathf.RoundToInt(length / _dotSpacing));
                float clearance = length > 0f ? Mathf.Min(0.4f, _nodeClearance / length) : 0.4f;

                Color color = segment < _currentIndex ? _roadDoneColor : _roadLockedColor;
                for (int k = 1; k < steps; k++)
                {
                    float t = (float)k / steps;
                    if (t < clearance || t > 1f - clearance)
                        continue;

                    Image dot = Instantiate(_roadDotPrefab, _area);
                    dot.name = $"Road_{segment}_{k}";
                    Place(dot.rectTransform, ToAnchor(Bezier(from, control, to, t)));
                    Vector2 tangent = BezierTangent(from, control, to, t);
                    dot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
                    dot.color = color;

                    if (segment == _currentIndex)
                    {
                        _routeDots.Add(dot);
                        _routeDotT.Add(t);
                    }
                }

                if (segment == _currentIndex)
                {
                    _routeFrom = from;
                    _routeControl = control;
                    _routeTo = to;
                    _routeStart = clearance;
                    _routeEnd = 1f - clearance;
                }
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        private static Vector2 BezierTangent(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            return 2f * (1f - t) * (control - a) + 2f * t * (b - control);
        }

        private static float CurveLength(Vector2 a, Vector2 control, Vector2 b)
        {
            float length = 0f;
            Vector2 previous = a;
            for (int i = 1; i <= LengthSamples; i++)
            {
                Vector2 point = Bezier(a, control, b, (float)i / LengthSamples);
                length += (point - previous).magnitude;
                previous = point;
            }
            return length;
        }

        private Vector2 ToAnchor(Vector2 point)
        {
            return new Vector2(point.x / _referenceSize.x, point.y / _referenceSize.y);
        }

        private void BuildNodes()
        {
            for (int i = 0; i < _cities.Count; i++)
            {
                CityMapNodeView node = Instantiate(_nodePrefab, _area);
                node.name = $"City_{i + 1}_{_cities[i].Id}";
                Place((RectTransform)node.transform, _cities[i].MapPosition);
                node.Setup(i + 1, StateOf(i), _cities[i].BoothColor, _cities[i].MapIcon);

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
            CityMapNodeState state = StateOf(_selectedIndex);
            _selectedNameText.text = _loc.Get(city.NameKey);
            string status = _loc.Get(StatusKey(state));
            string details = _loc.Format("map.details", BigNumberFormatter.Format(city.PrestigeThreshold),
                FormatScale(city.ValueScale));

            if (_selectedStatusText != null)
            {
                _selectedStatusText.text = status;
                _selectedStatusText.color = state == CityMapNodeState.Next ? _nextStatusColor : _statusColor;
                _selectedInfoText.text = details;
            }
            else
            {
                _selectedInfoText.text = status + " · " + details;
            }

            if (_selectedIcon != null)
            {
                _selectedIcon.sprite = city.MapIcon;
                _selectedIcon.gameObject.SetActive(city.MapIcon != null);
            }
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
