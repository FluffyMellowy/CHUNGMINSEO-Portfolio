namespace Colorless.Stage.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Unityエディタメニューから実行できるステージデバッグ機能。
    /// エディタ専用なのでビルドには含まれない。
    /// </summary>
    public static class StageDebugMenu
    {
        private const string MENU_ROOT = "Colorless/Debug/";

        [MenuItem(MENU_ROOT + "Reset Stage Progress")]
        private static void ResetProgress()
        {
            StageProgress.ResetAll();
            Debug.Log("[Debug] ステージ進行度をリセットしました");
        }

        [MenuItem(MENU_ROOT + "Unlock All Stages (Mark All Cleared)")]
        private static void UnlockAllStages()
        {
            string[] guids = AssetDatabase.FindAssets("t:StageNode");
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageNode node = AssetDatabase.LoadAssetAtPath<StageNode>(path);
                if (node == null || string.IsNullOrEmpty(node.StageId)) continue;
                StageProgress.MarkCleared(node.StageId);
                count++;
            }
            Debug.Log($"[Debug] 全ステージ ({count}件) をクリア済みに");
        }
    }
}
