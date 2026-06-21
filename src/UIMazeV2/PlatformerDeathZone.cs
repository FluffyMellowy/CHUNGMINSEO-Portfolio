using St;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーの死亡判定（落下・障害物共通）
    /// </summary>
    public class PlatformerDeathZone : MonoBehaviour
    {
        public static event System.Action OnPlayerDeath; // プレイヤー死亡イベント

        // プレイヤー本体に isTrigger=0 のCapsuleColliderと isTrigger=1 の_groundTriggerが両方付いており、
        // 1回の死亡で OnTriggerEnter2D が両方分=2回発火してしまう。Add()もイベントも二重通知になるため
        // 短時間のクールダウンで実質1回扱いにする。staticなのでクラス内のどのインスタンスから来ても効く
        private const float KILL_COOLDOWN_SEC = 0.5f;
        private static float s_lastKillTime = -10f;

        /// <summary>
        /// 外部から死亡イベントを通知する
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