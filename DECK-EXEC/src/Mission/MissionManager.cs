namespace Colorless.Mission
{
    using System.Collections.Generic;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Card;
    using Colorless.Entity;
    using Colorless.Entity.Enemy;
    using Colorless.Sequence;
    using Colorless.Stage;

    /// <summary>
    /// ステージ内のミッション群を順次進行し、失敗時はミッション開始時点へロールバックする。
    /// 初期化は StageInitializer から OnStageReady() で明示的にトリガーされる。
    /// </summary>
    public sealed class MissionManager : MonoBehaviour
    {
        [Title("Mission Order")]
        [InfoBox("このステージのミッションを順番に並べる")]
        [SerializeField] private List<Mission> _missions = new();

        [Inject] private GameContext _ctx;
        [Inject] private CardHand _hand;
        [Inject] private SequenceQueue _queue;
        [Inject] private SequenceExecutor _executor;
        [Inject] private StageManager _stage;

        [Title("Runtime State")]
        [ShowInInspector, ReadOnly] private int _currentIndex = -1;
        private MissionSnapshot _snapshot;
        private readonly List<EnemyBase> _enemies = new();
        private readonly List<Box> _boxes = new();
        private readonly CompositeDisposable _subscriptions = new();

        /* === ライフサイクルイベント（StageStateOverlay などが購読） === */
        private readonly Subject<MissionAdvancedInfo> _onMissionAdvanced = new();
        private readonly Subject<Unit> _onStageCleared = new();
        private readonly Subject<Unit> _onMissionFailed = new();

        /// <summary>ミッションがクリアされ、次のミッションに進んだ時に発火。</summary>
        public Observable<MissionAdvancedInfo> OnMissionAdvanced => _onMissionAdvanced;

        /// <summary>ステージの最後のミッションがクリアされた時に発火（stage.Clear() の直前）。</summary>
        public Observable<Unit> OnStageCleared => _onStageCleared;

        /// <summary>ミッション失敗でロールバックされた時に発火。</summary>
        public Observable<Unit> OnMissionFailed => _onMissionFailed;

        public Mission Current => (_currentIndex >= 0 && _currentIndex < _missions.Count)
            ? _missions[_currentIndex] : null;

        public int CurrentIndex => _currentIndex;
        public int MissionCount => _missions.Count;

        private void Start()
        {
            /* SequenceExecutor のイベントを購読（StageInitializer 完了前でも安全） */
            _executor.OnPlayerDied
                .Subscribe(_ => RollbackCurrentMission())
                .AddTo(_subscriptions);

            _executor.OnSequenceFinished
                .Subscribe(_ => HandleSequenceFinished())
                .AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        /// <summary>
        /// StageInitializer から全エンティティ初期化完了後に呼ばれる。
        /// シーン内のエンティティをキャッシュし、最初のミッションを開始する。
        /// </summary>
        public void OnStageReady()
        {
            CollectEntities();
            BeginMission(0);
        }

        private void CollectEntities()
        {
            _enemies.Clear();
            _boxes.Clear();
            foreach (EnemyBase e in FindObjectsByType<EnemyBase>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                _enemies.Add(e);
            foreach (Box b in FindObjectsByType<Box>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                _boxes.Add(b);
        }

        private void BeginMission(int idx)
        {
            if (idx < 0 || idx >= _missions.Count)
            {
                Debug.LogWarning($"[MissionManager] 無効なミッションインデックス: {idx}");
                return;
            }

            _currentIndex = idx;
            Mission m = _missions[idx];

            /* 手札を差し替え */
            var entries = new List<(Card, int)>(m.AvailableCards.Count);
            foreach (Mission.CardEntry e in m.AvailableCards)
                entries.Add((e.Card, e.Count));
            _hand.Initialize(entries);

            /* 初期 facing を反映（snapshot 前に） */
            if (_ctx?.Player != null && m.InitialFacing != Vector2Int.zero)
                _ctx.Player.SetFacing(m.InitialFacing);

            /* キュークリア */
            _queue.Clear();

            /* ミッション境界で履歴をクリア（混乱防止） */
            _executor.ClearHistory();

            /* スナップショット作成 */
            _snapshot = MissionSnapshot.Capture(_ctx, _hand, _enemies, _boxes);
        }

        public bool IsCurrentMissionCleared()
        {
            Mission m = Current;
            if (m == null || m.ClearCondition == null) return false;
            return m.ClearCondition.IsCleared(_ctx);
        }

        public void AdvanceMission()
        {
            bool wasLast = _currentIndex + 1 >= _missions.Count;
            int clearedIndex = _currentIndex;

            if (wasLast)
            {
                /* 最終ミッション → ステージクリア。発火順は overlay 表示優先で stage.Clear() の前。 */
                _onStageCleared.OnNext(Unit.Default);
                NarrateStageCleared();
                _stage.Clear();
                return;
            }

            /* 中間ミッション → 次へ */
            int nextIndex = _currentIndex + 1;
            _onMissionAdvanced.OnNext(new MissionAdvancedInfo(clearedIndex, nextIndex));
            NarrateMissionAdvanced(clearedIndex, nextIndex);
            BeginMission(nextIndex);
        }

        public void RollbackCurrentMission()
        {
            if (_snapshot == null) return;
            _snapshot.Restore(_ctx, _hand);
            _queue.Clear();

            _onMissionFailed.OnNext(Unit.Default);
            NarrateMissionFailed();
        }

        /// <summary>
        /// 直近実行したシーケンスをキューに復元する（履歴の最新項目）。
        /// </summary>
        public void RestoreLastSequence()
        {
            int last = _executor.History.Count - 1;
            if (last >= 0) RestoreFromHistory(last);
        }

        /// <summary>
        /// 履歴の指定 index のシーケンスをキューに復元する。
        /// 既にキューにあるカードはまず手札へ戻し、そこから履歴シーケンス分を再消費する。
        /// </summary>
        public void RestoreFromHistory(int index)
        {
            if (index < 0 || index >= _executor.History.Count) return;

            IReadOnlyList<QueuedCard> seq = _executor.History[index];
            if (seq == null || seq.Count == 0) return;

            /* 現在のキュー内カードを手札に戻す（残量保全） */
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                Card c = _queue.Items[i].Card;
                if (c != null) _hand.Restore(c);
            }
            _queue.Clear();

            /* 履歴シーケンスを手札から消費して enqueue */
            foreach (QueuedCard qc in seq)
            {
                if (qc.Card == null) continue;
                /* 残量があるカードだけ復元（不足なら飛ばす） */
                if (_hand.TryConsume(qc.Card))
                    _queue.Enqueue(qc);
            }
        }

        private void HandleSequenceFinished()
        {
            if (IsCurrentMissionCleared())
                AdvanceMission();
            else
                RollbackCurrentMission();
        }

        /* === narration ヘルパ（任意。Logger / Palette 無ければスキップ） === */

        private void NarrateMissionAdvanced(int clearedIndex, int nextIndex)
        {
            if (_ctx?.Logger == null || _ctx?.Palette == null) return;
            LogColorPalette p = _ctx.Palette;
            _ctx.Logger.Log($"{p.T_System($"[MISSION {clearedIndex + 1}]")} {p.T_Success("CLEAR")} → {p.T_System($"[MISSION {nextIndex + 1}]")}");
        }

        private void NarrateStageCleared()
        {
            if (_ctx?.Logger == null || _ctx?.Palette == null) return;
            LogColorPalette p = _ctx.Palette;
            _ctx.Logger.Log($"{p.T_System("[STAGE]")} {p.T_Success("CLEAR!")}");
        }

        private void NarrateMissionFailed()
        {
            if (_ctx?.Logger == null || _ctx?.Palette == null) return;
            LogColorPalette p = _ctx.Palette;
            _ctx.Logger.Log($"{p.T_System("[RESET]")} {p.T_Danger("ミッション開始地点へロールバック")}");
        }

        /// <summary>OnMissionAdvanced のペイロード。</summary>
        public readonly struct MissionAdvancedInfo
        {
            public int ClearedIndex { get; }
            public int NextIndex { get; }

            public MissionAdvancedInfo(int cleared, int next)
            {
                ClearedIndex = cleared;
                NextIndex = next;
            }
        }
    }
}
