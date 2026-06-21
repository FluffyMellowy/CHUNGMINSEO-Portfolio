using KanKikuchi.AudioManager;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ウィンドウ下端などに配置するトリガー。発板が突入すると破壊演出を行い、本体を削除する
    /// 演出用Prefab（パーティクル/ Feel MMF_Player等）を生成し、SEを鳴らす
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CreditDestroyZone : MonoBehaviour
    {
        [Header("演出")]
        [SerializeField] private GameObject _shatterFxPrefab; // 破壊演出のPrefab（生成後自動で破棄される想定）
        [SerializeField] private string _shatterSEPath = ""; // 破壊SE(任意、必要なら指定)

        [Header("除外")]
        [SerializeField] private string _excludeTag = "Player"; // このタグを持つColliderは破壊しない

        private void Reset()
        {
            // インスペクターで初追加された時にIsTriggerをONにする
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // プレイヤー等の除外対象はスキップ
            if (!string.IsNullOrEmpty(_excludeTag) && other.CompareTag(_excludeTag)) return;

            // 発板に相当するもの（Rigidbody2Dを持つ）のみ対象
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            var target = rb.gameObject;

            // 演出Prefab生成
            if (_shatterFxPrefab != null)
                Instantiate(_shatterFxPrefab, target.transform.position, target.transform.rotation);

            // SE再生
            SafeSE.Play(_shatterSEPath);

            // 発板本体を削除
            Destroy(target);
        }
    }
}
