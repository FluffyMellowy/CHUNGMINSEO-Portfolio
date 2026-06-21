namespace Colorless.Card.Effects
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using UnityEngine;
    using Colorless.Entity;
    using Colorless.Grid;
    using Colorless.Sequence;

    /// <summary>
    /// 指定方向に 1 セル移動する効果。Card.asset の SerializeReference 上で
    /// _direction を 4 方向のいずれかに設定する（Card_MoveUp/Down/Left/Right 各 1 つ）。
    /// 移動できなくても facing は更新される（プレイヤーの "意図" として扱う）。
    /// </summary>
    [Serializable]
    public sealed class MoveEffect : ICardEffect
    {
        [SerializeField] private Vector2Int _direction = new(0, 1);
        [SerializeField] private float _animationDuration = 0.18f;

        public CardPreview BuildPreview(GameContext ctx, PreviewState state, Vector2Int? direction)
        {
            Vector2Int dir = _direction;
            Vector2Int from = state.PlayerPos;

            /* facing は移動可否に関わらず更新（"意図" として） */
            state.Facing = dir;

            Vector2Int to = from + dir;

            /* 仮想状態を考慮した進入判定（先行 Push で動いた Box の位置も反映） */
            if (!state.CanPlayerEnter(ctx, to)) return CardPreview.Stationary(from, dir);

            state.PlayerPos = to;
            return new CardPreview(new[] { from, to }, to, Array.Empty<Vector2Int>(), dir);
        }

        public async UniTask ExecuteAsync(GameContext ctx, Vector2Int? direction, CancellationToken ct)
        {
            Vector2Int dir = _direction;

            /* facing 更新（移動の成否と無関係に） */
            ctx.Player.SetFacing(dir);

            Vector2Int from = ctx.Player.GridPosition;
            Vector2Int to = from + dir;

            LogColorPalette p = ctx.Palette;

            /* 移動先が無効 → narration だけ流して終了 */
            if (!CanEnter(ctx, to))
            {
                ctx.Logger?.Log($"{p.T_Player("Player")}の{p.T_Verb("移動")}が{p.T_Direction(LogFormat.DirText(dir))}に{p.T_Danger("阻まれた")}");
                return;
            }

            /* DOTween で移動アニメーション */
            Vector3 worldTarget = ctx.Grid.GridToWorld(to);
            Tween tween = ctx.Player.transform
                .DOMove(worldTarget, _animationDuration)
                .SetEase(Ease.OutQuad);

            UniTaskCompletionSource tcs = new();
            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() => tcs.TrySetResult());

            using (ct.Register(() => { if (tween.IsActive()) tween.Kill(); }))
            {
                await tcs.Task;
            }

            if (ct.IsCancellationRequested) return;
            ctx.Player.ExecuteMove(to);

            /* narration: "Player が (3,4) へ移動！" */
            ctx.Logger?.Log($"{p.T_Player("Player")}が{p.T_Coord(LogFormat.Coord(to))}へ{p.T_Verb("移動")}！");
        }

        private static bool CanEnter(GameContext ctx, Vector2Int pos)
        {
            GridCell cell = ctx.Grid.GetCell(pos);
            if (cell == null) return false;
            if (!cell.IsWalkable) return false;
            return cell.Occupant == null;
        }
    }
}
