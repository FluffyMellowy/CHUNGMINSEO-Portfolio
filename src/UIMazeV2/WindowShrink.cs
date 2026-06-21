using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ウィンドウを左側からのみ縮小させる
    /// 右端は固定、左端が右へ移動する
    /// 死亡時にリセットして再開する
    /// </summary>
    public class WindowShrink : MonoBehaviour
    {
        [SerializeField] private Transform _windowFrame; // 縮小対象のウィンドウフレーム
        [SerializeField] private float _initialDelay = 0.5f; // 縮小開始までの待機（着地直後の猶予）
        [SerializeField] private float _shrinkDuration = 30f; // 最小サイズになるまでの時間（秒）
        [SerializeField] private float _minWidthRatio = 0.3f; // 最小幅の割合

        [Header("着地ゲート")]
        [Tooltip("このプレイヤーが地面に着地するまで縮小を開始しない。" +
                 "リスポーン後も再着地まで待機する")]
        [SerializeField] private PlatformerPlayer _player;

        private Vector3 _initialPos; // 初期位置
        private Vector3 _initialScale; // 初期スケール
        private float _initialWidth; // 初期幅
        private float _rightEdge; // 右端のX座標（固定）
        private float _timer;
        private bool _isShrinking;
        private bool _waitingForLanding; // 着地待ち中（trueの間は_isShrinkingがfalse）

        private void Awake()
        {
            _initialPos = _windowFrame.position;
            _initialScale = _windowFrame.localScale;
            _initialWidth = _windowFrame.GetComponent<SpriteRenderer>().bounds.size.x;
            _rightEdge = _windowFrame.position.x + _initialWidth * 0.5f;
        }

        private void OnEnable()
        {
            Minigame2Manager.OnPlayerRespawn += ResetShrink;
            ResetShrink();
        }

        private void OnDisable()
        {
            Minigame2Manager.OnPlayerRespawn -= ResetShrink;
        }

        private void Update()
        {
            // 着地待ち中:プレイヤーが地面に着いた瞬間に縮小開始
            if (_waitingForLanding)
            {
                if (_player != null && _player.IsGrounded)
                {
                    _waitingForLanding = false;
                    _isShrinking = true;
                    _timer = -_initialDelay;
                }
                return;
            }

            if (!_isShrinking) return;

            _timer += Time.deltaTime;
            // _timerが負の間は待機（_initialDelay中）
            if (_timer < 0f) return;

            float t = Mathf.Clamp01(_timer / _shrinkDuration);

            float currentWidth = Mathf.Lerp(_initialWidth, _initialWidth * _minWidthRatio, t);
            float newCenterX = _rightEdge - currentWidth * 0.5f;
            float scaleRatio = currentWidth / _initialWidth;

            _windowFrame.position = new Vector3(newCenterX, _initialPos.y, _initialPos.z);
            _windowFrame.localScale = new Vector3(_initialScale.x * scaleRatio, _initialScale.y, _initialScale.z);

            if (t >= 1f) _isShrinking = false;
        }

        private void ResetShrink()
        {
            // 着地ゲートを再武装。プレイヤーが再び地面に着くまで縮小は始まらない
            _isShrinking = false;
            _waitingForLanding = true;
            _timer = -_initialDelay;
            _windowFrame.position = _initialPos;
            _windowFrame.localScale = _initialScale;
        }
    }
}