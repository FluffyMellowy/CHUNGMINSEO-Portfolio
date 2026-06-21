namespace Colorless.Debugging
{
    using System.Collections.Generic;
    using System.Text;
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using VContainer;
    using Colorless.Card;
    using Colorless.Entity;
    using Colorless.Mission;
    using Colorless.Sequence;
    using Colorless.Stage;

    /// <summary>
    /// 開発中の状態確認＋操作用 HUD。OnGUI で左上に表示。
    /// F10 で表示トグル。
    /// </summary>
    public sealed class DebugHUD : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private bool _visibleAtStart = true;
        [SerializeField] private Key _toggleKey = Key.F10;
        [SerializeField] private Vector2 _position = new(10, 10);
        [SerializeField] private float _width = 300f;

        [Title("Stage Jump (Phase 11)")]
        [InfoBox("Phase 11 ステージプレハブスワップ用。割り当てた StageGraph の全ノードを HUD 末尾にボタン表示する。未割当ならセクション非表示。")]
        [SerializeField] private StageGraph _stageGraph;
        [InfoBox("StageLoader の参照（インスペクターで永続シーンの StageLoader を割り当て）。未割当ならジャンプ操作は無効化される。")]
        [SerializeField] private StageLoader _loader;

        [Inject] private Player _player;
        [Inject] private CardHand _hand;
        [Inject] private SequenceQueue _queue;
        [Inject] private SequenceExecutor _executor;
        [Inject] private MissionManager _missions;
        [Inject] private StageManager _stage;

        private bool _visible;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private readonly StringBuilder _sb = new();
        private readonly List<Card> _cardCache = new();

        private void Awake()
        {
            _visible = _visibleAtStart;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb[_toggleKey].wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            GUILayout.BeginArea(new Rect(_position.x, _position.y, _width, Screen.height - _position.y - 10));

            /* === 状態表示 === */
            _sb.Clear();
            _sb.Append("<b>[DEBUG HUD]</b>  <size=10>(F10 toggle)</size>\n");
            _sb.Append($"Mission: {(_missions?.Current != null ? _missions.Current.MissionId : "—")}\n");
            _sb.Append($"Player:  {(_player != null ? _player.GridPosition.ToString() : "—")}\n");
            _sb.Append($"Facing:  {(_player != null ? FacingArrow(_player.Facing) : "—")}\n");
            _sb.Append($"Queue:   {_queue?.Count ?? 0}\n");
            _sb.Append($"Exec:    {(_executor?.IsRunning ?? false)}\n");

            if (_hand != null)
            {
                _sb.Append("Hand:\n");
                _cardCache.Clear();
                _cardCache.AddRange(_hand.Cards);
                if (_cardCache.Count == 0) _sb.Append("  (empty)\n");
                else
                    foreach (Card c in _cardCache)
                        if (c != null) _sb.Append($"  {c.DisplayName} ×{_hand.Get(c)}\n");
            }
            GUILayout.Box(_sb.ToString(), _boxStyle);

            GUILayout.Space(4);

            /* === 操作ボタン === */
            GUILayout.Label("Mission", _headerStyle);
            if (GUILayout.Button("▶ Skip Mission (advance)", _buttonStyle))
                _missions?.AdvanceMission();
            if (GUILayout.Button("⟲ Rollback Mission", _buttonStyle))
                _missions?.RollbackCurrentMission();

            GUILayout.Space(2);
            GUILayout.Label("Stage", _headerStyle);
            if (GUILayout.Button("▶ Skip Stage (clear & next)", _buttonStyle))
                _stage?.Clear();
            if (GUILayout.Button("🔄 Reset Stage", _buttonStyle))
                _stage?.ResetStage();

            GUILayout.Space(2);
            GUILayout.Label("Cards", _headerStyle);
            if (GUILayout.Button("➕ Refill Hand (+5 each)", _buttonStyle))
                RefillHand(5);
            if (GUILayout.Button("✕ Clear Queue", _buttonStyle))
                _queue?.Clear();

            int lastCount = _executor?.LastExecuted?.Count ?? 0;
            GUI.enabled = lastCount > 0 && !(_executor?.IsRunning ?? false);
            if (GUILayout.Button($"↺ Restore Last ({lastCount})", _buttonStyle))
                _missions?.RestoreLastSequence();
            GUI.enabled = true;

            /* === Stage Jump（Phase 11） === */
            DrawStageJumpSection();

            GUILayout.EndArea();
        }

        private void DrawStageJumpSection()
        {
            if (_stageGraph == null || _stageGraph.AllNodes == null || _stageGraph.AllNodes.Count == 0) return;

            GUILayout.Space(2);
            GUILayout.Label("Stage Jump", _headerStyle);

            bool canLoad = _loader != null && !_loader.IsLoading;
            foreach (StageNode node in _stageGraph.AllNodes)
            {
                if (node == null) continue;
                string label = string.IsNullOrEmpty(node.DisplayName) ? node.StageId : node.DisplayName;
                string suffix = node.HasPrefab ? "" : "  (no prefab)";
                bool clickable = canLoad && node.HasPrefab;

                GUI.enabled = clickable;
                if (GUILayout.Button($"↦ {label}{suffix}", _buttonStyle))
                    _loader.LoadStageAsync(node, this.GetCancellationTokenOnDestroy()).Forget();
                GUI.enabled = true;
            }
        }

        private void RefillHand(int amount)
        {
            if (_hand == null) return;
            _cardCache.Clear();
            _cardCache.AddRange(_hand.Cards);
            foreach (Card c in _cardCache)
            {
                if (c == null) continue;
                for (int i = 0; i < amount; i++) _hand.Restore(c);
            }
        }

        private static string FacingArrow(Vector2Int f)
        {
            if (f == Vector2Int.up) return "↑ (0,1)";
            if (f == Vector2Int.down) return "↓ (0,-1)";
            if (f == Vector2Int.left) return "← (-1,0)";
            if (f == Vector2Int.right) return "→ (1,0)";
            return f.ToString();
        }

        private void EnsureStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    richText = true,
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 6, 6),
                    wordWrap = false,
                    stretchWidth = true,
                };
                _boxStyle.normal.textColor = Color.white;
            }
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                };
                _headerStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);
            }
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(8, 8, 4, 4),
                };
            }
        }
    }
}
