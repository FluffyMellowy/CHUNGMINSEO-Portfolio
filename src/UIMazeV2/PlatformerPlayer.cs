using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UIMazeV2
{
    /// <summary>
    /// 横スクロールミニゲーム用のプレイヤーコントローラー
    /// 左右移動・ジャンプ・リスポーンを制御する
    /// </summary>
    public class PlatformerPlayer : MonoBehaviour
    {
        [Header("移動設定")]
        [SerializeField] protected float _moveSpeed = 5f; // 左右移動速度
        [SerializeField] protected float _jumpForce = 10f; // ジャンプ力
        [SerializeField] protected float _defaultGravity = 3f; // デフォルト重力スケール

        [Header("接地判定")]
        [SerializeField] protected BoxCollider2D _groundTrigger; // 足元の接地判定トリガー
        [SerializeField] protected ContactFilter2D _groundFilter; // 地面レイヤーフィルター

        [Header("サウンド")]
        [SerializeField] protected string _jumpSEPath = "SE_Chung/SE6"; // ジャンプSE
        [SerializeField] protected string _walkSEPath = "SE_Chung/SE3"; // 歩行SE(地上で水平移動中、_walkStepInterval毎に再生)
        [SerializeField] protected float _walkStepInterval = 0.32f;     // 歩行SE再生の間隔(秒)
        [Tooltip("これ以下の入力速度では歩行SE再生しない。スティック微入力対策")]
        [SerializeField] protected float _walkMinAbsInput = 0.1f;

        private Rigidbody2D _rb;
        private float _moveInput; // 左右入力値
        private bool _isGrounded; // 接地フラグ
        private bool _jumpRequested; // ジャンプリクエストフラグ
        private bool _isDead; // 死亡フラグ
        private float _walkStepTimer; // 歩行SEのインターバル計測用

        /// <summary>
        /// 接地中かどうか（外部参照用）。WindowShrink/CreditScrollerが着地ゲート判定に使う
        /// </summary>
        public bool IsGrounded => _isGrounded;

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        protected virtual void OnEnable()
        {
            _rb.simulated = true; // 物理シミュレーション有効化
            _rb.gravityScale = _defaultGravity; // 重力を有効化
            _rb.linearVelocity = Vector2.zero; // 速度リセット
            _isDead = false;
        }

        protected virtual void OnDisable() { }

        protected virtual void Update()
        {
            if (_isDead) return;

            ReadInput();
            CheckGround();
            Move();
            Jump();
        }

        /// <summary>
        /// 入力を読み取る。ゲームパッド（左スティック＋D-pad）とキーボード（矢印・Z/Alt/Space）の両対応
        /// 移動はパッド優先、ゼロなら矢印キーへフォールバック。ジャンプはA/B/Z/Alt/Spaceのいずれかでも発火
        /// </summary>
        private void ReadInput()
        {
            float moveX = 0f;
            bool jumpPressed = false;

            // ゲームパッド
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float stickX = gamepad.leftStick.x.ReadValue();
                float dpadX = gamepad.dpad.x.ReadValue();
                moveX = Mathf.Abs(stickX) > Mathf.Abs(dpadX) ? stickX : dpadX;

                // A（buttonSouth）またはB（buttonEast）でジャンプ
                if (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame)
                    jumpPressed = true;
            }

            // キーボード：パッドが無入力なら矢印キーで移動、ジャンプはZ/LeftAlt/Spaceのいずれか
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (Mathf.Approximately(moveX, 0f))
                {
                    if (kb.leftArrowKey.isPressed) moveX = -1f;
                    else if (kb.rightArrowKey.isPressed) moveX = 1f;
                }

                if (kb.zKey.wasPressedThisFrame
                    || kb.leftAltKey.wasPressedThisFrame
                    || kb.spaceKey.wasPressedThisFrame)
                {
                    jumpPressed = true;
                }
            }

            _moveInput = moveX;

            if (jumpPressed && _isGrounded)
                _jumpRequested = true;
        }

        /// <summary>
        /// 足元トリガーで接地判定を行う
        /// </summary>
        private void CheckGround()
        {
            _isGrounded = _groundTrigger.IsTouching(_groundFilter);
        }

        /// <summary>
        /// 左右移動を適用する
        /// </summary>
        private void Move()
        {
            _rb.linearVelocity = new Vector2(_moveInput * _moveSpeed, _rb.linearVelocity.y);
            UpdateWalkSE();
        }

        /// <summary>
        /// 接地中で水平入力がある間、_walkStepInterval間隔で歩行SEを再生する。
        /// 空中 or 静止状態ではタイマーをリセットして無音
        /// </summary>
        private void UpdateWalkSE()
        {
            bool walking = _isGrounded && Mathf.Abs(_moveInput) >= _walkMinAbsInput;
            if (!walking)
            {
                // 停止/空中ならタイマーリセット。次に踏み出した直後すぐ1回目が鳴るようInterval満了状態にしておく
                _walkStepTimer = _walkStepInterval;
                return;
            }

            _walkStepTimer += Time.deltaTime;
            if (_walkStepTimer >= _walkStepInterval)
            {
                _walkStepTimer = 0f;
                SafeSE.Play(_walkSEPath);
            }
        }

        /// <summary>
        /// ジャンプを適用する
        /// </summary>
        private void Jump()
        {
            if (!_jumpRequested) return;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            _jumpRequested = false;

            // SafeSEに委譲。未登録パスは初回ロード試行後にキャッシュされ、以降スキップされる
            SafeSE.Play(_jumpSEPath);
        }

        /// <summary>
        /// 死亡状態にする（外部マネージャーから呼ばれる）
        /// </summary>
        public void Kill()
        {
            _isDead = true;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        /// <summary>
        /// 復活させる（外部マネージャーから呼ばれる）
        /// </summary>
        public void Revive()
        {
            _rb.simulated = true;
            _rb.gravityScale = _defaultGravity;
            _rb.linearVelocity = Vector2.zero;
            _isDead = false;
        }
    }
}