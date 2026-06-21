namespace Colorless.Card
{
    using UnityEngine;

    /// <summary>
    /// シーケンスキューに格納されたカード + 方向パラメータ。
    /// </summary>
    public readonly struct QueuedCard
    {
        public Card Card { get; }
        public Vector2Int? Direction { get; }

        public QueuedCard(Card card, Vector2Int? direction)
        {
            Card = card;
            Direction = direction;
        }
    }
}
