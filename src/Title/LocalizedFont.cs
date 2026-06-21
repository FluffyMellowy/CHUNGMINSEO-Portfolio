using TMPro;
using UnityEngine;

namespace Language
{
    /// <summary>
    /// 同じGameObjectのTMP_Textに対し、FontManagerの現在フォントを自動で適用する
    /// LocalizedTextと併用可。動的にtextを差し替えるスクリプト（DialogueUI等）にも単独で付けられる
    /// MaterialはTMPがfontから自動同期するため、本コンポーネントでは扱わない
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedFont : MonoBehaviour
    {
        [Header("言語別マテリアル（任意 / アウトライン等を維持したい時用）")]
        [Tooltip("JP表示時に強制適用するMaterial。空の場合はフォントのデフォルトMaterialに任せる")]
        [SerializeField] private Material _materialJP;
        [Tooltip("EN表示時に強制適用するMaterial。空の場合はフォントのデフォルトMaterialに任せる")]
        [SerializeField] private Material _materialEN;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Apply();
            // FontManager.OnFontChanged のみ購読する。
            // LanguageManager.OnLanguageChanged を直接購読すると、FontManager より先に呼ばれて
            // _text.font が旧フォントのままMaterialだけ更新される一時的な不整合状態が発生し、
            // TMPがmesh再構築する際に古いatlas基準でoutline/dilateが計算されてしまう。
            // FontManager が OnLanguageChanged を受けて CurrentFont 更新後に OnFontChanged を発火するので、
            // それだけ購読すれば常に font/material 両方が新しい状態でApplyできる。
            if (FontManager.Instance != null)
                FontManager.Instance.OnFontChanged += OnFontChanged;
        }

        private void OnDisable()
        {
            if (FontManager.Instance != null)
                FontManager.Instance.OnFontChanged -= OnFontChanged;
        }

        private void OnFontChanged(TMP_FontAsset font) => Apply();

        /// <summary>
        /// 現在のフォントをTMP_Textに反映する。_text.font 代入時にTMPが自動で
        /// fontSharedMaterial をフォントのデフォルトに上書きしてしまうため、
        /// インスペクターで _materialJP / _materialEN が設定されている場合は
        /// フォント代入後に明示的に上書きしてアウトライン等の見た目を維持する。
        /// FontManagerが居ない時は何もしない（既定フォントを維持）
        /// </summary>
        private void Apply()
        {
            if (_text == null || FontManager.Instance == null) return;

            var font = FontManager.Instance.CurrentFont;
            if (font == null) return;

            _text.font = font;

            // 言語別Material指定があればフォント変更で巻き戻ったMaterialを上書きする
            var lang = LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.JP;
            var mat = lang == LanguageManager.Language.EN ? _materialEN : _materialJP;
            if (mat != null) _text.fontSharedMaterial = mat;

            // Material変更をTMPに通知。mesh再構築はテキスト更新側(TitleDialogueLoop.Display)に任せる
            _text.SetMaterialDirty();
        }
    }
}
