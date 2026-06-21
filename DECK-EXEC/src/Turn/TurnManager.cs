namespace Colorless.Turn
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using Colorless.Entity;
    using Colorless.Grid;

    public sealed class TurnManager : MonoBehaviour
    {
        [Inject] private GridManager _gridManager;

        private List<IEntity> _entities = new List<IEntity>();
        private bool _isProcessing = false;

        [Title("Runtime Debug")]
        [ShowInInspector, ReadOnly, LabelText("Registered Entities")]
        private int RegisteredCountDebug => _entities?.Count ?? 0;

        /// <summary>
        /// ターン処理中かどうか
        /// </summary>
        [ShowInInspector, ReadOnly]
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// エンティティを登録
        /// </summary>
        public void RegisterEntity(IEntity entity)
        {
            if (!_entities.Contains(entity))
                _entities.Add(entity);
        }

        /// <summary>
        /// エンティティを解除
        /// </summary>
        public void UnregisterEntity(IEntity entity)
        {
            _entities.Remove(entity);
        }

        /// <summary>
        /// プレイヤーが行動した後に呼ばれる。
        /// 登録された全エンティティを順番に行動させる。
        /// </summary>
        public void ProcessTurn()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            /* 全エンティティが順番に行動 */
            foreach (IEntity entity in _entities)
            {
                entity.Act();
            }

            /* ボタン状態更新 */
            _gridManager.UpdateButtons();

            _isProcessing = false;
        }
    }
}
