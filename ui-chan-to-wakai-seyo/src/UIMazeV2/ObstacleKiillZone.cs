using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// 障害物の子オブジェクトに付けるプレイヤー死亡判定トリガー
    /// </summary>
    public class ObstacleKillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            PlatformerDeathZone.Kill();
            Destroy(transform.parent.gameObject); // 親の障害物ごと消滅
        }
    }
}