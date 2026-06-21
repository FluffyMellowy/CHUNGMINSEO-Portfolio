using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーがトリガー範囲内に入った時、指定された通行不可オブジェクトを有効化する
    /// 迷路内で動的に通路を塞ぐ用途
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MazeProximityBlocker : MonoBehaviour
    {
        [Header("起動対象")]
        [SerializeField] private GameObject[] _blockObjects; // 有効化する通行不可オブジェクト

        [Header("挙動")]
        [SerializeField] private bool _oneShot = true; // trueなら一度きり、falseなら出入りで切り替え

        private bool _hasFired; // 一度起動済みか

        private void Reset()
        {
            // インスペクターで初追加された時にIsTriggerをONにする
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (_oneShot && _hasFired) return;

            SetBlocksActive(true);
            _hasFired = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_oneShot) return;
            if (!other.CompareTag("Player")) return;

            SetBlocksActive(false);
        }

        private void SetBlocksActive(bool active)
        {
            if (_blockObjects == null) return;
            for (int i = 0; i < _blockObjects.Length; i++)
            {
                if (_blockObjects[i] != null)
                    _blockObjects[i].SetActive(active);
            }
        }

        /// <summary>
        /// 死亡リセット時などに外部から呼び出し、状態を初期化する
        /// </summary>
        public void ResetState()
        {
            _hasFired = false;
            SetBlocksActive(false);
        }
    }
}
