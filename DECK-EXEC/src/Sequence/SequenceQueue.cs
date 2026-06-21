namespace Colorless.Sequence
{
    using System.Collections.Generic;
    using R3;
    using Colorless.Card;

    /// <summary>
    /// プレイヤーが入力した未実行のカード列を保持する。
    /// 追加・削除・順序変更時に Changed を発火し、UI とプレビューが自動更新される。
    /// </summary>
    public sealed class SequenceQueue
    {
        private readonly List<QueuedCard> _items = new();
        private readonly Subject<Unit> _changed = new();

        public Observable<Unit> Changed => _changed;
        public int Count => _items.Count;
        public IReadOnlyList<QueuedCard> Items => _items;

        public void Enqueue(QueuedCard card)
        {
            _items.Add(card);
            _changed.OnNext(Unit.Default);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            _items.RemoveAt(index);
            _changed.OnNext(Unit.Default);
        }

        public void Reorder(int from, int to)
        {
            if (from < 0 || from >= _items.Count) return;
            if (to < 0 || to >= _items.Count) return;
            if (from == to) return;
            QueuedCard moved = _items[from];
            _items.RemoveAt(from);
            _items.Insert(to, moved);
            _changed.OnNext(Unit.Default);
        }

        /// <summary>指定インデックスのカードを上書き（方向再選択など）。</summary>
        public void Replace(int index, QueuedCard newCard)
        {
            if (index < 0 || index >= _items.Count) return;
            _items[index] = newCard;
            _changed.OnNext(Unit.Default);
        }

        public void Clear()
        {
            if (_items.Count == 0) return;
            _items.Clear();
            _changed.OnNext(Unit.Default);
        }

        /// <summary>反復処理用にコピーを返す（実行中の変更耐性）。</summary>
        public QueuedCard[] Snapshot() => _items.ToArray();
    }
}
