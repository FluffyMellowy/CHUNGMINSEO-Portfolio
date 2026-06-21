namespace Colorless.UI.Desktop
{
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// デスクトップのルートコンポーネント。
    /// 壁紙画像、WindowManager 参照、ウィンドウが座標計算で使う親 RectTransform を保持する。
    /// このコンポーネントの子に WindowFrame を配置することを想定。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class DesktopController : MonoBehaviour
    {
        [Title("Refs")]
        [InfoBox("壁紙を表示する Image。背景色だけで運用するなら未割当でも可。")]
        [SerializeField] private Image _wallpaper;
        [InfoBox("子階層に置いた WindowManager。WindowFrame はこのマネージャー越しに z-order を管理する。")]
        [SerializeField] private WindowManager _windowManager;

        [Title("Wallpaper")]
        [SerializeField] private Sprite _defaultWallpaper;
        [SerializeField] private Color _wallpaperTint = Color.white;

        private RectTransform _rect;

        public RectTransform Rect => _rect;
        public WindowManager WindowManager => _windowManager;
        public Sprite CurrentWallpaper => _wallpaper != null ? _wallpaper.sprite : null;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            if (_wallpaper != null)
            {
                if (_defaultWallpaper != null) _wallpaper.sprite = _defaultWallpaper;
                _wallpaper.color = _wallpaperTint;
            }
        }

        /// <summary>
        /// 壁紙画像を差し替える（チャプター変更時など）。
        /// </summary>
        public void SetWallpaper(Sprite sprite)
        {
            if (_wallpaper == null) return;
            _wallpaper.sprite = sprite;
        }

        /// <summary>
        /// 壁紙の Tint カラーを差し替える。
        /// </summary>
        public void SetWallpaperTint(Color color)
        {
            if (_wallpaper == null) return;
            _wallpaper.color = color;
        }
    }
}
