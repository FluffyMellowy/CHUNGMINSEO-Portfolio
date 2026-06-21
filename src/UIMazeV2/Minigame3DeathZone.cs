using St;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム3専用のプレイヤー死亡判定
    /// PlatformerDeathZoneとは独立したイベントを通知する
    /// </summary>
    public class Minigame3DeathZone : MonoBehaviour
    {
        public static event System.Action OnPlayerDeath; // ミニゲーム3プレイヤー死亡イベント

        // プレイヤー本体に複数Collider(Capsule + groundTrigger)が付いていて
        // 1回の死亡で OnTriggerEnter2D が2回発火し、Add()が二重に増えてしまう。
        // クールダウンで実質1回扱いに揃える。staticなのでインスタンス間でも共有
        private const float KILL_COOLDOWN_SEC = 0.5f;
        private static float s_lastKillTime = -10f;

        /// <summary>
        /// 外部から死亡イベントをRaiseする
        /// 同時にGameoverCounterへ1カウント加算する（Result分岐用）
        /// クールダウン内の重複呼び出しは無視
        /// </summary>
        public static void Kill()
        {
            if (Time.time - s_lastKillTime < KILL_COOLDOWN_SEC) return;
            s_lastKillTime = Time.time;

            if (GameoverCounter.Instance != null) GameoverCounter.Instance.Add();
            OnPlayerDeath?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            Kill();
        }
    }
}
