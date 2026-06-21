namespace Colorless.Card
{
    using System;
    using UnityEngine;

    /// <summary>
    /// カード使用時のプレビュー描画データ。MapPreviewRenderer がこれを元に描画する。
    /// </summary>
    public readonly struct CardPreview
    {
        /// <summary>移動経路のセル列（点線描画用）。最低1点。</summary>
        public Vector2Int[] PathCells { get; }

        /// <summary>到達セル（半透明シルエット表示位置）。</summary>
        public Vector2Int FinalPosition { get; }

        /// <summary>環境変化が起きるセル群（オーバーレイ用）。</summary>
        public Vector2Int[] AffectedCells { get; }

        /// <summary>方向矢印（不要なら null）。</summary>
        public Vector2Int? Arrow { get; }

        public CardPreview(Vector2Int[] pathCells, Vector2Int finalPosition,
                           Vector2Int[] affectedCells, Vector2Int? arrow)
        {
            PathCells = pathCells ?? Array.Empty<Vector2Int>();
            FinalPosition = finalPosition;
            AffectedCells = affectedCells ?? Array.Empty<Vector2Int>();
            Arrow = arrow;
        }

        /// <summary>
        /// 動かない場合のプレビュー（現在位置にとどまる）を生成。
        /// </summary>
        public static CardPreview Stationary(Vector2Int currentPos, Vector2Int? arrow = null)
        {
            return new CardPreview(new[] { currentPos }, currentPos, Array.Empty<Vector2Int>(), arrow);
        }
    }
}
