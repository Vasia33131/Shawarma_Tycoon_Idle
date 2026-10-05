using ShaurmaTycoon.Economy;
using ShaurmaTycoon.Retention;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShaurmaTycoon.UI
{
    /// <summary>Значок награды (монета, купюра, репутация, скин) и сумма «+X» рядом — без слов, чтобы не путаться в падежах.</summary>
    public sealed class RewardIconView : MonoBehaviour
    {
        [SerializeField] private Image _icon = null;
        [SerializeField] private Image _mark = null;
        [SerializeField] private TMP_Text _amountText = null;
        [Tooltip("Нарисованный значок поверх кружка; есть картинка — кружок и метка прячутся.")]
        [SerializeField] private Image _art = null;
        [SerializeField] private Sprite _coinSprite = null;
        [SerializeField] private Sprite _billSprite = null;
        [SerializeField] private Sprite _reputationSprite = null;

        [SerializeField] private Color _coinColor = new Color(1f, 0.80f, 0.20f);
        [SerializeField] private Color _coinMark = new Color(1f, 0.65f, 0.10f);
        [SerializeField] private Color _billColor = new Color(0.25f, 0.62f, 0.33f);
        [SerializeField] private Color _billMark = new Color(0.72f, 0.90f, 0.70f);
        [SerializeField] private Color _reputationColor = new Color(0.55f, 0.35f, 0.85f);
        [SerializeField] private Color _reputationMark = new Color(0.80f, 0.70f, 1f);
        [SerializeField] private Color _skinColor = new Color(0.93f, 0.68f, 0.12f);
        [SerializeField] private Color _skinMark = new Color(0.85f, 0.30f, 0.20f);

        private readonly NumberText _amount = new NumberText();

        /// <param name="skinLabel">Подпись для скина (вместо суммы).</param>
        public void Set(RewardGrant reward, string skinLabel)
        {
            switch (reward.Kind)
            {
                case RewardKind.Bills:
                    Apply(_billColor, _billMark, _billSprite);
                    break;
                case RewardKind.Reputation:
                    Apply(_reputationColor, _reputationMark, _reputationSprite);
                    break;
                case RewardKind.KioskSkin:
                    Apply(_skinColor, _skinMark, null);
                    _amountText.text = skinLabel;
                    return;
                default:
                    Apply(_coinColor, _coinMark, _coinSprite);
                    break;
            }

            _amountText.SetNumberText(_amount.Clear().Append('+').AppendNumber(reward.Amount));
        }

        private void Apply(Color icon, Color mark, Sprite art)
        {
            bool hasArt = art != null && _art != null;
            _icon.color = icon;
            _mark.color = mark;
            _icon.enabled = !hasArt;
            _mark.enabled = !hasArt;
            if (_art == null)
                return;
            _art.sprite = art;
            _art.enabled = hasArt;
        }
    }
}
