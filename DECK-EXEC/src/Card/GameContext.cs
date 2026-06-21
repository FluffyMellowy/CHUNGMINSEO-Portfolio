namespace Colorless.Card
{
    using VContainer;
    using Colorless.Entity;
    using Colorless.Grid;
    using Colorless.Sequence;
    using Colorless.Turn;

    /// <summary>
    /// カード効果に渡される依存集合。VContainer から構築される。
    /// プロパティは段階的に拡張する（CardHand, EntityCollisionResolver など）。
    /// </summary>
    public sealed class GameContext
    {
        public GridManager Grid { get; }
        public Player Player { get; }
        public TurnManager Turn { get; }

        /// <summary>実行中の narration を発行するロガー。</summary>
        public IActionLogger Logger { get; }

        /// <summary>narration の rich text 色トークン。</summary>
        public LogColorPalette Palette { get; }

        [Inject]
        public GameContext(
            GridManager grid,
            Player player,
            TurnManager turn,
            IActionLogger logger,
            LogColorPalette palette)
        {
            Grid = grid;
            Player = player;
            Turn = turn;
            Logger = logger;
            Palette = palette;
        }
    }
}
