namespace Colorless.Stage
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using DG.Tweening;
    using MoreMountains.Feedbacks;

    /// <summary>
    /// シーン遷移時のフェードイン/アウトを担当するDontDestroyOnLoadシングルトン。
    /// Feelの MMF_Player を使ってビジュアル演出を行う。
    /// </summary>
    public sealed class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance { get; private set; }

        [Title("Fade Feedbacks")]
        [Required, SerializeField] private MMF_Player _fadeOutFeedback;
        [Required, SerializeField] private MMF_Player _fadeInFeedback;

        [Title("Runtime State")]
        [ShowInInspector, ReadOnly] private bool _isTransitioning = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            /* 初回シーンもフェードイン演出 */
            _fadeInFeedback.PlayFeedbacks();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void LoadScene(string sceneName)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            _fadeOutFeedback.PlayFeedbacks();
            DOVirtual.DelayedCall(_fadeOutFeedback.TotalDuration, () =>
            {
                SceneManager.LoadScene(sceneName);
            });
        }

        public void LoadScene(int buildIndex)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            _fadeOutFeedback.PlayFeedbacks();
            DOVirtual.DelayedCall(_fadeOutFeedback.TotalDuration, () =>
            {
                SceneManager.LoadScene(buildIndex);
            });
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _isTransitioning = false;
            _fadeInFeedback.PlayFeedbacks();
        }

        /* === Phase 11+ : ステージプレハブスワップ用の UniTask 版フェード API ===
           StageLoader からのみ呼ばれる想定。シーン遷移とは独立に動く（_isTransitioning に触らない）。 */

        /// <summary>
        /// フェードアウトを再生して終了まで待つ。
        /// MMF_Player の TotalDuration をそのまま待機時間とする。
        /// </summary>
        public async UniTask FadeOutAsync(CancellationToken ct = default)
        {
            if (_fadeOutFeedback == null) return;
            _fadeOutFeedback.PlayFeedbacks();
            float dur = Mathf.Max(0f, _fadeOutFeedback.TotalDuration);
            if (dur <= 0f) return;
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: ct);
        }

        /// <summary>
        /// フェードインを再生して終了まで待つ。
        /// </summary>
        public async UniTask FadeInAsync(CancellationToken ct = default)
        {
            if (_fadeInFeedback == null) return;
            _fadeInFeedback.PlayFeedbacks();
            float dur = Mathf.Max(0f, _fadeInFeedback.TotalDuration);
            if (dur <= 0f) return;
            await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: ct);
        }

        [Title("Debug"), Button("Test Fade Out")]
        private void TestFadeOut() => _fadeOutFeedback.PlayFeedbacks();

        [Button("Test Fade In")]
        private void TestFadeIn() => _fadeInFeedback.PlayFeedbacks();
    }
}
