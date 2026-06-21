using UnityEngine;
using UnityEngine.EventSystems;

namespace Title
{
    /// <summary>
    /// GameManagerのAdditive読み込みで複数EventSystemが活性化される問題を防ぐシングルトンガード
    /// 動作:
    ///   -既に別のEventSystemが活性なら自分を破棄
    ///   -自分が最初ならDontDestroyOnLoad化してシーン跨ぎでも生存
    /// 各シーンのEventSystem GameObjectにアタッチして使う
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public class EventSystemSingleton : MonoBehaviour
    {
        private void Awake()
        {
            // 自分以外のEventSystemが既にあれば、自分を破棄
            var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            foreach (var es in all)
            {
                if (es != null && es.gameObject != gameObject && es.gameObject.activeInHierarchy)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            // 自分がただ一つのEventSystemなら、シーン跨ぎでも生存させる
            // DontDestroyOnLoadはルートGameObjectでしか効かないので親があれば分離
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }
    }
}
