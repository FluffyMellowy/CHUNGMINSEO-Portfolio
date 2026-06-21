namespace Colorless.Sequence
{
    using System.Collections.Generic;
    using R3;
    using Colorless.Card;

    /// <summary>
    /// 現ミッションで使用可能なカードと残数を保持する。
    /// UI は CountOf(card) を購読して残数表示を自動更新する。
    /// </summary>
    public sealed class CardHand
    {
        private readonly Dictionary<Card, ReactiveProperty<int>> _counts = new();
        private readonly Subject<Unit> _changed = new();

        /// <summary>カード一覧の変化通知（追加・削除を含む構造変化）。</summary>
        public Observable<Unit> Changed => _changed;

        /// <summary>登録されているカード列挙。</summary>
        public IEnumerable<Card> Cards => _counts.Keys;

        /// <summary>
        /// ミッション開始時に手札を初期化する。
        /// 既存の購読は無効化されないが、内部 ReactiveProperty は再生成される。
        /// </summary>
        public void Initialize(IEnumerable<(Card card, int count)> entries)
        {
            /* 既存の ReactiveProperty を破棄してリセット */
            foreach (ReactiveProperty<int> rp in _counts.Values) rp.Dispose();
            _counts.Clear();

            foreach (var (card, count) in entries)
            {
                if (card == null) continue;
                _counts[card] = new ReactiveProperty<int>(count);
            }

            _changed.OnNext(Unit.Default);
        }

        /// <summary>指定カードの残数を購読可能なプロパティとして取得。未登録は null。</summary>
        public ReadOnlyReactiveProperty<int> CountOf(Card card)
        {
            return _counts.TryGetValue(card, out ReactiveProperty<int> rp)
                ? rp.ToReadOnlyReactiveProperty()
                : null;
        }

        /// <summary>残数を取得（購読不要な場合）。</summary>
        public int Get(Card card)
        {
            return _counts.TryGetValue(card, out ReactiveProperty<int> rp) ? rp.Value : 0;
        }

        /// <summary>残数を 1 消費する。0 ならば false を返し変更しない。</summary>
        public bool TryConsume(Card card)
        {
            if (!_counts.TryGetValue(card, out ReactiveProperty<int> rp)) return false;
            if (rp.Value <= 0) return false;
            rp.Value--;
            return true;
        }

        /// <summary>残数を 1 戻す（キュー削除・ロールバック時）。</summary>
        public void Restore(Card card)
        {
            if (!_counts.TryGetValue(card, out ReactiveProperty<int> rp)) return;
            rp.Value++;
        }

        /// <summary>スナップショット作成（ロールバック用）。</summary>
        public Dictionary<Card, int> CaptureSnapshot()
        {
            var snapshot = new Dictionary<Card, int>(_counts.Count);
            foreach (var (card, rp) in _counts)
                snapshot[card] = rp.Value;
            return snapshot;
        }

        /// <summary>スナップショットから復元（カード構成は変えずに残数のみ戻す）。</summary>
        public void RestoreFromSnapshot(Dictionary<Card, int> snapshot)
        {
            if (snapshot == null) return;
            foreach (var (card, count) in snapshot)
            {
                if (_counts.TryGetValue(card, out ReactiveProperty<int> rp))
                    rp.Value = count;
            }
        }
    }
}
