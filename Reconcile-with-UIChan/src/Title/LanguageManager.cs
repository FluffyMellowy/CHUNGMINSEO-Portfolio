using System;
using Cysharp.Threading.Tasks;
using St;
using UnityEngine;

namespace Language
{
    /// <summary>
    /// ゲーム全体の言語設定を管理するシングルトン
    /// </summary>
    public class LanguageManager : MonoBehaviour, IInitializable
    {
        public static LanguageManager Instance { get; private set; }

        public enum Language { JP, EN }

        public Language CurrentLanguage { get; private set; } = Language.JP; // デフォルト日本語

        /// <summary>
        /// 言語が切り替わった瞬間に通知する。引数は新しい言語
        /// LocalizedText等のリスナーがこれを購読して表示を更新する
        /// </summary>
        public event Action<Language> OnLanguageChanged;

        private void Awake()
        {
            // Awakeで先にInstanceを確定させる（OnEnable購読が早い段階で動作するように）
            if (Instance != null && Instance != this)
            {
                LanguageDiagnostic.Log("LanguageManager.Awake",
                    $"duplicate instance detected on scene='{gameObject.scene.name}', self-destroying. existing.CurrentLanguage={Instance.CurrentLanguage}");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoadはルートGameObjectでしか動かないので、親があれば分離してから登録
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);

            LanguageDiagnostic.Log("LanguageManager.Awake",
                $"primary instance set on scene='{gameObject.scene.name}', default CurrentLanguage={CurrentLanguage}");
        }

        public async UniTask InitializeAsync()
        {
            // Awakeで初期化済み。IInitializable契約を満たすために残しておく
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 言語を切り替える。実際に変わった時だけイベント通知
        /// </summary>
        public void SetLanguage(Language lang)
        {
            if (CurrentLanguage == lang) return;

            // 診断: 誰がいつ言語を変えているか追跡するためのスタックトレース付きログ。
            // Editor の Console と Application.persistentDataPath のテキストファイル両方に書き出す
            Debug.Log($"[LanguageManager] SetLanguage: {CurrentLanguage} -> {lang}\n{System.Environment.StackTrace}", this);
            LanguageDiagnostic.Log("LanguageManager.SetLanguage",
                $"{CurrentLanguage} -> {lang}", captureStackTrace: true);

            CurrentLanguage = lang;
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }

        /// <summary>
        /// 現在の言語を反転する（JP↔EN）
        /// </summary>
        public void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == Language.JP ? Language.EN : Language.JP);
        }
    }
}
