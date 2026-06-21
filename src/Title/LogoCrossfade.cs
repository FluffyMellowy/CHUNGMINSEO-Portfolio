using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Title
{
    /// <summary>
    /// 2枚のロゴUI Imageをアルファでクロスフェードさせる
    /// PlayCrossfade()を外部から呼ぶと_fromImageが消えながら_toImageが現れる
    /// TitleDialogueLoopの_onSkipRequested UnityEventにインスペクター上で接続する想定
    /// </summary>
    public class LogoCrossfade : MonoBehaviour
    {
        [Header("ロゴImage（同じ位置に重ねる）")]
        [Tooltip("初期表示のロゴ。クロスフェードで消える側")]
        [SerializeField] private Image _fromImage;
        [Tooltip("スキップ後に出現するロゴ。クロスフェードで現れる側。インスペクターで非アクティブにしておく想定")]
        [SerializeField] private Image _toImage;

        [Header("演出")]
        [Tooltip("クロスフェードにかける秒数")]
        [SerializeField] private float _duration = 0.8f;
        [SerializeField] private Ease _ease = Ease.InOutSine;
        [Tooltip("PlayCrossfade完了後に_fromImage GameObjectを非アクティブ化する（描画コスト削減）")]
        [SerializeField] private bool _disableFromOnComplete = true;

        private Tween _fromTween;
        private Tween _toTween;
        private bool _hasPlayed;

        private void Awake()
        {
            // 初期状態：fromのアルファだけ念のため不透明にリセットする
            // toはシーン上のアクティブ状態（インスペクターで非アクティブにしてある想定）をそのまま尊重し、Awakeでは触らない
            if (_fromImage != null) SetAlpha(_fromImage, 1f);
        }

        /// <summary>
        /// クロスフェードを開始する。多重呼び出しはガードで無視
        /// インスペクターのUnityEvent経由で呼ばれる
        /// </summary>
        public void PlayCrossfade()
        {
            Debug.Log($"[LogoCrossfade] PlayCrossfade called. hasPlayed={_hasPlayed}, from={(_fromImage != null ? _fromImage.name : "null")}, to={(_toImage != null ? _toImage.name : "null")}, duration={_duration}");
            if (_hasPlayed) return;
            _hasPlayed = true;

            _fromTween?.Kill();
            _toTween?.Kill();

            if (_fromImage != null)
            {
                _fromTween = _fromImage.DOFade(0f, _duration)
                    .SetEase(_ease)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        if (_disableFromOnComplete && _fromImage != null)
                            _fromImage.gameObject.SetActive(false);
                    });
            }

            if (_toImage != null)
            {
                // 非アクティブで眠っていた_toをアルファ0で起動してからフェードインさせる
                SetAlpha(_toImage, 0f);
                if (!_toImage.gameObject.activeSelf)
                    _toImage.gameObject.SetActive(true);

                _toTween = _toImage.DOFade(1f, _duration)
                    .SetEase(_ease)
                    .SetLink(gameObject);
            }
        }

        private static void SetAlpha(Image img, float a)
        {
            var c = img.color;
            c.a = a;
            img.color = c;
        }
    }
}
