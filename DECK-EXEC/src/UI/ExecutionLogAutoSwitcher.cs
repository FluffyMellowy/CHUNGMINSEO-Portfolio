namespace Colorless.UI
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Mission;
    using Colorless.Sequence;

    /// <summary>
    /// CmdTabsUI を SequenceExecutor の状態に追従して自動切替する。
    /// RUN 開始時に Log タブへ、シーケンス終了から ReturnDelay 秒後に Input タブへ戻す。
    /// </summary>
    public sealed class ExecutionLogAutoSwitcher : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private CmdTabsUI _tabs;

        [Title("Tab Ids")]
        [SerializeField] private string _logTabId = "Log";
        [SerializeField] private string _inputTabId = "Input";

        [Title("Timing")]
        [InfoBox("シーケンス終了後、何秒待って Input タブへ戻すか。")]
        [SerializeField, Range(0f, 5f)] private float _returnDelay = 1.5f;

        [Inject] private SequenceExecutor _executor;
        [Inject] private MissionManager _missions;

        private readonly CompositeDisposable _subscriptions = new();
        private CancellationTokenSource _returnCts;

        private void Start()
        {
            _executor.OnRunStarted
                .Subscribe(_ => OnRunStarted())
                .AddTo(_subscriptions);

            _executor.OnSequenceFinished
                .Subscribe(_ => ScheduleReturnToInput())
                .AddTo(_subscriptions);

            _executor.OnPlayerDied
                .Subscribe(_ => ScheduleReturnToInput())
                .AddTo(_subscriptions);

            _missions.OnMissionFailed
                .Subscribe(_ => ScheduleReturnToInput())
                .AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _returnCts?.Cancel();
            _returnCts?.Dispose();
            _subscriptions.Dispose();
        }

        private void OnRunStarted()
        {
            /* 進行中の return キャンセル（新しい RUN が始まった） */
            _returnCts?.Cancel();
            _returnCts?.Dispose();
            _returnCts = null;

            if (_tabs != null) _tabs.SelectById(_logTabId);
        }

        private void ScheduleReturnToInput()
        {
            /* 既存の return を上書き（最後のシーケンス終了で計時） */
            _returnCts?.Cancel();
            _returnCts?.Dispose();
            _returnCts = new CancellationTokenSource();
            ReturnAfterDelayAsync(_returnCts.Token).Forget();
        }

        private async UniTaskVoid ReturnAfterDelayAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(_returnDelay), cancellationToken: ct);
                if (_tabs != null) _tabs.SelectById(_inputTabId);
            }
            catch (System.OperationCanceledException) { /* 新しい RUN が始まったらキャンセル */ }
        }
    }
}
