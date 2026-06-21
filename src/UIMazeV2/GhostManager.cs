using System.Collections.Generic;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーの移動経路を記録し、一定時間後に分身を生成する
    /// 分身がプレイヤーに接触するとMinigame1Managerに通知する
    /// </summary>
    public class GhostManager : MonoBehaviour
    {
        [Header("分身設定")]
        [SerializeField] private float _spawnDelay = 5f; // 最初の移動から分身生成までの遅延（秒）
        [SerializeField] private Transform _player; // プレイヤーの参照
        [SerializeField] private GameObject _ghostPrefab; // 分身プレハブ

        [Header("記録設定")]
        [SerializeField] private float _recordInterval = 0.05f; // 経路記録の間隔（秒）

        [Header("参照")]
        [SerializeField] private Minigame1Manager _minigameManager; // ミニゲーム1マネージャー
        [SerializeField] private Transform _spawnPoint; // スポーン位置（移動開始の基準点）

        [Header("移動開始検知")]
        [Tooltip("プレイヤーがこの距離以上動いたら「移動開始」と判定する。" +
                 "シーン遷移直後の物理同期で起きる微小ズレを無視するため、0.01程度ではなく0.3〜0.5を推奨")]
        [SerializeField] private float _movementThreshold = 0.5f;
        [Tooltip("距離に加えて、プレイヤーのRigidbody2D速度がこの値より大きいことも要求するか。" +
                 "TopViewPlayerはMovePositionで動くためvelocityが常に0になりがちで、" +
                 "ONにすると永久に移動開始判定されない。距離だけで十分なケースが多いのでデフォルトOFF")]
        [SerializeField] private bool _requireVelocity = false;
        [Tooltip("_requireVelocity=true 時、velocityのsqrMagnitudeがこの値より大きい必要がある")]
        [SerializeField] private float _velocitySqrThreshold = 0.01f;

        private readonly List<Vector3> _recordedPath = new(); // 記録した経路
        private float _recordTimer; // 記録タイマー
        private float _spawnTimer; // 生成タイマー
        private bool _hasStartedMoving; // 移動開始フラグ
        private bool _waitingForPosition; // 位置確定待ちフラグ
        private Vector3 _startPosition; // プレイヤーの初期位置
        private GhostClone _activeGhost; // 生成された分身

        /// <summary>
        /// 分身の状態をリセットする（Minigame1Managerから呼ばれる）
        /// </summary>
        public void ResetGhost()
        {
            _waitingForPosition = true;
            _hasStartedMoving = false;
            _recordTimer = 0f;
            _spawnTimer = 0f;
            _recordedPath.Clear();

            if (_activeGhost != null)
            {
                Destroy(_activeGhost.gameObject);
                _activeGhost = null;
            }
        }

        /// <summary>
        /// 分身を停止する（死亡演出中に呼ばれる）
        /// </summary>
        public void StopGhost()
        {
            if (_activeGhost != null)
                _activeGhost.enabled = false;
        }

        private void OnEnable()
        {
            _hasStartedMoving = false;
            _waitingForPosition = true;
            _recordTimer = 0f;
            _spawnTimer = 0f;
            _recordedPath.Clear();
        }

        private void Update()
        {
            if (_player == null || _minigameManager.IsDead) return;

            // WindowBoundsのクランプ後にプレイヤーの実際の位置を記録する
            if (_waitingForPosition)
            {
                _startPosition = _player.position;
                _waitingForPosition = false;
                return;
            }

            // 最初の移動を検知。シーン遷移直後の物理同期で起きる微小ズレで誤検知して
            // 「動いてないのにゴーストが沸いて即死」になるのを防ぐため、
            // 距離だけでなくRigidbody2D速度の有無も条件に入れる
            if (!_hasStartedMoving)
            {
                bool movedFar = Vector2.Distance(_player.position, _startPosition) > _movementThreshold;
                bool hasVelocity = true;
                if (_requireVelocity)
                {
                    var rb = _player.GetComponent<Rigidbody2D>();
                    hasVelocity = (rb != null) && (rb.linearVelocity.sqrMagnitude > _velocitySqrThreshold);
                }
                if (movedFar && hasVelocity)
                {
                    _hasStartedMoving = true;
                }
                return;
            }

            // 経路を記録
            _recordTimer += Time.deltaTime;
            if (_recordTimer >= _recordInterval)
            {
                _recordTimer = 0f;
                _recordedPath.Add(_player.position);
            }

            // 分身がまだない場合、タイマーで生成
            if (_activeGhost == null)
            {
                _spawnTimer += Time.deltaTime;
                if (_spawnTimer >= _spawnDelay)
                {
                    SpawnGhost();
                }
            }
        }

        /// <summary>
        /// 分身を初期位置に生成する
        /// </summary>
        private void SpawnGhost()
        {
            var ghostObj = Instantiate(_ghostPrefab, _startPosition, Quaternion.identity, transform);
            _activeGhost = ghostObj.GetComponent<GhostClone>();
            _activeGhost.Initialize(_recordedPath, _recordInterval, this);
        }

        /// <summary>
        /// 分身がプレイヤーに接触した時に呼ばれる
        /// </summary>
        public void OnGhostCaughtPlayer()
        {
            _minigameManager.OnPlayerDeath();
        }
    }
}
