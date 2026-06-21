using Cysharp.Threading.Tasks;
using DG.Tweening;
using KanKikuchi.AudioManager;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム3（クレジットスクロール）の管理
    /// 2ウィンドウ間のテレポート、および死亡演出・リセットを担当する
    /// Minigame3DeathZoneに接触すると死亡シーケンスが発動する
    /// </summary>
    public class CreditMinigameManager : MonoBehaviour
    {
        [Header("ウィンドウ参照")]
        [SerializeField] private CreditScroller _windowA; // 左ウィンドウ
        [SerializeField] private CreditScroller _windowB; // 右ウィンドウ
        [SerializeField] private Transform _player; // プレイヤー

        [Header("死亡演出")]
        [SerializeField] private float _deathDelay = 1f; // 死亡後リセットまでの待機時間（秒）
        [SerializeField] private int _flashCount = 5; // 点滅回数
        [SerializeField] private float _flashInterval = 0.1f; // 点滅間隔（秒）

        [Header("ウィンドウ演出")]
        [SerializeField] private Transform _windowFrameA; // ウィンドウAのフレームTransform
        [SerializeField] private Transform _windowFrameB; // ウィンドウBのフレームTransform
        [SerializeField] private MonoBehaviour _windowFrameTrackerA; // ウィンドウAの追従スクリプト
        [SerializeField] private MonoBehaviour _windowFrameTrackerB; // ウィンドウBの追従スクリプト
        [SerializeField] private float _windowCloseDuration = 0.3f;
        [SerializeField] private float _windowOpenDuration = 0.4f;

        [Header("リスポーン")]
        [SerializeField] private Transform _spawnPoint; // 初期スポーン位置（ウィンドウAの開始地点）
        [SerializeField] private PlatformerPlayerB _controller; // プレイヤーコントローラー（ミニゲーム3専用サブクラス）
        [SerializeField] private SpriteRenderer _playerSprite; // プレイヤースプライト

        [Header("サウンド")]
        [SerializeField] private string _deathSEPath = "SE_Chung/SE5";        // 失敗音
        [SerializeField] private string _windowCloseSEPath = "";              // ウィンドウ閉じSE(任意)
        [SerializeField] private string _windowOpenSEPath = "SE_Chung/SE2";   // ウィンドウ出現音
        [SerializeField] private string _respawnNextStageSEPath = "SE_Chung/SE4"; // リスポーンでウィンドウ再オープン時のSE(次のステージ音)

        [Header("テレポート演出（Feel）")]
        [Tooltip("ポータルを通った瞬間に再生する全体演出のMMF_Player。空ならスキップ。" +
                 "画面全体フラッシュ/SE/ポーズ等をまとめる用。" +
                 "プレイヤーサイズの局所フラッシュは _teleportFlashPrefab で別途扱う")]
        [SerializeField] private MMF_Player _teleportFeedback;

        [Tooltip("テレポート瞬間に出発側と到着側それぞれの位置でInstantiateするフラッシュ用プレハブ。" +
                 "プレイヤーキャラと同じくらいのサイズのSpriteRenderer(白)+短いトゥイーン/Animatorで作る。" +
                 "プレハブ側で自動Destroy(寿命2秒程度)を仕込んでおく前提。" +
                 "空ならスキップ")]
        [SerializeField] private GameObject _teleportFlashPrefab;
        [Tooltip("Instantiateしたフラッシュの自動破棄までの寿命(秒)。プレハブ側にAutoDestroyが無くてもこれで掃除される")]
        [SerializeField] private float _teleportFlashLifetimeSec = 1.0f;

        public static event System.Action OnPlayerRespawn;

        private CreditScroller _currentWindow; // プレイヤーが現在いるウィンドウ
        private bool _isDead;
        private Vector3 _windowInitialScaleA;
        private Vector3 _windowInitialScaleB;
        private System.Action _deathHandler;

        private void Awake()
        {
            if (_windowFrameA != null) _windowInitialScaleA = _windowFrameA.localScale;
            if (_windowFrameB != null) _windowInitialScaleB = _windowFrameB.localScale;
        }

        private void OnEnable()
        {
            TeleportPlatform.OnTeleport += HandleTeleport;

            _isDead = false;
            _deathHandler = () => OnPlayerDeath();
            Minigame3DeathZone.OnPlayerDeath += _deathHandler;
        }

        private void OnDisable()
        {
            TeleportPlatform.OnTeleport -= HandleTeleport;
            Minigame3DeathZone.OnPlayerDeath -= _deathHandler;
        }

        private void Start()
        {
            _currentWindow = _windowA;
        }

        /// <summary>
        /// テレポート発動時、現在ウィンドウを到着点の所属ウィンドウへ更新する
        /// プレイヤー位置の移動はTeleportPlatform側で完了済みなので、ここでは追跡情報のみ同期する
        /// あわせて全体演出(_teleportFeedback)と、出発/到着両位置の局所フラッシュ(_teleportFlashPrefab)を発火する
        /// </summary>
        private void HandleTeleport(TeleportPlatform source, TeleportPlatform destination)
        {
            Debug.Log($"[CreditMinigameManager] HandleTeleport called. source={(source!=null?source.name:"null")} dest={(destination!=null?destination.name:"null")} prefab={(_teleportFlashPrefab!=null?_teleportFlashPrefab.name:"NULL")}", this);

            if (destination == null) return;

            // 到着点の親階層からCreditScrollerを引いて現在ウィンドウを判定
            var scroller = destination.GetComponentInParent<CreditScroller>();
            if (scroller != null) _currentWindow = scroller;

            // 全体演出（空ならスキップ）
            if (_teleportFeedback != null) _teleportFeedback.PlayFeedbacks();

            // 局所フラッシュ：出発側 + 到着側、両方の位置にプレハブをInstantiateして
            // プレイヤーキャラのサイズ感で「シュッと消えてパッと出る」表現を出す
            if (_teleportFlashPrefab != null)
            {
                if (source != null)
                    SpawnTeleportFlash(source.transform.position);
                SpawnTeleportFlash(destination.transform.position);
            }
            else
            {
                Debug.LogWarning("[CreditMinigameManager] _teleportFlashPrefab is NULL - flash effect skipped", this);
            }
        }

        /// <summary>
        /// 指定位置にフラッシュプレハブをInstantiateし、寿命経過後にDestroyする。
        /// プレハブ自体にFeel演出やAnimatorが入っている想定。ここでは寿命管理だけする
        /// </summary>
        private void SpawnTeleportFlash(Vector3 worldPos)
        {
            var go = Instantiate(_teleportFlashPrefab, worldPos, Quaternion.identity);
            Debug.Log($"[CreditMinigameManager] SpawnTeleportFlash at {worldPos}, instance active={go.activeSelf}, activeInHierarchy={go.activeInHierarchy}", go);

            // プレハブ側にAutoDestroyが無い場合に備えて、寿命でDestroyする保険
            if (_teleportFlashLifetimeSec > 0f) Destroy(go, _teleportFlashLifetimeSec);
        }

        /// <summary>
        /// 現在プレイヤーがいるウィンドウを返す
        /// </summary>
        public CreditScroller GetCurrentWindow() => _currentWindow;

        private void OnPlayerDeath()
        {
            if (_isDead) return;
            HandleDeath().Forget();
        }

        /// <summary>
        /// 死亡演出→両ウィンドウ閉じ→プレイヤーリセット→両ウィンドウ開き→操作再開
        /// </summary>
        private async UniTaskVoid HandleDeath()
        {
            _isDead = true;
            _controller.Kill();

            PlaySE(_deathSEPath);

            // スプライト点滅
            for (int i = 0; i < _flashCount; i++)
            {
                _playerSprite.enabled = false;
                await UniTask.Delay((int)(_flashInterval * 1000), cancellationToken: destroyCancellationToken);
                _playerSprite.enabled = true;
                await UniTask.Delay((int)(_flashInterval * 1000), cancellationToken: destroyCancellationToken);
            }

            _playerSprite.enabled = false;

            // 両ウィンドウを閉じる
            if (_windowFrameTrackerA != null) _windowFrameTrackerA.enabled = false;
            if (_windowFrameTrackerB != null) _windowFrameTrackerB.enabled = false;
            PlaySE(_windowCloseSEPath);

            var closeTasks = new System.Collections.Generic.List<UniTask>();
            if (_windowFrameA != null)
                closeTasks.Add(_windowFrameA.DOScale(Vector3.zero, _windowCloseDuration).SetEase(Ease.InBack).ToUniTask(cancellationToken: destroyCancellationToken));
            if (_windowFrameB != null)
                closeTasks.Add(_windowFrameB.DOScale(Vector3.zero, _windowCloseDuration).SetEase(Ease.InBack).ToUniTask(cancellationToken: destroyCancellationToken));
            await UniTask.WhenAll(closeTasks);

            await UniTask.Delay((int)(_deathDelay * 1000), cancellationToken: destroyCancellationToken);

            // プレイヤーをスポーンに戻し、現在ウィンドウをAに戻す
            _player.position = _spawnPoint.position;
            _currentWindow = _windowA;

            // ステージスクロール位置と動く発板の位相を初期状態へ戻す
            // ウィンドウが閉じている間に行うことで、巻き戻しの見た目を隠せる
            if (_windowA != null) _windowA.ResetStage();
            if (_windowB != null) _windowB.ResetStage();

            // 両ウィンドウを開く。SE2(ウィンドウ出現)とSE4(次のステージ)を同時に。SE2は途中カットOK
            PlaySE(_windowOpenSEPath);
            PlaySE(_respawnNextStageSEPath);
            var openTasks = new System.Collections.Generic.List<UniTask>();
            if (_windowFrameA != null)
            {
                _windowFrameA.localScale = Vector3.zero;
                openTasks.Add(_windowFrameA.DOScale(_windowInitialScaleA, _windowOpenDuration).SetEase(Ease.OutBack).ToUniTask(cancellationToken: destroyCancellationToken));
            }
            if (_windowFrameB != null)
            {
                _windowFrameB.localScale = Vector3.zero;
                openTasks.Add(_windowFrameB.DOScale(_windowInitialScaleB, _windowOpenDuration).SetEase(Ease.OutBack).ToUniTask(cancellationToken: destroyCancellationToken));
            }
            await UniTask.WhenAll(openTasks);

            // 操作再開
            _controller.Revive();
            _playerSprite.enabled = true;
            if (_windowFrameTrackerA != null) _windowFrameTrackerA.enabled = true;
            if (_windowFrameTrackerB != null) _windowFrameTrackerB.enabled = true;
            _isDead = false;
            OnPlayerRespawn?.Invoke();
        }

        /// <summary>
        /// SEを再生する。SafeSEに委譲（空パス/未登録パスは安全にスキップ）
        /// </summary>
        private void PlaySE(string path) => SafeSE.Play(path);
    }
}
