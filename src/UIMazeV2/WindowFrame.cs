using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーの位置に追従するウィンドウフレーム
    /// カメラの境界内に制限する
    /// </summary>
    public class WindowFrame : MonoBehaviour
    {
        [SerializeField] private Transform _player; // プレイヤーの参照

        [Header("カメラ境界の緩和（タイトルバー等を画面外へはみ出させる用）")]
        [Tooltip("ウィンドウ中心が この量だけカメラ上端を超えて上に行ける（タイトルバー高さ分入れると、タイトルバーがカメラ外に出てプレイ領域の最上段が見える）")]
        [SerializeField] private float _topClampOffset = 0f;
        [SerializeField] private float _bottomClampOffset = 0f;
        [SerializeField] private float _leftClampOffset = 0f;
        [SerializeField] private float _rightClampOffset = 0f;

        [Header("カメラ参照")]
        [Tooltip("2Dゲーム描画用のカメラ。3D背景カメラと分離している場合に明示的に指定。未指定なら Camera.main を使用")]
        [SerializeField] private Camera _mainCamera;

        private SpriteRenderer _spriteRenderer; // SpriteRendererキャッシュ

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            // インスペクターで明示指定されていなければCamera.mainにフォールバック
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void Update()
        {
            // GameManager経由のシーン遷移でAwake時にCamera.mainがTitleシーン側を掴むケースがある
            // その後Titleシーンがアンロードされて_mainCameraが破棄状態になり、orthographicSize取得で例外
            // 使用直前に破棄チェックして必要なら再取得する
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return; // フォールバック：カメラ未取得時はクランプを諦める

            Vector3 playerPos = _player.position;

            // フレームサイズを自動取得
            float halfW = _spriteRenderer.bounds.size.x * 0.5f;
            float halfH = _spriteRenderer.bounds.size.y * 0.5f;

            // インセット分カメラ境界を緩和してウィンドウが画面外に少しはみ出せるようにする
            // playerPos.zは無視して自分のZを保持
            Vector3 target = new Vector3(playerPos.x, playerPos.y, transform.position.z);
            transform.position = CameraClamp.ClampToCamera(
                target, _mainCamera, halfW, halfH,
                _topClampOffset, _bottomClampOffset, _leftClampOffset, _rightClampOffset);
        }
    }
}