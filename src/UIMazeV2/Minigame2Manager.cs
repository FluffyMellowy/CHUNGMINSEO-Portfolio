using Cysharp.Threading.Tasks;
using DG.Tweening;
using KanKikuchi.AudioManager;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム2（縮小ウィンドウ＋障害物落下）の死亡演出・リセットを管理する
    /// 死亡時:障害物停止→障害物ウィンドウを元の位置に戻す→メインウィンドウを元サイズに復元→プレイヤーをスタートへ移動→再開
    /// </summary>
    public class Minigame2Manager : MonoBehaviour
    {
        [Header("死亡演出")]
        [SerializeField] private int _flashCount = 5; // 点滅回数
        [SerializeField] private float _flashInterval = 0.1f; // 点滅間隔（秒）
        [SerializeField] private float _tweenDuration = 0.5f; // 各DOTweenアニメーションの時間（秒）

        [Header("参照")]
        [SerializeField] private Transform _player; // プレイヤーの参照
        [SerializeField] private Transform _spawnPoint; // リスポーン位置
        [SerializeField] private PlatformerPlayer _controller; // プレイヤーコントローラー
        [SerializeField] private SpriteRenderer _playerSprite; // プレイヤーのスプライト

        [Header("ウィンドウ")]
        [SerializeField] private WindowShrink _windowShrink; // ウィンドウ縮小スクリプト
        [SerializeField] private Transform _windowFrame; // メインウィンドウフレーム
        [SerializeField] private MonoBehaviour _windowFrameTracker; // ウィンドウフレーム追従スクリプト

        [Header("障害物")]
        [SerializeField] private ObstacleSpawner _obstacleSpawner; // 障害物スポナー
        [SerializeField] private ObstacleWindow[] _obstacleWindows; // 障害物ウィンドウ配列

        [Header("サウンド")]
        [SerializeField] private string _deathSEPath = "SE_Chung/SE5"; // 失敗音
        [SerializeField] private string _resetSEPath = "";             // リセット演出SE(任意)

        public static event System.Action OnPlayerRespawn; // リスポーン完了イベント

        private bool _isDead;
        private Vector3 _windowInitialPos; // メインウィンドウの初期位置
        private Vector3 _windowInitialScale; // メインウィンドウの初期スケール
        private Vector3[] _obstacleInitialPositions; // 障害物ウィンドウの初期位置
        private System.Action _deathHandler;

        private void Awake()
        {
            if (_windowFrame != null)
            {
                _windowInitialPos = _windowFrame.position;
                _windowInitialScale = _windowFrame.localScale;
            }

            // 障害物ウィンドウの初期位置を保存
            if (_obstacleWindows != null)
            {
                _obstacleInitialPositions = new Vector3[_obstacleWindows.Length];
                for (int i = 0; i < _obstacleWindows.Length; i++)
                {
                    if (_obstacleWindows[i] != null)
                        _obstacleInitialPositions[i] = _obstacleWindows[i].transform.position;
                }
            }
        }

        private void OnEnable()
        {
            _isDead = false;
            FallingObstacle.DroppedParent = transform; // 落下障害物の共通親を自分に設定
            _deathHandler = () => OnPlayerDeath();
            PlatformerDeathZone.OnPlayerDeath += _deathHandler;
        }

        private void OnDisable()
        {
            PlatformerDeathZone.OnPlayerDeath -= _deathHandler;
        }

        private void OnPlayerDeath()
        {
            if (_isDead) return;
            HandleDeath().Forget();
        }

        /// <summary>
        /// 死亡演出→障害物リセット→ウィンドウ復元→プレイヤー移動→再開
        /// </summary>
        private async UniTaskVoid HandleDeath()
        {
            _isDead = true;
            _controller.Kill();
            _windowFrameTracker.enabled = false;

            SafeSE.Play(_deathSEPath);
            SafeSE.Play(_resetSEPath);

            // 障害物を停止
            StopObstacles();
            _windowShrink.enabled = false;

            // 点滅+全リセットアニメーションを同時実行
            await UniTask.WhenAll(
                FlashSprite(),
                MoveObstacleWindowsToInitial(),
                ResetWindowFrame(),
                _player
                    .DOMove(_spawnPoint.position, _tweenDuration)
                    .SetEase(Ease.OutCubic)
                    .ToUniTask(cancellationToken: destroyCancellationToken)
            );

            // 全リセットして再開
            _controller.Revive();
            _windowFrameTracker.enabled = true;
            _windowShrink.enabled = true;

            // 障害物を再起動
            RestartObstacles();

            _isDead = false;
            OnPlayerRespawn?.Invoke();
        }

        /// <summary>
        /// 全障害物を停止する
        /// </summary>
        private void StopObstacles()
        {
            // 自分の子にある落下済み障害物を削除
            var droppedObstacles = GetComponentsInChildren<FallingObstacle>();
            foreach (var obs in droppedObstacles)
            {
                Destroy(obs.gameObject);
            }

            // 障害物ウィンドウを無効化（子の障害物も含めて停止）
            foreach (var window in _obstacleWindows)
            {
                if (window != null)
                    window.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// スプライト点滅演出
        /// </summary>
        private async UniTask FlashSprite()
        {
            for (int i = 0; i < _flashCount; i++)
            {
                _playerSprite.enabled = false;
                await UniTask.Delay((int)(_flashInterval * 1000), cancellationToken: destroyCancellationToken);
                _playerSprite.enabled = true;
                await UniTask.Delay((int)(_flashInterval * 1000), cancellationToken: destroyCancellationToken);
            }
        }

        /// <summary>
        /// メインウィンドウを初期サイズ・位置に復元する
        /// </summary>
        private async UniTask ResetWindowFrame()
        {
            await UniTask.WhenAll(
                _windowFrame
                    .DOMove(_windowInitialPos, _tweenDuration)
                    .SetEase(Ease.OutCubic)
                    .ToUniTask(cancellationToken: destroyCancellationToken),
                _windowFrame
                    .DOScale(_windowInitialScale, _tweenDuration)
                    .SetEase(Ease.OutCubic)
                    .ToUniTask(cancellationToken: destroyCancellationToken)
            );
        }

        /// <summary>
        /// 障害物ウィンドウをDOTweenで元の位置に戻す
        /// </summary>
        private async UniTask MoveObstacleWindowsToInitial()
        {
            var tasks = new System.Collections.Generic.List<UniTask>();

            for (int i = 0; i < _obstacleWindows.Length; i++)
            {
                if (_obstacleWindows[i] == null) continue;

                // 非アクティブ状態でもTransformは操作可能
                var task = _obstacleWindows[i].transform
                    .DOMove(_obstacleInitialPositions[i], _tweenDuration)
                    .SetEase(Ease.OutCubic)
                    .ToUniTask(cancellationToken: destroyCancellationToken);
                tasks.Add(task);
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 障害物ウィンドウを再起動する
        /// </summary>
        private void RestartObstacles()
        {
            foreach (var window in _obstacleWindows)
            {
                if (window == null) continue;
                window.gameObject.SetActive(true);
            }
        }
    }
}
