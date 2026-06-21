namespace Colorless.UI
{
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// 実行ログの 1 行分の UI。ActionLogUI が動的に生成する。
    /// QueueLineUI と異なり削除ボタンは無く、読み取り専用の rich text 表示のみ。
    /// </summary>
    public sealed class ActionLogEntryUI : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private TextMeshProUGUI _label;

        public void Setup(string richText)
        {
            _label.text = richText;
        }
    }
}
