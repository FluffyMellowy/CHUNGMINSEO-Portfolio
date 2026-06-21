using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIMazeV2
{
    public class ObstacleWindow : MonoBehaviour
    {
        [Header("タイミング設定")]
        [SerializeField] private Vector2 _startDelayRange = new(0f, 1f); // 開始遅延の範囲
        [SerializeField] private Vector2 _respawnDelayRange = new(0.5f, 2f); // 再出現遅延の範囲
        [SerializeField] private Vector2 _despawnDelayRange = new(0.3f, 1f); // 停止後待機の範囲
        [SerializeField] private Vector2 _fallSpeedRange = new(5f, 15f); // 落下速度の範囲

        [SerializeField] private GameObject _obstaclePrefab;

        private Rigidbody2D _rb;
        private bool _stopped;
        private Vector3 _initialPosition;
        private List<Vector3> _obstacleLocalPositions = new();
        private bool _initialized;
        private CancellationTokenSource _loopCts; // ループ制御用CTS

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _initialPosition = transform.position;

            foreach (Transform child in transform)
            {
                if (child.GetComponent<FallingObstacle>() != null)
                {
                    _obstacleLocalPositions.Add(child.localPosition);
                }
            }

            _initialized = true;
        }

        private void OnEnable()
        {
            if (!_initialized) return;

            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = new CancellationTokenSource();
            StartSequenceAsync(_loopCts.Token).Forget();
        }

        private void OnDisable()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;

            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private void OnDestroy()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
        }

        /// <summary>
        /// 遅延→落下→衝突→消滅→再出現のループ
        /// </summary>
        private async UniTaskVoid StartSequenceAsync(CancellationToken token)
        {
            while (true)
            {
                transform.position = _initialPosition;
                RegenerateObstacles();

                // ランダム開始遅延
                float startDelay = Random.Range(_startDelayRange.x, _startDelayRange.y);
                await UniTask.Delay((int)(startDelay * 1000), cancellationToken: token);

                // ランダム速度で落下開始
                _stopped = false;
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = 0f;
                _rb.linearVelocity = Vector2.down * Random.Range(_fallSpeedRange.x, _fallSpeedRange.y);

                await UniTask.WaitUntil(() => _stopped, cancellationToken: token);

                // ランダム停止後待機
                float despawnDelay = Random.Range(_despawnDelayRange.x, _despawnDelayRange.y);
                await UniTask.Delay((int)(despawnDelay * 1000), cancellationToken: token);

                // ランダム再出現待機
                float respawnDelay = Random.Range(_respawnDelayRange.x, _respawnDelayRange.y);
                await UniTask.Delay((int)(respawnDelay * 1000), cancellationToken: token);
            }
        }

        private void RegenerateObstacles()
        {
            var existing = GetComponentsInChildren<FallingObstacle>();
            foreach (var obs in existing)
            {
                Destroy(obs.gameObject);
            }

            foreach (var localPos in _obstacleLocalPositions)
            {
                var obj = Instantiate(_obstaclePrefab, transform);
                obj.transform.localPosition = localPos;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_stopped) return;
            if (!collision.collider.CompareTag("PlatformerWindow")) return;

            _stopped = true;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            ReleaseObstacles();
        }

        private void ReleaseObstacles()
        {
            var obstacles = GetComponentsInChildren<FallingObstacle>();
            foreach (var obstacle in obstacles)
            {
                obstacle.Drop();
            }
        }
    }
}