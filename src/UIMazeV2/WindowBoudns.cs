using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーの移動範囲をマスク内に制限する
    /// プレイヤーのスプライト半サイズ分内側にクランプして、絵がマスクからはみ出ないようにする
    /// </summary>
    public class WindowBounds : MonoBehaviour
    {
        [SerializeField] private Transform _player; // 制限対象のプレイヤー
        [SerializeField] private SpriteMask _mask; // 基準となるSpriteMask

        [Tooltip("プレイヤーのSpriteRendererから自動取得する。オフなら下記の手動値を使う")]
        [SerializeField] private bool _autoDetectFromSprite = true;
        [Tooltip("自動取得しない場合の手動半サイズ（X/Y、ワールドユニット）")]
        [SerializeField] private Vector2 _manualHalfSize = Vector2.zero;
        [Tooltip("常に追加で適用する余白（マスクから絵を少し離したい時用、X/Y 共通の縮小）")]
        [SerializeField] private Vector2 _padding = Vector2.zero;

        [Header("辺ごとのインセット（タイトルバー等の非プレイ領域を除外）")]
        [Tooltip("マスク上辺から下に何ユニット入った位置を上限にするか（タイトルバーの高さ分）")]
        [SerializeField] private float _topInset = 0f;
        [SerializeField] private float _bottomInset = 0f;
        [SerializeField] private float _leftInset = 0f;
        [SerializeField] private float _rightInset = 0f;

        private SpriteRenderer _playerSR;

        private void Awake()
        {
            if (_player != null) _playerSR = _player.GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (_player == null || _mask == null) return;

            var bounds = _mask.bounds;
            var pos = _player.position;

            // プレイヤーの半サイズを取得（自動or手動）
            Vector2 half = _autoDetectFromSprite && _playerSR != null
                ? new Vector2(_playerSR.bounds.size.x * 0.5f, _playerSR.bounds.size.y * 0.5f)
                : _manualHalfSize;

            half += _padding;

            // 各辺のインセットを反映してプレイ可能領域を狭める
            float minX = bounds.min.x + half.x + _leftInset;
            float maxX = bounds.max.x - half.x - _rightInset;
            float minY = bounds.min.y + half.y + _bottomInset;
            float maxY = bounds.max.y - half.y - _topInset;

            // インセットが過剰でmin>maxになった軸はその中間に固定（暴走防止）
            pos.x = (minX > maxX) ? (minX + maxX) * 0.5f : Mathf.Clamp(pos.x, minX, maxX);
            pos.y = (minY > maxY) ? (minY + maxY) * 0.5f : Mathf.Clamp(pos.y, minY, maxY);

            _player.position = pos;
        }
    }
}
