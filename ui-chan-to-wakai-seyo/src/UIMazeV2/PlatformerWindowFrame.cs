using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// 横スクロールミニゲーム用のウィンドウフレーム
    /// プレイヤーの位置に合わせてウィンドウを横方向に追従させる
    /// </summary>
    public class PlatformerWindowFrame : MonoBehaviour
    {
        [SerializeField] private Transform _player; // 追従対象のプレイヤー
        [SerializeField] private Vector2 _offset; // プレイヤーからのオフセット

        private void LateUpdate()
        {
            if (_player == null) return;

            // プレイヤーの位置にオフセットを加えて追従
            transform.position = new Vector3(
                _player.position.x + _offset.x,
                _player.position.y + _offset.y,
                transform.position.z
            );
        }
    }
}