namespace Colorless.Card
{
    using System.Collections.Generic;
    using UnityEngine;
    using Colorless.Entity;
    using Colorless.Grid;

    /// <summary>
    /// シーケンスを仮想シミュレーションする際の状態。
    /// 各カードの BuildPreview が受け取り、必要に応じて変更する。
    /// MapPreviewRenderer がキュー全体を反復しながらこれを更新する。
    /// </summary>
    public sealed class PreviewState
    {
        public Vector2Int PlayerPos { get; set; }

        /// <summary>仮想シーケンスでのプレイヤー向き。</summary>
        public Vector2Int Facing { get; set; }

        /// <summary>プレビュー対象となる全ボックス（シーン内、非アクティブ含む可）。</summary>
        public IReadOnlyList<Box> AllBoxes { get; }

        /// <summary>
        /// ボックスの仮想位置オーバーライド。
        /// 値が null なら "穴に落ちて消滅" を意味する。
        /// 未登録キーは Box.GridPosition（実位置）を使う。
        /// </summary>
        private readonly Dictionary<Box, Vector2Int?> _boxOverrides = new();

        public PreviewState(Vector2Int playerPos, Vector2Int facing, IReadOnlyList<Box> allBoxes = null)
        {
            PlayerPos = playerPos;
            Facing = facing;
            AllBoxes = allBoxes ?? System.Array.Empty<Box>();
        }

        /// <summary>
        /// 指定 Box の仮想位置を取得。
        /// null は "消滅"（穴に落ちた）を意味する。
        /// </summary>
        public Vector2Int? GetBoxPos(Box box)
        {
            if (box == null) return null;
            if (_boxOverrides.TryGetValue(box, out Vector2Int? overridePos)) return overridePos;
            if (!box.gameObject.activeSelf) return null;
            return box.GridPosition;
        }

        /// <summary>
        /// Box の仮想位置を設定。pos が null なら消滅扱い。
        /// </summary>
        public void SetBoxPos(Box box, Vector2Int? pos)
        {
            if (box == null) return;
            _boxOverrides[box] = pos;
        }

        /// <summary>仮想位置で当該セルに居る Box を返す（無ければ null）。</summary>
        public Box GetBoxAt(Vector2Int cell)
        {
            foreach (Box b in AllBoxes)
            {
                if (b == null) continue;
                Vector2Int? p = GetBoxPos(b);
                if (p.HasValue && p.Value == cell) return b;
            }
            return null;
        }

        /// <summary>仮想位置で当該セルに Box が居るか。</summary>
        public bool HasBoxAt(Vector2Int cell) => GetBoxAt(cell) != null;

        /// <summary>
        /// プレイヤーが当該セルに進入可能か（壁・穴・占有・仮想 Box を考慮）。
        /// </summary>
        public bool CanPlayerEnter(GameContext ctx, Vector2Int cell)
        {
            GridCell gc = ctx.Grid.GetCell(cell);
            if (gc == null || !gc.IsWalkable) return false;

            /* 仮想 Box が当該セルに居る → 進入不可 */
            if (HasBoxAt(cell)) return false;

            /* 実グリッドの Occupant をチェック。
               - Box は仮想位置で判定済みなので除外
               - プレイヤー自身の実位置は「仮想ではすでに別セルへ移動した」ものとして扱い、進入可能
                 （プレビュー中にスタート地点へ戻ってこれるようにするための重要例外）
               - それ以外（敵など）は進入不可 */
            if (gc.Occupant != null)
            {
                Box realBox = gc.Occupant.GetComponent<Box>();
                bool isPlayerSelf = ctx.Player != null && gc.Occupant == ctx.Player.gameObject;
                if (realBox == null && !isPlayerSelf) return false;
            }
            return true;
        }

        public PreviewState Clone()
        {
            PreviewState copy = new(PlayerPos, Facing, AllBoxes);
            foreach (var kv in _boxOverrides)
                copy._boxOverrides[kv.Key] = kv.Value;
            return copy;
        }
    }
}
