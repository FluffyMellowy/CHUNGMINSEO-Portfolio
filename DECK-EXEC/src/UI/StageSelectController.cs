namespace Colorless.UI
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using Colorless.Core;
    using Colorless.Stage;

    /// <summary>
    /// ステージ選択画面のメインコントローラ。
    /// StageGraph から全ノードを読み取り、UIノードと接続線を動的生成する。
    /// </summary>
    public sealed class StageSelectController : MonoBehaviour
    {
        [Title("Data")]
        [Required, SerializeField] private StageGraph _graph;

        [Title("UI Prefabs")]
        [Required, SerializeField] private StageNodeButton _nodePrefab;
        [Required, SerializeField] private RectTransform _linePrefab;

        [Title("Containers")]
        [InfoBox("ラインとノードは別コンテナ推奨（ラインを後ろに描画するため）")]
        [Required, SerializeField] private RectTransform _nodeContainer;
        [Required, SerializeField] private RectTransform _lineContainer;

        [Title("Settings")]
        [InfoBox("グリッド1マスあたりの画面上の距離（ピクセル）")]
        [SerializeField] private float _nodeSpacing = 80f;
        [SerializeField] private string _mainMenuScene = "MainMenu";

        private readonly List<StageNodeButton> _spawnedButtons = new List<StageNodeButton>();
        private readonly List<RectTransform> _spawnedLines = new List<RectTransform>();

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.StageSelect);

            BuildMap();
        }

        private void BuildMap()
        {
            /* 接続線を先に生成（ノードの裏に描画） */
            foreach (StageNode node in _graph.AllNodes)
            {
                if (node == null) continue;
                foreach (StageNode connected in node.Connections)
                {
                    if (connected == null) continue;
                    DrawLine(ToPixel(node.GridCoordinate), ToPixel(connected.GridCoordinate));
                }
            }

            /* ノードボタン生成 */
            foreach (StageNode node in _graph.AllNodes)
            {
                if (node == null) continue;
                StageNodeButton btn = Instantiate(_nodePrefab, _nodeContainer);
                RectTransform rt = btn.GetComponent<RectTransform>();
                rt.anchoredPosition = ToPixel(node.GridCoordinate);
                btn.Setup(node, OnNodeClicked);
                _spawnedButtons.Add(btn);
            }
        }

        private Vector2 ToPixel(Vector2 gridCoord) => gridCoord * _nodeSpacing;

        /// <summary>
        /// 2点間にUI Imageを伸縮・回転させて線として描画。
        /// </summary>
        private void DrawLine(Vector2 start, Vector2 end)
        {
            RectTransform line = Instantiate(_linePrefab, _lineContainer);

            Vector2 midpoint = (start + end) / 2f;
            float length = Vector2.Distance(start, end);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

            line.anchoredPosition = midpoint;
            line.sizeDelta = new Vector2(length, line.sizeDelta.y);
            line.localRotation = Quaternion.Euler(0f, 0f, angle);

            _spawnedLines.Add(line);
        }

        private void OnNodeClicked(StageNode node)
        {
            SceneTransitioner.Instance.LoadScene(node.SceneName);
        }

        /// <summary>
        /// メインメニューへ戻る。BackボタンOnClickから呼び出す。
        /// </summary>
        public void OnBack()
        {
            SceneTransitioner.Instance.LoadScene(_mainMenuScene);
        }
    }
}
