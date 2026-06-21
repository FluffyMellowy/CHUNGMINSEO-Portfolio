using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// 各ミニゲーム開始時にプレイヤー頭上に表示する操作ガイド矢印。
    /// プレイヤーが一定距離動いたらフェードアウトして消える。
    /// プレイヤーの子GameObjectに置いてSpriteRendererを持たせる前提。
    /// WindowManagerのShowWindow時にSetActive(true)で都度表示するだけで動く
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MoveArrowIndicator : MonoBehaviour
    {
        [Header("追跡対象")]
        [Tooltip("移動を検知するプレイヤーのTransform。空なら親をプレイヤーとみなす")]
        [SerializeField] private Transform _player;

        [Header("Bob(上下揺れ)")]
        [Tooltip("上下に揺れる振幅(unit)")]
        [SerializeField] private float _bobAmplitude = 0.15f;
        [Tooltip("片道にかかる時間(秒)。短いほど早く揺れる")]
        [SerializeField] private float _bobDuration = 0.5f;
        [Tooltip("揺れEase")]
        [SerializeField] private Ease _bobEase = Ease.InOutSine;

        [Header("消滅")]
        [Tooltip("この距離以上動いたら『移動開始』と判定する。小さすぎると微振動で誤検知")]
        [SerializeField] private float _moveThreshold = 0.3f;
        [Tooltip("移動開始からフェードアウト開始までの待機時間(秒)")]
        [SerializeField] private float _hideDelaySec = 1.0f;
        [Tooltip("フェードアウトにかかる時間(秒)")]
        [SerializeField] private float _fadeOutSec = 0.3f;

        private SpriteRenderer _sr;
        private Vector3 _baseLocalPos;        // OnEnable時のlocalPosition (bob基準)
        private Vector3 _playerStartWorldPos; // OnEnable時のプレイヤー位置
        private bool _hasStartedMoving;
        private float _hideTimer;
        private bool _fadeStarted;            // フェードアウトを既に開始したか (Updateの多重呼び出し防止)
        private Tween _bobTween;
        private Tween _fadeTween;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            // _player未設定なら親をプレイヤーとみなす
            if (_player == null && transform.parent != null) _player = transform.parent;
            _baseLocalPos = transform.localPosition;
            Debug.Log($"[MoveArrow.Awake] go={name}, parent={(transform.parent ? transform.parent.name : "null")}, _player={(_player ? _player.name : "null")}, baseLocal={_baseLocalPos}", this);
        }

        private void OnEnable()
        {
            _hasStartedMoving = false;
            _hideTimer = 0f;
            _fadeStarted = false;

            // 表示状態を確実にリセット (前回フェードアウトでalpha=0のままの可能性に備える)
            if (_sr != null)
            {
                var c = _sr.color; c.a = 1f; _sr.color = c;
            }
            transform.localPosition = _baseLocalPos;

            if (_player != null) _playerStartWorldPos = _player.position;

            StartBob();
            Debug.Log($"[MoveArrow.OnEnable] localPos={transform.localPosition}, playerStart={_playerStartWorldPos}, bobAmp={_bobAmplitude}, bobDur={_bobDuration}, tweenActive={_bobTween?.IsActive()}", this);
        }

        private void OnDisable()
        {
            _bobTween?.Kill();
            _fadeTween?.Kill();
        }

        private float _diagTimer;
        private void Update()
        {
            // 診断: 0.5秒に1回、Updateが回っているか / tweenが生きているか / localY / プレイヤー距離 を出力
            _diagTimer += Time.deltaTime;
            if (_diagTimer >= 0.5f)
            {
                _diagTimer = 0f;
                float dist = _player != null ? Vector2.Distance(_player.position, _playerStartWorldPos) : -1f;
                Debug.Log($"[MoveArrow.Update] localY={transform.localPosition.y:F3} tweenActive={_bobTween?.IsActive()} tweenPlaying={_bobTween?.IsPlaying()} dist={dist:F3} hasMoved={_hasStartedMoving} hideTimer={_hideTimer:F2}", this);
            }

            if (_player == null) return;

            if (!_hasStartedMoving)
            {
                if (Vector2.Distance(_player.position, _playerStartWorldPos) > _moveThreshold)
                    _hasStartedMoving = true;
                return;
            }

            // 既にフェード開始済みなら何もせず待つ (StartFadeOutの多重呼び出し防止)。
            // enabled=falseで止めるとOnDisableが呼ばれ、_fadeTweenがKillされてOnCompleteが
            // 走らずSetActive(false)に到達しない不具合があったため、フラグで制御する
            if (_fadeStarted) return;

            _hideTimer += Time.deltaTime;
            if (_hideTimer >= _hideDelaySec)
            {
                _fadeStarted = true;
                StartFadeOut();
            }
        }

        /// <summary>
        /// 上下にゆったり揺れるDOTweenループを開始。GameObject破棄で自動停止するためSetLinkを付ける
        /// </summary>
        private void StartBob()
        {
            _bobTween?.Kill();
            float targetY = _baseLocalPos.y + _bobAmplitude;
            _bobTween = transform
                .DOLocalMoveY(targetY, _bobDuration)
                .SetEase(_bobEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        /// <summary>
        /// フェードアウト→SetActive(false)。次のミニゲームで再有効化すれば再びalpha=1から始まる
        /// </summary>
        private void StartFadeOut()
        {
            _fadeTween?.Kill();
            if (_sr == null)
            {
                gameObject.SetActive(false);
                return;
            }
            _fadeTween = _sr
                .DOFade(0f, _fadeOutSec)
                .OnComplete(() =>
                {
                    if (this != null) gameObject.SetActive(false);
                });
        }
    }
}
