using TMPro;
using UnityEngine;

namespace Language
{
    /// <summary>
    /// TMP_Textに日本語/英語の二言語版を持たせ、LanguageManagerの現在言語に合わせて表示を切り替える
    /// 同じGameObjectのTMP_Text（TextMeshProUGUI / TextMeshPro共に可）にアタッチする
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField, TextArea(2, 6)] private string _textJP;
        [SerializeField, TextArea(2, 6)] private string _textEN;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            if (_text == null)
                Debug.LogError("[LocalizedText] TMP_Textが見つかりません", this);
        }

        private void OnEnable()
        {
            Apply();
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged += OnLanguageChanged;
            if (FontManager.Instance != null)
                FontManager.Instance.OnFontChanged += OnFontChanged;
        }

        private void OnDisable()
        {
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            if (FontManager.Instance != null)
                FontManager.Instance.OnFontChanged -= OnFontChanged;
        }

        private void OnLanguageChanged(LanguageManager.Language lang) => Apply();
        private void OnFontChanged(TMPro.TMP_FontAsset font) => ApplyFont();

        /// <summary>
        /// 現在言語に合わせて文字を当てはめる。LanguageManagerが居ない時はJPを既定とする
        /// 同時にFontManagerが居れば現在フォントも反映する
        /// </summary>
        private void Apply()
        {
            if (_text == null) return;
            var lang = LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.JP;
            _text.text = lang == LanguageManager.Language.JP ? _textJP : _textEN;
            ApplyFont();
        }

        /// <summary>
        /// FontManagerの現在フォントをTMP_Textに適用する。FontManagerが居ない時は既定フォントを維持
        /// </summary>
        private void ApplyFont()
        {
            if (_text == null || FontManager.Instance == null) return;
            var font = FontManager.Instance.CurrentFont;
            if (font != null) _text.font = font;
        }
    }
}
