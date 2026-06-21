namespace Colorless.UI.Desktop
{
    using R3;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Win95 風のポップアップウィンドウ枠。
    /// タイトルバー（ドラッグ可）／クローズボタン／リサイズハンドル／中身の Content Area を統合する。
    /// 中身は ContentArea の子として任意の UI を入れて使う。
    /// WindowManager が親階層にいれば z-order を統合管理、無くても SetAsLastSibling で単独動作する。
    /// </summary>
    public sealed class WindowFrame : MonoBehaviour
    {
        public enum CloseBehavior
        {
            /// <summary>SetActive(false) で隠す（再表示可能）</summary>
            Hide,
            /// <summary>GameObject ごと破棄</summary>
            Destroy,
            /// <summary>イベントのみ発火（外部側で挙動を決める）</summary>
            EventOnly,
        }

        [Title("Refs - Frame")]
        [Required, SerializeField] private RectTransform _root;
        [Required, SerializeField] private RectTransform _titleBar;
        [Required, SerializeField] private TextMeshProUGUI _titleLabel;
        [Required, SerializeField] private Button _closeButton;
        [SerializeField] private Button _minimizeButton;
        [SerializeField] private Button _maximizeButton;
        [Required, SerializeField] private RectTransform _contentArea;
        [SerializeField] private RectTransform _resizeHandle;

        [Title("Settings")]
        [SerializeField] private string _title = "untitled.exe";
        [SerializeField] private CloseBehavior _closeBehavior = CloseBehavior.Hide;
        [InfoBox("リサイズ／最大化時の制約。デフォルト値は適宜調整可。")]
        [SerializeField] private Vector2 _minSize = new(160f, 100f);
        [SerializeField] private Vector2 _maxSize = new(1920f, 1080f);
        [SerializeField] private bool _draggable = true;
        [SerializeField] private bool _resizable = true;
        [InfoBox("Hierarchy 上で親方向へ探索して WindowManager を取得する。見つからない場合は単独動作。")]
        [SerializeField] private bool _autoFindManager = true;

        [Title("Runtime State")]
        [ShowInInspector, ReadOnly] private bool _isMaximized;
        [ShowInInspector, ReadOnly] private Vector2 _preMaxSize;
        [ShowInInspector, ReadOnly] private Vector3 _preMaxPos;

        private WindowManager _manager;

        /* === イベント === */
        private readonly Subject<Unit> _onOpened = new();
        private readonly Subject<Unit> _onClosed = new();
        private readonly Subject<Unit> _onFocused = new();
        private readonly Subject<Unit> _onCloseRequested = new();

        public Observable<Unit> OnOpened => _onOpened;
        public Observable<Unit> OnClosed => _onClosed;
        public Observable<Unit> OnFocused => _onFocused;
        public Observable<Unit> OnCloseRequested => _onCloseRequested;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                if (_titleLabel != null) _titleLabel.text = value;
            }
        }

        public RectTransform ContentArea => _contentArea;
        public RectTransform Root => _root;
        public Vector2 MinSize => _minSize;
        public Vector2 MaxSize => _maxSize;

        private void Awake()
        {
            if (_titleLabel != null) _titleLabel.text = _title;
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
            if (_maximizeButton != null) _maximizeButton.onClick.AddListener(OnMaximizeClicked);
            if (_minimizeButton != null) _minimizeButton.onClick.AddListener(OnMinimizeClicked);

            if (_autoFindManager)
                _manager = GetComponentInParent<WindowManager>(includeInactive: true);

            /* ドラッグハンドラをタイトルバーに自動装着 */
            if (_draggable && _titleBar != null)
            {
                WindowDragHandler drag = _titleBar.gameObject.GetComponent<WindowDragHandler>();
                if (drag == null) drag = _titleBar.gameObject.AddComponent<WindowDragHandler>();
                drag.Bind(this);
            }

            /* リサイズハンドラを設置 */
            if (_resizable && _resizeHandle != null)
            {
                WindowResizeHandler resize = _resizeHandle.gameObject.GetComponent<WindowResizeHandler>();
                if (resize == null) resize = _resizeHandle.gameObject.AddComponent<WindowResizeHandler>();
                resize.Bind(this);
            }
        }

        private void OnEnable()
        {
            _manager?.Register(this);
            BringToFront();
            _onOpened.OnNext(Unit.Default);
        }

        private void OnDisable()
        {
            _manager?.Unregister(this);
            _onClosed.OnNext(Unit.Default);
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
            if (_maximizeButton != null) _maximizeButton.onClick.RemoveListener(OnMaximizeClicked);
            if (_minimizeButton != null) _minimizeButton.onClick.RemoveListener(OnMinimizeClicked);
            _onOpened.Dispose();
            _onClosed.Dispose();
            _onFocused.Dispose();
            _onCloseRequested.Dispose();
        }

        /// <summary>
        /// このウィンドウを最前面へ。WindowManager があればそちらが担当、無ければ SetAsLastSibling。
        /// </summary>
        public void BringToFront()
        {
            if (_manager != null) _manager.Focus(this);
            else if (_root != null) _root.SetAsLastSibling();
            else transform.SetAsLastSibling();
            _onFocused.OnNext(Unit.Default);
        }

        /// <summary>ウィンドウを開く（非アクティブ→アクティブ）。</summary>
        public void Open()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            else BringToFront();
        }

        /// <summary>クローズ要求を発行する。CloseBehavior によって挙動が変わる。</summary>
        public void RequestClose()
        {
            _onCloseRequested.OnNext(Unit.Default);
            switch (_closeBehavior)
            {
                case CloseBehavior.Hide:
                    gameObject.SetActive(false);
                    break;
                case CloseBehavior.Destroy:
                    Destroy(gameObject);
                    break;
                case CloseBehavior.EventOnly:
                    /* 外部側で開閉ロジックを担当 */
                    break;
            }
        }

        /// <summary>最大化／復元のトグル。</summary>
        public void ToggleMaximize()
        {
            if (_root == null) return;
            RectTransform parent = _root.parent as RectTransform;
            if (parent == null) return;

            if (_isMaximized)
            {
                _root.sizeDelta = _preMaxSize;
                _root.localPosition = _preMaxPos;
                _isMaximized = false;
            }
            else
            {
                _preMaxSize = _root.sizeDelta;
                _preMaxPos = _root.localPosition;
                _root.anchorMin = new Vector2(0f, 0f);
                _root.anchorMax = new Vector2(1f, 1f);
                _root.offsetMin = Vector2.zero;
                _root.offsetMax = Vector2.zero;
                _isMaximized = true;
            }
        }

        private void OnCloseClicked() => RequestClose();
        private void OnMaximizeClicked() => ToggleMaximize();
        private void OnMinimizeClicked() => gameObject.SetActive(false);
    }
}
