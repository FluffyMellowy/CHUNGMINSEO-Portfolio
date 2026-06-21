namespace Colorless.Stage
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// Main Camera を MapPanel の画面上の領域に合わせて自動配置する。
    /// MapPanel の中心に Stage の中心が描画されるようカメラ position を計算。
    /// 画面サイズ・解像度・MapPanel の位置/サイズが変わっても LateUpdate で追従する。
    ///
    /// 前提:
    ///  - Camera は Orthographic
    ///  - Canvas は Screen Space Overlay
    ///  - Pixel Perfect Camera を使う場合、ortho size は PPC が自動制御するので
    ///    本コンポーネントは position のみ調整
    /// </summary>
    [ExecuteAlways]
    public sealed class CameraToMapPanelFitter : MonoBehaviour
    {
        [Title("Refs")]
        [Required, SerializeField] private Camera _camera;
        [Required, SerializeField] private RectTransform _mapPanel;

        [Title("Focus")]
        [InfoBox("Stage 中心の world 座標。未指定なら (0,0)。\nGridManager から動的計算も可能だがまずは手動指定で。")]
        [SerializeField] private Transform _stageCenter;

        [Title("Debug")]
        [ShowInInspector, ReadOnly] private Vector2 _lastPanelCenter;
        [ShowInInspector, ReadOnly] private Vector2 _lastOffsetWorld;

        private void LateUpdate()
        {
            Fit();
        }

        public void Fit()
        {
            if (_camera == null || _mapPanel == null) return;
            if (!_camera.orthographic) return;
            if (_camera.orthographicSize <= 0f) return;

            /* MapPanel の画面上の中心（Screen Space Overlay では GetWorldCorners が
               そのまま画面ピクセル座標を返す） */
            Vector3[] corners = new Vector3[4];
            _mapPanel.GetWorldCorners(corners);
            Vector2 panelMin = corners[0];
            Vector2 panelMax = corners[2];
            Vector2 panelCenter = (panelMin + panelMax) * 0.5f;
            _lastPanelCenter = panelCenter;

            /* 画面中心と MapPanel 中心の差（ピクセル） */
            Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 offsetScreen = panelCenter - screenCenter;

            /* 1 world unit が画面上で何ピクセルか
               ortho size は world 単位での画面高さの半分なので、
               pixelsPerWorldUnit = Screen.height / (2 * orthoSize) */
            float pixelsPerWorldUnit = Screen.height / (_camera.orthographicSize * 2f);
            if (pixelsPerWorldUnit <= 0f) return;

            Vector2 offsetWorld = offsetScreen / pixelsPerWorldUnit;
            _lastOffsetWorld = offsetWorld;

            /* stage 中心が MapPanel 中心に来るようカメラ position を逆算 */
            Vector3 stagePos = _stageCenter != null ? _stageCenter.position : Vector3.zero;
            Vector3 newPos = new(
                stagePos.x - offsetWorld.x,
                stagePos.y - offsetWorld.y,
                _camera.transform.position.z
            );
            _camera.transform.position = newPos;
        }
    }
}
