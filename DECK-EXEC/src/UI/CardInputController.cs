namespace Colorless.UI
{
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Card;
    using Colorless.Sequence;

    /// <summary>
    /// カード選択フローの統合コントローラ。
    /// CardHandUI からのクリックを受け取り、方向選択が必要なら DirectionSelectorUI を経由してから
    /// SequenceQueue へ追加する。
    /// </summary>
    public sealed class CardInputController : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private DirectionSelectorUI _directionSelector;

        [Inject] private CardHand _hand;
        [Inject] private SequenceQueue _queue;
        [Inject] private SequenceExecutor _executor;

        private bool _isSelecting;

        public void OnCardClicked(Card card)
        {
            if (card == null) return;
            if (_isSelecting) return;
            /* 実行中は入力受付けない */
            if (_executor.IsRunning) return;
            /* 残量 0 のカードは追加できない */
            if (_hand.Get(card) <= 0) return;

            if (card.RequiresDirection)
                HandleDirectionalAsync(card).Forget();
            else
                Enqueue(card, null);
        }

        private async UniTaskVoid HandleDirectionalAsync(Card card)
        {
            _isSelecting = true;
            try
            {
                Vector2Int? dir = await _directionSelector.SelectAsync();
                if (dir == null) return;
                /* 選択中に残数が 0 になることはないが念のため再チェック */
                if (_hand.Get(card) <= 0) return;
                Enqueue(card, dir.Value);
            }
            finally
            {
                _isSelecting = false;
            }
        }

        private void Enqueue(Card card, Vector2Int? direction)
        {
            if (!_hand.TryConsume(card)) return;
            _queue.Enqueue(new QueuedCard(card, direction));
        }
    }
}
