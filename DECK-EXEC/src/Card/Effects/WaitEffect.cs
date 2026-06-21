namespace Colorless.Card.Effects
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using Colorless.Sequence;

    /// <summary>
    /// 1 ターンを「待機」して消費する効果。
    /// プレイヤーは動かないが、TurnManager.ProcessTurn は通常通り走る（敵 AI は進む）ため、
    /// 敵の動線を「待って外す」タイミング調整カードとして機能する。
    /// facing は変更しない。
    /// </summary>
    [Serializable]
    public sealed class WaitEffect : ICardEffect
    {
        [SerializeField, Min(0f)] private float _holdDuration = 0.15f;

        public CardPreview BuildPreview(GameContext ctx, PreviewState state, Vector2Int? direction)
        {
            /* 仮想状態は何も変えない。プレビュー描画は現在地に止まるだけ。 */
            return CardPreview.Stationary(state.PlayerPos, state.Facing);
        }

        public async UniTask ExecuteAsync(GameContext ctx, Vector2Int? direction, CancellationToken ct)
        {
            LogColorPalette p = ctx.Palette;
            ctx.Logger?.Log($"{p.T_Player("Player")}が{p.T_Time("一拍")}{p.T_Verb("待機")}");

            /* 視覚的に「待った感」を出すために短いディレイを入れる。0 でも機能上の問題なし。 */
            if (_holdDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(_holdDuration), cancellationToken: ct);
        }
    }
}
