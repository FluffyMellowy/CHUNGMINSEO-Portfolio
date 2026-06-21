namespace Colorless.Card
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// カードの静的データ。Data/Cards/ 配下に ScriptableObject アセットとして作成する。
    /// </summary>
    [CreateAssetMenu(fileName = "Card_", menuName = "DECK::EXEC/Card")]
    public sealed class Card : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _cardId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [Title("Behavior")]
        [InfoBox("方向パラメータが必要な場合は true（Move, Dash, PushBox など）")]
        [SerializeField] private bool _requiresDirection;

        [Title("Effect")]
        [InfoBox("ICardEffect 実装を割り当てる。Effects/ 配下のクラスから選択。")]
        [SerializeReference] private ICardEffect _effect;

        public string CardId => _cardId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public bool RequiresDirection => _requiresDirection;
        public ICardEffect Effect => _effect;
    }
}
