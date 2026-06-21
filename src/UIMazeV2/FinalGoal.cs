using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム3（最終）のゴール判定
    /// UIMazeV2全体のクリアをSectionTypeEventで通知する
    /// </summary>
    public class FinalGoal : MonoBehaviour
    {
        [SerializeField] private SectionTypeEvent _clear; // 全体クリア通知イベント
        [SerializeField] private St.SectionType _sectionType; // 通知するセクションタイプ

        private bool _isReached; // 到達済みフラグ

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isReached) return;
            if (!other.CompareTag("Player")) return;

            _isReached = true;
            _clear.Raise(_sectionType);
            Debug.Log("UIMazeV2 Clear");
        }
    }
}