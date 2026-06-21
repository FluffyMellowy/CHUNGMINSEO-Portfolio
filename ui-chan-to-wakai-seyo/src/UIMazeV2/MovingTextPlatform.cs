using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// 動くテキスト発板の振動方向
    /// </summary>
    public enum MoveAxis
    {
        Horizontal, // 左右
        Vertical, // 上下
    }

    /// <summary>
    /// 動くテキスト発板：左右または上下に振動する追加モーションを提供する
    /// CreditScrollerのFixedUpdate内でGetFrameDelta()が呼ばれ、スクロール量と合算されて
    /// 1回のMovePositionで適用される（プレイヤーが発板上に乗っている時の挙動を安定させるため）
    /// </summary>
    public class MovingTextPlatform : MonoBehaviour
    {
        [Header("移動設定")]
        [Tooltip("振動方向：左右 or 上下")]
        [SerializeField] private MoveAxis _axis = MoveAxis.Horizontal;

        [Tooltip("中心からの最大距離（ワールド単位）")]
        [SerializeField] private float _amplitude = 1f;

        [Tooltip("移動速度（unit/秒）")]
        [SerializeField] private float _speed = 2f;

        [Tooltip("初期方向：trueで正方向（右 or 上）、falseで負方向")]
        [SerializeField] private bool _startPositive = true;

        private float _direction; // 現在の進行方向（+1 or -1）
        private float _offsetFromOrigin; // スポーン位置からの累積オフセット

        private void Awake()
        {
            _direction = _startPositive ? 1f : -1f;
        }

        /// <summary>
        /// 振動の累積オフセットと進行方向を初期状態に戻す
        /// CreditScrollerのResetStage()からまとめて呼ばれる想定
        /// </summary>
        public void ResetMotion()
        {
            _offsetFromOrigin = 0f;
            _direction = _startPositive ? 1f : -1f;
        }

        /// <summary>
        /// このフレームで発板が左右/上下に動く分のデルタ移動量を返す
        /// CreditScrollerが本値とスクロール量を合算してMovePositionに渡す
        /// 振幅の端点に到達したら自動で方向反転する（ping-pong）
        /// </summary>
        public Vector2 GetFrameDelta(float deltaTime)
        {
            float step = _speed * deltaTime * _direction;
            float newOffset = _offsetFromOrigin + step;

            // 端点を超える場合はクランプして方向反転
            if (newOffset > _amplitude)
            {
                step = _amplitude - _offsetFromOrigin;
                newOffset = _amplitude;
                _direction = -1f;
            }
            else if (newOffset < -_amplitude)
            {
                step = -_amplitude - _offsetFromOrigin;
                newOffset = -_amplitude;
                _direction = 1f;
            }

            _offsetFromOrigin = newOffset;

            return _axis == MoveAxis.Horizontal
                ? new Vector2(step, 0f)
                : new Vector2(0f, step);
        }
    }
}
