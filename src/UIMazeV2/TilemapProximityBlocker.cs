using UnityEngine;
using UnityEngine.Tilemaps;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーがトリガー範囲内に入った時、指定されたTilemapの指定セルにタイルを書き込んで通路を塞ぐ
    /// 単一のTilemap内で複数の独立した障害物グループを管理できる（GameObject分割不要）
    ///
    /// セットアップ:
    ///   1.障害物用Tilemapを1つ用意（最初は障害物部分のセルが空 — 通行可の状態）
    ///   2.本コンポーネントを別GameObject (BoxCollider2D + IsTrigger)に付ける
    ///   3. _targetTilemapに対象Tilemapを割り当て
    ///   4. _tileToPlaceに置きたいタイル(TileBase)を割り当て
    ///   5. _fillAreaで塗りつぶす矩形セル範囲を指定（または_explicitPositionsで明示的な座標リスト）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TilemapProximityBlocker : MonoBehaviour
    {
        [Header("対象")]
        [SerializeField] private Tilemap _targetTilemap;
        [SerializeField] private TileBase _tileToPlace;

        [Header("塗り範囲")]
        [Tooltip("矩形範囲のすべてのセルを塗りつぶす（Sizeが0以下なら無視）")]
        [SerializeField] private BoundsInt _fillArea;
        [Tooltip("追加で個別指定したいセル座標（_fillAreaに加算される）")]
        [SerializeField] private Vector3Int[] _explicitPositions;

        [Header("挙動")]
        [SerializeField] private bool _oneShot = true; // trueなら一度きり、falseなら出入りで切り替え

        private bool _hasFired;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (_oneShot && _hasFired) return;

            SetTilesActive(true);
            _hasFired = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_oneShot) return;
            if (!other.CompareTag("Player")) return;

            SetTilesActive(false);
        }

        /// <summary>
        /// 死亡リセット時などに外部から呼び出し、状態を初期化する
        /// </summary>
        public void ResetState()
        {
            _hasFired = false;
            SetTilesActive(false);
        }

        /// <summary>
        /// _fillArea + _explicitPositionsのセルをタイルで埋める/消す
        /// </summary>
        private void SetTilesActive(bool active)
        {
            if (_targetTilemap == null) return;

            TileBase tile = active ? _tileToPlace : null;

            // 矩形範囲塗りつぶし
            if (_fillArea.size.x > 0 && _fillArea.size.y > 0)
            {
                foreach (var pos in _fillArea.allPositionsWithin)
                {
                    _targetTilemap.SetTile(pos, tile);
                }
            }

            // 個別座標
            if (_explicitPositions != null)
            {
                for (int i = 0; i < _explicitPositions.Length; i++)
                {
                    _targetTilemap.SetTile(_explicitPositions[i], tile);
                }
            }
        }
    }
}
