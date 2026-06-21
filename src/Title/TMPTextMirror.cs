using TMPro;
using UnityEngine;

namespace Title
{
    /// <summary>
    /// master の TMP_Text に表示中の text と maxVisibleCharacters を、
    /// slaves の TMP_Text 群へ毎フレーム同期するミラー。
    /// 多重アウトライン演出（同じ文字を3レイヤー重ねる）で、
    /// TypewriterComponentを持つmasterの進行に合わせて他レイヤーも段階表示させる用途。
    /// LateUpdateで反映するのでTypewriter更新後の値を確実に拾える。
    /// _mirrorFontSize=true の時は fontSize も同期する。
    /// これによりmasterにLocalizedFontSizeを付ければ全レイヤーが言語別サイズに追従する
    /// </summary>
    public class TMPTextMirror : MonoBehaviour
    {
        [Tooltip("基準となるTMP_Text。TypewriterComponentが付いている前提")]
        [SerializeField] private TMP_Text _master;

        [Tooltip("masterに追従させたいTMP_Text群（アウトライン用レイヤー）")]
        [SerializeField] private TMP_Text[] _slaves;

        [Tooltip("fontSize も master に同期するか。LocalizedFontSize 等で言語別サイズを変える時にON")]
        [SerializeField] private bool _mirrorFontSize = true;

        private void LateUpdate()
        {
            if (_master == null || _slaves == null) return;

            string text = _master.text;
            int visible = _master.maxVisibleCharacters;
            float size = _master.fontSize;

            foreach (var s in _slaves)
            {
                if (s == null) continue;
                if (s.text != text) s.text = text;
                if (s.maxVisibleCharacters != visible) s.maxVisibleCharacters = visible;
                if (_mirrorFontSize && !Mathf.Approximately(s.fontSize, size))
                    s.fontSize = size;
            }
        }
    }
}
