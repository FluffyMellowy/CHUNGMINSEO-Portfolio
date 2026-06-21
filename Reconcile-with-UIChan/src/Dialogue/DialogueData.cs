using Language;

namespace Dialogue
{
    // ダイアログ中に発生するイベントの種類
    public enum TriggerType
    {
        None,
        FinishTitleScene,      // タイトル（イントロ含む）終了 → ミニゲーム1へ
        FinishDialogueScene1,  // ミニゲーム1後ダイアログ終了 → ミニゲーム2へ
        FinishDialogueScene2,  // ミニゲーム2後ダイアログ終了 → ミニゲーム3へ
    }

    // CSVの1行に対応するダイアログデータ
    public class DialogueData
    {
        public string Id;
        public string TextJP;
        public string TextEN;
        public string NextId;
        public string ChoiceA;
        public string ChoiceAId;
        public string ChoiceB;
        public string ChoiceBId;
        public string Image;
        public string VoiceJP; // Resourcesパス（拡張子なし）— 例: "Voice/JP/line_001"
        public string VoiceEN; // Resourcesパス（拡張子なし）
        public TriggerType Trigger;

        public bool HasChoice => !string.IsNullOrEmpty(ChoiceA);
        public bool HasTrigger => Trigger != TriggerType.None;

        public string GetText()
        {
            if (LanguageManager.Instance == null) return TextJP;
            return LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.JP
                ? TextJP
                : TextEN;
        }

        /// <summary>
        /// 現在言語に対応するボイスファイルのResourcesパスを返す。空文字なら再生しない
        /// </summary>
        public string GetVoice()
        {
            if (LanguageManager.Instance == null) return VoiceJP;
            return LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.JP
                ? VoiceJP
                : VoiceEN;
        }
    }
}