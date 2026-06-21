namespace Colorless.Entity
{
    using System.Collections.Generic;
    using UnityEngine;
    using Colorless.Entity.Enemy;
    using Colorless.Grid;

    /// <summary>
    /// 敵 1 ターン進行後に呼ばれ、敵同士の衝突を解決する。
    /// 現状の TurnManager は順次 Act() 実行で同時進入が発生しないため、
    /// 将来「先に移動した敵が勝つ」以外の振る舞いが必要になった時点で本格実装する。
    /// </summary>
    public sealed class EntityCollisionResolver
    {
        /// <summary>
        /// 衝突解決を実行する。現在は no-op。
        /// </summary>
        public void ResolveCollisions()
        {
            /* TODO: 敵-敵 同時衝突の相互消滅（CLAUDE.md TODO）
             * 現在の Act() 順次モデルでは同時進入が発生しない。
             * 相互消滅仕様を完全実装するには次のいずれか:
             *   1) IEntity.Plan() を追加して desired position を収集 → Resolve → Apply
             *   2) Act() 終了時に「自分のひとつ前の位置に他の敵が居る」スワップ衝突を検出
             */
        }
    }
}
