namespace Colorless.Mission
{
    using System.Collections.Generic;
    using UnityEngine;
    using Colorless.Card;
    using Colorless.Entity;
    using Colorless.Entity.Enemy;
    using Colorless.Grid;
    using Colorless.Sequence;

    /// <summary>
    /// ミッション開始時点の状態を保存し、失敗時にロールバックする。
    /// </summary>
    public sealed class MissionSnapshot
    {
        private struct EnemyEntry
        {
            public EnemyBase Enemy;
            public Vector2Int Position;
            public bool Active;
            public object StateData;
        }

        private struct BoxEntry
        {
            public Box Box;
            public Vector2Int Position;
            public bool Active;
        }

        private Vector2Int _playerPos;
        private Vector2Int _playerFacing;
        private List<EnemyEntry> _enemies;
        private List<BoxEntry> _boxes;
        private Dictionary<Card, int> _handCounts;

        /// <summary>
        /// 現在の状態をスナップショットとして保存する。
        /// </summary>
        public static MissionSnapshot Capture(
            GameContext ctx,
            CardHand hand,
            IReadOnlyList<EnemyBase> enemies,
            IReadOnlyList<Box> boxes)
        {
            MissionSnapshot snap = new();

            snap._playerPos = ctx.Player.GridPosition;
            snap._playerFacing = ctx.Player.Facing;

            snap._enemies = new List<EnemyEntry>(enemies.Count);
            foreach (EnemyBase e in enemies)
            {
                if (e == null) continue;
                snap._enemies.Add(new EnemyEntry
                {
                    Enemy = e,
                    Position = e.GridPosition,
                    Active = e.gameObject.activeSelf,
                    StateData = e.CaptureState(),
                });
            }

            snap._boxes = new List<BoxEntry>(boxes.Count);
            foreach (Box b in boxes)
            {
                if (b == null) continue;
                snap._boxes.Add(new BoxEntry
                {
                    Box = b,
                    Position = b.GridPosition,
                    Active = b.gameObject.activeSelf,
                });
            }

            snap._handCounts = hand.CaptureSnapshot();

            return snap;
        }

        /// <summary>
        /// 保存した状態を復元する。Occupant 整合性もここで一括管理。
        /// </summary>
        public void Restore(GameContext ctx, CardHand hand)
        {
            /* 全エンティティを一度グリッドから引き剥がしてから再配置 */
            ClearOccupantOf(ctx.Player.gameObject, ctx.Grid, ctx.Player.GridPosition);

            foreach (EnemyEntry e in _enemies)
                ClearOccupantOf(e.Enemy.gameObject, ctx.Grid, e.Enemy.GridPosition);

            foreach (BoxEntry b in _boxes)
                ClearOccupantOf(b.Box.gameObject, ctx.Grid, b.Box.GridPosition);

            /* プレイヤー復元（位置 + 向き） */
            ctx.Player.SetGridPosition(_playerPos);
            ctx.Player.SetFacing(_playerFacing);
            GridCell pcell = ctx.Grid.GetCell(_playerPos);
            if (pcell != null) pcell.Occupant = ctx.Player.gameObject;

            /* 敵復元 */
            foreach (EnemyEntry e in _enemies)
            {
                if (e.Enemy == null) continue;
                e.Enemy.gameObject.SetActive(e.Active);
                e.Enemy.SetGridPosition(e.Position);
                e.Enemy.RestoreState(e.StateData);
                if (e.Active)
                {
                    GridCell cell = ctx.Grid.GetCell(e.Position);
                    if (cell != null) cell.Occupant = e.Enemy.gameObject;
                }
            }

            /* ボックス復元 */
            foreach (BoxEntry b in _boxes)
            {
                if (b.Box == null) continue;
                b.Box.gameObject.SetActive(b.Active);
                b.Box.SetGridPosition(b.Position);
                if (b.Active)
                {
                    GridCell cell = ctx.Grid.GetCell(b.Position);
                    if (cell != null) cell.Occupant = b.Box.gameObject;
                }
            }

            /* 手札残数復元 */
            hand.RestoreFromSnapshot(_handCounts);
        }

        private static void ClearOccupantOf(GameObject obj, GridManager grid, Vector2Int pos)
        {
            GridCell cell = grid.GetCell(pos);
            if (cell != null && cell.Occupant == obj) cell.Occupant = null;
        }
    }
}
