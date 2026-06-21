using DG.Tweening;
using KanKikuchi.AudioManager;
using St;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Title
{
    /// <summary>
    /// タイトル画面のSkipボタン
    /// 押下時のフィードバック（パンチスケール＋色変更）とSE再生、シーン遷移イベントのRaiseを担当する
    /// ButtonコンポーネントのOnClickにOnClick()を紐付けて使用する
    /// EventSystemで本ボタンが選択状態なら、ゲームパッドの決定ボタン（A/Submit）でも自動的にOnClickが実行される
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TitleSkipButton : MonoBehaviour
    {
        [Header("遷移")]
        [SerializeField] private SectionTypeEvent _finishEvent; // シーン遷移通知イベント（統合実行時に使用）
        [SerializeField] private string _standaloneNextScene = "DialogueScene"; // 単独実行時に直接ロードするシーン名（空文字で無効）
        [SerializeField] private TitleDialogueLoop _titleLoop; // セット時はOnClickでこれにスキップ要求を送り、シーン遷移はLoop側に委ねる

        [Header("押下フィードバック")]
        [SerializeField] private float _punchScale = 0.3f; // パンチスケールの強度
        [SerializeField] private float _punchDuration = 0.25f; // パンチ演出時間（秒）
        [SerializeField] private Color _pressedColor = Color.white; // 押下後の色（復帰せずシーン遷移までそのまま）

        [Header("アイドル演出")]
        [SerializeField] private float _idleScaleAmplitude = 0.08f; // アイドル時の拡大率（0.08 =初期サイズの+8%まで膨らむ）
        [SerializeField] private float _idleDuration = 0.8f; // アイドル拡縮の片道時間（秒）

        [Header("サウンド")]
        [SerializeField] private string _skipSEPath = "SE_Chung/SE0"; // スキップ音

        private Image _image;
        private bool _pressed;
        private Vector3 _initialScale; // 初期スケールキャッシュ
        private Tween _idleTween; // アイドル拡縮トゥイーン

        private void Awake()
        {
            _image = GetComponent<Image>();
            _initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            StartIdleTween();
        }

        private void OnDisable()
        {
            _idleTween?.Kill();
            transform.localScale = _initialScale;
        }

        /// <summary>
        /// アイドル時の拡縮Yoyoループを開始する
        /// </summary>
        private void StartIdleTween()
        {
            _idleTween?.Kill();
            transform.localScale = _initialScale;
            _idleTween = transform
                .DOScale(_initialScale * (1f + _idleScaleAmplitude), _idleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// Button.OnClickから呼び出す。押下フィードバック→SE→遷移イベントRaiseの順で実行する
        /// </summary>
        public void OnClick()
        {
            if (_pressed) return;
            _pressed = true;

            // アイドル拡縮を停止して初期スケールに戻してからパンチ
            _idleTween?.Kill();
            transform.localScale = _initialScale;

            // 視覚フィードバック
            _image.transform.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 1, 0.5f);
            _image.color = _pressedColor;

            // SE
            SafeSE.Play(_skipSEPath);

            // タイトルダイアログループが設定されていれば、そちらに委譲して遷移はLoop完了時に行う
            if (_titleLoop != null)
            {
                _titleLoop.RequestSkip();
                return;
            }

            // シーン遷移：単独実行時はSceneManagerで直接ロード、統合実行時はイベント発信
            if (!string.IsNullOrEmpty(_standaloneNextScene))
                SceneManager.LoadScene(_standaloneNextScene);
            else if (_finishEvent != null)
                _finishEvent.Raise(SectionType.Title);
        }
    }
}
