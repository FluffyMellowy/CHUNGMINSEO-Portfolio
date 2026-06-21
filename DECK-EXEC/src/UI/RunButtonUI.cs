namespace Colorless.UI
{
    using System;
    using Cysharp.Threading.Tasks;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.UI;
    using VContainer;
    using Colorless.Sequence;

    /// <summary>
    /// RUN ボタン。クリックすると SequenceExecutor が現在のキューを実行する。
    /// 実行中は自身を無効化する。
    /// </summary>
    public sealed class RunButtonUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private Button _button;

        [Inject] private SequenceExecutor _executor;
        [Inject] private SequenceQueue _queue;

        private IDisposable _queueSubscription;

        private void Start()
        {
            _button.onClick.AddListener(HandleClick);

            /* キューが空のときはボタン無効化 */
            _queueSubscription = _queue.Changed.Subscribe(_ => UpdateInteractable());
            UpdateInteractable();
        }

        private void OnDestroy()
        {
            _queueSubscription?.Dispose();
        }

        private void HandleClick()
        {
            if (_executor.IsRunning) return;
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            _button.interactable = false;
            try
            {
                await _executor.RunAsync();
            }
            finally
            {
                UpdateInteractable();
            }
        }

        private void UpdateInteractable()
        {
            _button.interactable = _queue.Count > 0 && !_executor.IsRunning;
        }
    }
}
