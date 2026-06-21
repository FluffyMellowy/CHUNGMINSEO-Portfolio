using UnityEngine;
using UnityEngine.InputSystem;

namespace UIMaze
{
    public class MazeCursor : MazeUIElement
    {

        [SerializeField] private float _speed = 300f;   // カーソルの移動速度
        [SerializeField] private RectTransform _gameArea;   // カーソルの移動範囲を制限するゲームエリアのRectTransform

        private Vector2 _prevPosition;

        private MazeWall[] _walls;  // 壁配列のキャッシュ

        /// <summary>
        /// 毎フレームの更新処理
        /// 左スティックの入力値をもとにカーソルを移動させる
        /// </summary>
        private void Update()
        {
            if (Gamepad.current == null) return;

            if (Gamepad.current != null)
            {
                Debug.Log("ゲームパッドon");
            }

            Vector2 input = Gamepad.current.leftStick.ReadValue();
            Vector2 prevPos = _rect.anchoredPosition;

            // X軸移動後に壁との重なりを確認
            _rect.anchoredPosition = new Vector2(prevPos.x + input.x * _speed * Time.deltaTime, prevPos.y);
            ClampToArea();
            if (IsOverlappingAnyWall())
                _rect.anchoredPosition = new Vector2(prevPos.x, _rect.anchoredPosition.y);

            // Y軸移動後に壁との重なりを確認
            Vector2 afterX = _rect.anchoredPosition;
            _rect.anchoredPosition = new Vector2(afterX.x, afterX.y + input.y * _speed * Time.deltaTime);
            ClampToArea();
            if (IsOverlappingAnyWall())
                _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, afterX.y);
        }

        public void SetWalls(MazeWall[] walls)
        {
            _walls = walls;
            Debug.Log($"SetWalls called: {_walls.Length}");
        }

        private bool IsOverlappingAnyWall()
        {
            if (_walls == null || _walls.Length == 0) return false;
            foreach (var wall in _walls)
            {
                Debug.Log($"Wall Rect: {wall.Rect}, Cursor Rect: {_rect}");
                if (RectOverlaps(_rect, wall.Rect))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// カーソルをゲームエリア内に制限する
        /// ゲームエリアの半分のサイズを上限・下限としてClampする
        /// </summary>
        private void ClampToArea()
        {
            // ゲームエリアの半サイズを取得（中心が原点のため）
            Vector2 areaSize = _gameArea.rect.size * 0.5f;
            Vector2 pos = _rect.anchoredPosition;
            // X・Yそれぞれをエリア内に収める
            pos.x = Mathf.Clamp(pos.x, -areaSize.x, areaSize.x);
            pos.y = Mathf.Clamp(pos.y, -areaSize.y, areaSize.y);
            _rect.anchoredPosition = pos;
        }

        public void RevertPosition()
        {
            // 壁とぶつかった時以前位置に戻す
            _rect.anchoredPosition = _prevPosition;
        }
    }
}
