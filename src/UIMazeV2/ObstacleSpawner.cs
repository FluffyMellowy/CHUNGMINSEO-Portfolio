using UnityEngine;

namespace UIMazeV2
{
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private ObstacleWindow[] _windows;

        private void OnEnable()
        {
            Minigame2Manager.OnPlayerRespawn += ResetAll;

            foreach (var window in _windows)
            {
                if (window != null) window.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            Minigame2Manager.OnPlayerRespawn -= ResetAll;

            foreach (var window in _windows)
            {
                if (window != null) window.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 全ウィンドウと落下中の障害物をリセットする
        /// </summary>
        private void ResetAll()
        {
            foreach (var window in _windows)
            {
                if (window == null) continue;

                // 各ウィンドウの子にある障害物を削除
                var obstacles = window.GetComponentsInChildren<FallingObstacle>();
                foreach (var obs in obstacles)
                {
                    Destroy(obs.gameObject);
                }

                // ウィンドウを再起動
                window.gameObject.SetActive(false);
                window.gameObject.SetActive(true);
            }
        }
    }
}