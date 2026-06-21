using Language;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// 1つのウィンドウ内で静的なステージ全体（発板群）を上から下にスクロールさせる
    /// 仕様書のレイアウトを編集時にそのまま配置できる構造
    /// 2つのインスタンスが同時に動作する想定（ウィンドウA/B）
    ///
    /// 言語別に2つのステージRoot（JP/EN）を持ち、Start時にLanguageManagerに応じて
    /// 一方を選択してアクティブにし、もう一方は非アクティブ化する
    /// 選択されたステージ配下のRigidbody2D KinematicにMovePositionで
    /// スクロール量+ MovingTextPlatformの横揺れを合算して適用する
    /// </summary>
    public class CreditScroller : MonoBehaviour
    {
        [Header("スクロール設定")]
        [SerializeField] private float _scrollSpeed = 2f; // スクロール速度（unit/秒）

        [Header("ステージ参照（言語別）")]
        [Tooltip("JP用ステージのRoot。発板群を子に持つTransform。LanguageがJPの時に使われる")]
        [SerializeField] private Transform _stageRootJP;
        [Tooltip("EN用ステージのRoot。発板群を子に持つTransform。LanguageがENの時に使われる。未設定ならJPにフォールバック")]
        [SerializeField] private Transform _stageRootEN;

        [Header("着地ゲート")]
        [Tooltip("ミニゲーム3はPlatformerPlayerBが唯一のプレイヤー。型をPlatformerPlayerBに限定して" +
                 "PlatformerPlayerAが誤って割り当てられるのを防ぐ。CreditScrollerA/B両方の" +
                 "_playerに同じインスタンスを割り当てると、初着地のフレームでA/Bが同時にスクロール開始する")]
        [SerializeField] private PlatformerPlayerB _player;

        private Transform _stageRoot; // 起動時に言語を見て_stageRootJP / _stageRootENのどちらかが入る
        private Rigidbody2D[] _cachedRigidbodies; // 起動時にキャッシュした子のRB2D
        private MovingTextPlatform[] _cachedMovers; // 同インデックスの動くテキスト（無ければnull）
        private Vector2[] _initialPositions; // 各発板の初期位置（リセット時に戻す先）
        private TeleportPlatform[] _cachedTeleports; // 死亡リセット時に復元するテレポート群
        private bool _isScrolling; // 着地してスクロール開始済みか

        private void Start()
        {
            SelectStageByLanguage();
            CacheChildren();
        }

        /// <summary>
        /// LanguageManagerの現在言語に応じて使用するステージRootを決定する
        /// 選んだ側をアクティブ化、もう一方は非アクティブ化（描画/物理コスト削減）
        /// LanguageManagerが居ない、またはENのRootが未設定ならJPにフォールバック
        /// </summary>
        private void SelectStageByLanguage()
        {
            var lang = LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : LanguageManager.Language.JP;

            bool useEN = (lang == LanguageManager.Language.EN) && (_stageRootEN != null);
            _stageRoot = useEN ? _stageRootEN : _stageRootJP;

            if (_stageRootJP != null) _stageRootJP.gameObject.SetActive(_stageRoot == _stageRootJP);
            if (_stageRootEN != null) _stageRootEN.gameObject.SetActive(_stageRoot == _stageRootEN);
        }

        /// <summary>
        /// _stageRoot配下の発板を一度だけ走査してキャッシュする
        /// 同時に各発板の初期位置を記録し、ResetStage()で復元できるようにする
        /// 静的ステージ前提のため、ランタイム追加には未対応
        /// </summary>
        private void CacheChildren()
        {
            if (_stageRoot == null)
            {
                _cachedRigidbodies = System.Array.Empty<Rigidbody2D>();
                _cachedMovers = System.Array.Empty<MovingTextPlatform>();
                _initialPositions = System.Array.Empty<Vector2>();
                _cachedTeleports = System.Array.Empty<TeleportPlatform>();
                return;
            }

            _cachedRigidbodies = _stageRoot.GetComponentsInChildren<Rigidbody2D>(true);
            _cachedMovers = new MovingTextPlatform[_cachedRigidbodies.Length];
            _initialPositions = new Vector2[_cachedRigidbodies.Length];
            for (int i = 0; i < _cachedRigidbodies.Length; i++)
            {
                _cachedMovers[i] = _cachedRigidbodies[i].GetComponent<MovingTextPlatform>();
                _initialPositions[i] = _cachedRigidbodies[i].position;
            }

            // 死亡リセット時に Consume 状態を巻き戻すためテレポートも別途キャッシュ
            _cachedTeleports = _stageRoot.GetComponentsInChildren<TeleportPlatform>(true);
        }

        /// <summary>
        /// 言語変更時に呼ばれる。新しい言語に応じてステージRootを選び直し、キャッシュも作り直して初期位置に戻す
        /// MinigameDebugOverlayの言語切替ボタンなど、ランタイムでの言語スワップ用エントリポイント
        /// </summary>
        public void ReinitializeForLanguageChange()
        {
            SelectStageByLanguage();
            CacheChildren();
            ResetStage();
        }

        /// <summary>
        /// ステージを起動時の状態に戻す（プレイヤー死亡からのリスポーン時に呼ばれる）
        /// 各発板を初期位置へ戻し、MovingTextPlatformの位相もリセットする
        /// </summary>
        public void ResetStage()
        {
            // 着地ゲートを再武装。プレイヤーが再び地面に着くまでスクロール再開しない
            _isScrolling = false;

            if (_cachedRigidbodies == null) return;

            for (int i = 0; i < _cachedRigidbodies.Length; i++)
            {
                var rb = _cachedRigidbodies[i];
                if (rb == null) continue;

                rb.position = _initialPositions[i];
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                var mover = _cachedMovers[i];
                if (mover != null) mover.ResetMotion();
            }

            // テレポート群もConsume状態から戻す。ペア両方が同一or別ウィンドウのどちらにあっても、
            // CreditMinigameManagerが両ウィンドウのResetStageを呼ぶので結果的に全テレポートが復元される
            if (_cachedTeleports != null)
            {
                for (int i = 0; i < _cachedTeleports.Length; i++)
                {
                    if (_cachedTeleports[i] != null) _cachedTeleports[i].ResetState();
                }
            }
        }

        /// <summary>
        /// 紐づけたPlatformerPlayerBが接地済みかチェック（IsGroundedは基底クラスPlatformerPlayerの公開プロパティ）
        /// </summary>
        private bool PlayerLanded()
        {
            return _player != null && _player.IsGrounded;
        }

        /// <summary>
        /// 物理ステップで各発板にスクロール量と動きの合算をMovePositionで適用する
        /// プレイヤー乗車時の安定性のため、1発板につき1回のMovePositionにまとめる
        /// </summary>
        private void FixedUpdate()
        {
            if (_cachedRigidbodies == null || _cachedRigidbodies.Length == 0) return;

            // 着地ゲート:プレイヤーが地面に着いた最初のフレームでスクロール開始
            if (!_isScrolling)
            {
                if (!PlayerLanded())
                {
                    // 診断: 毎フレームスパムしないよう約1秒に1回だけログ出力。
                    // _player が null か / IsGrounded が false かを切り分けるための情報を流す
                    if (Time.frameCount % 60 == 0)
                        Debug.Log($"[CreditScroller:{name}] waiting for land: _player={(_player!=null)} grounded={(_player!=null && _player.IsGrounded)}", this);
                    return;
                }
                _isScrolling = true;
                Debug.Log($"[CreditScroller:{name}] player landed → scroll START", this);
            }

            float dt = Time.fixedDeltaTime;
            Vector2 scrollDelta = Vector2.down * (_scrollSpeed * dt);

            for (int i = 0; i < _cachedRigidbodies.Length; i++)
            {
                var rb = _cachedRigidbodies[i];
                if (rb == null) continue;

                Vector2 totalDelta = scrollDelta;
                var mover = _cachedMovers[i];
                if (mover != null) totalDelta += mover.GetFrameDelta(dt);

                rb.MovePosition(rb.position + totalDelta);
            }
        }
    }
}
