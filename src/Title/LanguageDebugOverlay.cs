using UnityEngine;
using UnityEngine.InputSystem;

namespace Language
{
    /// <summary>
    /// どのシーンでも使える汎用言語デバッグオーバーレイ
    /// JP/EN切替、現在言語確認、フォント確認のみのシンプル構成
    /// 各メンバーが自分のシーンで言語切替テストする時にこれをドラッグ＆ドロップする
    /// クラス自体は常にコンパイルされるが、ビルドではUpdate/OnGUIが空になる
    /// </summary>
    public class LanguageDebugOverlay : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField] private Key _toggleKey = Key.F1;
        [SerializeField] private bool _visibleOnStart = true;
        [SerializeField] private Vector2 _position = new Vector2(10, 10);

        private bool _visible;

        private void Start()
        {
#if UNITY_EDITOR
            _visible = _visibleOnStart;
#endif
        }

#if UNITY_EDITOR
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 14;

            GUILayout.BeginArea(new Rect(_position.x, _position.y, 280, 200), GUI.skin.box);

            GUILayout.Label($"<b>Language Debug</b>  ({_toggleKey} で表示切替)");
            GUILayout.Space(6);

            GUILayout.Label("── Language ──");
            if (LanguageManager.Instance == null)
            {
                GUILayout.Label("LanguageManager: null");
                GUILayout.Label("（LocalizationBootstrap が未配置）");
            }
            else
            {
                GUILayout.Label($"Current: {LanguageManager.Instance.CurrentLanguage}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("JP")) LanguageManager.Instance.SetLanguage(LanguageManager.Language.JP);
                if (GUILayout.Button("EN")) LanguageManager.Instance.SetLanguage(LanguageManager.Language.EN);
                if (GUILayout.Button("Toggle")) LanguageManager.Instance.ToggleLanguage();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);

            GUILayout.Label("── Font ──");
            if (FontManager.Instance == null)
            {
                GUILayout.Label("FontManager: null");
            }
            else
            {
                var f = FontManager.Instance.CurrentFont;
                GUILayout.Label($"CurrentFont: {(f != null ? f.name : "null")}");
            }

            GUILayout.EndArea();
        }
#endif
    }
}
