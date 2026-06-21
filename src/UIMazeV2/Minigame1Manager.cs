using Cysharp.Threading.Tasks;
using DG.Tweening;
using KanKikuchi.AudioManager;
using St;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム1の死亡演出・ウィンドウ演出・リセットを管理する
    /// </summary>
    public class Minigame1Manager : MonoBehaviour
    {
        [Header("死亡演出")]
        [SerializeField] private float _deathDelay = 1f; // 死亡後リセットまでの待機時間（秒）
        [SerializeField] private int _flashCount = 5; // 点滅回数
        [SerializeField] private float _flashInterval = 0.1f; // 点滅間隔（秒）

        [Header("ウィンドウ演出")]
        [SerializeField] private Transform _windowFrame; // ウィンドウフレームのTransform
        [SerializeField] private WindowFrame _windowFrameTracker; // ウィンドウフレームの追従スクリプト
        [SerializeField] private float _windowCloseDuration = 0.3f; // 閉じるアニメーションの時間
        [SerializeField] private float _windowOpenDuration = 0.4f; // 開くアニメーションの時間

        [Header("参照")]
        [SerializeField] private Transform _player; // プレイヤーの参照
        [SerializeField] private Transform _spawnPoint; // リスポーン位置
        [SerializeField] private TopViewPlayer _controller; // プレイヤーコントローラー
        [SerializeField] private SpriteRenderer _playerSprite; // プレイヤーのスプライト
        [SerializeField] private GhostManager _ghostManager; // 分身マネージャー
        [Tooltip("2Dゲーム描画用のカメラ。3D背景カメラと分離している場合に明示的に指定。未指定なら Camera.main を使用")]
        [SerializeField] private Camera _mainCamera; // 2Dゲーム用カメラ（インスペクター指定）

        [Header("サウンド")]
        [SerializeField] private string _deathSEPath = "SE_Chung/SE5";        // 失敗音
        [SerializeField] private string _windowCloseSEPath = "";              // ウィンドウ閉じSE(任意、サウンド指定無しなら無音)
        [SerializeField] private string _windowOpenSEPath = "SE_Chung/SE2";   // ウィンドウ出現音
        [SerializeField] private string _respawnNextStageSEPath = "SE_Chung/SE4"; // リスポーンでウィンドウが再び開く瞬間のSE(次のステージ音と同一)

        /// <summary>
        /// 死亡中かどうか（外部から参照用）
        /// </summary>
        public bool IsDead { get; private set; }
        private Vector3 _windowInitialScale; // ウィンドウの初期スケール
        private float _windowHalfW; // ウィンドウフレームの半幅
        private float _windowHalfH; // ウィンドウフレームの半高

        private void Awake()
        {
            if (_windowFrame != null)
            {
                _windowInitialScale = _windowFrame.localScale;
                var sr = _windowFrame.GetComponent<SpriteRenderer>();
                _windowHalfW = sr.bounds.size.x * 0.5f;
                _windowHalfH = sr.bounds.size.y * 0.5f;
            }
            // インスペクターで明示指定されていなければCamera.mainにフォールバック
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        /// <summary>
        /// プレイヤー死亡時に呼ばれる（GhostManager等から）
        /// IsDeadガードで二重カウントを防ぐ
        /// </summary>
        public void OnPlayerDeath()
        {
            if (IsDead) return;
            if (GameoverCounter.Instance != null) GameoverCounter.Instance.Add();
            HandleDeath().Forget();
        }

        /// <summary>
        /// 死亡演出→ウィンドウ閉じ→リセット→ウィンドウ開き→操作再開
        /// </summary>
        private async UniTaskVoid HandleDeath()
        {
            IsDead = true;
            _controller.enabled = false;

            // コントローラーをdisableしただけだとRigidbody2DのlinearVelocityが残って
            // 死亡演出中もプレイヤーが滑り続けるため、明示的にゼロ化＋物理停止
            var rb = _player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            _ghostManager.StopGhost();

            PlaySE(_deathSEPath);

            // スプライト点滅演出
            for (int i = 0; i < _flashCount; i++)
            {
                _playerSprite.enabled = false;
                await UniTask.Delay((int)(_flashInterval * 1000), cancellationToken: destroyCancellationToken);
                _playerSprite.enabled = true;
                await UniTask.Delay((int)(_flashInterval * 1000), cancellationToken: destroyCancellationToken);
            }

            _playerSprite.enabled = false;

            // ウィンドウ閉じる演出
            _windowFrameTracker.enabled = false;
            PlaySE(_windowCloseSEPath);
            await _windowFrame
                .DOScale(Vector3.zero, _windowCloseDuration)
                .SetEase(Ease.InBack)
                .ToUniTask(cancellationToken: destroyCancellationToken);

            // ウィンドウが閉じたら分身を削除
            _ghostManager.ResetGhost();

            // リセット待機
            await UniTask.Delay((int)(_deathDelay * 1000), cancellationToken: destroyCancellationToken);

            // プレイヤーをスポーン位置（_spawnPoint = StartPoint）に戻してからウィンドウを開く
            // 初期入場時のWindowManager._spawnPoints[0]と同じTransformを指す想定
            _player.position = _spawnPoint.position;

            // ウィンドウ開く演出（カメラ境界内にクランプした位置で開く）
            _windowFrame.position = ClampToCamera(_spawnPoint.position);
            _windowFrame.localScale = Vector3.zero;
            // SE2(ウィンドウ出現)とSE4(次のステージ音)を同時にトリガー。SE2は途中でカットされてもOK
            PlaySE(_windowOpenSEPath);
            PlaySE(_respawnNextStageSEPath);
            await _windowFrame
                .DOScale(_windowInitialScale, _windowOpenDuration)
                .SetEase(Ease.OutBack)
                .ToUniTask(cancellationToken: destroyCancellationToken);

            // 分身をリセット
            _ghostManager.ResetGhost();
            _playerSprite.enabled = true;
            _windowFrameTracker.enabled = true;

            // 物理シミュレーション再開（HandleDeathの冒頭でsimulated=falseにしたため）
            // velocityも念のためゼロから再開
            var rbRevive = _player.GetComponent<Rigidbody2D>();
            if (rbRevive != null)
            {
                rbRevive.linearVelocity = Vector2.zero;
                rbRevive.angularVelocity = 0f;
                rbRevive.simulated = true;
            }

            _controller.enabled = true; // TopViewPlayer.OnEnableがgravity=0を再設定
            IsDead = false;
        }

        /// <summary>
        /// SEを再生する。SafeSEに委譲（空パス/未登録パスは安全にスキップ）
        /// </summary>
        private void PlaySE(string path) => SafeSE.Play(path);

        /// <summary>
        /// WindowFrameと同じロジックでカメラ境界内にクランプする。CameraClamp共通ヘルパー経由
        /// </summary>
        private Vector3 ClampToCamera(Vector3 targetPos)
        {
            // GameManager経由のシーン遷移でAwake時にCamera.mainがTitleシーン側を掴むケースがある
            // その後Titleシーンがアンロードされて_mainCameraが破棄状態になり、
            // 死亡時のHandleDeath内でMissingReferenceExceptionが発生してリトライ不能に陥る
            // 使用直前に有効性をチェックし、必要なら再取得する
            if (_mainCamera == null) _mainCamera = Camera.main;

            Vector3 input = new Vector3(targetPos.x, targetPos.y, _windowFrame.position.z);
            if (_mainCamera == null) return input; // 最終フォールバック：クランプせずそのまま返す
            return CameraClamp.ClampToCamera(input, _mainCamera, _windowHalfW, _windowHalfH);
        }
    }
}
