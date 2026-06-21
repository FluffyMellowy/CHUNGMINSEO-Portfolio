namespace Colorless.UI
{
    using System;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// CMD ログの 1 行分の UI。SequenceQueueUI が動的に生成する。
    /// クリックでこの行をキューから削除する。
    /// </summary>
    public sealed class QueueLineUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private TextMeshProUGUI _label;
        [Required, SerializeField] private Button _removeButton;

        private Action _onRemove;

        public void Setup(string text, Action onRemove)
        {
            _label.text = text;
            _onRemove = onRemove;

            _removeButton.onClick.RemoveAllListeners();
            _removeButton.onClick.AddListener(HandleRemove);
        }

        private void HandleRemove()
        {
            _onRemove?.Invoke();
        }
    }
}
