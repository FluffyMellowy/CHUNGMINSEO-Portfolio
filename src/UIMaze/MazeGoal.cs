using UnityEngine;

namespace UIMaze
{
    public class MazeGoal : MazeUIElement
    {
        [SerializeField] private MazeMinigameManager _manager; // ゴール到達時にクリアを通知するマネージャー
        private RectTransform _cursorRect;                     // カーソルのRectTransform（キャッシュ）

        [SerializeField] private SectionTypeEvent _clear;

        private bool _isReached = false;

        public void SetCursor(MazeCursor cursor)
        {
            _cursorRect = cursor.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_cursorRect == null) return;
            // 毎フレームカーソルとの重なりを確認
            if (RectOverlaps(_rect, _cursorRect))
            {
                if (!_isReached)
                {
                    // ゴールに到達したらクリアを通知
                    // _clear.Raise(St.SectionType.Maze);
                    Debug.Log("Clear");
                    _isReached = true;
                }
                
            }
        }
    }
}