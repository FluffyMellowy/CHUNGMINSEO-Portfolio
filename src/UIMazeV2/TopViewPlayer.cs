using UnityEngine;
using UnityEngine.InputSystem;

namespace UIMazeV2
{
    /// <summary>
    /// 見下ろし型ミニゲーム用のプレイヤーコントローラー
    /// 4方向入力で移動する（重力なし）
    /// </summary>
    public class TopViewPlayer : MonoBehaviour
    {
        [SerializeField] private float _speed = 5f; // 移動速度

        [Header("サウンド")]
        [SerializeField] private string _walkSEPath = "SE_Chung/SE3";       // 歩行SE
        [SerializeField] private float _walkStepInterval = 0.32f;     // 歩行SE再生間隔(秒)
        [SerializeField] private float _walkMinAbsInput = 0.1f;       // この値未満の入力では歩行SE鳴らさない

        private Rigidbody2D _rigidbody2D; // Rigidbody2Dキャッシュ
        private Vector2 _input; // 入力キャッシュ
        private float _walkStepTimer; // 歩行SEのインターバル計測用

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _rigidbody2D.gravityScale = 0f; // 見下ろし視点なので重力なし
            _rigidbody2D.linearVelocity = Vector2.zero; // 速度リセット
            _input = Vector2.zero; // 入力リセット
        }

        private void Update()
        {
            Vector2 raw = ReadDirectionalInput();

            if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
                _input = new Vector2(Mathf.Sign(raw.x), 0);
            else if (raw.y != 0)
                _input = new Vector2(0, Mathf.Sign(raw.y));
            else
                _input = Vector2.zero;
        }

        /// <summary>
        /// ゲームパッドとキーボードの両方から方向入力を読み取り、活性側の値を返す
        /// パッド優先、入力ゼロならキーボード（矢印キー）にフォールバック
        /// </summary>
        private Vector2 ReadDirectionalInput()
        {
            // ゲームパッド：左スティックor D-pad（大きい方）
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                Vector2 dpad = Gamepad.current.dpad.ReadValue();
                Vector2 pad = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
                if (pad != Vector2.zero) return pad;
            }

            // キーボード：矢印キー
            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.leftArrowKey.isPressed) k.x -= 1f;
                if (kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.upArrowKey.isPressed) k.y += 1f;
                if (kb.downArrowKey.isPressed) k.y -= 1f;
                return k;
            }

            return Vector2.zero;
        }

        private void FixedUpdate()
        {
            _rigidbody2D.MovePosition(_rigidbody2D.position + _input * _speed * Time.fixedDeltaTime);
            UpdateWalkSE();
        }

        /// <summary>
        /// 入力中(8方向どこかが_walkMinAbsInput以上)に_walkStepInterval間隔で歩行SEを再生する。
        /// 停止状態ならタイマーをリセットし、再開直後にすぐ1回目が鳴るようInterval満了状態へ
        /// </summary>
        private void UpdateWalkSE()
        {
            bool walking = Mathf.Abs(_input.x) >= _walkMinAbsInput || Mathf.Abs(_input.y) >= _walkMinAbsInput;
            if (!walking)
            {
                _walkStepTimer = _walkStepInterval;
                return;
            }

            _walkStepTimer += Time.fixedDeltaTime;
            if (_walkStepTimer >= _walkStepInterval)
            {
                _walkStepTimer = 0f;
                SafeSE.Play(_walkSEPath);
            }
        }
    }
}