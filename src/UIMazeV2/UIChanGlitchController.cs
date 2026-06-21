using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// UIちゃん立ち絵にグリッチ演出を適用するコントローラーの基底クラス
    ///   ① OnEnable時(またはゲート開放時)に登場トゥイーン（Y下から+スケールアップ）を再生
    ///   ② グリッチシェーダー(UIChan/Glitch)の_Intensityを一定間隔でパルス（バースト）
    ///   ③ 外部からTriggerOnce()で任意タイミングでも発動可能（ダメージ演出等）
    /// SpriteRenderer / UI Image両対応。MaterialPropertyBlock経由でMaterial共用OK
    ///
    /// ミニゲーム毎に異なる発動トリガーを使いたいため abstract。各ミニゲーム用のSealed subclass
    /// (UIChanGlitchM1 / UIChanGlitchM2 / UIChanGlitchM3) が IsGateOpen() を実装する。
    /// </summary>
    public abstract class UIChanGlitchController : MonoBehaviour
    {
        [Header("対象（どちらか片方でOK）")]
        [SerializeField] private SpriteRenderer _spriteRenderer; // 通常スプライトの場合
        [SerializeField] private UnityEngine.UI.Image _uiImage; // UI Imageの場合

        [Header("登場演出")]
        [SerializeField] private bool _playEntranceOnEnable = true; // OnEnableで自動再生
        [SerializeField] private Vector3 _entranceStartScale = new Vector3(0.4f, 0.4f, 1f);
        [SerializeField] private Vector3 _entranceEndScale = Vector3.one;
        [SerializeField] private float _entranceYOffset = -300f; // 下から登場するオフセット量(px or unit)
        [SerializeField] private float _entranceDuration = 1.0f;
        [SerializeField] private Ease _entranceEase = Ease.OutCubic;
        [Tooltip("登場中に強グリッチを重ねる（観察した動画準拠）")]
        [SerializeField] private bool _glitchDuringEntrance = true;

        [Header("グリッチパルス（ループ）")]
        [SerializeField] private bool _enableGlitchLoop = true;
        [SerializeField] private float _pulseInterval = 2.5f; // パルス間隔
        [SerializeField] private float _pulseDuration = 0.3f; // パルス1回の長さ
        [Range(0f, 1f)]
        [SerializeField] private float _pulseIntensity = 1.0f; // パルス時の最大強度
        [SerializeField] private float _intervalRandomness = 0.5f; // 間隔のランダムブレ ±

        [Header("常時モード")]
        [Tooltip("trueなら起動時に_constantIntensityへ即セットしてパルスループは無効。鎮め時間なしで常に演出")]
        [SerializeField] private bool _constantMode = false;
        [Range(0f, 1f)]
        [SerializeField] private float _constantIntensity = 1.0f;

        [Header("トリガー演出（外部呼び出し用）")]
        [Range(0f, 1f)]
        [SerializeField] private float _triggerOnceIntensity = 1.0f;
        [SerializeField] private float _triggerOnceDuration = 0.4f;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private MaterialPropertyBlock _mpb;
        private Tween _entranceScaleTween;
        private Tween _entranceMoveTween;
        private Sequence _pulseSequence;
        private Tween _entranceGlitchTween;
        private Vector3 _basePosition;
        private float _currentIntensity;
        private bool _started;        // 発動済みフラグ
        private bool _waitingForGate; // ゲート開放待ち中

        /// <summary>
        /// 発動条件。trueを返した瞬間に登場演出+グリッチループを開始する。
        /// 各ミニゲーム用のsealed subclassで実装する。
        /// ゲートを使わない（即発動したい）subclassは常にtrueを返せばよい。
        /// </summary>
        protected abstract bool IsGateOpen();

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _basePosition = transform.localPosition;
        }

        private void OnEnable()
        {
            ApplyIntensity(0f);
            _started = false;
            _waitingForGate = true;

            // 常時モード：エントランス・ループを無視して指定強度を維持
            if (_constantMode)
            {
                ApplyIntensity(_constantIntensity);
                _waitingForGate = false;
                _started = true;
                return;
            }

            // 初フレームでゲートが既に開いていれば即発動
            TryStart();
        }

        private void Update()
        {
            // ゲートが閉じている間だけpollingしてフレーム毎に開放を待つ
            if (_waitingForGate) TryStart();
        }

        /// <summary>
        /// ゲートが開いていれば登場演出とグリッチループを起動する。多重発動防止のため_startedガード付き
        /// </summary>
        private void TryStart()
        {
            if (_started) return;
            if (!IsGateOpen()) return;

            _started = true;
            _waitingForGate = false;

            if (_playEntranceOnEnable)
                PlayEntrance();

            if (_enableGlitchLoop)
                ScheduleNextPulse();
        }

        private void OnDisable()
        {
            KillAllTweens();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            _entranceScaleTween?.Kill();
            _entranceMoveTween?.Kill();
            _entranceGlitchTween?.Kill();
            _pulseSequence?.Kill();
        }

        /// <summary>
        /// 登場トゥイーン再生（Y下から上へ+スケールアップ+オプションでグリッチ重ね）
        /// </summary>
        public void PlayEntrance()
        {
            _entranceScaleTween?.Kill();
            _entranceMoveTween?.Kill();
            _entranceGlitchTween?.Kill();

            transform.localScale = _entranceStartScale;
            transform.localPosition = _basePosition + new Vector3(0f, _entranceYOffset, 0f);

            _entranceScaleTween = transform
                .DOScale(_entranceEndScale, _entranceDuration)
                .SetEase(_entranceEase);

            _entranceMoveTween = transform
                .DOLocalMove(_basePosition, _entranceDuration)
                .SetEase(_entranceEase);

            // 登場中はグリッチを高めに維持してから消す
            if (_glitchDuringEntrance)
            {
                _entranceGlitchTween = DOTween
                    .To(() => _currentIntensity, ApplyIntensity, _pulseIntensity, _entranceDuration * 0.6f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _entranceGlitchTween = DOTween
                            .To(() => _currentIntensity, ApplyIntensity, 0f, _entranceDuration * 0.4f)
                            .SetEase(Ease.InQuad);
                    });
            }
        }

        /// <summary>
        /// 次のパルスをランダム間隔で予約。終了後に自分自身を再呼び出しして連続させる
        /// </summary>
        private void ScheduleNextPulse()
        {
            _pulseSequence?.Kill();

            float jitter = Random.Range(-_intervalRandomness, _intervalRandomness);
            float wait = Mathf.Max(0.1f, _pulseInterval + jitter);

            _pulseSequence = DOTween.Sequence();
            _pulseSequence
                .AppendInterval(wait)
                .Append(DOTween
                    .To(() => _currentIntensity, ApplyIntensity, _pulseIntensity, _pulseDuration * 0.4f)
                    .SetEase(Ease.InQuad))
                .Append(DOTween
                    .To(() => _currentIntensity, ApplyIntensity, 0f, _pulseDuration * 0.6f)
                    .SetEase(Ease.OutQuad))
                .OnComplete(() =>
                {
                    if (_enableGlitchLoop && isActiveAndEnabled)
                        ScheduleNextPulse();
                });
        }

        /// <summary>
        /// 任意タイミングで強グリッチを1回発動する。ダメージ演出やイベント反応に使う
        /// </summary>
        public void TriggerOnce()
        {
            DOTween.Sequence()
                .Append(DOTween
                    .To(() => _currentIntensity, ApplyIntensity, _triggerOnceIntensity, _triggerOnceDuration * 0.3f)
                    .SetEase(Ease.InQuad))
                .Append(DOTween
                    .To(() => _currentIntensity, ApplyIntensity, 0f, _triggerOnceDuration * 0.7f)
                    .SetEase(Ease.OutQuad));
        }

        /// <summary>
        /// グリッチを永続的に強くする/弱くする（フェーズ進行で底上げしたい時等）
        /// </summary>
        public void SetBaseIntensity(float intensity, float duration = 0.5f)
        {
            DOTween
                .To(() => _currentIntensity, ApplyIntensity, Mathf.Clamp01(intensity), duration)
                .SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// シェーダーへ_Intensityを書き込む。SpriteRendererはMPB、UI Imageは直接Materialへ
        /// </summary>
        private void ApplyIntensity(float v)
        {
            _currentIntensity = v;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(IntensityId, v);
                _spriteRenderer.SetPropertyBlock(_mpb);
            }
            if (_uiImage != null && _uiImage.material != null)
            {
                // UI ImageはMPB非対応のため直接書き込み（インスタンス化される点に注意）
                _uiImage.material.SetFloat(IntensityId, v);
            }
        }
    }
}
