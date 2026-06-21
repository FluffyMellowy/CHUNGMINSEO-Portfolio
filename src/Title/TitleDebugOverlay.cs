using Language;
using St;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Title
{
    /// <summary>
    /// タイトルシーン用デバッグオーバーレイ
    /// JP/EN切替、現在言語表示、TitleDialogueLoopのスキップ強制発動を提供する
    /// SectionType.Titleを直接発火してUIMazeへ即時遷移するボタンも持つ
    /// クラス自体は常にコンパイルされるが、ビルドではUpdate/OnGUIが空になる（シーン参照を保つため）
    /// </summary>
    public class TitleDebugOverlay : MonoBehaviour
    {
        [Header("参照（未設定なら自動取得）")]
        [SerializeField] private TitleDialogueLoop _titleLoop;

        [Tooltip("GameManagerの_finishEventと同じSectionTypeEvent (FinishEventSO) を割り当てる。" +
                 "SectionType.Titleを発火するとGameManagerがUIMazeシーンへ遷移する")]
        [SerializeField] private SectionTypeEvent _finishEvent;

        [Header("表示設定")]
        [SerializeField] private Key _toggleKey = Key.F1;
        [SerializeField] private bool _visibleOnStart = true;

        private bool _visible;

        private void Awake()
        {
#if UNITY_EDITOR
            if (_titleLoop == null) _titleLoop = FindFirstObjectByType<TitleDialogueLoop>();
#endif
        }

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

            GUILayout.BeginArea(new Rect(10, 10, 300, 300), GUI.skin.box);

            GUILayout.Label($"<b>Title Debug</b>  ({_toggleKey} で表示切替)");
            GUILayout.Space(6);

            // 言語切替
            GUILayout.Label("── Language ──");
            if (LanguageManager.Instance == null)
            {
                GUILayout.Label("LanguageManager: null（既定でJP扱い）");
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

            // フォント状態確認
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

            GUILayout.Space(6);

            // タイトルループ操作
            GUILayout.Label("── Title Loop ──");
            if (_titleLoop == null)
            {
                GUILayout.Label("TitleDialogueLoop: null");
            }
            else
            {
                if (GUILayout.Button("Force Skip"))
                    _titleLoop.RequestSkip();
            }

            GUILayout.Space(6);

            // セクション遷移（GameManagerのFlowを直接トリガー）
            GUILayout.Label("── Section Jump ──");
            if (_finishEvent == null)
            {
                GUILayout.Label("FinishEvent: null（インスペクターで割り当ててね）");
            }
            else
            {
                if (GUILayout.Button("→ UIMaze (Raise SectionType.Title)"))
                    _finishEvent.Raise(SectionType.Title);
            }

            GUILayout.EndArea();
        }
#endif
    }
}
