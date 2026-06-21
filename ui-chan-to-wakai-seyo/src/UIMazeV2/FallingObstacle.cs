using UnityEngine;
using UnityEngine.Tilemaps;

namespace UIMazeV2
{
    /// <summary>
    /// ウィンドウから落下する障害物
    /// 一切の停止判定を持たず、メインウィンドウの上枠・下枠どちらも貫通して画面外へ落下し、
    /// _lifetime経過で自動消滅する
    /// </summary>
    public class FallingObstacle : MonoBehaviour
    {
        [SerializeField] private float _fallSpeed = 8f; // 落下速度
        [SerializeField] private float _lifetime = 5f; // 最大生存時間（秒）
        [SerializeField] private string _dropSEPath = "SE_Chung/SE7"; // 落下開始時に鳴らすSE(障害物落下音)

        /// <summary>
        /// 落下済み障害物の共通親（設定されていればSetParent(null)の代わりに使用）
        /// </summary>
        public static Transform DroppedParent { get; set; }

        private Rigidbody2D _rb;
        private bool _isDropped; // 落下開始フラグ

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.simulated = false;
            GetComponent<BoxCollider2D>().enabled = false;

            // ウィンドウのSpriteMaskに左右されず常に表示させるため、子の全Rendererを強制的にNoneに
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                sr.maskInteraction = SpriteMaskInteraction.None;
            foreach (var tr in GetComponentsInChildren<TilemapRenderer>(true))
                tr.maskInteraction = SpriteMaskInteraction.None;
        }

        /// <summary>
        /// 落下を開始する
        /// </summary>
        public void Drop()
        {
            _isDropped = true;

            var myCol = GetComponent<BoxCollider2D>();

            // (1)旧親ObstacleWindowとその子（兄弟FallingObstacle含む）を無視
            // 落下開始位置で本体・兄弟コライダーに引っかからないようにする
            if (transform.parent != null)
            {
                foreach (var parentCol in transform.parent.GetComponentsInChildren<Collider2D>(true))
                {
                    if (parentCol != null && parentCol != myCol)
                        Physics2D.IgnoreCollision(myCol, parentCol, true);
                }
            }

            // (2)メインウィンドウの枠を全て貫通させるため、PlatformerWindowタグおよびGroundレイヤーの
            // 全コライダーをIgnore対象に登録する。これにより上枠/下枠どちらに当たっても止まらず、
            // そのまま画面外まで落下する
            int groundLayer = LayerMask.NameToLayer("Ground");
            var allCols = FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var col in allCols)
            {
                if (col == null || col == myCol) continue;
                if (col.CompareTag("PlatformerWindow") || col.gameObject.layer == groundLayer)
                    Physics2D.IgnoreCollision(myCol, col, true);
            }

            transform.SetParent(DroppedParent);
            myCol.enabled = true;
            _rb.simulated = true;
            _rb.linearVelocity = Vector2.down * _fallSpeed;

            // 落下開始SE
            SafeSE.Play(_dropSEPath);

            // 一定時間後に自動消滅。地面/枠を貫通して画面外に出た後の後片付けに使う
            Destroy(gameObject, _lifetime);
        }
    }
}
