namespace Colorless.Stage
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// ステージクリア状況の永続化。Easy Save 3（ES3）を使って保存する。
    /// クリア済みIDのHashSetをメモリに保持し、変更時にES3へSaveする。
    /// </summary>
    public static class StageProgress
    {
        private const string SAVE_KEY = "cleared_stages";

        private static HashSet<string> _cleared;

        /// <summary>
        /// 進行度が変更されたときに発火。UI更新などに利用。
        /// </summary>
        public static event System.Action OnChanged;

        /// <summary>
        /// Domain Reload無効化時のstatic状態リセット。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _cleared = null;
            OnChanged = null;
        }

        private static void EnsureLoaded()
        {
            if (_cleared != null) return;

            if (ES3.KeyExists(SAVE_KEY))
                _cleared = new HashSet<string>(ES3.Load<string[]>(SAVE_KEY));
            else
                _cleared = new HashSet<string>();
        }

        /// <summary>
        /// 指定ステージがクリア済みか
        /// </summary>
        public static bool IsCleared(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return false;
            EnsureLoaded();
            return _cleared.Contains(stageId);
        }

        /// <summary>
        /// ステージをクリア済みとしてマーク。新規登録時のみES3へ保存。
        /// </summary>
        public static void MarkCleared(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return;
            EnsureLoaded();
            if (_cleared.Add(stageId))
            {
                ES3.Save(SAVE_KEY, _cleared.ToArray());
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        /// ノードの前提条件が全て満たされているか（解放済みか）を判定
        /// </summary>
        public static bool IsUnlocked(StageNode node)
        {
            if (node == null) return false;
            if (node.Prerequisites.Count == 0) return true;

            foreach (StageNode prereq in node.Prerequisites)
            {
                if (prereq == null) continue;
                if (!IsCleared(prereq.StageId)) return false;
            }
            return true;
        }

        /// <summary>
        /// 全進行度をリセット（デバッグ用）
        /// </summary>
        public static void ResetAll()
        {
            EnsureLoaded();
            _cleared.Clear();
            if (ES3.KeyExists(SAVE_KEY))
                ES3.DeleteKey(SAVE_KEY);
            OnChanged?.Invoke();
        }
    }
}
