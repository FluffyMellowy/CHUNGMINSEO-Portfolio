namespace Language
{
    /// <summary>
    /// コードから動的にローカライズ文字列を取得するための静的ヘルパー
    /// インスペクターで設定する静的テキストはLocalizedText、ScoreやTimerなど
    /// 文字列を組み立てる動的テキストはこのPick系メソッドを通して言語別に切り替える
    /// </summary>
    public static class LocalizedString
    {
        /// <summary>
        /// 現在の言語に合わせてJP/EN文字列のどちらかを返す
        /// LanguageManagerが居ない時はJPを既定とする
        /// </summary>
        public static string Pick(string jp, string en)
        {
            if (LanguageManager.Instance == null) return jp;
            return LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.JP ? jp : en;
        }

        /// <summary>
        /// string.Formatのローカライズ版。テンプレートを言語別に持って引数を埋め込む
        /// 例: PickFormat("スコア: {0}", "Score: {0}", score)
        /// </summary>
        public static string PickFormat(string jpFormat, string enFormat, params object[] args)
        {
            return string.Format(Pick(jpFormat, enFormat), args);
        }

        /// <summary>現在言語がJPか</summary>
        public static bool IsJapanese =>
            LanguageManager.Instance == null
            || LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.JP;

        /// <summary>現在言語がENか</summary>
        public static bool IsEnglish =>
            LanguageManager.Instance != null
            && LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.EN;
    }
}
