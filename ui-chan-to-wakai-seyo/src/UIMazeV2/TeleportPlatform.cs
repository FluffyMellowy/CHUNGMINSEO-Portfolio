using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ペアになった2点間でプレイヤーを即時テレポートさせる空中ワープポイント
    /// 発板ではないため、足場としては機能せず通り抜けトリガー扱い
    /// 通常2点1組（A窓内 ↔ B窓内）で配置し、複数ペアを同シーンに置ける
    ///
    /// 動作:
    /// プレイヤーが範囲に入る → ペア相手のpositionへ移動+ペア側にクールダウン付与
    /// ペア側のクールダウン中は接触判定が無視され、無限ループ防止
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class TeleportPlatform : MonoBehaviour
    {
        [Header("ペア")]
        [Tooltip("プレイヤーが触れた時、ここへワープする相手のワープポイント")]
        [SerializeField] private TeleportPlatform _pairedPoint;

        [Header("挙動")]
        [Tooltip("到着後、このポイントが再びトリガーを受ける猶予時間（秒）。無限ピンポン防止")]
        [SerializeField] private float _cooldownAfterArrival = 0.5f;
        [Tooltip("一度使用したらペア両方を非活性化して以降使えなくする(ワンショット)")]
        [SerializeField] private bool _disableAfterUse = true;
        [Tooltip("非活性化後にビジュアル(_visualRoot)を残すか。" +
                 "true=スプライトはそのまま表示、トリガーだけ無効。" +
                 "false=ビジュアルごと非表示にして完全に消す")]
        [SerializeField] private bool _keepVisualAfterUse = true;
        [Tooltip("非活性化時に消すビジュアルRoot。空ならこのGameObject直下のSpriteRendererを対象にする")]
        [SerializeField] private GameObject _visualRoot;

        /// <summary>
        /// テレポート発動時イベント（送信元と送信先のワープポイントを通知）
        /// マネージャーが現在ウィンドウの追跡などに利用する
        /// </summary>
        public static event System.Action<TeleportPlatform, TeleportPlatform> OnTeleport;

        private float _cooldownEndTime;
        private bool _consumed; // ワンショットモードで既に使用済みか
        private Vector2 _initialPosition; // 起動時の位置(死亡リセット時に戻す先)
        private bool _hasInitialPosition; // _initialPosition記録済みか
        private Rigidbody2D _rb; // 起動時にキャッシュ。スクロールでMovePosition対象にもなる本体

        private void Reset()
        {
            // インスペクターで初追加時、Collider2DはTriggerに、Rigidbody2DはKinematicに自動設定
            // CreditScrollerはRigidbody2DをMovePositionでスクロールさせるためKinematic必須
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // 起動時の位置を記録。スクロールでズレた後でも、死亡リセット時にここへ戻る。
            // Rigidbody2DがKinematicなのでrb.positionとtransform.positionはほぼ同義だが、
            // rb側を基準にしてMovePosition系の整合性を保つ
            _initialPosition = _rb != null ? _rb.position : (Vector2)transform.position;
            _hasInitialPosition = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // ワンショット使用済みなら以降一切反応しない
            if (_consumed) return;
            // クールダウン中は無視
            if (Time.time < _cooldownEndTime) return;
            if (!other.CompareTag("Player")) return;
            if (_pairedPoint == null) return;

            // プレイヤー本体（Rigidbody2Dが付いているTransform）を移動
            var rb = other.attachedRigidbody;
            Transform playerTr = rb != null ? rb.transform : other.transform;
            playerTr.position = _pairedPoint.transform.position;

            // 着地後すぐにペア側のトリガーで再発動しないようクールダウン付与
            _pairedPoint._cooldownEndTime = Time.time + _cooldownAfterArrival;

            OnTeleport?.Invoke(this, _pairedPoint);

            // ワンショットモードなら自身とペアを非活性化(ビジュアル残すかはインスペクター設定に従う)
            if (_disableAfterUse)
            {
                ConsumePair();
            }
        }

        /// <summary>
        /// このポイントとペア相手の両方を「使用済み」状態にする。
        /// トリガーを切ってテレポート発動を止め、必要ならビジュアルも消す。
        /// </summary>
        private void ConsumePair()
        {
            Consume();
            if (_pairedPoint != null) _pairedPoint.Consume();
        }

        /// <summary>
        /// 単独で「使用済み」状態に遷移する。Collider2Dを切ってトリガーを無効化し、
        /// _keepVisualAfterUse=falseの場合のみビジュアルもSetActive(false)で消す
        /// </summary>
        private void Consume()
        {
            _consumed = true;

            // トリガー無効化。ポイント自身のGameObjectをSetActive(false)すると、
            // CreditScrollerがキャッシュしているRigidbody2DのMovePositionが効かなくなる懸念があるので
            // GameObjectは生かしたままCollider2Dだけ無効化する方式を取る
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            if (!_keepVisualAfterUse)
            {
                // ビジュアルを消すモード: _visualRoot指定があればそれをSetActive(false)、
                // 無ければこのGameObject直下の全SpriteRendererを無効化(子は別Rigidbody2Dスクロール対象なので安全な方を取る)
                if (_visualRoot != null)
                {
                    _visualRoot.SetActive(false);
                }
                else
                {
                    foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// プレイヤー死亡からの復帰時に外部(CreditScroller.ResetStage)から呼ばれる。
        /// 使用済みフラグを下ろし、位置を起動時の場所に戻し、Collider2Dとビジュアルを再活性化する
        /// </summary>
        public void ResetState()
        {
            _consumed = false;
            _cooldownEndTime = 0f;

            // 位置を起動時の場所へ戻す。Rigidbody2D経由とTransform両方を更新して、
            // CreditScrollerのMovePosition履歴(キャッシュ済み_initialPositionsとは別管理)とズレないようにする
            if (_hasInitialPosition)
            {
                if (_rb != null)
                {
                    _rb.position = _initialPosition;
                    _rb.linearVelocity = Vector2.zero;
                    _rb.angularVelocity = 0f;
                }
                transform.position = _initialPosition;
            }

            // トリガー再有効化
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            // ビジュアル復元: _visualRootを使ったモードならSetActive(true)で戻す。
            // SpriteRenderer.enabled方式で消した場合はそれを戻す
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(true);
            }
            else
            {
                foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.enabled = true;
                }
            }
        }
    }
}
