namespace Colorless.UI
{
    using System;
    using System.Collections.Generic;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Card;
    using Colorless.Entity;
    using Colorless.Sequence;

    /// <summary>
    /// シーケンス入力中の予測経路をマップにオーバーレイ描画する。
    /// SequenceQueue を購読してリアクティブに再描画する。
    /// </summary>
    public sealed class MapPreviewRenderer : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private LineRenderer _pathLine;
        [Required, SerializeField] private SpriteRenderer _silhouettePrefab;
        [SerializeField] private SpriteRenderer _overlayPrefab;
        [InfoBox("Box の仮想位置に表示する半透明ゴースト用プレハブ。未割当ならボックスゴーストは表示されない。スプライトは各 Box から自動コピーされる。")]
        [SerializeField] private SpriteRenderer _boxGhostPrefab;

        [Inject] private SequenceQueue _queue;
        [Inject] private GameContext _ctx;

        private readonly List<SpriteRenderer> _silhouettes = new();
        private readonly List<SpriteRenderer> _overlays = new();
        private readonly List<SpriteRenderer> _boxGhosts = new();
        private readonly List<Box> _cachedBoxes = new();
        private IDisposable _subscription;

        private void Start()
        {
            _subscription = _queue.Changed.Subscribe(_ => Refresh());
            CacheBoxes();
            Refresh();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }

        /// <summary>
        /// シーン内の Box を全て収集（プレビュー用に仮想位置追跡する対象）。
        /// シーンに Box が動的追加される場合は再収集が必要。
        /// </summary>
        public void CacheBoxes()
        {
            _cachedBoxes.Clear();
            foreach (Box b in FindObjectsByType<Box>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                _cachedBoxes.Add(b);
        }

        public void Refresh()
        {
            ClearSpawned();

            if (_queue.Count == 0)
            {
                _pathLine.positionCount = 0;
                return;
            }

            PreviewState state = new(_ctx.Player.GridPosition, _ctx.Player.Facing, _cachedBoxes);
            List<Vector3> pathPoints = new() { _ctx.Grid.GridToWorld(state.PlayerPos) };

            foreach (QueuedCard q in _queue.Items)
            {
                if (q.Card == null || q.Card.Effect == null) continue;
                CardPreview preview = q.Card.Effect.BuildPreview(_ctx, state, q.Direction);

                /* 経路セルをワールド座標に変換して連結 */
                for (int i = 1; i < preview.PathCells.Length; i++)
                    pathPoints.Add(_ctx.Grid.GridToWorld(preview.PathCells[i]));

                /* 環境変化セルにオーバーレイ表示 */
                if (_overlayPrefab != null)
                {
                    foreach (Vector2Int cell in preview.AffectedCells)
                    {
                        SpriteRenderer ov = Instantiate(_overlayPrefab, transform);
                        ov.transform.position = _ctx.Grid.GridToWorld(cell);
                        _overlays.Add(ov);
                    }
                }
            }

            /* 最終到達セルにシルエット表示 */
            SpriteRenderer sil = Instantiate(_silhouettePrefab, transform);
            sil.transform.position = _ctx.Grid.GridToWorld(state.PlayerPos);
            _silhouettes.Add(sil);

            /* Box ゴースト：仮想位置が実位置と異なる場合のみ表示。
               穴に落ちた（仮想位置 null）ケースは表示しない（"消滅" の演出は別途）。 */
            if (_boxGhostPrefab != null)
            {
                foreach (Box box in _cachedBoxes)
                {
                    if (box == null) continue;
                    Vector2Int? virtualPos = state.GetBoxPos(box);
                    if (!virtualPos.HasValue) continue;            /* 仮想で消滅 */
                    if (virtualPos.Value == box.GridPosition) continue; /* 動いていない */

                    SpriteRenderer ghost = Instantiate(_boxGhostPrefab, transform);
                    ghost.transform.position = _ctx.Grid.GridToWorld(virtualPos.Value);

                    /* 元の Box のスプライトを反映（Box 種類が複数あっても見た目が一致するように）。
                       SpriteRenderer がルートでなく子にあるケースにも対応するため GetComponentInChildren を使う。 */
                    SpriteRenderer boxSprite = box.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
                    if (boxSprite != null && boxSprite.sprite != null)
                        ghost.sprite = boxSprite.sprite;

                    _boxGhosts.Add(ghost);
                }
            }

            /* 線描画 */
            _pathLine.positionCount = pathPoints.Count;
            for (int i = 0; i < pathPoints.Count; i++)
                _pathLine.SetPosition(i, pathPoints[i]);
        }

        private void ClearSpawned()
        {
            foreach (SpriteRenderer s in _silhouettes)
                if (s != null) Destroy(s.gameObject);
            _silhouettes.Clear();

            foreach (SpriteRenderer o in _overlays)
                if (o != null) Destroy(o.gameObject);
            _overlays.Clear();

            foreach (SpriteRenderer g in _boxGhosts)
                if (g != null) Destroy(g.gameObject);
            _boxGhosts.Clear();
        }
    }
}
