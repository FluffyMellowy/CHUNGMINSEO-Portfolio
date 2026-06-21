using Cysharp.Threading.Tasks;
using St;
using UnityEngine;

namespace Result
{
    /// <summary>
    /// リザルト画面の管理
    /// 失敗回数（GameoverCounter.Instance.GameOverCount）に応じてエンディング画像を切り替える
    /// BGM再生はHayashi側で管理するためここでは扱わない
    /// 仕様書「D.リザルト画面」準拠
    /// </summary>
    public class ResultManager : MonoBehaviour, IInitializable
    {
        [Header("エンディング画像")]
        [SerializeField] private GameObject _goodEndImage; // 失敗<ノーマル閾値
        [SerializeField] private GameObject _normalEndImage; // ノーマル閾値 ≦ 失敗<バッド閾値
        [SerializeField] private GameObject _badEndImage; // バッド閾値 ≦ 失敗

        [Header("分岐閾値")]
        [SerializeField] private int _normalEndThreshold = 1; // この値以上でノーマルエンド
        [SerializeField] private int _badEndThreshold = 3; // この値以上でバッドエンド

        [Header("デバッグ")]
        [SerializeField] private bool _useDebugCount; // GameoverCounterが無い場合に使う
        [SerializeField] private int _debugCount; // 単独テスト用の擬似失敗回数

        [Tooltip("trueにするとGameManager不在でもStart()で自動初期化する。ResultScene単独Play用")]
        [SerializeField] private bool _runStandalone = false; // 単独再生フラグ

        private void Start()
        {
            // _runStandaloneはエディタ単独テスト専用。ビルドでは強制OFF扱い
#if UNITY_EDITOR
            if (_runStandalone)
                InitializeAsync().Forget();
#endif
        }

        public async UniTask InitializeAsync()
        {
            int count = GetGameOverCount();
            ApplyEnding(count);
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// GameoverCounterから失敗回数を取得する
        /// エディタ単独テストのみ_useDebugCountで_debugCountを強制（ビルドでは無視）
        /// </summary>
        private int GetGameOverCount()
        {
#if UNITY_EDITOR
            if (_useDebugCount)
                return _debugCount;
#endif
            return GameoverCounter.Instance != null ? GameoverCounter.Instance.GameOverCount : 0;
        }

        /// <summary>
        /// 失敗回数に応じてエンディング画像を1枚だけアクティブ化する
        /// </summary>
        private void ApplyEnding(int count)
        {
            SetActiveSafe(_goodEndImage, false);
            SetActiveSafe(_normalEndImage, false);
            SetActiveSafe(_badEndImage, false);

            if (count >= _badEndThreshold)
                SetActiveSafe(_badEndImage, true);
            else if (count >= _normalEndThreshold)
                SetActiveSafe(_normalEndImage, true);
            else
                SetActiveSafe(_goodEndImage, true);
        }

        private void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
