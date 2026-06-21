namespace Colorless.Card.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using UnityEngine;
    using Colorless.Grid;
    using Colorless.Sequence;

    /// <summary>
    /// プレイヤーの facing 方向に最大 _maxDistance セル直進する効果。
    /// 障害物（壁・穴・Box・敵）の手前で停止。1 セルも進めない場合は narration のみ。
    /// 距離が異なるバリエーション（Dash3 / Dash5 等）は _maxDistance を変えた別 Card.asset で表現。
    /// Move と同様、facing は移動可否と無関係に毎回更新する（"意図" として）。
    /// </summary>
    [Serializable]
    public sealed class DashEffect : ICardEffect
    {
        [SerializeField, Min(1)] private int _maxDistance = 3;
        [SerializeField] private float _perCellDuration = 0.06f;
        [SerializeField] private float _minTotalDuration = 0.18f;

        public CardPreview BuildPreview(GameContext ctx, PreviewState state, Vector2Int? direction)
        {
            Vector2Int dir = state.Facing;
            Vector2Int from = state.PlayerPos;

            /* facing 更新（実 Execute と一致させる） */
            state.Facing = dir;

            /* 通過セルを順に判定して最遠到達点を決める */
            List<Vector2Int> path = new() { from };
            Vector2Int cur = from;
            int travelled = 0;
            for (int i = 0; i < _maxDistance; i++)
            {
                Vector2Int next = cur + dir;
                if (!state.CanPlayerEnter(ctx, next)) break;
                path.Add(next);
                cur = next;
                travelled++;
            }

            if (travelled == 0)
                return CardPreview.Stationary(from, dir);

            state.PlayerPos = cur;
            return new CardPreview(path.ToArray(), cur, Array.Empty<Vector2Int>(), dir);
        }

        public async UniTask ExecuteAsync(GameContext ctx, Vector2Int? direction, CancellationToken ct)
        {
            Vector2Int dir = ctx.Player.Facing;

            /* facing 更新（移動の成否と無関係に） */
            ctx.Player.SetFacing(dir);

            Vector2Int from = ctx.Player.GridPosition;
            Vector2Int cur = from;
            int travelled = 0;
            for (int i = 0; i < _maxDistance; i++)
            {
                Vector2Int next = cur + dir;
                if (!CanEnter(ctx, next)) break;
                cur = next;
                travelled++;
            }

            LogColorPalette p = ctx.Palette;

            if (travelled == 0)
            {
                ctx.Logger?.Log($"{p.T_Player("Player")}の{p.T_Verb("Dash")}が{p.T_Direction(LogFormat.DirText(dir))}に{p.T_Danger("阻まれた")}");
                return;
            }

            /* アニメーション時間：1 セル当たり _perCellDuration、最低 _minTotalDuration を保証 */
            float duration = Mathf.Max(_minTotalDuration, _perCellDuration * travelled);

            Vector3 worldTarget = ctx.Grid.GridToWorld(cur);
            Tween tween = ctx.Player.transform
                .DOMove(worldTarget, duration)
                .SetEase(Ease.OutCubic);

            UniTaskCompletionSource tcs = new();
            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() => tcs.TrySetResult());

            using (ct.Register(() => { if (tween.IsActive()) tween.Kill(); }))
            {
                await tcs.Task;
            }

            if (ct.IsCancellationRequested) return;
            ctx.Player.ExecuteMove(cur);

            /* narration: 走った距離を含めて表示 */
            ctx.Logger?.Log($"{p.T_Player("Player")}が{p.T_Direction(LogFormat.DirText(dir))}へ{p.T_Verb("Dash")} → {p.T_Coord(LogFormat.Coord(cur))} ({travelled}マス)");
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
