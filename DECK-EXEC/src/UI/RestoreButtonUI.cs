namespace Colorless.UI
{
    using System;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.UI;
    using VContainer;
    using Colorless.Mission;
    using Colorless.Sequence;

    /// <summary>
    /// 直近実行したシーケンスをキューに復元するボタン。
    /// 失敗してロールバック後に「同じ手順をベースに微調整」する UX 用。
    /// LastExecuted が空 or 実行中なら自動で disable される。
    /// </summary>
    public sealed class RestoreButtonUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private Button _button;

        [Inject] private MissionManager _missions;
        [Inject] private SequenceExecutor _executor;

        private void Start()
        {
            _button.onClick.AddListener(HandleClick);
            UpdateInteractable();
        }

        private void Update()
        {
            /* 毎フレーム LastExecuted 状態を反映（イベント駆動でも可だが軽量） */
            UpdateInteractable();
        }

        private void HandleClick()
        {
            if (_executor.IsRunning) return;
            _missions.RestoreLastSequence();
        }

        private void UpdateInteractable()
        {
            bool canRestore = _executor != null
                              && !_executor.IsRunning
                              && _executor.LastExecuted != null
                              && _executor.LastExecuted.Count > 0;
            if (_button.interactable != canRestore)
                _button.interactable = canRestore;
        }
    }
}
