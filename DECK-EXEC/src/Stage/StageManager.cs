namespace Colorless.Stage
{
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.SceneManagement;
    using Colorless.Core;

    public sealed class StageManager : MonoBehaviour
    {
        [Title("Stage Identity")]
        [InfoBox("このステージに対応するStageNodeアセットを割り当てる。クリア時にこのノードのStageIdがStageProgressに記録される。")]
        [SerializeField] private StageNode _thisStage;

        [Title("Settings")]
        [InfoBox("R長押しでリセットするまでの時間（秒）")]
        [SerializeField] private float _resetHoldTime = 1.0f;

        private float _resetTimer = 0f;
        private Key _resetKey = Key.R;

        public StageNode ThisStage => _thisStage;

        private void Start()
        {
            /* ステージ進入時に Playing 状態へ */
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);
        }

        private void Update()
        {
            if (Keyboard.current[_resetKey].isPressed)
            {
                _resetTimer += Time.deltaTime;
                if (_resetTimer >= _resetHoldTime)
                {
                    ResetStage();
                }
            }
            else
            {
                _resetTimer = 0f;
            }
        }

        /// <summary>
        /// ステージクリア処理：StageProgress に記録してから次のシーンへ遷移。
        /// MissionManager が全ミッション完了時に呼び出す。
        /// </summary>
        public void Clear()
        {
            if (_thisStage != null)
                StageProgress.MarkCleared(_thisStage.StageId);
            LoadNextStage();
        }

        [Title("Debug"), Button("Reset Stage")]
        public void ResetStage()
        {
            SceneTransitioner.Instance.LoadScene(SceneManager.GetActiveScene().name);
        }

        [Button("Load Next Stage")]
        public void LoadNextStage()
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneTransitioner.Instance.LoadScene(nextIndex);
            }
        }

        [Button("Reset All Progress (Debug)")]
        private void ResetAllProgressDebug()
        {
            StageProgress.ResetAll();
            Debug.Log("[StageProgress] 全クリア状況をリセット");
        }

        [Button("Mark This Stage Cleared (Debug)")]
        private void DebugMarkCurrentCleared()
        {
            if (_thisStage == null)
            {
                Debug.LogWarning("[StageProgress] ThisStage未設定");
                return;
            }
            StageProgress.MarkCleared(_thisStage.StageId);
            Debug.Log($"[StageProgress] {_thisStage.StageId} をクリア済みに");
        }

        [ValueDropdown("GetAllStageSceneNames"), SerializeField]
        private string _debugJumpTarget;

        [Button("Jump to Selected Stage (Debug)")]
        private void DebugJumpToStage()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Debug] Playモード中のみ利用可能");
                return;
            }
            if (string.IsNullOrEmpty(_debugJumpTarget)) return;
            SceneTransitioner.Instance.LoadScene(_debugJumpTarget);
        }

        private IEnumerable<string> GetAllStageSceneNames()
        {
#if UNITY_EDITOR
            List<string> names = new List<string>();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StageNode");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                StageNode node = UnityEditor.AssetDatabase.LoadAssetAtPath<StageNode>(path);
                if (node != null && !string.IsNullOrEmpty(node.SceneName))
                    names.Add(node.SceneName);
            }
            return names;
#else
            return System.Array.Empty<string>();
#endif
        }
    }
}
