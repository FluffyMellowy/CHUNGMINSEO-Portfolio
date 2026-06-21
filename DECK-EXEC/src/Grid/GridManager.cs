namespace Colorless.Grid
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public sealed class GridManager : MonoBehaviour
    {
        [System.Serializable]
        public struct TilemapEntry
        {
            public Tilemap Tilemap;
            public TileType TileType;
        }
        [SerializeField] private TilemapEntry[] _tilemapEntries;

        private GridCell[,] _grid;
        private BoundsInt _bounds;

        public int Width => _bounds.size.x;
        public int Height => _bounds.size.y;
        public BoundsInt Bounds => _bounds;

        private List<Vector2Int> _buttonPositions = new List<Vector2Int>();

        private void Awake()
        {
            CreateGridFromTilemap();
            CenterCamera();
        }

        private void CreateGridFromTilemap()
        {
            /* 全タイルマップの範囲を統合 */
            foreach (var entry in _tilemapEntries)
            {
                entry.Tilemap.CompressBounds();
            }
            /* 全タイルマップの中で最大の範囲を取得 */
            BoundsInt bounds = _tilemapEntries[0].Tilemap.cellBounds;
            for (int i = 1; i < _tilemapEntries.Length; i++)
            {
                BoundsInt b = _tilemapEntries[i].Tilemap.cellBounds;
                bounds.xMin = Mathf.Min(bounds.xMin, b.xMin);
                bounds.yMin = Mathf.Min(bounds.yMin, b.yMin);
                bounds.xMax = Mathf.Max(bounds.xMax, b.xMax);
                bounds.yMax = Mathf.Max(bounds.yMax, b.yMax);
            }
            _bounds = bounds;

            _grid = new GridCell[_bounds.size.x, _bounds.size.y];

            for (int x = 0; x < _bounds.size.x; x++)
            {
                for (int y = 0; y < _bounds.size.y; y++)
                {
                    Vector3Int tilePos = new Vector3Int(
                        _bounds.xMin + x,
                        _bounds.yMin + y,
                        0
                    );

                    TileType type = GetTileTypeAt(tilePos);
                    _grid[x, y] = new GridCell(new Vector2Int(x, y), type);
                }
            }

            /* ボタン位置を自動収集 */
            for (int x = 0; x < _bounds.size.x; x++)
            {
                for (int y = 0; y < _bounds.size.y; y++)
                {
                    if (_grid[x, y].TileType == TileType.Exit_Button)
                    {
                        _buttonPositions.Add(new Vector2Int(x, y));
                    }
                }
            }
            if (_buttonPositions.Count > 0)
            {
                SetAllExitsLocked(true);
            }
        }

        private TileType GetTileTypeAt(Vector3Int tilePos)
        {
            /* 配列の上から順に優先度が高い */
            foreach (var entry in _tilemapEntries)
            {
                if (entry.Tilemap.HasTile(tilePos)) return entry.TileType;
            }
            return TileType.Hole;
        }

        /// <summary>
        /// グリッド座標 → ワールド座標
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            Vector3Int tilePos = new Vector3Int(
                _bounds.xMin + gridPos.x,
                _bounds.yMin + gridPos.y,
                0
            );
            return _tilemapEntries[0].Tilemap.GetCellCenterWorld(tilePos);
        }

        /// <summary>
        /// ワールド座標 → グリッド座標
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3Int tilePos = _tilemapEntries[0].Tilemap.WorldToCell(worldPos);
            return new Vector2Int(
                tilePos.x - _bounds.xMin,
                tilePos.y - _bounds.yMin
            );
        }

        /// <summary>
        /// 範囲チェック
        /// </summary>
        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _bounds.size.x
                && pos.y >= 0 && pos.y < _bounds.size.y;
        }

        /// <summary>
        /// セル取得
        /// </summary>
        public GridCell GetCell(Vector2Int pos)
        {
            if (!IsInBounds(pos)) return null;
            return _grid[pos.x, pos.y];
        }

        public GridCell GetCell(int x, int y)
        {
            return GetCell(new Vector2Int(x, y));
        }

        private void CenterCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 center = GridToWorld(new Vector2Int(_bounds.size.x / 2, _bounds.size.y / 2));
            center.z = cam.transform.position.z;
            cam.transform.position = center;
        }

        public void UpdateButtons()
        {
            bool anyPressed = false;
            foreach (Vector2Int pos in _buttonPositions)
            {
                GridCell cell = GetCell(pos);
                if (cell != null && cell.Occupant != null)
                {
                    anyPressed = true;
                    break;
                }
            }
            SetAllExitsLocked(!anyPressed);
        }

        public void SetAllExitsLocked(bool locked)
        {
            for (int x = 0; x < _bounds.size.x; x++)
            {
                for (int y = 0; y < _bounds.size.y; y++)
                {
                    GridCell cell = _grid[x, y];
                    if (cell.TileType == TileType.Exit_Stairs || cell.TileType == TileType.Exit_Door)
                    {
                        cell.IsLocked = locked;
                    }
                }
            }
        }

    }
}
