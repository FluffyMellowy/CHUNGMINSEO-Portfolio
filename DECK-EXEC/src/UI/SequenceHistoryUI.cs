namespace Colorless.UI
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Card;
    using Colorless.Mission;
    using Colorless.Sequence;

    /// <summary>
    /// 履歴ビュー。SequenceExecutor.OnHistoryChanged を購読して
    /// 過去のシーケンス実行リストを描画する。
    /// 各エントリをクリックすると MissionManager.RestoreFromHistory が呼ばれる。
    /// </summary>
    public sealed class SequenceHistoryUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private RectTransform _container;
        [Required, SerializeField] private HistoryEntryUI _entryPrefab;

        [Title("Optional")]
        [InfoBox("項目クリック時に Input タブへ自動復帰させたいなら CmdTabsUI を指定。")]
        [SerializeField] private CmdTabsUI _tabs;
        [InfoBox("Input タブの Id（CmdTabsUI 側で設定した文字列と一致させる）")]
        [SerializeField] private string _inputTabId = "Input";

        [Inject] private SequenceExecutor _executor;
        [Inject] private MissionManager _missions;

        private readonly List<HistoryEntryUI> _spawned = new();
        private IDisposable _subscription;

        private void Start()
        {
            _subscription = _executor.OnHistoryChanged.Subscribe(_ => Rebuild());
            Rebuild();
        }

        private void OnEnable()
        {
            /* 表示切替で再アクティブ化された時にも再描画 */
            if (_executor != null) Rebuild();
        }

        private void OnDestroy() => _subscription?.Dispose();

        private void Rebuild()
        {
            foreach (HistoryEntryUI e in _spawned)
                if (e != null) Destroy(e.gameObject);
            _spawned.Clear();

            /* 最新が上に来るよう逆順で描画 */
            int n = _executor.History.Count;
            for (int i = n - 1; i >= 0; i--)
            {
                IReadOnlyList<QueuedCard> seq = _executor.History[i];
                HistoryEntryUI entry = Instantiate(_entryPrefab, _container);
                int capturedIndex = i;
                int runNumber = i + 1;
                entry.Setup(FormatSummary(runNumber, seq), () => OnEntrySelected(capturedIndex));
                _spawned.Add(entry);
            }
        }

        private void OnEntrySelected(int index)
        {
            _missions.RestoreFromHistory(index);
            /* 履歴タブ → Input タブへ自動復帰 */
            if (_tabs != null) _tabs.SelectById(_inputTabId);
        }

        /// <summary>例: "#3 (5): Move↑ Move→ Push Move↑ Move↑"</summary>
        private static string FormatSummary(int runNumber, IReadOnlyList<QueuedCard> seq)
        {
            StringBuilder sb = new();
            sb.Append('#').Append(runNumber)
              .Append(" (").Append(seq.Count).Append(")  ");
            for (int i = 0; i < seq.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(seq[i].Card != null ? seq[i].Card.DisplayName : "?");
            }
            return sb.ToString();
        }
    }
}
