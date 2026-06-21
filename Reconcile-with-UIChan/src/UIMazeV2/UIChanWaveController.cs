using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// UIちゃん立ち絵に適用するウェーブシェーダー(UIChan/Wave)のパラメータを制御する
    /// 起動時はベース値を維持。Pulse() / SetIntensity()で外部から強弱を切り替えられる
    /// MaterialPropertyBlockを使うのでMaterialインスタンス化なしで複数キャラに同じMaterialを共用可
    /// </summary>
    public class UIChanWaveController : MonoBehaviour
    {
        [Header("対象")]
        [SerializeField] private SpriteRenderer _spriteRenderer; // 通常スプライト用
        [SerializeField] private UnityEngine.UI.Image _uiImage; // UI Image用（どちらかでOK）

        [Header("ベース値（通常時の揺らぎ）")]
        [SerializeField] private float _baseAmplitude = 0.02f;
        [SerializeField] private float _baseFrequency = 12f;
        [SerializeField] private float _baseSpeed = 2f;

        [Header("Pulse（一時的に強める時の値）")]
        [SerializeField] private float _pulseAmplitude = 0.06f;
        [SerializeField] private float _pulseDuration = 0.4f;

        private static readonly int AmplitudeId = Shader.PropertyToID("_Amplitude");
        private static readonly int FrequencyId = Shader.PropertyToID("_Frequency");
        private static readonly int SpeedId = Shader.PropertyToID("_Speed");

        private MaterialPropertyBlock _mpb;
        private Tween _pulseTween;
        private float _currentAmplitude;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _currentAmplitude = _baseAmplitude;
        }

        private void OnEnable()
        {
            ApplyAll();
        }

        private void OnDisable()
        {
            _pulseTween?.Kill();
        }

        /// <summary>
        /// 現在のベース値をシェーダーに反映する
        /// </summary>
        private void ApplyAll()
        {
            ApplyFloat(AmplitudeId, _currentAmplitude);
            ApplyFloat(FrequencyId, _baseFrequency);
            ApplyFloat(SpeedId, _baseSpeed);
        }

        /// <summary>
        /// 指定IDのfloatプロパティをMPB経由で書き込む
        /// SpriteRenderer / Imageどちらにも対応
        /// </summary>
        private void ApplyFloat(int id, float value)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(id, value);
                _spriteRenderer.SetPropertyBlock(_mpb);
            }
            if (_uiImage != null && _uiImage.material != null)
            {
                // UI ImageはMPBが効かないので直接Materialに書く（インスタンス化される点に注意）
                _uiImage.material.SetFloat(id, value);
            }
        }

        /// <summary>
        /// 振幅を一時的に強めて元に戻す。ダメージ演出やクリック反応等に使う
        /// </summary>
        public void Pulse()
        {
            _pulseTween?.Kill();
            _pulseTween = DOTween.Sequence()
                .Append(DOTween.To(() => _currentAmplitude, SetAmplitude, _pulseAmplitude, _pulseDuration * 0.3f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => _currentAmplitude, SetAmplitude, _baseAmplitude, _pulseDuration * 0.7f).SetEase(Ease.InQuad));
        }

        /// <summary>
        /// 任意の振幅に滑らかに遷移する。ステージ進行に応じた強度変更等に使う
        /// </summary>
        public void SetIntensity(float amplitude, float duration = 0.5f)
        {
            _pulseTween?.Kill();
            _pulseTween = DOTween.To(() => _currentAmplitude, SetAmplitude, amplitude, duration)
                .SetEase(Ease.InOutSine);
        }

        private void SetAmplitude(float v)
        {
            _currentAmplitude = v;
            ApplyFloat(AmplitudeId, v);
        }
    }
}
