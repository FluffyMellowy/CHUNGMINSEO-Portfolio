using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Title
{
    /// <summary>
    /// タイトルの高速再生中、画面全体にグリッチオーバーレイを表示する
    /// 専用シェーダー(UIChan/FullscreenGlitch)の_IntensityプロパティをDOTweenで0 ⇄ peakに動かす
    ///
    /// セットアップ:
    ///   1. Canvas（Screen Space - Overlay推奨）の最前面に
    /// フルスクリーンのUI Imageを作る（Anchor Stretch全方向）
    ///   2. ProjectにMaterialを1つ作り、Shaderを「UIChan/FullscreenGlitch」に
    ///   3. ImageのMaterialスロットに割り当て、初期は非アクティブ
    ///   4.このコンポーネントを別GameObjectに付け、_glitchImageと_glitchMaterialを割り当て
    ///   5. TitleDialogueLoopのUnityEventに繋ぐ:
    ///        _onSkipRequested      → BeginGlitch
    ///        _onFastPlaybackEnded  → EndGlitch
    /// </summary>
    public class TitleScreenGlitch : MonoBehaviour
    {
        [Header("対象")]
        [SerializeField] private Image _glitchImage;
        [Tooltip("UIChan/FullscreenGlitch を Shader に持つMaterial。_Intensity をTweenする")]
        [SerializeField] private Material _glitchMaterial;

        [Header("Intensity アニメーション")]
        [SerializeField] private float _fadeInDuration = 0.05f;
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [Range(0f, 1f)]
        [SerializeField] private float _peakIntensity = 1f;

        [Header("シェイク（任意）")]
        [Tooltip("Canvas内のコンテナRectTransformを入れるとUI全体を揺らせる")]
        [SerializeField] private Transform _shakeTarget;
        [SerializeField] private float _shakeStrength = 15f;
        [SerializeField] private int _shakeVibrato = 30;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        private Tween _intensityTween;
        private Tween _shakeTween;
        private Vector3 _shakeInitialPos;
        private float _currentIntensity;

        private void Awake()
        {
            if (_shakeTarget != null) _shakeInitialPos = _shakeTarget.localPosition;
            HideImmediately();
        }

        public void BeginGlitch()
        {
            _intensityTween?.Kill();
            _shakeTween?.Kill();

            if (_glitchImage != null) _glitchImage.gameObject.SetActive(true);

            _currentIntensity = 0f;
            ApplyIntensity(0f);

            _intensityTween = DOTween
                .To(() => _currentIntensity, ApplyIntensity, _peakIntensity, _fadeInDuration)
                .SetEase(Ease.OutQuad);

            if (_shakeTarget != null)
            {
                _shakeTween = _shakeTarget
                    .DOShakePosition(9999f, _shakeStrength, _shakeVibrato, 90f, false, false)
                    .SetEase(Ease.Linear);
            }
        }

        public void EndGlitch()
        {
            _shakeTween?.Kill();
            if (_shakeTarget != null) _shakeTarget.localPosition = _shakeInitialPos;

            _intensityTween?.Kill();
            _intensityTween = DOTween
                .To(() => _currentIntensity, ApplyIntensity, 0f, _fadeOutDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    if (_glitchImage != null) _glitchImage.gameObject.SetActive(false);
                });
        }

        private void HideImmediately()
        {
            _currentIntensity = 0f;
            ApplyIntensity(0f);
            if (_glitchImage != null) _glitchImage.gameObject.SetActive(false);
        }

        private void ApplyIntensity(float v)
        {
            _currentIntensity = v;
            if (_glitchMaterial != null)
                _glitchMaterial.SetFloat(IntensityId, v);
        }

        private void OnDisable()
        {
            _intensityTween?.Kill();
            _shakeTween?.Kill();
            if (_shakeTarget != null) _shakeTarget.localPosition = _shakeInitialPos;
            ApplyIntensity(0f);
        }
    }
}
