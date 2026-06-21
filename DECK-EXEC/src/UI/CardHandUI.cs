namespace Colorless.UI
{
    using System;
    using System.Collections.Generic;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Card;
    using Colorless.Sequence;

    /// <summary>
    /// 右下の手札パネル。CardHand を購読してカードボタンを動的生成する。
    /// </summary>
    public sealed class CardHandUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private CardButton _buttonPrefab;
        [Required, SerializeField] private RectTransform _container;

        [Inject] private CardHand _hand;
        [Inject] private CardInputController _input;

        private readonly List<CardButton> _spawned = new();
        private IDisposable _handSubscription;

        private void Start()
        {
            _handSubscription = _hand.Changed.Subscribe(_ => Rebuild());
            Rebuild();
        }

        private void OnDestroy()
        {
            _handSubscription?.Dispose();
        }

        private void Rebuild()
        {
            /* 既存ボタン破棄 */
            foreach (CardButton b in _spawned)
                if (b != null) Destroy(b.gameObject);
            _spawned.Clear();

            /* 新規生成 */
            foreach (Card card in _hand.Cards)
            {
                if (card == null) continue;
                CardButton btn = Instantiate(_buttonPrefab, _container);
                btn.Setup(card, _hand, _input.OnCardClicked);
                _spawned.Add(btn);
            }
        }
    }
}
