using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤースプライトの上→下方向ディゾルブ演出
    /// PlayDissolveOut(): 1→0で上から消える
    /// PlayDissolveIn():  0→1で上から現れる
    /// MaterialPropertyBlock経由で_DissolveAmountを制御するためMaterialインスタンス化なし
    /// </summary>
    public class PlayerDissolveEffect : MonoBehaviour
    {
        [Header("対象")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("タイミング")]
        [SerializeField] private float _outDuration = 0.6f;
        [SerializeField] private float _inDuration = 0.6f;
        [SerializeField] private Ease _outEase = Ease.InQuad;
        [SerializeField] private Ease _inEase = Ease.OutQuad;

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        private MaterialPropertyBlock _mpb;
        private Tween _tween;
        private float _current = 1f;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            ApplyDissolve(1f); // 初期は完全表示
        }

        /// <summary>
        /// プレイヤースプライトを上から下へディゾルブして消す
        /// </summary>
        public void PlayDissolveOut()
        {
            _tween?.Kill();
            _current = 1f;
            ApplyDissolve(_current);
            _tween = DOTween
                .To(() => _current, ApplyDissolve, 0f, _outDuration)
                .SetEase(_outEase);
        }

        /// <summary>
        /// 上から下へディゾルブで現れる
        /// </summary>
        public void PlayDissolveIn()
        {
            _tween?.Kill();
            _current = 0f;
            ApplyDissolve(_current);
            _tween = DOTween
                .To(() => _current, ApplyDissolve, 1f, _inDuration)
                .SetEase(_inEase);
        }

        /// <summary>即座に完全表示（リセット用）</summary>
        public void ResetVisible()
        {
            _tween?.Kill();
            _current = 1f;
            ApplyDissolve(1f);
        }

        private void ApplyDissolve(float v)
        {
            _current = v;
            if (_spriteRenderer == null) return;
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(DissolveAmountId, v);
            _spriteRenderer.SetPropertyBlock(_mpb);
        }

        public float OutDuration => _outDuration;
        public float InDuration => _inDuration;

        private void OnDisable()
        {
            _tween?.Kill();
        }
    }
}
