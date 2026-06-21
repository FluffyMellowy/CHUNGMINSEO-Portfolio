namespace Colorless.Stage
{
    using UnityEngine;
    using Colorless.Mission;

    /// <summary>
    /// 1 個のロードされたステージのスナップショット。
    /// StageLoader がプレハブをインスタンス化した直後に作成し、永続シーン側の subscriber へ通知する。
    /// </summary>
    public sealed class StageInstance
    {
        public StageNode Node { get; }
        public GameObject Root { get; }
        public MissionManager Mission { get; }

        public StageInstance(StageNode node, GameObject root, MissionManager mission)
        {
            Node = node;
            Root = root;
            Mission = mission;
        }
    }
}
