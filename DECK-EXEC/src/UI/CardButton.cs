namespace Colorless.UI
{
    using System;
    using MoreMountains.Feedbacks;
    using R3;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Colorless.Card;
    using Colorless.Sequence;

    /// <summary>
    /// 手札に並ぶカード 1 枚分のボタン UI。CardHandUI が動的に生成する。
    /// </summary>
    public sealed class CardButton : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private Button _button;
        [Required, SerializeField] private Image _iconImage;
        [Required, SerializeField] private TextMeshProUGUI _nameLabel;
        [Required, SerializeField] private TextMeshProUGUI _countLabel;

        [Title("Feedback")]
        [InfoBox("クリック時に再生する MMF_Player（任意）。未設定なら音は鳴らない。")]
        [SerializeField] private MMF_Player _clickFeedback;

        private Card _card;
        private Action<Card> _onClicked;
        private IDisposable _countSubscription;

        public Card Card => _card;

        public void Setup(Card card, CardHand hand, Action<Card> onClicked)
        {
            _card = card;
            _onClicked = onClicked;

            if (card.Icon != null) _iconImage.sprite = card.Icon;
            _nameLabel.text = card.DisplayName;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(HandleClick);

            /* 残数の購読：表示と active 状態を自動更新 */
            _countSubscription?.Dispose();
            _countSubscription = hand.CountOf(card)?.Subscribe(c =>
            {
                _countLabel.text = $"×{c}";
                _button.interactable = c > 0;
            });
        }

        private void OnDestroy()
        {
            _countSubscription?.Dispose();
        }

        private void HandleClick()
        {
            if (_clickFeedback != null) _clickFeedback.PlayFeedbacks();
            _onClicked?.Invoke(_card);
        }
    }
}
