namespace Colorless.Sequence
{
    using R3;

    /// <summary>
    /// IActionLogger の標準実装。Subject を 2 本持つだけのシンプルなブロードキャスト。
    /// </summary>
    public sealed class ActionLogger : IActionLogger
    {
        private readonly Subject<string> _entries = new();
        private readonly Subject<Unit> _cleared = new();

        public Observable<string> Entries => _entries;
        public Observable<Unit> Cleared => _cleared;

        public void Log(string richTextMessage)
        {
            if (string.IsNullOrEmpty(richTextMessage)) return;
            _entries.OnNext(richTextMessage);
        }

        public void Clear()
        {
            _cleared.OnNext(Unit.Default);
        }
    }
}
