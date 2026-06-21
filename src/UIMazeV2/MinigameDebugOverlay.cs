using Language;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム1/2/3を個別に再生確認するためのデバッグオーバーレイ
    /// IMGUIで現在のウィンドウインデックス表示と各ミニゲームへの即時ジャンプボタンを提供する
    /// クラス自体は常にコンパイルされるが、ビルドではUpdate/OnGUIが空になる
    /// </summary>
    public class MinigameDebugOverlay : MonoBehaviour
    {
        [Header("参照（未設定なら自動取得）")]
        [SerializeField] private WindowManager _windowManager;

        [Header("表示設定")]
        [SerializeField] private Key _toggleKey = Key.F1;
        [SerializeField] private bool _visibleOnStart = true;
        [SerializeField] private Vector2 _position = new Vector2(10, 10);

        [Header("ボタンラベル（インデックス順）")]
        [Tooltip("WindowManagerの_windowsインデックスに対応する表示ラベル")]
        [SerializeField]
        private string[] _windowLabels =
        {
            "Minigame 1",
            "Minigame 2",
            "Minigame 3",
        };

        private bool _visible;

        private void Awake()
        {
#if UNITY_EDITOR
            if (_windowManager == null) _windowManager = FindFirstObjectByType<WindowManager>();
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

            GUILayout.BeginArea(new Rect(_position.x, _position.y, 280, 380), GUI.skin.box);

            GUILayout.Label($"<b>Minigame Debug</b>  ({_toggleKey} で表示切替)");
            GUILayout.Space(6);

            if (_windowManager == null)
            {
                GUILayout.Label("WindowManager: null");
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"Current Window: {_windowManager.CurrentIndex} / {_windowManager.WindowCount - 1}");
            GUILayout.Space(6);

            GUILayout.Label("── Jump To ──");
            for (int i = 0; i < _windowManager.WindowCount; i++)
            {
                string label = (i < _windowLabels.Length && !string.IsNullOrEmpty(_windowLabels[i]))
                    ? _windowLabels[i]
                    : $"Window {i}";
                if (GUILayout.Button(label))
                    _windowManager.JumpToWindow(i);
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Next Window"))
                _windowManager.NextWindow();

            GUILayout.Space(10);
            GUILayout.Label("── Clear ──");
            if (GUILayout.Button("Force Clear"))
                ForceClearMinigame();

            GUILayout.Space(10);
            GUILayout.Label("── Language ──");
            if (LanguageManager.Instance == null)
            {
                GUILayout.Label("(LanguageManager なし)");
            }
            else
            {
                var current = LanguageManager.Instance.CurrentLanguage;
                var next = current == LanguageManager.Language.JP
                    ? LanguageManager.Language.EN
                    : LanguageManager.Language.JP;
                if (GUILayout.Button($"Toggle: {current} → {next}"))
                    SwitchLanguageAndReset(next);
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// デバッグ用：シーン内のSurvivalClearを強制発火させてミニゲームクリアイベントを通知する
        /// クリア時の遷移ロジック（GameManager / Section遷移）をテストする際に使用
        /// </summary>
        private void ForceClearMinigame()
        {
            var clears = FindObjectsByType<SurvivalClear>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (clears.Length == 0)
            {
                Debug.LogWarning("[MinigameDebugOverlay] SurvivalClear がシーンに見つかりません");
                return;
            }
            foreach (var c in clears) c.ForceClear();
        }

        /// <summary>
        /// 言語を切替えてから現在のミニゲームを最初から再生する
        /// クレジットスクロールミニゲームでは言語別ステージ選択を再実行する必要があるため、
        /// CreditScrollerのReinitializeForLanguageChangeを呼んでからWindowManagerでリスポーン
        /// </summary>
        private void SwitchLanguageAndReset(LanguageManager.Language lang)
        {
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.SetLanguage(lang);

            // クレジットスクロールがある場合は言語別ステージ選択をやり直す
            var scrollers = FindObjectsByType<CreditScroller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in scrollers)
                s.ReinitializeForLanguageChange();

            // SurvivalClearのターゲットTransform（クレジット末尾TMP）も言語別に再選択
            var clears = FindObjectsByType<SurvivalClear>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in clears)
                c.ReinitializeForLanguageChange();

            // 現在のウィンドウを再表示 → プレイヤーがスポーン位置に戻り、コントローラーOnEnableで初期化される
            if (_windowManager != null)
                _windowManager.JumpToWindow(_windowManager.CurrentIndex);
        }
#endif
    }
}
