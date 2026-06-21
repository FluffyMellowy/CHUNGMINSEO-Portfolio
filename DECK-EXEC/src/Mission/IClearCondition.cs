namespace Colorless.Mission
{
    using Colorless.Card;

    /// <summary>
    /// ミッションのクリア条件を抽象化するインターフェース。
    /// 実装クラスには [System.Serializable] を付与し、Mission SO 上で SerializeReference で割り当てる。
    /// </summary>
    public interface IClearCondition
    {
        bool IsCleared(GameContext ctx);
    }
}
