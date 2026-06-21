namespace Colorless.Sequence
{
    using R3;

    /// <summary>
    /// 実行中の narration（"Player が (3,4) に移動！" 等）を発行するロガー。
    /// カード効果実装が GameContext.Logger 経由で呼び出し、UI が購読して表示する。
    /// rich text タグはここでは関与せず、呼び出し側が LogColorPalette のヘルパで生成。
    /// </summary>
    public interface IActionLogger
    {
        /// <summary>新規ログエントリの発行ストリーム。</summary>
        Observable<string> Entries { get; }

        /// <summary>ログのクリア通知。UI は受信時に既存表示を一掃する。</summary>
        Observable<Unit> Cleared { get; }

        /// <summary>1 行分の narration を発行する（rich text 含む）。</summary>
        void Log(string richTextMessage);

        /// <summary>これまでのログ表示をクリアする。新しい RUN 開始時に呼び出される想定。</summary>
        void Clear();
    }
}
