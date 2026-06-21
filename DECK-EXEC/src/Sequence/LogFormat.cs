namespace Colorless.Sequence
{
    using UnityEngine;

    /// <summary>
    /// narration の組み立て補助。方向や座標を日本語表記化する小ヘルパ群。
    /// </summary>
    public static class LogFormat
    {
        /// <summary>Vector2Int の方向を日本語表記に。</summary>
        public static string DirText(Vector2Int d)
        {
            if (d == Vector2Int.up) return "上";
            if (d == Vector2Int.down) return "下";
            if (d == Vector2Int.left) return "左";
            if (d == Vector2Int.right) return "右";
            return "?";
        }

        /// <summary>座標を (x,y) 形式の文字列に。</summary>
        public static string Coord(Vector2Int pos) => $"({pos.x},{pos.y})";
    }
}
