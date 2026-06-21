namespace Colorless.UI
{
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 方向選択ポップアップ。方向パラメータが必要なカードを選択した直後に表示される。
    /// SelectAsync で 4 方向のいずれか、または null（キャンセル）を返す。
    /// </summary>
    public sealed class DirectionSelectorUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private GameObject _panel;
        [Required, SerializeField] private Button _upButton;
        [Required, SerializeField] private Button _downButton;
        [Required, SerializeField] private Button _leftButton;
        [Required, SerializeField] private Button _rightButton;
        [SerializeField] private Button _cancelButton;

        [Title("Modal Background (auto)")]
        [InfoBox("Overlay 自身の Image。SerializeField で割り当てなければ GetComponent で取得")]
        [SerializeField] private Image _overlayBackground;

        private UniTaskCompletionSource<Vector2Int?> _tcs;

        private void Awake()
        {
            _upButton.onClick.AddListener(() => Complete(Vector2Int.up));
            _downButton.onClick.AddListener(() => Complete(Vector2Int.down));
            _leftButton.onClick.AddListener(() => Complete(Vector2Int.left));
            _rightButton.onClick.AddListener(() => Complete(Vector2Int.right));
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(() => Complete(null));

            if (_overlayBackground == null)
                _overlayBackground = GetComponent<Image>();

            _panel.SetActive(false);
            SetModalBlocking(false);
        }

        /// <summary>
        /// ポップアップを開き、方向選択（または null=キャンセル）を待つ。
        /// </summary>
        public UniTask<Vector2Int?> SelectAsync()
        {
            // Debug.Log("[DirSelect] SelectAsync called, opening panel");
            _tcs?.TrySetResult(null);
            _tcs = new UniTaskCompletionSource<Vector2Int?>();
            _panel.SetActive(true);
            SetModalBlocking(true);
            return _tcs.Task;
        }

        /// <summary>
        /// キーボード方向キーからの選択も受け付ける。
        /// </summary>
        private void Update()
        {
            if (!_panel.activeSelf) return;
            if (_tcs == null) return;

            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.upArrowKey.wasPressedThisFrame) Complete(Vector2Int.up);
            else if (kb.downArrowKey.wasPressedThisFrame) Complete(Vector2Int.down);
            else if (kb.leftArrowKey.wasPressedThisFrame) Complete(Vector2Int.left);
            else if (kb.rightArrowKey.wasPressedThisFrame) Complete(Vector2Int.right);
            else if (kb.escapeKey.wasPressedThisFrame) Complete(null);
        }

        private void Complete(Vector2Int? result)
        {
            // Debug.Log($"[DirSelect] Complete: {(result.HasValue ? result.Value.ToString() : "Cancel")}");
            _panel.SetActive(false);
            SetModalBlocking(false);
            UniTaskCompletionSource<Vector2Int?> tcs = _tcs;
            _tcs = null;
            tcs?.TrySetResult(result);
        }

        /// <summary>
        /// オーバーレイ背景の Raycast Target を切替えてモーダル動作を制御。
        /// 閉じている時はクリックが背後（手札・マップ）に通り抜けるようにする。
        /// </summary>
        private void SetModalBlocking(bool block)
        {
            if (_overlayBackground != null)
                _overlayBackground.raycastTarget = block;
        }
    }
}
