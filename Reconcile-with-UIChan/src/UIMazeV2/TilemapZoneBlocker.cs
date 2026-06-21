using UnityEngine;
using UnityEngine.Tilemaps;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーがトリガー範囲内に入った時、Source Tilemapの指定セル範囲をTarget Tilemapに複製して通路を塞ぐ
    ///
    /// セットアップ:
    ///   1. Source Tilemap:デザイン時にすべての障害物グループを描き込んだTilemap（実行時は非アクティブor不可視レイヤー）
    ///   2. Target Tilemap:実行時に塗られる空のTilemap（TilemapCollider2D + Composite + Static Rigidbody）
    ///   3.各トリガーにBoundsIntで「自グループ」のセル範囲を指定
    ///   4.トリガー進入時、Sourceの該当セルを読んでTargetに書き込む
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TilemapZoneBlocker : MonoBehaviour
    {
        [Header("対象")]
        [Tooltip("デザイン時にすべての障害物が描かれている参照用Tilemap（実行時は使わないがGetTileのために存在は必要）")]
        [SerializeField] private Tilemap _sourceTilemap;
        [Tooltip("実行時にコピーされて衝突判定を持つ空のTilemap")]
        [SerializeField] private Tilemap _targetTilemap;

        [Header("塗り範囲")]
        [Tooltip("Source から Target にコピーするセル範囲（Position + Size、矩形）")]
        [SerializeField] private BoundsInt _zone;

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
            Debug.Log($"[ZoneBlocker:{name}] OnTriggerEnter2D: other={other.name} tag={other.tag}");
            if (!other.CompareTag("Player"))
            {
                Debug.Log($"[ZoneBlocker:{name}] tag is not Player, skip");
                return;
            }
            if (_oneShot && _hasFired)
            {
                Debug.Log($"[ZoneBlocker:{name}] already fired (oneShot), skip");
                return;
            }

            Debug.Log($"[ZoneBlocker:{name}] activating zone {_zone.position}~{_zone.position + _zone.size}");

            // 診断: Source Tilemap全体に存在するタイルの座標を一覧表示
            if (_sourceTilemap != null)
            {
                _sourceTilemap.CompressBounds();
                var bounds = _sourceTilemap.cellBounds;
                var found = new System.Text.StringBuilder();
                int total = 0;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (_sourceTilemap.GetTile(pos) != null)
                    {
                        found.Append($" {pos}");
                        total++;
                    }
                }
                Debug.Log($"[ZoneBlocker:{name}] Source 全体スキャン: {total} cells with tiles, bounds={bounds} | cells:{found}");
            }

            SetZoneActive(true);
            _hasFired = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_oneShot) return;
            if (!other.CompareTag("Player")) return;

            SetZoneActive(false);
        }

        /// <summary>
        /// 死亡リセット時などに外部から呼び出し、状態を初期化する
        /// </summary>
        public void ResetState()
        {
            _hasFired = false;
            SetZoneActive(false);
        }

        /// <summary>
        /// _zoneの各セルについて、active=trueならSourceからコピー、falseならnullで消す
        /// </summary>
        private void SetZoneActive(bool active)
        {
            if (_targetTilemap == null)
            {
                Debug.LogWarning($"[ZoneBlocker:{name}] Target Tilemap が未設定");
                return;
            }
            if (active && _sourceTilemap == null)
            {
                Debug.LogWarning($"[ZoneBlocker:{name}] Source Tilemap が未設定");
                return;
            }

            int placed = 0, nullCount = 0;
            var details = new System.Text.StringBuilder();
            foreach (var pos in _zone.allPositionsWithin)
            {
                if (active)
                {
                    // Tile +回転/反転(matrix) +色を一緒にコピーする
                    var tile = _sourceTilemap.GetTile(pos);
                    if (tile != null)
                    {
                        var matrix = _sourceTilemap.GetTransformMatrix(pos);
                        var color = _sourceTilemap.GetColor(pos);
                        _targetTilemap.SetTile(pos, tile);
                        _targetTilemap.SetTransformMatrix(pos, matrix);
                        _targetTilemap.SetColor(pos, color);
                        placed++;
                        details.Append($" {pos}=OK");
                    }
                    else
                    {
                        _targetTilemap.SetTile(pos, null);
                        nullCount++;
                        details.Append($" {pos}=null");
                    }
                }
                else
                {
                    _targetTilemap.SetTile(pos, null);
                }
            }
            if (active)
                Debug.Log($"[ZoneBlocker:{name}] placed={placed} null={nullCount} | cells:{details}");
        }
    }
}
