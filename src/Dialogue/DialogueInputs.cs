using UnityEngine.InputSystem;

namespace Dialogue
{
    /// <summary>
    /// ダイアログ系の入力判定を集約する静的ヘルパー。
    /// DialogueManager と DialogueUI に重複定義されていた IsAdvancePressed をここに統合。
    /// </summary>
    internal static class DialogueInputs
    {
        /// <summary>
        /// セリフ進行入力判定：ゲームパッドのA/B、キーボードのZ/Space/Enterのいずれか押下フレームでtrue
        /// </summary>
        public static bool IsAdvancePressed()
        {
            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame))
                return true;

            var kb = Keyboard.current;
            if (kb != null && (kb.zKey.wasPressedThisFrame
                || kb.spaceKey.wasPressedThisFrame
                || kb.enterKey.wasPressedThisFrame))
                return true;

            return false;
        }
    }
}
