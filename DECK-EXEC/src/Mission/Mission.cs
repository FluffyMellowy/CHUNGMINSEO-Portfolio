namespace Colorless.Mission
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using Colorless.Card;

    /// <summary>
    /// ステージ内の 1 ミッションの定義。Data/Missions/ 配下にアセットとして作成する。
    /// </summary>
    [CreateAssetMenu(fileName = "Mission_", menuName = "DECK::EXEC/Mission")]
    public sealed class Mission : ScriptableObject
    {
        [Serializable]
        public struct CardEntry
        {
            public Card Card;
            [Min(0)] public int Count;
        }

        [Title("Identity")]
        [SerializeField] private string _missionId;
        [SerializeField, TextArea] private string _description;

        [Title("Initial State")]
        [InfoBox("ミッション開始時のプレイヤー向き。Move カードがなくても Dash/PushBox 等が使えるようにするための初期値。デフォルト下向き。")]
        [SerializeField] private Vector2Int _initialFacing = new(0, -1);

        [Title("Available Cards")]
        [InfoBox("このミッションで使えるカードと枚数")]
        [SerializeField] private List<CardEntry> _availableCards = new();

        [Title("Clear Condition")]
        [InfoBox("クリア条件を SerializeReference で選択（Conditions/ 配下のクラス）")]
        [SerializeReference] private IClearCondition _clearCondition;

        public string MissionId => _missionId;
        public string Description => _description;
        public Vector2Int InitialFacing => _initialFacing;
        public IReadOnlyList<CardEntry> AvailableCards => _availableCards;
        public IClearCondition ClearCondition => _clearCondition;
    }
}
