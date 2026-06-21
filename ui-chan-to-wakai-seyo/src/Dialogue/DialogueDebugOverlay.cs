using Cysharp.Threading.Tasks;
using Language;
using St;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dialogue
{
    /// <summary>
    /// ダイアログシーン単独実行時のデバッグ用オーバーレイ
    /// IMGUIで言語切替、AUTO/SKIPトグル、任意IDから再生、リザルト分岐テスト等を提供する
    /// クラス自体は常にコンパイルされるが、ビルドではUpdate/OnGUIが完全に空になりキーも反応しない
    /// （シーンのシリアライズ参照を保つため、フィールドだけは残す）
    /// </summary>
    public class DialogueDebugOverlay : MonoBehaviour
    {
        [Header("参照（未設定なら自動取得）")]
        [SerializeField] private DialogueManager _manager;
        [SerializeField] private DialogueSettings _settings;

        [Header("表示設定")]
        [Tooltip("オーバーレイ表示/非表示を切り替えるキー")]
        [SerializeField] private Key _toggleKey = Key.F1;
        [SerializeField] private bool _visibleOnStart = true;

        [Header("クイック再生ID")]
        [Tooltip("ボタン1クリックで再生するID一覧。CSVに存在しないIDはエラーログを吐く")]
        [SerializeField]
        private string[] _quickIds =
        {
            "1", "20", "30", "40",
            "100", "110", "120",
        };

        [Header("セクション終了通知")]
        [Tooltip("DialogueManagerの_sectionTypeEventと同じSO (FinishEventSO) を割り当てる。" +
                 "ID 34末尾のFinishDialogueScene1 / ID 44末尾のFinishDialogueScene2 と同等のRaiseを直接行い、" +
                 "ダイアログを最後まで読まなくても次のシーンへ進める")]
        [SerializeField] private SectionTypeEvent _finishEvent;

        private bool _visible;
        private string _customId = "";
        private Vector2 _scrollPos;

        private void Awake()
        {
#if UNITY_EDITOR
            if (_manager == null) _manager = FindFirstObjectByType<DialogueManager>();
            if (_settings == null) _settings = FindFirstObjectByType<DialogueSettings>();
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
            // 新Input Systemでトグルキー検知
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            // 解像度に依存しない見やすいフォントサイズ
            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 14;
            GUI.skin.textField.fontSize = 14;

            GUILayout.BeginArea(new Rect(10, 10, 500, Screen.height - 20), GUI.skin.box);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            GUILayout.Space(6);
            DrawState();
            GUILayout.Space(6);
            DrawLanguage();
            GUILayout.Space(6);
            DrawDialogueModes();
            GUILayout.Space(6);
            DrawGameOverCounter();
            GUILayout.Space(6);
            DrawSectionEvents();
            GUILayout.Space(6);
            DrawQuickIds();
            GUILayout.Space(6);
            DrawCustomId();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.Label($"<b>Dialogue Debug</b>  ({_toggleKey} で表示切替)");
        }

        /// <summary>
        /// テーブル読み込み状態と現在再生中IDを表示
        /// </summary>
        private void DrawState()
        {
            GUILayout.Label("── State ──");
            if (_manager == null)
            {
                GUILayout.Label("DialogueManager: null");
                return;
            }
            GUILayout.Label($"TableLoaded: {_manager.IsTableLoaded}");
            GUILayout.Label($"CurrentId: {_manager.CurrentId ?? "(none)"}");
            if (_manager.AvailableIds != null)
                GUILayout.Label($"AvailableIds: {_manager.AvailableIds.Count}");
        }

        /// <summary>
        /// 言語切替（LanguageManager経由）
        /// </summary>
        private void DrawLanguage()
        {
            GUILayout.Label("── Language ──");
            if (LanguageManager.Instance == null)
            {
                GUILayout.Label("LanguageManager: null（既定でJP扱い）");
                return;
            }
            GUILayout.Label($"Current: {LanguageManager.Instance.CurrentLanguage}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("JP")) LanguageManager.Instance.SetLanguage(LanguageManager.Language.JP);
            if (GUILayout.Button("EN")) LanguageManager.Instance.SetLanguage(LanguageManager.Language.EN);
            if (GUILayout.Button("Toggle")) LanguageManager.Instance.ToggleLanguage();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// AUTO/SKIPモードトグル
        /// </summary>
        private void DrawDialogueModes()
        {
            GUILayout.Label("── Dialogue Mode ──");
            if (_settings == null)
            {
                GUILayout.Label("DialogueSettings: null");
                return;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"AUTO: {(_settings.IsAuto ? "ON" : "OFF")}"))
                _settings.ToggleAuto();
            if (GUILayout.Button($"SKIP: {(_settings.IsSkip ? "ON" : "OFF")}"))
                _settings.ToggleSkip();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// GameoverCounterの操作（リザルト分岐テスト用）
        /// </summary>
        private void DrawGameOverCounter()
        {
            GUILayout.Label("── GameOverCount ──");
            if (GameoverCounter.Instance == null)
            {
                GUILayout.Label("GameoverCounter: null");
                GUILayout.Label("（DialogueManagerの_standaloneGameOverCountで代用可）");
                return;
            }
            GUILayout.Label($"Count: {GameoverCounter.Instance.GameOverCount}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1")) GameoverCounter.Instance.Add();
            if (GUILayout.Button("Reset")) GameoverCounter.Instance.Reset();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// ID 34/44 の末尾trigger（FinishDialogueScene1 / FinishDialogueScene2）と同等のRaiseを直接発行する
        /// DialogueManager.FireTrigger経由ではなく_sectionTypeEventに直接Raiseするので、
        /// 現在表示中のダイアログは中断されないが、GameManagerは即座に次のシーンへ遷移を開始する
        /// </summary>
        private void DrawSectionEvents()
        {
            GUILayout.Label("── Skip to Next Section ──");
            if (_finishEvent == null)
            {
                GUILayout.Label("FinishEvent: null（インスペクターで割り当ててね）");
                return;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("→ Action (ID 34末尾と同等)"))
                _finishEvent.Raise(SectionType.UIMaze_Dialogue);
            if (GUILayout.Button("→ DatingSim (ID 44末尾と同等)"))
                _finishEvent.Raise(SectionType.Action_Dialogue);
            GUILayout.EndHorizontal();

            // 最終ミニゲーム(Hayashi DatingSim)をスキップしてResultシーンへ直行する分岐別ボタン
            // 押下時にGameoverCounterを当該分岐の閾値に揃えてからRaise(DatingSim)するので、
            // ResultManagerの画像分岐とDialogueManagerのID分岐が一致した状態でResultが開く
            GUILayout.Label("Result分岐 (DeathCount調整＋Raise):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("→ GoodEnd"))
                JumpToResultWithCount(0);
            if (GUILayout.Button("→ NormalEnd"))
                JumpToResultWithCount(1);
            if (GUILayout.Button("→ BadEnd"))
                JumpToResultWithCount(3);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// GameOverCountを指定値に強制してからResult遷移イベントを発火する
        /// GameoverCounterのsetterはprivateなのでReset() → Add()×Nで望む値を作る
        /// </summary>
        private void JumpToResultWithCount(int target)
        {
            if (GameoverCounter.Instance == null)
            {
                Debug.LogWarning("[DialogueDebugOverlay] GameoverCounter.Instance == null。" +
                                 "ManagerScene未起動のためカウント調整できません。Raiseのみ実行");
            }
            else
            {
                GameoverCounter.Instance.Reset();
                for (int i = 0; i < target; i++)
                    GameoverCounter.Instance.Add();
            }
            _finishEvent.Raise(SectionType.DatingSim);
        }

        /// <summary>
        /// よく使うIDのワンクリック再生ボタン
        /// </summary>
        private void DrawQuickIds()
        {
            GUILayout.Label("── Quick Start ──");
            if (_manager == null) return;

            const int columns = 4;
            for (int i = 0; i < _quickIds.Length; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int j = 0; j < columns && i + j < _quickIds.Length; j++)
                {
                    var id = _quickIds[i + j];
                    if (GUILayout.Button(id, GUILayout.Width(70)))
                        TryStart(id);
                }
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 任意ID入力欄
        /// </summary>
        private void DrawCustomId()
        {
            GUILayout.Label("── Custom ID ──");
            GUILayout.BeginHorizontal();
            _customId = GUILayout.TextField(_customId, GUILayout.Width(200));
            if (GUILayout.Button("Start", GUILayout.Width(80)))
                TryStart(_customId);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 指定IDから再生する。テーブル未ロードや空ID等のガード付き
        /// </summary>
        private void TryStart(string id)
        {
            if (_manager == null)
            {
                Debug.LogWarning("[DialogueDebugOverlay] DialogueManager未設定");
                return;
            }
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[DialogueDebugOverlay] IDが空です");
                return;
            }
            if (!_manager.IsTableLoaded)
            {
                Debug.LogWarning("[DialogueDebugOverlay] CSV未ロード（DialogueManagerの_runStandaloneを確認）");
                return;
            }
            _manager.StartDialogue(id).Forget();
        }
#endif // UNITY_EDITOR
    }
}
