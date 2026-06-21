using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ゴール到達時に元スプライトを隠し、複数の小片を放射状に飛ばして消える「散る」演出
    /// MinigameGoalの_onReached UnityEventからPlayScatter()を呼ぶ
    /// 演出の所要時間は_duration。MinigameGoalのTransition Delayとほぼ揃えると遷移がスムーズ
    /// </summary>
    public class GoalScatterEffect : MonoBehaviour
    {
        [Header("対象")]
        [Tooltip("散る対象のスプライト。再生時にこのSpriteRendererを非表示にする")]
        [SerializeField] private SpriteRenderer _sourceRenderer;

        [Header("破片設定")]
        [SerializeField] private int _pieceCount = 12;
        [Tooltip("各破片が飛ぶ最大距離（ワールドユニット）")]
        [SerializeField] private float _scatterRadius = 2.5f;
        [Tooltip("各破片の初期スケール（元スプライトに対する倍率）")]
        [SerializeField] private float _pieceScale = 0.4f;
        [Tooltip("破片を均等放射 vs ランダム角度。ON ならピザ状に均等")]
        [SerializeField] private bool _evenDistribution = true;
        [Tooltip("各破片の角度に加わるランダム揺らぎ（度）")]
        [SerializeField] private float _angleJitter = 25f;

        [Header("タイミング")]
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private Ease _moveEase = Ease.OutCubic;
        [SerializeField] private Ease _fadeEase = Ease.InQuad;

        [Header("回転")]
        [SerializeField] private float _maxRotation = 360f;

        /// <summary>
        /// 散る演出を再生する。MinigameGoalのOnReached UnityEventから呼ぶ
        /// </summary>
        public void PlayScatter()
        {
            if (_sourceRenderer == null || _sourceRenderer.sprite == null)
            {
                Debug.LogWarning("[GoalScatterEffect] _sourceRenderer または sprite が未設定");
                return;
            }

            // 元スプライトを非表示
            _sourceRenderer.enabled = false;

            // ソースの基準値
            Vector3 origin = _sourceRenderer.transform.position;
            int sortingLayerID = _sourceRenderer.sortingLayerID;
            int sortingOrder = _sourceRenderer.sortingOrder;
            Material sharedMat = _sourceRenderer.sharedMaterial;
            Color baseColor = _sourceRenderer.color;
            Sprite baseSprite = _sourceRenderer.sprite;
            int maskInteraction = (int)_sourceRenderer.maskInteraction;

            // 破片を生成
            for (int i = 0; i < _pieceCount; i++)
            {
                // 破片用GameObject
                var piece = new GameObject($"ScatterPiece_{i}");
                piece.transform.SetParent(transform.parent, false);
                piece.transform.position = origin;
                piece.transform.localScale = _sourceRenderer.transform.localScale * _pieceScale;

                var sr = piece.AddComponent<SpriteRenderer>();
                sr.sprite = baseSprite;
                sr.color = baseColor;
                sr.sortingLayerID = sortingLayerID;
                sr.sortingOrder = sortingOrder;
                sr.sharedMaterial = sharedMat;
                sr.maskInteraction = (SpriteMaskInteraction)maskInteraction;

                // 飛ぶ方向：均等放射+ジッター
                float baseAngle = _evenDistribution
                    ? (360f / _pieceCount) * i
                    : Random.Range(0f, 360f);
                float angle = baseAngle + Random.Range(-_angleJitter, _angleJitter);
                Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;
                float dist = _scatterRadius * Random.Range(0.7f, 1.3f);

                // 移動+回転+フェード+スケールダウンを並列実行
                piece.transform.DOMove(origin + dir * dist, _duration).SetEase(_moveEase);
                piece.transform.DORotate(new Vector3(0, 0, Random.Range(-_maxRotation, _maxRotation)), _duration);
                sr.DOFade(0f, _duration).SetEase(_fadeEase);
                piece.transform.DOScale(Vector3.zero, _duration).SetEase(_fadeEase);

                // 演出完了後に破棄
                Destroy(piece, _duration + 0.05f);
            }
        }
    }
}
