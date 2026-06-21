using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using St;
using System;

namespace UIMaze
{
    public class MazeSaveLoadPoint : MazeUIElement, IInit
    {
        public enum PointType { Save, Load } // セーブ・ロードの種類

        [SerializeField] private PointType _type;                  // このオブジェクトの種類
        [SerializeField] private MazeMinigameManager _manager;     // マネージャー参照
        [SerializeField] private MazeSystemMessage _systemMessage; // システムメッセージ参照
        private RectTransform _cursorRect;                         // カーソルのRectTransform（キャッシュ）

        [SerializeField] private float _saveCooltime = 3f;         // セーブのクールタイム
        private float _lastSaveTime = -999f;                       // 最後にセーブした時間

        [SerializeField] private float _randomDuratonMinValue = 1;
        [SerializeField] private float _randomDuratonMaxValue = 3;

        public void SetCursor(MazeCursor cursor)
        {
            _cursorRect = cursor.GetComponent<RectTransform>();
        }

        public async UniTask Init()
        {
            float randomDuration = UnityEngine.Random.Range(_randomDuratonMinValue, _randomDuratonMaxValue);

            // PointTypeがLoadの場合のみ上下移動
            if (_type == PointType.Load)
            {
                var tween = _rect.DOAnchorPosY(_rect.anchoredPosition.y + 400f, randomDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetLink(gameObject);
            }
            await UniTask.CompletedTask;
        }

        private void Update()
        {
            if (_cursorRect == null) return;
            if (!RectOverlaps(_rect, _cursorRect)) return;

            if (_type == PointType.Save)
            {
                // クールタイム中は動作しない
                if (Time.time - _lastSaveTime < _saveCooltime) return;

                _lastSaveTime = Time.time;
                _manager.SavePosition(_cursorRect.anchoredPosition);
                _systemMessage.ShowMessage($"({_cursorRect.anchoredPosition.x:F0}, {_cursorRect.anchoredPosition.y:F0})にカーソルをセーブしました。");
            }
            else
            {
                Vector2 savedPos = _manager.SavedPosition;
                _cursorRect.anchoredPosition = savedPos;
                // ロード直後はセーブ無効化
                _lastSaveTime = Time.time;
                _systemMessage.ShowMessage($"({savedPos.x:F0}, {savedPos.y:F0})にカーソルをロードしました。");
            }
        }
    }
}