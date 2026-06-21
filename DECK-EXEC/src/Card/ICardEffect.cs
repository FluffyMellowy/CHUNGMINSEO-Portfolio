namespace Colorless.Card
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// カード効果の抽象インターフェース。
    /// 実装クラスには [System.Serializable] を付与すること（SerializeReference で Card.asset に保存可能にするため）。
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// プレビュー描画用データを生成し、PreviewState を進める。
        /// state の PlayerPos などを変更してシーケンスの仮想進行を反映させる。
        /// 実際のゲーム状態は変更してはならない。
        /// </summary>
        CardPreview BuildPreview(GameContext ctx, PreviewState state, Vector2Int? direction);

        /// <summary>
        /// カード効果を実行する。アニメーション込み、ゲーム状態を変更する。
        /// </summary>
        UniTask ExecuteAsync(GameContext ctx, Vector2Int? direction, CancellationToken ct);
    }
}
