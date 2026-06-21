using Language;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIMazeV2
{
    /// <summary>
    /// UIMazeV2全体のクリア通知コンポーネント
    /// Modeで判定方式を切替：
    ///   Time     — 一定時間生存したらクリア（従来動作。死亡でタイマーリセット）
    ///   Trigger  — 指定したTransform（例：クレジット末尾のTMPスプライト）が
    /// このGameObjectのCollider2D（IsTrigger）に触れたらクリア
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SurvivalClear : MonoBehaviour
    {
        public enum ClearMode
        {
            Time,    // 時間ベース（旧仕様）
            Trigger, // ターゲット接触ベース
        }

        [Header("クリア判定方式")]
        [SerializeField] private ClearMode _mode = ClearMode.Trigger;

        [Header("Time モード設定")]
        [Tooltip("クリアまでの生存時間（秒）。Time モード時のみ使用")]
        [SerializeField] private float _survivalTime = 30f;

        [Header("Trigger モード設定（言語別）")]
        [Tooltip("JP用ターゲット。LanguageがJPの時、このTransform（または子Collider2D）が触れたらクリア発火")]
        [FormerlySerializedAs("_targetTransform")]
        [SerializeField] private Transform _targetTransformJP;
        [Tooltip("EN用ターゲット。LanguageがENの時に使用。未設定ならJP側にフォールバック")]
        [SerializeField] private Transform _targetTransformEN;

        private Transform _targetTransform; // 起動時/言語変更時に_targetTransformJP / _targetTransformENのどちらかが入る

        [Header("クリア通知")]
        [SerializeField] private SectionTypeEvent _clear;
        [SerializeField] private St.SectionType _sectionType;

        [Header("サウンド")]
        [Tooltip("クリア発火時に鳴らす成功SE。空文字なら無音")]
        [SerializeField] private string _successSEPath = "SE_Chung/SE8";

        private float _timer;
        private bool _cleared;

        private void Reset()
        {
            // 新規追加時、Collider2DはTriggerモードに自動設定
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnEnable()
        {
            _timer = 0f;
            _cleared = false;
            SelectTargetByLanguage();
            Minigame3DeathZone.OnPlayerDeath += ResetTimer;
        }

        /// <summary>
        /// LanguageManagerの現在言語に応じて使用するターゲットTransformを決定する
        /// EN指定でも_targetTransformENが未設定ならJPにフォールバック
        /// </summary>
        private void SelectTargetByLanguage()
        {
            var lang = LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.JP;

            bool useEN = (lang == LanguageManager.Language.EN) && (_targetTransformEN != null);
            _targetTransform = useEN ? _targetTransformEN : _targetTransformJP;
        }

        /// <summary>
        /// 言語変更時に外部から呼ばれる。ターゲットTransformを選び直す
        /// MinigameDebugOverlayの言語トグルや、本番の言語切替フックから利用
        /// </summary>
        public void ReinitializeForLanguageChange()
        {
            SelectTargetByLanguage();
        }

        private void OnDisable()
        {
            Minigame3DeathZone.OnPlayerDeath -= ResetTimer;
        }

        private void Update()
        {
            if (_cleared) return;
            if (_mode != ClearMode.Time) return;

            _timer += Time.deltaTime;
            if (_timer >= _survivalTime)
            {
                RaiseClear();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_cleared) return;
            if (_mode != ClearMode.Trigger)
            {
                Debug.Log($"[SurvivalClear] OnTriggerEnter2D fired but mode={_mode} (Triggerでないため無視) — other={other.name}");
                return;
            }
            if (_targetTransform == null)
            {
                Debug.LogWarning($"[SurvivalClear] _targetTransform 未設定。Triggerモードで動作しません。other={other.name}");
                return;
            }

            // 接触したColliderがターゲット本体、または子孫の場合にクリア発火
            // （TMPスプライト側のColliderが子オブジェクトに付いていても拾えるように）
            bool isMatch = other.transform == _targetTransform || other.transform.IsChildOf(_targetTransform);
            Debug.Log($"[SurvivalClear] OnTriggerEnter2D: other='{other.name}' (parent='{(other.transform.parent != null ? other.transform.parent.name : "null")}'), target='{_targetTransform.name}', match={isMatch}");
            if (isMatch)
            {
                RaiseClear();
            }
        }

        /// <summary>
        /// デバッグ用：外部から強制的にクリアを発火させる
        /// MinigameDebugOverlayの「クリア即時発火」ボタンから呼ばれる
        /// </summary>
        public void ForceClear()
        {
            if (_cleared) return;
            Debug.Log("[SurvivalClear] ForceClear called (debug override)");
            RaiseClear();
        }

        private void RaiseClear()
        {
            _cleared = true;

            if (_clear == null)
            {
                Debug.LogError($"[SurvivalClear] _clear (SectionTypeEvent SO) が未設定！ クリア発行できません (sectionType={_sectionType})");
                return;
            }

            Debug.Log($"[SurvivalClear] === CLEAR RAISED === mode={_mode}, sectionType={_sectionType}, eventSO='{_clear.name}' (frame={Time.frameCount}, time={Time.time:F2}s)");
            // 成功音(SE8)。実プレイで使われるクリア発火ポイント
            SafeSE.Play(_successSEPath);
            _clear.Raise(_sectionType);
            Debug.Log($"[SurvivalClear] _clear.Raise({_sectionType}) 呼び出し完了。GameManagerのStartNextSectionが反応すれば次のシーンへ");
        }

        private void ResetTimer()
        {
            // Timeモード用のリセット。Triggerモードではタイマーを使わないので影響なし
            _timer = 0f;
        }
    }
}
