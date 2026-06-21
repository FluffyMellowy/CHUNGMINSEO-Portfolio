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
    /// プレイヤーの facing 方向にある Box を 1 セル押す効果。
    /// 押す先が穴なら Box は非表示化（ロールバックで復元可能）。
    /// プレイヤー位置・facing は変更されない（隣接セルへの作用のみ）。
    /// </summary>
    [Serializable]
    public sealed class PushBoxEffect : ICardEffect
    {
        [SerializeField] private float _animationDuration = 0.2f;

        public CardPreview BuildPreview(GameContext ctx, PreviewState state, Vector2Int? direction)
        {
            Vector2Int dir = state.Facing;
            Vector2Int adjacent = state.PlayerPos + dir;

            /* 仮想状態で隣接セルの Box を探す（先行 Push で動いたケースも反映） */
            Box box = state.GetBoxAt(adjacent);
            if (box == null)
                return CardPreview.Stationary(state.PlayerPos, dir);

            /* 押し先セルの検証（壁・他 Box は不可、穴なら消滅扱い） */
            Vector2Int boxDest = adjacent + dir;
            GridCell destCell = ctx.Grid.GetCell(boxDest);
            if (destCell == null)
                return CardPreview.Stationary(state.PlayerPos, dir);

            bool fallsIntoHole = destCell.TileType == TileType.Hole;
            bool wallBlocked = !destCell.IsWalkable && !fallsIntoHole;
            bool boxBlocked = state.HasBoxAt(boxDest); /* 仮想で他 Box がいる */

            if (wallBlocked || boxBlocked)
                return CardPreview.Stationary(state.PlayerPos, dir);

            /* 仮想位置を更新（穴なら null = 消滅、それ以外は新位置） */
            state.SetBoxPos(box, fallsIntoHole ? (Vector2Int?)null : boxDest);

            return new CardPreview(
                new[] { state.PlayerPos },
                state.PlayerPos,
                new[] { adjacent, boxDest },
                dir);
        }

        public async UniTask ExecuteAsync(GameContext ctx, Vector2Int? direction, CancellationToken ct)
        {
            Vector2Int dir = ctx.Player.Facing;
            Vector2Int adjacent = ctx.Player.GridPosition + dir;
            LogColorPalette p = ctx.Palette;
            string dirText = LogFormat.DirText(dir);

            GridCell cell = ctx.Grid.GetCell(adjacent);

            /* 隣接セル無し or Occupant 無し → 空振り */
            if (cell == null || cell.Occupant == null)
            {
                ctx.Logger?.Log($"{p.T_Player("Player")}の{p.T_Verb("Push")}が{p.T_System("空中")}を{p.T_Verb("空振り")}");
                return;
            }

            Box box = cell.Occupant.GetComponent<Box>();
            if (box == null)
            {
                /* Box ではない occupant（敵など）。Push は無生物専用なので空振り扱い */
                ctx.Logger?.Log($"{p.T_Player("Player")}の{p.T_Verb("Push")}は{p.T_Box("Box")}にのみ有効");
                return;
            }

            /* 押し先セルが有効か検証（穴 OR 進入可能） */
            Vector2Int boxTargetPos = adjacent + dir;
            GridCell boxTargetCell = ctx.Grid.GetCell(boxTargetPos);
            if (boxTargetCell == null)
            {
                ctx.Logger?.Log($"{p.T_Box("Box")}が{p.T_Wall("壁")}に{p.T_Danger("阻まれた")}");
                return;
            }

            bool fallsIntoHole = boxTargetCell.TileType == TileType.Hole;
            if (!fallsIntoHole && !boxTargetCell.CanEnter())
            {
                ctx.Logger?.Log($"{p.T_Box("Box")}が{p.T_Wall("壁")}に{p.T_Danger("阻まれた")}");
                return;
            }

            /* Box の現在位置から目標位置へアニメーション */
            Vector3 worldTarget = ctx.Grid.GridToWorld(boxTargetPos);
            Tween tween = box.transform
                .DOMove(worldTarget, _animationDuration)
                .SetEase(Ease.OutBack);

            UniTaskCompletionSource tcs = new();
            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() => tcs.TrySetResult());

            using (ct.Register(() => { if (tween.IsActive()) tween.Kill(); }))
            {
                await tcs.Task;
            }

            if (ct.IsCancellationRequested) return;

            /* 状態反映（位置はアニメーションで設定済みだが、Box.TryPush が
               Occupant・gridPosition・穴落ち時の SetActive(false) を一括処理） */
            box.TryPush(dir);

            /* narration: 通常 push と穴落ちを別行で */
            ctx.Logger?.Log($"{p.T_Player("Player")}が{p.T_Box("Box")}を{p.T_Direction(dirText)}へ{p.T_Verb("押した")}");
            if (fallsIntoHole)
                ctx.Logger?.Log($"{p.T_Box("Box")}が{p.T_System("穴")}に{p.T_Danger("落ちた")}");
        }
    }
}
