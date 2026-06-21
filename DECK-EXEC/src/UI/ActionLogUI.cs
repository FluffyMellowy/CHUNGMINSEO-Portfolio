namespace Colorless.UI
{
    using System;
    using System.Collections.Generic;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.UI;
    using VContainer;
    using Colorless.Sequence;

    /// <summary>
    /// 実行中の narration（"Player が (3,4) へ移動！" 等）を表示するログビュー。
    /// IActionLogger.Entries を購読してエントリ追加、Cleared で全消去。
    /// CMD パネルの Log タブの中身として配置される。
    /// </summary>
    public sealed class ActionLogUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private RectTransform _container;
        [Required, SerializeField] private ActionLogEntryUI _entryPrefab;

        [Title("Auto-scroll")]
        [InfoBox("新規エントリ追加時に最下行へ自動スクロール。未指定なら自動スクロール無し。")]
        [SerializeField] private ScrollRect _scrollRect;

        [Title("Settings")]
        [SerializeField, Min(10)] private int _maxEntries = 200;

        [Inject] private IActionLogger _logger;

        private readonly List<ActionLogEntryUI> _spawned = new();
        private readonly CompositeDisposable _subscriptions = new();

        private void Start()
        {
            _logger.Entries
                .Subscribe(AppendEntry)
                .AddTo(_subscriptions);

            _logger.Cleared
                .Subscribe(_ => ClearAll())
                .AddTo(_subscriptions);
        }

        private void OnDestroy() => _subscriptions.Dispose();

        private void AppendEntry(string richText)
        {
            ActionLogEntryUI entry = Instantiate(_entryPrefab, _container);
            entry.Setup(richText);
            _spawned.Add(entry);

            /* 上限超過時は古い分から破棄（メモリ節約） */
            while (_spawned.Count > _maxEntries)
            {
                ActionLogEntryUI oldest = _spawned[0];
                _spawned.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            /* 次のレイアウト確定後に最下行へスクロール */
            if (_scrollRect != null)
                ScrollToBottomNextFrame();
        }

        private void ScrollToBottomNextFrame()
        {
            /* レイアウト計算後に位置を更新するため、Canvas update を一度待つ。
               コルーチン代わりに次フレームに DOTween 等使わず素朴に Invoke */
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void ClearAll()
        {
            foreach (ActionLogEntryUI e in _spawned)
                if (e != null) Destroy(e.gameObject);
            _spawned.Clear();
        }
    }
}
