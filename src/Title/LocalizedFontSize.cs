using TMPro;
using UnityEngine;

namespace Language
{
    /// <summary>
    /// 同じGameObjectのTMP_Textに対し、LanguageManagerの現在言語に応じた fontSize を自動で適用する。
    /// LocalizedFontと併用想定（あちらは font asset の切り替え、こちらは fontSize の切り替え）。
    /// JP/ENそれぞれの値はインスペクターで設定可能で、デフォルトはJP=36, EN=28。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedFontSize : MonoBehaviour
    {
        [Tooltip("日本語表示時に使うfontSize")]
        [SerializeField] private float _sizeJP = 36f;
        [Tooltip("英語表示時に使うfontSize")]
        [SerializeField] private float _sizeEN = 28f;

        private TMP_Text _text;
        private bool _subscribed; // OnLanguageChanged購読済みフラグ

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            // LanguageManager のAwake順次第ではここでInstanceがまだnullの可能性がある。
            // その場合は購読できずに失敗するので、Start でも再試行する（FontManagerと同じ安全網）
            TrySubscribe();
            Apply();
        }

        private void Start()
        {
            // OnEnable時にLanguageManagerが居なかった場合の補完
            TrySubscribe();
            Apply();
        }

        private void OnDisable()
        {
            if (_subscribed && LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (LanguageManager.Instance == null) return;
            LanguageManager.Instance.OnLanguageChanged += OnLanguageChanged;
            _subscribed = true;
        }

        private void OnLanguageChanged(LanguageManager.Language lang) => Apply();

        /// <summary>
        /// 現在言語に応じてfontSizeを差し替える。LanguageManagerが居ない時はJPとみなす
        /// </summary>
        private void Apply()
        {
            if (_text == null) return;

            var lang = LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.JP;

            _text.fontSize = lang == LanguageManager.Language.EN ? _sizeEN : _sizeJP;
        }
    }
}
