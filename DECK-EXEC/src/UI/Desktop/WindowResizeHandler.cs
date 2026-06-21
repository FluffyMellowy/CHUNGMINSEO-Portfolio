namespace Colorless.UI.Desktop
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// ウィンドウの右下角に置くリサイズハンドラ。
    /// ドラッグ量を sizeDelta に反映し、WindowFrame.MinSize / MaxSize でクランプする。
    /// ウィンドウのピボットは左上 (0,1) を想定（右下方向にドラッグすると拡大）。他のピボットでも機能はするが体感が変わる。
    /// </summary>
    public sealed class WindowResizeHandler : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        private WindowFrame _window;
        private RectTransform _windowRoot;
        private RectTransform _parent;

        private Vector2 _startPointerLocal;
        private Vector2 _startSize;

        public void Bind(WindowFrame window)
        {
            _window = window;
            _windowRoot = window != null ? window.Root : null;
            _parent = _windowRoot != null ? _windowRoot.parent as RectTransform : null;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_window != null) _window.BringToFront();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_windowRoot == null || _parent == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, e.position, e.pressEventCamera, out _startPointerLocal);
            _startSize = _windowRoot.sizeDelta;
        }

        public void OnDrag(PointerEventData e)
        {
            if (_windowRoot == null || _parent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, e.position, e.pressEventCamera, out Vector2 currentLocal)) return;

            Vector2 delta = currentLocal - _startPointerLocal;
            /* 右下角ドラッグ：横は + 、縦は -（下方向に引くほど高さが増える、ピボット左上前提） */
            Vector2 newSize = _startSize + new Vector2(delta.x, -delta.y);

            Vector2 min = _window != null ? _window.MinSize : new Vector2(60f, 40f);
            Vector2 max = _window != null ? _window.MaxSize : new Vector2(4096f, 4096f);
            newSize.x = Mathf.Clamp(newSize.x, min.x, max.x);
            newSize.y = Mathf.Clamp(newSize.y, min.y, max.y);

            _windowRoot.sizeDelta = newSize;
        }
    }
}
