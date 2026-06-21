namespace Colorless.UI
{
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Colorless.Stage;

    /// <summary>
    /// ステージ選択画面の個別ノードボタン。
    /// StageSelectControllerがStageGraphから動的に生成する。
    /// </summary>
    public sealed class StageNodeButton : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _label;

        [Title("State Overlays")]
        [InfoBox("解放済みで未クリアの状態はデフォルト表示。下記は条件ごとの追加表示")]
        [SerializeField] private GameObject _clearedOverlay;
        [SerializeField] private GameObject _lockedOverlay;

        private StageNode _node;
        private System.Action<StageNode> _onClicked;

        public StageNode Node => _node;

        private void OnEnable()
        {
            StageProgress.OnChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            StageProgress.OnChanged -= HandleProgressChanged;
        }

        private void HandleProgressChanged()
        {
            if (_node != null) Refresh();
        }

        public void Setup(StageNode node, System.Action<StageNode> onClicked)
        {
            _node = node;
            _onClicked = onClicked;

            if (_label != null) _label.text = node.DisplayName;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(HandleClick);

            Refresh();
        }

        /// <summary>
        /// 解放/クリア状態を再評価して見た目を更新。
        /// </summary>
        public void Refresh()
        {
            bool unlocked = StageProgress.IsUnlocked(_node);
            bool cleared = StageProgress.IsCleared(_node.StageId);

            _button.interactable = unlocked;

            if (_lockedOverlay != null) _lockedOverlay.SetActive(!unlocked);
            if (_clearedOverlay != null) _clearedOverlay.SetActive(cleared);
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_node);
        }
    }
}
