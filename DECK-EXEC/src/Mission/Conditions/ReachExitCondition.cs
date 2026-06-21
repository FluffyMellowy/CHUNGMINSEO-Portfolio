namespace Colorless.Mission.Conditions
{
    using System;
    using Colorless.Card;
    using Colorless.Grid;

    /// <summary>
    /// プレイヤーが Exit_Stairs / Exit_Door に到達したらクリア。
    /// IsLocked のドアは未クリア扱い。
    /// </summary>
    [Serializable]
    public sealed class ReachExitCondition : IClearCondition
    {
        public bool IsCleared(GameContext ctx)
        {
            GridCell cell = ctx.Grid.GetCell(ctx.Player.GridPosition);
            if (cell == null) return false;

            bool isExitTile = cell.TileType == TileType.Exit_Stairs
                           || cell.TileType == TileType.Exit_Door;
            if (!isExitTile) return false;

            return !cell.IsLocked;
        }
    }
}
