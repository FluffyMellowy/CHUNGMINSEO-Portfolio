namespace Colorless.UI
{
    using UnityEngine;
    using Colorless.Core;
    using Colorless.Stage;

    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string _firstStageScene = "Scene1";
        [SerializeField] private string _stageSelectScene = "StageSelect";

        private void Start()
        {
            /* メインメニュー状態に設定 */
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.MainMenu);
        }

        /// <summary>
        /// ゲーム開始（最初のステージへ）
        /// </summary>
        public void OnStartGame()
        {
            SceneTransitioner.Instance.LoadScene(_firstStageScene);
        }

        /// <summary>
        /// ステージ選択画面へ
        /// </summary>
        public void OnStageSelect()
        {
            SceneTransitioner.Instance.LoadScene(_stageSelectScene);
        }

        /// <summary>
        /// 設定（未実装）
        /// </summary>
        public void OnSettings()
        {
            /* TODO: 設定画面を開く */
        }

        /// <summary>
        /// ゲーム終了
        /// </summary>
        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
