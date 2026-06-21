using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// テレポートマーカー用の点滅・拡縮ループ演出
    /// SpriteRendererのアルファとTransformスケールをDOTweenでYoYoループさせる
    /// 見た目だけの装飾なのでロジック側（TeleportPlatform）には干渉しない
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TeleportMarkerGlow : MonoBehaviour
    {
        [Header("点滅（アルファ）")]
        [SerializeField] private bool _enableAlphaPulse = true;
        [Tooltip("最低アルファ値（0~1）")]
        [Range(0f, 1f)] [SerializeField] private float _minAlpha = 0.4f;
        [Tooltip("最大アルファ値（0~1）")]
        [Range(0f, 1f)] [SerializeField] private float _maxAlpha = 1f;

        [Header("拡縮")]
        [SerializeField] private bool _enableScalePulse = true;
        [Tooltip("基準スケールに対する縮小倍率")]
        [SerializeField] private float _minScaleMul = 0.9f;
        [Tooltip("基準スケールに対する拡大倍率")]
        [SerializeField] private float _maxScaleMul = 1.15f;

        [Header("共通")]
        [Tooltip("1往復にかける時間（秒）。短いほど速く点滅する")]
        [SerializeField] private float _duration = 0.8f;
        [SerializeField] private Ease _ease = Ease.InOutSine;

        private SpriteRenderer _sr;
        private Vector3 _baseScale;
        private Color _baseColor;
        private Tween _alphaTween;
        private Tween _scaleTween;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _baseColor = _sr.color;
        }

        private void OnEnable()
        {
            // アルファ：最大↔最小をYoYoループ
            if (_enableAlphaPulse)
            {
                _sr.color = WithAlpha(_baseColor, _maxAlpha);
                _alphaTween = DOTween.To(
                        () => _sr.color.a,
                        a => _sr.color = WithAlpha(_baseColor, a),
                        _minAlpha,
                        _duration)
                    .SetEase(_ease)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            }

            // スケール：基準スケール × 倍率レンジでYoYoループ
            if (_enableScalePulse)
            {
                transform.localScale = _baseScale * _maxScaleMul;
                _scaleTween = transform.DOScale(_baseScale * _minScaleMul, _duration)
                    .SetEase(_ease)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            }
        }

        private void OnDisable()
        {
            // SetLinkで自動破棄もされるが、明示的にKillして即座に演出を止める
            _alphaTween?.Kill();
            _scaleTween?.Kill();
            _alphaTween = null;
            _scaleTween = null;

            // 元の見た目に戻す（再度ONになった時に違和感が出ないように）
            if (_sr != null) _sr.color = _baseColor;
            transform.localScale = _baseScale;
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
