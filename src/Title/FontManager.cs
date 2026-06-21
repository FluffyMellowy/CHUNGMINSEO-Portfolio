using System;
using TMPro;
using UnityEngine;

namespace Language
{
    /// <summary>
    /// 言語に応じてゲーム全体のTMPフォントを切り替えるシングルトン
    /// LanguageManagerのOnLanguageChangedを購読し、現在言語に対応するフォントを保持する
    /// </summary>
    public class FontManager : MonoBehaviour
    {
        public static FontManager Instance { get; private set; }

        [Header("言語別フォント")]
        [SerializeField] private TMP_FontAsset _fontJP; // 日本語用フォントアセット
        [SerializeField] private TMP_FontAsset _fontEN; // 英語用フォントアセット

        /// <summary>現在の言語に対応するフォントアセット</summary>
        public TMP_FontAsset CurrentFont { get; private set; }

        /// <summary>
        /// フォントが切り替わった瞬間に通知する。LocalizedText / LocalizedFont等が購読する
        /// </summary>
        public event Action<TMP_FontAsset> OnFontChanged;

        private void Awake()
        {
            // Awakeで先にInstanceを確定させる（OnEnable購読が早い段階で動作するように）
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoadはルートGameObjectでしか動かないので、親があれば分離してから登録
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);

            // 初期フォントを確定（LanguageManagerが居なければJP扱い）
            CurrentFont = ResolveFont(GetCurrentLanguage());
        }

        private bool _subscribed; // OnLanguageChanged購読済みフラグ

        private void OnEnable()
        {
            // LanguageManagerの生成タイミングが後の場合に備え、OnEnableで購読を試みる
            TrySubscribe();
        }

        private void Start()
        {
            // Awake順次第ではOnEnable時にLanguageManagerが未初期化なケースがある
            // 全Awake完了後のStartで補完購読する（DialogueScene単独起動時の対策）
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribed && LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            _subscribed = false;
        }

        /// <summary>
        /// LanguageManagerがまだ存在しなければ何もせず、存在すれば1回だけ購読する
        /// </summary>
        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (LanguageManager.Instance == null) return;

            LanguageManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            _subscribed = true;

            // 現在言語で再適用（Awake時にJP固定だったCurrentFontを正しく上書き）
            Apply(LanguageManager.Instance.CurrentLanguage);
        }

        private void HandleLanguageChanged(LanguageManager.Language lang) => Apply(lang);

        /// <summary>
        /// 指定言語に対応するフォントを反映し、変更があればイベントを通知する
        /// </summary>
        private void Apply(LanguageManager.Language lang)
        {
            var font = ResolveFont(lang);
            if (font == CurrentFont) return;
            CurrentFont = font;
            OnFontChanged?.Invoke(CurrentFont);
        }

        private TMP_FontAsset ResolveFont(LanguageManager.Language lang)
        {
            return lang == LanguageManager.Language.EN ? _fontEN : _fontJP;
        }

        private LanguageManager.Language GetCurrentLanguage()
        {
            return LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.JP;
        }
    }
}
