namespace Colorless.UI
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using R3;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using VContainer;
    using Colorless.Card;
    using Colorless.Sequence;

    /// <summary>
    /// 右上の CMD ログ UI。SequenceQueue を購読してテキスト表示する。
    /// 各行クリックでキューから削除可能。
    /// </summary>
    public sealed class SequenceQueueUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private RectTransform _container;
        [Required, SerializeField] private QueueLineUI _linePrefab;

        [Inject] private SequenceQueue _queue;
        [Inject] private CardHand _hand;

        private readonly List<QueueLineUI> _spawned = new();
        private IDisposable _subscription;

        private void Start()
        {
            _subscription = _queue.Changed.Subscribe(_ => Rebuild());
            Rebuild();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }

        private void Rebuild()
        {
            foreach (QueueLineUI line in _spawned)
                if (line != null) Destroy(line.gameObject);
            _spawned.Clear();

            for (int i = 0; i < _queue.Count; i++)
            {
                QueuedCard q = _queue.Items[i];
                QueueLineUI line = Instantiate(_linePrefab, _container);
                int capturedIndex = i;
                line.Setup(FormatLine(q), () => OnRemoveClicked(capturedIndex, q.Card));
                _spawned.Add(line);
            }
        }

        private void OnRemoveClicked(int index, Card card)
        {
            _queue.RemoveAt(index);
            if (card != null) _hand.Restore(card);
        }

        /// <summary>
        /// `> Move(↑)` 形式の文字列を生成。
        /// </summary>
        private static string FormatLine(QueuedCard q)
        {
            if (q.Card == null) return "> ???";
            StringBuilder sb = new();
            sb.Append("> ").Append(q.Card.DisplayName);
            if (q.Direction.HasValue)
                sb.Append("(").Append(ArrowOf(q.Direction.Value)).Append(")");
            return sb.ToString();
        }

        private static string ArrowOf(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return "↑";
            if (dir == Vector2Int.down) return "↓";
            if (dir == Vector2Int.left) return "←";
            if (dir == Vector2Int.right) return "→";
            return "?";
        }
    }
}
