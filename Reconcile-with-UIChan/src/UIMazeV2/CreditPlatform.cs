using UnityEngine;
using TMPro;

namespace UIMazeV2
{
    /// <summary>
    /// クレジット名が表示される足場
    /// テキスト表示と物理当たり判定を持つ
    /// </summary>
    public class CreditPlatform : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _nameText; // 名前表示用テキスト

        /// <summary>
        /// 表示する名前をセットする
        /// </summary>
        public void SetName(string creditName)
        {
            _nameText.text = creditName;
        }
    }
}