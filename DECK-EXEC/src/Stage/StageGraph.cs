namespace Colorless.Stage
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// 全スタージのグラフ構造を保持するアセット。
    /// StageSelectController が参照し、ノードと接続線を動的に生成する。
    /// </summary>
    [CreateAssetMenu(fileName = "StageGraph", menuName = "DECK::EXEC/Stage Graph")]
    public sealed class StageGraph : ScriptableObject
    {
        [Title("Entry Point")]
        [InfoBox("ゲーム開始時や初期表示のルートノード")]
        [Required, SerializeField] private StageNode _rootNode;

        [Title("All Nodes")]
        [InfoBox("グラフに含まれる全ノードをここに登録")]
        [SerializeField] private List<StageNode> _allNodes = new List<StageNode>();

        public StageNode Root => _rootNode;
        public IReadOnlyList<StageNode> AllNodes => _allNodes;
    }
}
