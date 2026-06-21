namespace Colorless.Core
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    public enum GameState
    {
        MainMenu,
        StageSelect,
        Playing,
        Paused
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Title("Runtime State")]
        [ShowInInspector, ReadOnly]
        public GameState CurrentState { get; private set; }

        private GameState _stateBeforePause;

        /// <summary>
        /// Domain Reloadを無効化した場合のstatic状態リセット。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
        }

        [Title("Debug"), Button("Pause")]
        public void Pause()
        {
            if (CurrentState != GameState.Playing) return;
            _stateBeforePause = CurrentState;
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        [Button("Resume")]
        public void Resume()
        {
            if (CurrentState != GameState.Paused) return;
            CurrentState = _stateBeforePause;
            Time.timeScale = 1f;
        }
    }
}
