namespace Colorless.UI
{
    using DG.Tweening;
    using R3;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using VContainer;
    using Colorless.Mission;
    using Colorless.Sequence;

    /// <summary>
    /// ステージ上に大きく表示するシステム文字オーバーレイ。
    /// "EXECUTE" / "MISSION N" / "CLEAR!" / "RESET..." を状態遷移に合わせて表示する。
    /// </summary>
    public sealed class StageStateOverlay : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private CanvasGroup _group;
        [Required, SerializeField] private TextMeshProUGUI _label;
        [Required, SerializeField] private RectTransform _scaleTarget;

        [Title("Settings")]
        [SerializeField] private float _fadeInDuration = 0.15f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField, Range(0.5f, 1.5f)] private float _enterScale = 0.8f;

        [Title("Per State")]
        [SerializeField] private StateStyle _execute = new("EXECUTE", new Color(0.50f, 0.87f, 0.92f), 36, 0.6f);
        [SerializeField] private StateStyle _missionAdvanced = new("MISSION", new Color(0.62f, 0.62f, 0.62f), 28, 0.7f);
        [SerializeField] private StateStyle _stageCleared = new("CLEAR!", new Color(0.51f, 0.78f, 0.52f), 72, 1.5f);
        [SerializeField] private StateStyle _reset = new("RESET...", new Color(0.94f, 0.33f, 0.31f), 48, 1.0f);

        [Inject] private SequenceExecutor _executor;
        [Inject] private MissionManager _missions;

        private readonly CompositeDisposable _subscriptions = new();
        private Sequence _currentTween;

        private void Awake()
        {
            _group.alpha = 0f;
            _scaleTarget.localScale = Vector3.one * _enterScale;
        }

        private void Start()
        {
            _executor.OnRunStarted
                .Subscribe(_ => Show(_execute))
                .AddTo(_subscriptions);

            _missions.OnMissionAdvanced
                .Subscribe(info => Show(_missionAdvanced.WithText($"MISSION {info.NextIndex + 1}")))
                .AddTo(_subscriptions);

            _missions.OnStageCleared
                .Subscribe(_ => Show(_stageCleared))
                .AddTo(_subscriptions);

            _missions.OnMissionFailed
                .Subscribe(_ => Show(_reset))
                .AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
            _subscriptions.Dispose();
        }

        private void Show(StateStyle style)
        {
            _currentTween?.Kill();

            _label.text = style.Text;
            _label.color = style.Color;
            _label.fontSize = style.FontSize;
            _scaleTarget.localScale = Vector3.one * _enterScale;

            Sequence seq = DOTween.Sequence();
            /* フェード＋スケールイン */
            seq.Append(_group.DOFade(1f, _fadeInDuration));
            seq.Join(_scaleTarget.DOScale(Vector3.one, _fadeInDuration).SetEase(Ease.OutBack));
            /* 保持 */
            seq.AppendInterval(style.HoldDuration);
            /* フェードアウト */
            seq.Append(_group.DOFade(0f, _fadeOutDuration));
            _currentTween = seq;
        }

        [System.Serializable]
        public struct StateStyle
        {
            public string Text;
            public Color Color;
            public int FontSize;
            public float HoldDuration;

            public StateStyle(string text, Color color, int fontSize, float holdDuration)
            {
                Text = text;
                Color = color;
                FontSize = fontSize;
                HoldDuration = holdDuration;
            }

            public StateStyle WithText(string newText) =>
                new(newText, Color, FontSize, HoldDuration);
        }
    }
}
