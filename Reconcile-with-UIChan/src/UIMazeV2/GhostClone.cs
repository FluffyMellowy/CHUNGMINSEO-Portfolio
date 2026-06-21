using System.Collections.Generic;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーの記録経路を再生する分身
    /// プレイヤーに接触するとリセットを通知する
    /// </summary>
    public class GhostClone : MonoBehaviour
    {
        private List<Vector3> _path; // 再生する経路
        private float _interval; // 記録時の間隔
        private GhostManager _manager; // マネージャーの参照
        private int _currentIndex; // 現在の再生インデックス
        private float _timer; // 再生タイマー

        /// <summary>
        /// 経路データで初期化する
        /// </summary>
        public void Initialize(List<Vector3> path, float interval, GhostManager manager)
        {
            _path = path;
            _interval = interval;
            _manager = manager;
            _currentIndex = 0;
            _timer = 0f;
        }

        private void Update()
        {
            if (_path == null || _currentIndex >= _path.Count) return;

            _timer += Time.deltaTime;
            if (_timer >= _interval)
            {
                _timer = 0f;
                transform.position = _path[_currentIndex];
                _currentIndex++;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            _manager.OnGhostCaughtPlayer();
        }
    }
}
