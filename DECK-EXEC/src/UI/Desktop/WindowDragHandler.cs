namespace Colorless.UI.Desktop
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// WindowFrame のタイトルバーに付くドラッグハンドラ。
    /// タイトルバー上の pointer down でフォーカスを移し、ドラッグでウィンドウルートを追従移動させる。
    /// WindowFrame.Awake が自動装着するため、通常は手動で AddComponent しなくて良い。
    /// </summary>
    public sealed class WindowDragHandler : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        private WindowFrame _window;
        private RectTransform _windowRoot;
        private RectTransform _parent;

        private Vector2 _startPointerLocal;
        private Vector3 _startWindowPos;

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
            _startWindowPos = _windowRoot.localPosition;
        }

        public void OnDrag(PointerEventData e)
        {
            if (_windowRoot == null || _parent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parent, e.position, e.pressEventCamera, out Vector2 currentLocal)) return;

            Vector2 delta = currentLocal - _startPointerLocal;
            _windowRoot.localPosition = _startWindowPos + new Vector3(delta.x, delta.y, 0f);
        }
    }
}
