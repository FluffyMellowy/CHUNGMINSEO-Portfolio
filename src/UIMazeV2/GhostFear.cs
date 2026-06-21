using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// 追走するゴーストに恐怖演出を追加するコンポーネント
    /// 1)残像トレイル：一定間隔で半透明の分身スプライトを置き、自然に消滅させる（白⇄黒交互）
    /// 2)カラーフリッカー：本体の色を白⇄黒で往復させる（点滅）
    /// GhostCloneの位置設定ロジックには干渉しないよう、SpriteRendererの色と別GameObject生成のみ扱う
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GhostFear : MonoBehaviour
    {
        [Header("残像トレイル")]
        [SerializeField] private bool _afterimageEnabled = true;
        [Tooltip("残像を生成する間隔（秒）")]
        [SerializeField] private float _afterimageInterval = 0.12f;
        [Tooltip("残像が消えるまでの寿命（秒）")]
        [SerializeField] private float _afterimageLifetime = 0.5f;
        [Tooltip("残像の初期アルファ。本体より薄くする")]
        [Range(0f, 1f)] [SerializeField] private float _afterimageStartAlpha = 0.7f;
        [Tooltip("奇数番目の残像ティント（既定：白）")]
        [SerializeField] private Color _afterimageTintA = Color.white;
        [Tooltip("偶数番目の残像ティント（既定：黒）")]
        [SerializeField] private Color _afterimageTintB = Color.black;

        [Header("カラーフリッカー（本体）")]
        [SerializeField] private bool _flickerEnabled = true;
        [Tooltip("カラー往復の片端（既定：白）")]
        [SerializeField] private Color _flickerColorA = Color.white;
        [Tooltip("カラー往復のもう片端（既定：黒）")]
        [SerializeField] private Color _flickerColorB = Color.black;
        [Tooltip("往復の速度。値が大きいほど速く点滅する")]
        [SerializeField] private float _flickerSpeed = 6f;
        [Tooltip("trueでサイン波（滑らかなクロスフェード）、falseで矩形波（硬い瞬間切替）")]
        [SerializeField] private bool _smoothFlicker = false;

        private static readonly int SolidColorId = Shader.PropertyToID("_SolidColor");

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;
        private float _afterimageTimer;
        private float _noiseSeed; // インスタンスごとの位相オフセット
        private int _afterimageToggle; // 残像A/B交互スポーン用

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            _noiseSeed = Random.Range(0f, 1000f); // インスタンスごとに違う位相
        }

        private void Update()
        {
            if (_flickerEnabled) ApplyFlicker();
            if (_afterimageEnabled) UpdateAfterimage();
        }

        /// <summary>
        /// 本体マテリアルの_SolidColorを_flickerColorA ⇄ _flickerColorBで往復させる
        /// SolidSilhouetteシェーダがRGBを完全に置き換えるため、Unityのcolor乗算と違って真っ白/真っ黒のシルエットが出る
        /// _smoothFlickerでサイン波（滑らか）と矩形波（瞬間切替）を切替
        /// </summary>
        private void ApplyFlicker()
        {
            float t;
            if (_smoothFlicker)
            {
                t = (Mathf.Sin((Time.time + _noiseSeed) * _flickerSpeed) + 1f) * 0.5f;
            }
            else
            {
                float phase = ((Time.time + _noiseSeed) * _flickerSpeed) % (Mathf.PI * 2f);
                t = phase < Mathf.PI ? 0f : 1f;
            }

            // MaterialPropertyBlockで該当SpriteRendererだけに値を流し込み、マテリアル本体は変更しない
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(SolidColorId, Color.Lerp(_flickerColorA, _flickerColorB, t));
            _sr.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// 一定間隔で残像スプライトをスポーンする
        /// </summary>
        private void UpdateAfterimage()
        {
            _afterimageTimer += Time.deltaTime;
            if (_afterimageTimer < _afterimageInterval) return;
            _afterimageTimer = 0f;
            SpawnAfterimage();
        }

        /// <summary>
        /// 現在の見た目を継承した残像GameObjectを生成し、自然消滅Tweenを仕込む
        /// 本体と同じSolidSilhouetteマテリアルを共有することで、残像も白/黒のシルエットになる
        /// </summary>
        private void SpawnAfterimage()
        {
            var go = new GameObject("GhostAfterimage");
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.lossyScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sr.sprite;
            sr.flipX = _sr.flipX;
            sr.flipY = _sr.flipY;
            sr.sortingLayerID = _sr.sortingLayerID;
            sr.sortingOrder = _sr.sortingOrder - 1; // 本体より一段奥
            sr.maskInteraction = _sr.maskInteraction; // ウィンドウマスクの中だけで見えるように継承
            sr.sharedMaterial = _sr.sharedMaterial; // SolidSilhouetteマテリアルを共有

            // A/B交互に色を選ぶ。MaterialPropertyBlockで残像個別に_SolidColorを設定
            var startColor = (_afterimageToggle % 2 == 0) ? _afterimageTintA : _afterimageTintB;
            _afterimageToggle++;

            var mpb = new MaterialPropertyBlock();
            sr.GetPropertyBlock(mpb);
            mpb.SetColor(SolidColorId, startColor);
            sr.SetPropertyBlock(mpb);

            // SpriteRendererのcolor.aを寿命の間に0へフェード（SolidSilhouetteシェーダがIN.color.aを乗算するので有効）
            sr.color = new Color(1f, 1f, 1f, _afterimageStartAlpha);
            sr.DOFade(0f, _afterimageLifetime)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => Destroy(go));
        }
    }
}
