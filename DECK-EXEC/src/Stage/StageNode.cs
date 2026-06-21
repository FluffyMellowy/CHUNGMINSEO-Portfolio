namespace Colorless.Stage
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// スタージ1つの設定データ。ScriptableObjectアセットとして各ステージごとに作成する。
    /// </summary>
    [CreateAssetMenu(fileName = "StageNode_", menuName = "DECK::EXEC/Stage Node")]
    public sealed class StageNode : ScriptableObject
    {
        [Title("Identity")]
        [Required, SerializeField] private string _stageId;
        [SerializeField] private string _displayName;

        [Title("Stage Content")]
        [InfoBox("Phase 11 以降の構成。Stage_X_Y プレハブ（Grid / Tilemap / Player / Box / Enemy / MissionManager を内包）を割り当てる。割り当てがあるとき StageLoader はこちらを使う。")]
        [SerializeField] private GameObject _stagePrefab;

        [InfoBox("【Legacy】旧シーン遷移用。Phase 11-E で完全に StagePrefab に置き換わる。新規ノードでは空のままで OK。")]
        [SerializeField] private string _sceneName;

        [Title("Map Display")]
        [InfoBox("相対グリッド座標。実際の表示位置 = GridCoordinate × StageSelectController._nodeSpacing")]
        [SerializeField] private Vector2 _gridCoordinate;

        [Title("Graph Connections")]
        [InfoBox("このノードから線を引いて繋がる次の候補ノード")]
        [SerializeField] private List<StageNode> _connections = new List<StageNode>();

        [InfoBox("このノードを解放するために事前にクリアが必要なノード一覧")]
        [SerializeField] private List<StageNode> _prerequisites = new List<StageNode>();

        public string StageId => _stageId;
        public GameObject StagePrefab => _stagePrefab;
        public string SceneName => _sceneName;
        public string DisplayName => _displayName;
        public Vector2 GridCoordinate => _gridCoordinate;
        public IReadOnlyList<StageNode> Connections => _connections;
        public IReadOnlyList<StageNode> Prerequisites => _prerequisites;

        /// <summary>StagePrefab が設定されているか（Phase 11 移行済み判定）。</summary>
        public bool HasPrefab => _stagePrefab != null;
    }
}
