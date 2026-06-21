using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Title
{
    /// <summary>
    /// クレジット表示用モーダル。
    /// Tabキー / パッドYボタン(buttonNorth) でトグル開閉。クレジットボタンや閉じるボタンからの直接呼び出しにも対応。
    /// 開閉時はDOTweenで scale (OutBack/InBack) + alpha (CanvasGroup) のアニメーションを行う。
    /// _panel に半透明背景＋クレジットテキストを持つ親GameObjectを割り当てる。
    /// </summary>
    public class CreditModal : MonoBehaviour
    {
        [Header("モーダル本体")]
        [Tooltip("半透明背景＋クレジット本文を含むパネルのルートGameObject。SetActiveで表示/非表示を切替")]
        [SerializeField] private GameObject _panel;

        [Header("演出対象（任意。空ならpanel側から自動取得）")]
        [Tooltip("拡縮アニメの対象RectTransform。ContentPanel等を入れるとBackgroundDimは拡縮されない自然な見た目になる。空ならpanel自体")]
        [SerializeField] private RectTransform _scaleTarget;
        [Tooltip("フェード用CanvasGroup。panelルートにCanvasGroupを付けてここに割り当てる")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("トグル入力")]
        [Tooltip("このキーで開閉トグル (デフォルト C)")]
        [SerializeField] private Key _toggleKey = Key.C;
        [Tooltip("パッドのYボタン(buttonNorth)でも開閉トグルするか。LanguageToggleButtonはXに割り当て済みなので競合しない")]
        [SerializeField] private bool _useGamepadToggle = true;

        [Header("ガード")]
        [Tooltip("アトラクト動画再生中はモーダル開閉入力を無視する。割り当てない場合はガードなし")]
        [SerializeField] private TitleAttractVideo _attractVideo;

        [Header("演出")]
        [Tooltip("開く時のアニメ時間（秒）")]
        [SerializeField] private float _openDuration = 0.25f;
        [Tooltip("閉じる時のアニメ時間（秒）")]
        [SerializeField] private float _closeDuration = 0.2f;
        [Tooltip("閉じた状態でのscale。0より少し大きい値だと弾むOutBack感が出る")]
        [SerializeField] private Vector3 _closedScale = new Vector3(0.85f, 0.85f, 1f);

        [Header("サウンド")]
        [Tooltip("開く時のSEパス。空文字なら無音")]
        [SerializeField] private string _openSEPath = "";
        [Tooltip("閉じる時のSEパス。空文字なら無音")]
        [SerializeField] private string _closeSEPath = "";

        private Tween _scaleTween;
        private Tween _fadeTween;
        private bool _isOpen;
        // インスペクターで設定された開きっぱなし時のScale。Awake時にキャッシュして、
        // トゥイーンのendValueに使う。これによりVector3.oneハードコードでサイズが変わる事故を防ぐ
        private Vector3 _openScale = Vector3.one;

        private void Awake()
        {
            if (_panel == null) return;

            // 未割り当てなら panel から自動取得
            if (_scaleTarget == null) _scaleTarget = _panel.transform as RectTransform;
            if (_canvasGroup == null) _canvasGroup = _panel.GetComponent<CanvasGroup>();

            // インスペクター指定の元Scaleを保存。開いた時はこのScaleまでトゥイーンする
            if (_scaleTarget != null) _openScale = _scaleTarget.localScale;

            // 起動時は閉じた状態に確定
            _panel.SetActive(false);
            _isOpen = false;
        }

        /// <summary>
        /// 開く。クレジットボタンのButton.OnClickから直接呼び出す想定
        /// </summary>
        public void Open()
        {
            if (_panel == null || _isOpen) return;
            _isOpen = true;

            // 進行中のトゥイーンは破棄
            _scaleTween?.Kill();
            _fadeTween?.Kill();

            _panel.SetActive(true);

            // 初期状態を強制セット。_closedScale は _openScale を基準にした比率扱い
            if (_scaleTarget != null)
                _scaleTarget.localScale = Vector3.Scale(_openScale, _closedScale);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            // scale: 弾むOutBackでインスペクター指定のScaleまで拡大
            if (_scaleTarget != null)
                _scaleTween = _scaleTarget.DOScale(_openScale, _openDuration).SetEase(Ease.OutBack);

            // alpha: フェードイン
            if (_canvasGroup != null)
                _fadeTween = _canvasGroup.DOFade(1f, _openDuration).SetEase(Ease.OutQuad);

            SafeSE.Play(_openSEPath);
        }

        /// <summary>
        /// 閉じる。パネル内の閉じるボタンのOnClickから呼び出す想定
        /// </summary>
        public void Close()
        {
            if (_panel == null || !_isOpen) return;
            _isOpen = false;

            _scaleTween?.Kill();
            _fadeTween?.Kill();

            // 入力を即座に遮断（連打防止）
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // scale: 縮みながら消える。_closedScaleは_openScale基準の比率
            if (_scaleTarget != null)
                _scaleTween = _scaleTarget.DOScale(Vector3.Scale(_openScale, _closedScale), _closeDuration).SetEase(Ease.InBack);

            // alpha: フェードアウト → 完了後にSetActive(false)
            Tween completionDriver = null;
            if (_canvasGroup != null)
            {
                _fadeTween = _canvasGroup.DOFade(0f, _closeDuration).SetEase(Ease.InQuad);
                completionDriver = _fadeTween;
            }
            else
            {
                completionDriver = _scaleTween;
            }

            if (completionDriver != null)
                completionDriver.OnComplete(() => { if (_panel != null) _panel.SetActive(false); });
            else
                _panel.SetActive(false);

            SafeSE.Play(_closeSEPath);
        }

        /// <summary>
        /// 開閉トグル。Tab / パッドYのショートカットと、トグル兼用ボタンから使う
        /// </summary>
        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        private void Update()
        {
            // アトラクト動画再生中はトグルしない(その間の入力はTitleAttractVideo側が「動画閉じる」で消費する)
            if (_attractVideo != null && _attractVideo.IsPlaying) return;

            // Tab / Yはモーダルが閉じている時も含めて常時監視する
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            if (_useGamepadToggle)
            {
                var gp = Gamepad.current;
                if (gp != null && gp.buttonNorth.wasPressedThisFrame)
                {
                    Toggle();
                }
            }
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            _fadeTween?.Kill();
        }
    }
}
