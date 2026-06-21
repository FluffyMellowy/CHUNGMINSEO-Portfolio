namespace Colorless.UI
{
    using System;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 履歴ビューの 1 行分の UI。SequenceHistoryUI が動的に生成する。
    /// クリックでこのシーケンスをキューに復元する。
    /// </summary>
    public sealed class HistoryEntryUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private TextMeshProUGUI _summaryLabel;
        [Required, SerializeField] private Button _selectButton;

        private Action _onSelect;

        public void Setup(string summary, Action onSelect)
        {
            _summaryLabel.text = summary;
            _onSelect = onSelect;

            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(HandleClick);
        }

        private void HandleClick() => _onSelect?.Invoke();
    }
}
