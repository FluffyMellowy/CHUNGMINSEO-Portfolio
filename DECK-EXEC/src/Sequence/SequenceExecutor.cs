namespace Colorless.Sequence
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using R3;
    using UnityEngine;
    using VContainer;
    using Colorless.Card;
    using Colorless.Entity;
    using Colorless.Grid;

    /// <summary>
    /// RUN ボタンが押されたとき、SequenceQueue を順次実行する。
    /// カード 1 枚実行ごとに TurnManager.ProcessTurn() で敵を 1 ターン進める。
    /// プレイヤー死亡・全カード実行後の判定は MissionManager がイベント購読して扱う。
    /// </summary>
    public sealed class SequenceExecutor
    {
        private readonly SequenceQueue _queue;
        private readonly GameContext _ctx;
        private readonly EntityCollisionResolver _collision;
        private readonly Subject<Unit> _onRunStarted = new();
        private readonly Subject<Unit> _onCardExecuted = new();
        private readonly Subject<Unit> _onSequenceFinished = new();
        private readonly Subject<Unit> _onPlayerDied = new();

        private CancellationTokenSource _cts;
        private bool _isRunning;

        /// <summary>履歴上限。これを超えると古い実行が破棄される。</summary>
        public const int MaxHistory = 20;

        private readonly List<QueuedCard[]> _history = new();
        private readonly Subject<Unit> _onHistoryChanged = new();

        /// <summary>RUN ボタン押下直後に発火。StageStateOverlay や ActionLog 自動切替が購読する。</summary>
        public Observable<Unit> OnRunStarted => _onRunStarted;
        public Observable<Unit> OnCardExecuted => _onCardExecuted;
        public Observable<Unit> OnSequenceFinished => _onSequenceFinished;
        public Observable<Unit> OnPlayerDied => _onPlayerDied;

        /// <summary>履歴が変更された通知（追加・クリア）。UI が購読して再描画。</summary>
        public Observable<Unit> OnHistoryChanged => _onHistoryChanged;
        public bool IsRunning => _isRunning;

        /// <summary>
        /// このミッション内で実行された全シーケンスの履歴。
        /// 古い順 → 新しい順。最後の要素が直近実行分。
        /// </summary>
        public IReadOnlyList<QueuedCard[]> History => _history;

        /// <summary>履歴の最後（直近実行分）を返す。空なら空配列。</summary>
        public IReadOnlyList<QueuedCard> LastExecuted =>
            _history.Count > 0 ? _history[_history.Count - 1]
                               : System.Array.Empty<QueuedCard>();

        /// <summary>履歴を空にする。ミッション開始時に呼び出す。</summary>
        public void ClearHistory()
        {
            if (_history.Count == 0) return;
            _history.Clear();
            _onHistoryChanged.OnNext(Unit.Default);
        }

        /// <summary>後方互換: 旧 ClearLastExecuted エイリアス。</summary>
        public void ClearLastExecuted() => ClearHistory();

        [Inject]
        public SequenceExecutor(SequenceQueue queue, GameContext ctx, EntityCollisionResolver collision)
        {
            _queue = queue;
            _ctx = ctx;
            _collision = collision;
        }

        /// <summary>
        /// 現在のキューを順次実行する。実行中なら何もしない。
        /// </summary>
        public async UniTask RunAsync()
        {
            if (_isRunning) return;
            if (_queue.Count == 0) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            /* ログをクリアしてから RunStarted 発火（UI 自動切替がこの順を期待） */
            _ctx.Logger?.Clear();
            _onRunStarted.OnNext(Unit.Default);

            try
            {
                QueuedCard[] snapshot = _queue.Snapshot();
                /* 履歴に追加（古い分は MaxHistory 超過時に破棄） */
                _history.Add(snapshot);
                if (_history.Count > MaxHistory) _history.RemoveAt(0);
                _onHistoryChanged.OnNext(Unit.Default);

                foreach (QueuedCard q in snapshot)
                {
                    if (q.Card == null || q.Card.Effect == null) continue;

                    /* カード効果を実行 */
                    await q.Card.Effect.ExecuteAsync(_ctx, q.Direction, _cts.Token);

                    /* 敵 1 ターン進行 → 衝突解決 */
                    _ctx.Turn.ProcessTurn();
                    _collision.ResolveCollisions();

                    _onCardExecuted.OnNext(Unit.Default);

                    /* プレイヤー死亡判定（敵 / Hazard 共通） */
                    bool diedByEnemy = IsPlayerOnEnemy();
                    bool diedByHazard = !diedByEnemy && IsPlayerOnHazard();
                    if (diedByEnemy || diedByHazard)
                    {
                        /* 死亡 narration */
                        LogColorPalette p = _ctx.Palette;
                        if (p != null)
                        {
                            string causeLabel = diedByHazard ? p.T_Danger("Spike") : p.T_Enemy("Enemy");
                            _ctx.Logger?.Log($"{p.T_Player("Player")}が{causeLabel}と{p.T_Verb("衝突")}、{p.T_Danger("シーケンス中断")}");
                        }
                        _onPlayerDied.OnNext(Unit.Default);
                        return;
                    }
                }

                _queue.Clear();
                _onSequenceFinished.OnNext(Unit.Default);
            }
            catch (OperationCanceledException) { /* キャンセル時は静かに終了 */ }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _isRunning = false;
            }
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// プレイヤーが現在敵と同セルにいるか。
        /// 敵 AI がプレイヤーセルに突っ込んだ直後の死亡判定に使う。
        /// </summary>
        private bool IsPlayerOnEnemy()
        {
            GridCell cell = _ctx.Grid.GetCell(_ctx.Player.GridPosition);
            if (cell == null) return false;
            if (cell.Occupant == null) return false;
            if (cell.Occupant == _ctx.Player.gameObject) return false;

            /* IEntity は敵を識別するマーカー（Box は実装していない） */
            return cell.Occupant.GetComponent<IEntity>() != null;
        }

        /// <summary>
        /// プレイヤーが現在 Hazard セル（SpikeTrap 等）にいるか。
        /// MoveEffect / DashEffect が Hazard セルへの進入をブロックしない（Phase 14-F で対応予定）ため、
        /// ここで進入後の死亡判定として拾う。
        /// </summary>
        private bool IsPlayerOnHazard()
        {
            GridCell cell = _ctx.Grid.GetCell(_ctx.Player.GridPosition);
            return cell != null && cell.Hazard != null;
        }
    }
}
