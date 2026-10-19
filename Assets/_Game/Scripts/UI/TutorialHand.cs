using ShaurmaTycoon.Core;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Onboarding;
using TMPro;
using UnityEngine;

namespace ShaurmaTycoon.UI
{
    /// <summary>
    /// Рука-подсказка онбординга (этап 11, GDD §9): показывает пальцем, что нажать на текущем шаге TutorialService,
    /// с короткой подписью. Появляется, только когда шаг можно сделать сейчас: не хватает монет — руки нет;
    /// нужная вкладка закрыта — на кнопку вкладки. Пока открыто любое окно, рука скрыта.
    /// Все картинки без raycastTarget — рука не мешает нажимать то, на что показывает.
    /// </summary>
    public sealed class TutorialHand : MonoBehaviour, ITickable, ISceneBinding
    {
        private const int StationsTab = 0;
        private const int ManagersTab = 1;
        private const int FirstStation = 0;
        private const int SecondStation = 1;

        [SerializeField] private GameObject _root = null;
        [Tooltip("Рука; pivot — кончик пальца.")]
        [SerializeField] private RectTransform _hand = null;
        [Tooltip("Расходящееся кольцо в точке нажатия.")]
        [SerializeField] private RectTransform _ring = null;
        [SerializeField] private CanvasGroup _ringGroup = null;
        [SerializeField] private RectTransform _bubble = null;
        [SerializeField] private TMP_Text _hintText = null;
        [Tooltip("Корни окон: пока какое-то открыто, подсказка скрыта.")]
        [SerializeField] private GameObject[] _blockers = new GameObject[0];
        [Tooltip("Пауза перед появлением руки на новом шаге, с.")]
        [SerializeField] private float _appearDelay = 0.6f;
        [SerializeField] private float _bobAmplitude = 22f;
        [SerializeField] private float _bobSpeed = 6f;
        [SerializeField] private float _bubbleOffset = 190f;

        private TutorialService _tutorial;
        private StationsPanel _stations;
        private ManagersPanel _managers;
        private TabBar _tabs;
        private Loc _loc;
        private RectTransform _area;

        private string _shownKey;
        private float _time;
        private float _visibleAfter;

        public void Bind(TutorialService tutorial, StationsPanel stations, ManagersPanel managers, TabBar tabs, Loc loc)
        {
            SceneBindings.Track(this);
            _tutorial = tutorial;
            _stations = stations;
            _managers = managers;
            _tabs = tabs;
            _loc = loc;
            _area = (RectTransform)transform;

            _tutorial.StepChanged += OnStepChanged;
            _loc.LanguageChanged += OnLanguageChanged;
            _visibleAfter = _appearDelay;
            _root.SetActive(false);
        }

        public void Tick(float deltaTime)
        {
            if (_tutorial == null)
                return;

            // Время подсказки не зависит от отладочного ускорения игры.
            _time += Time.unscaledDeltaTime;

            RectTransform target = null;
            string key = null;
            if (_tutorial.IsActive && _time >= _visibleAfter && !IsBlocked())
                ResolveTarget(out target, out key);

            bool visible = target != null && target.gameObject.activeInHierarchy;
            if (_root.activeSelf != visible)
                _root.SetActive(visible);
            if (!visible)
                return;

            if (!ReferenceEquals(key, _shownKey))
            {
                _shownKey = key;
                _hintText.text = _loc.Get(key);
            }

            Place(target);
        }

        private void ResolveTarget(out RectTransform target, out string key)
        {
            target = null;
            key = null;
            TutorialStep step = _tutorial.Step;

            // Рука появляется, только когда шаг можно сделать прямо сейчас (хватает монет), а не висит всё время.
            if (!_tutorial.CanDoCurrentStep)
                return;

            if (step == TutorialStep.HireManager)
            {
                if (_tabs.SelectedIndex != ManagersTab)
                {
                    target = _tabs.GetButton(ManagersTab);
                    key = "tutorial.open_managers";
                    return;
                }

                ManagerRowView managerRow = _managers.GetRow(FirstStation);
                target = managerRow != null ? managerRow.HireTarget : null;
                key = "tutorial.hire_manager";
                return;
            }

            if (_tabs.SelectedIndex != StationsTab)
            {
                target = _tabs.GetButton(StationsTab);
                key = "tutorial.open_stations";
                return;
            }

            StationRowView first = _stations.GetRow(FirstStation);
            if (first == null)
                return;

            if (step == TutorialStep.Tap)
            {
                target = first.TapTarget;
                key = "tutorial.tap";
                return;
            }

            if (step == TutorialStep.BuyLevel)
            {
                target = first.BuyTarget;
                key = "tutorial.buy_level";
            }
            else if (step == TutorialStep.UnlockStation)
            {
                StationRowView second = _stations.GetRow(SecondStation);
                target = second != null ? second.UnlockTarget : null;
                key = "tutorial.unlock_station";
            }
        }

        private void Place(RectTransform target)
        {
            // Центр прямоугольника цели, а не её pivot: у кнопок справа в строке pivot бывает на краю.
            Vector3 local = _area.InverseTransformPoint(target.TransformPoint(target.rect.center));
            float bob = Mathf.Abs(Mathf.Sin(_time * _bobSpeed)) * _bobAmplitude;
            _hand.localPosition = new Vector3(local.x, local.y - bob, 0f);

            // Ладонь уходит от кончика пальца вбок; в правой половине экрана отражаем руку,
            // чтобы она лежала внутри экрана, а не уезжала за край (под рекламный блок Яндекса).
            Rect area = _area.rect;
            float side = local.x > area.center.x ? -1f : 1f;
            _hand.localScale = new Vector3(side, 1f, 1f);

            float phase = Mathf.Repeat(_time * _bobSpeed / Mathf.PI, 1f);
            float ringScale = 0.6f + phase * 0.8f;
            _ring.localPosition = new Vector3(local.x, local.y, 0f);
            _ring.localScale = new Vector3(ringScale, ringScale, 1f);
            _ringGroup.alpha = 1f - phase;

            // Подпись — над целью в нижней половине экрана и под ней в верхней, не выходя за края.
            float halfWidth = _bubble.rect.width * 0.5f;
            float x = Mathf.Clamp(local.x, area.xMin + halfWidth + 16f, area.xMax - halfWidth - 16f);
            float y = local.y < area.center.y ? local.y + _bubbleOffset : local.y - _bubbleOffset - 60f;
            _bubble.localPosition = new Vector3(x, y, 0f);
        }

        private bool IsBlocked()
        {
            for (int i = 0; i < _blockers.Length; i++)
            {
                if (_blockers[i] != null && _blockers[i].activeInHierarchy)
                    return true;
            }
            return false;
        }

        private void OnStepChanged(TutorialStep step)
        {
            _visibleAfter = _time + _appearDelay;
        }

        private void OnLanguageChanged()
        {
            _shownKey = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Unbind()
        {
            if (_tutorial == null)
                return;

            _tutorial.StepChanged -= OnStepChanged;
            _loc.LanguageChanged -= OnLanguageChanged;
            _tutorial = null;
        }
    }
}
