using KanKikuchi.AudioManager;
using Title;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Language
{
    /// <summary>
    /// 言語をJP↔ENにトグルするボタン
    /// クリック/ Submit /指定キー/パッドのYボタン（buttonNorth）で反応する
    /// ButtonコンポーネントのOnClickにOnClick()を紐付けて使用する
    /// </summary>
    public class LanguageToggleButton : MonoBehaviour
    {
        [Header("ショートカット")]
        [SerializeField] private Key _shortcutKey = Key.L; // キーボードショートカット (Lキー)
        [SerializeField] private bool _useGamepadShortcut = true; // パッドのXボタン(buttonWest)でも切り替えるか

        [Header("ガード")]
        [Tooltip("アトラクト動画再生中はこのボタンの入力を無視する。割り当てない場合はガードなし")]
        [SerializeField] private TitleAttractVideo _attractVideo;

        [Header("サウンド")]
        [SerializeField] private string _toggleSEPath = ""; // 切替SEパス（空文字なら無音）

        /// <summary>
        /// Button.OnClickから呼び出す。直接クリック/Submit用
        /// </summary>
        public void OnClick()
        {
            Toggle();
        }

        private void Update()
        {
            // アトラクト動画再生中はトグルしない(その間の入力はTitleAttractVideo側が「動画閉じる」で消費する)
            if (_attractVideo != null && _attractVideo.IsPlaying) return;

            // キーボードショートカット
            if (Keyboard.current != null && Keyboard.current[_shortcutKey].wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            // パッドX（buttonWest = Xbox X / PS Square）で言語トグル。
            // Yボタン(buttonNorth)はCreditModalが使うので衝突を避けるためXに割り当てる
            if (_useGamepadShortcut && Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            if (LanguageManager.Instance == null) return;
            LanguageManager.Instance.ToggleLanguage();
            SafeSE.Play(_toggleSEPath);
        }
    }
}
