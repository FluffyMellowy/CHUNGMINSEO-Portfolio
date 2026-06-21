namespace Colorless.Sequence
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// 実行ログの rich text 色トークンを保持する SO。
    /// ヘルパメソッドで `<color=#XXXXXX>text</color>` 形式の文字列を生成し、
    /// カード効果側で narration を組み立てる。
    /// </summary>
    [CreateAssetMenu(fileName = "LogColorPalette", menuName = "DECK::EXEC/Log Color Palette")]
    public sealed class LogColorPalette : ScriptableObject
    {
        [Title("Entities")]
        [InfoBox("プレイヤー名（主役）。金色推奨。")]
        public Color Player = new(1f, 0.835f, 0.31f);    // #FFD54F
        [InfoBox("無生物オブジェクト（Box 等）。淡桃推奨。")]
        public Color Box = new(0.957f, 0.561f, 0.694f);  // #F48FB1
        [InfoBox("敵。鮮やかな赤推奨。")]
        public Color Enemy = new(1f, 0.322f, 0.322f);    // #FF5252
        [InfoBox("壁・地形。茶推奨。")]
        public Color Wall = new(0.631f, 0.533f, 0.498f); // #A1887F

        [Title("Data / Parameters")]
        [InfoBox("座標 (x,y) 表示。青緑推奨。")]
        public Color Coord = new(0.502f, 0.871f, 0.918f); // #80DEEA
        [InfoBox("方向（上・下・左・右）。淡緑推奨。")]
        public Color Direction = new(0.647f, 0.839f, 0.655f); // #A5D6A7

        [Title("Verbs")]
        [InfoBox("動詞（移動・押した・破壊 等）。オレンジ推奨。")]
        public Color Verb = new(1f, 0.718f, 0.302f);      // #FFB74D
        [InfoBox("時間・待機関連。紫推奨。")]
        public Color Time = new(0.729f, 0.408f, 0.784f); // #BA68C8

        [Title("Outcome")]
        [InfoBox("成功・クリア。緑推奨。")]
        public Color Success = new(0.506f, 0.78f, 0.518f); // #81C784
        [InfoBox("失敗・死亡・危険。赤推奨。")]
        public Color Danger = new(0.937f, 0.325f, 0.314f); // #EF5350
        [InfoBox("中性のシステム文字。グレー推奨。")]
        public Color System = new(0.62f, 0.62f, 0.62f);  // #9E9E9E

        /// <summary>任意の色で text を包む基本ヘルパ。</summary>
        public static string Wrap(Color c, string text)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";
        }

        /* 各トークン用のショートハンド：narration を読みやすく組み立てる */
        public string T_Player(string s) => Wrap(Player, s);
        public string T_Box(string s) => Wrap(Box, s);
        public string T_Enemy(string s) => Wrap(Enemy, s);
        public string T_Wall(string s) => Wrap(Wall, s);
        public string T_Coord(string s) => Wrap(Coord, s);
        public string T_Direction(string s) => Wrap(Direction, s);
        public string T_Verb(string s) => Wrap(Verb, s);
        public string T_Time(string s) => Wrap(Time, s);
        public string T_Success(string s) => Wrap(Success, s);
        public string T_Danger(string s) => Wrap(Danger, s);
        public string T_System(string s) => Wrap(System, s);
    }
}
