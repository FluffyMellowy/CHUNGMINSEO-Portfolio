namespace Colorless.UI.Desktop
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// デスクトップ上に存在する WindowFrame 群の z-order とフォーカスを管理する。
    /// WindowFrame は OnEnable で自身を Register、OnDisable で Unregister する。
    /// このマネージャーが存在しなくても WindowFrame は単独動作する（SetAsLastSibling のみ）。
    /// </summary>
    public sealed class WindowManager : MonoBehaviour
    {
        [Title("Runtime State")]
        [ShowInInspector, ReadOnly] private readonly List<WindowFrame> _windows = new();
        [ShowInInspector, ReadOnly] private WindowFrame _focused;

        public IReadOnlyList<WindowFrame> Windows => _windows;
        public WindowFrame Focused => _focused;

        public void Register(WindowFrame window)
        {
            if (window == null) return;
            if (_windows.Contains(window)) return;
            _windows.Add(window);
        }

        public void Unregister(WindowFrame window)
        {
            if (window == null) return;
            _windows.Remove(window);
            if (_focused == window) _focused = null;
        }

        /// <summary>
        /// 指定ウィンドウを最前面に移動しフォーカスする。
        /// </summary>
        public void Focus(WindowFrame window)
        {
            if (window == null) return;
            window.transform.SetAsLastSibling();
            _focused = window;
        }
    }
}
