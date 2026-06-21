using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// プレイヤーがトリガー範囲に入ると、パンチスプライト + 穴スプライト + ブロック用コライダーが同時に出現する。
    /// パンチスプライトは _punchDuration 秒後に消え、穴スプライトとコライダーはリセットされるまで残る。
    /// 単純な on/off ロジックで、Tilemap を使わずに同じ通路ブロック効果を表現する用途。
    /// 死亡時の復元は外部から ResetState() を呼ぶ
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PunchBlocker : MonoBehaviour
    {
        [Header("ビジュアル")]
        [Tooltip("パンチスプライト。発動時に表示され、_punchDuration秒後に非表示になる")]
        [SerializeField] private SpriteRenderer _punchSprite;
        [Tooltip("穴スプライト。発動時に表示され、ResetStateが呼ばれるまで残る")]
        [SerializeField] private SpriteRenderer _holeSprite;

        [Header("ブロックコライダー")]
        [Tooltip("発動時に有効化する通路ブロック用コライダー(別GameObjectに置いて参照する想定)。" +
                 "本コンポーネント自身のColliderはトリガー検出専用なので別途用意する")]
        [SerializeField] private Collider2D _blockerCollider;

        [Header("挙動")]
        [Tooltip("パンチスプライトを表示する時間(秒)")]
        [SerializeField] private float _punchDuration = 0.3f;
        [Tooltip("trueなら一度きり、falseなら出入りで切り替え")]
        [SerializeField] private bool _oneShot = true;

        [Header("サウンド")]
        [Tooltip("パンチ発動時に鳴らすSEパス。壁破壊扱い(SE9)。空文字なら無音")]
        [SerializeField] private string _punchSEPath = "SE_Chung/SE9";

        [Header("マスク")]
        [Tooltip("パンチ/穴スプライトの SpriteMask 振る舞い。" +
                 "ミニゲームウィンドウ内のSpriteMaskに左右されず常に表示したい場合は None、" +
                 "ウィンドウ内側のみ表示なら VisibleInsideMask")]
        [SerializeField] private SpriteMaskInteraction _maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        [Header("パンチアニメーション")]
        [Tooltip("パンチ出現時の最大拡大倍率。1.3 なら一瞬で元サイズの130%まで膨らんで戻る")]
        [SerializeField] private float _punchScaleMultiplier = 1.3f;
        [Tooltip("拡大→復帰の合計時間(秒)。拡大40%・復帰60%で按分される")]
        [SerializeField] private float _punchScaleDuration = 0.18f;
        [Tooltip("拡大フェーズのEase。OutBackなら少しオーバーシュートして元気な印象")]
        [SerializeField] private Ease _punchScaleEaseOut = Ease.OutBack;
        [Tooltip("復帰フェーズのEase")]
        [SerializeField] private Ease _punchScaleEaseIn = Ease.InQuad;

        private bool _hasFired;
        private Vector3 _punchOriginalScale = Vector3.one; // パンチTransformの元スケール(Awakeでキャッシュ)
        private Sequence _punchScaleSeq;                   // 進行中の拡大アニメ。再発火・破棄時に止める

        private void Reset()
        {
            // インスペクター追加時にトリガー判定用コライダーをisTrigger=ONにする
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            // SpriteMask振る舞いをインスペクター指定に揃える(マスク絡みの不可視事故を予防)
            if (_punchSprite != null) _punchSprite.maskInteraction = _maskInteraction;
            if (_holeSprite != null) _holeSprite.maskInteraction = _maskInteraction;

            // パンチTransformの元スケールをキャッシュ。シーン上で設定済みのサイズを起点に拡大する
            if (_punchSprite != null) _punchOriginalScale = _punchSprite.transform.localScale;

            // 起動時は全て非表示 + コライダー無効
            if (_punchSprite != null) _punchSprite.enabled = false;
            if (_holeSprite != null) _holeSprite.enabled = false;
            if (_blockerCollider != null) _blockerCollider.enabled = false;
        }

        private void OnDestroy()
        {
            // 進行中の拡大シーケンスがあれば破棄時に止める
            if (_punchScaleSeq != null && _punchScaleSeq.IsActive())
            {
                _punchScaleSeq.Kill();
                _punchScaleSeq = null;
            }
        }

        // 当たり判定はプレイヤー本体のCapsuleCollider2D(非トリガー)で取りたい。
        // _groundTrigger(isTrigger=1, プレイヤー足元の細長いボックス)はトリガー側なので
        // OnTriggerEnter2Dで拾ってしまうと、上から来た時しか反応せず横/下からは反応しない
        // 非トリガー同士の衝突イベント(OnCollisionEnter2D)で受けることで、
        // CapsuleColliderが接触した瞬間=本体がブロックに触れた瞬間に発動する。これは方向非依存
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.collider);
        }

        // 念のためトリガー経由でも受ける(プレイヤー以外のトリガー子オブジェクトを後から付けた場合の保険)。
        // 同じHandleHitに委譲することで_oneShotガードと一貫性を保つ
        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other);
        }

        private void HandleHit(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (_oneShot && _hasFired) return;
            _hasFired = true;

            // 診断ログ: スロット割り当てを Console で即確認できるようにする。問題切り分け後は削除可
            Debug.Log($"[PunchBlocker] Fire on {name} / punch={(_punchSprite!=null)} hole={(_holeSprite!=null)} col={(_blockerCollider!=null)}", this);
            if (_punchSprite != null)
                Debug.Log($"[PunchBlocker]  punchSprite GO active={_punchSprite.gameObject.activeInHierarchy} hasSprite={_punchSprite.sprite!=null} sortingLayer={_punchSprite.sortingLayerName} order={_punchSprite.sortingOrder}", _punchSprite);
            if (_holeSprite != null)
                Debug.Log($"[PunchBlocker]  holeSprite  GO active={_holeSprite.gameObject.activeInHierarchy} hasSprite={_holeSprite.sprite!=null} sortingLayer={_holeSprite.sortingLayerName} order={_holeSprite.sortingOrder}", _holeSprite);

            FireAsync().Forget();
        }

        // _oneShot=false モード(出入りで切替)時の解除側も衝突終了で受ける
        private void OnCollisionExit2D(Collision2D collision)
        {
            if (_oneShot) return;
            if (!collision.collider.CompareTag("Player")) return;

            if (_punchSprite != null) _punchSprite.enabled = false;
            if (_holeSprite != null) _holeSprite.enabled = false;
            if (_blockerCollider != null) _blockerCollider.enabled = false;
        }

        /// <summary>
        /// パンチ + 穴 + コライダーを同時に出現させ、_punchDuration秒後にパンチだけ消す
        /// </summary>
        private async UniTaskVoid FireAsync()
        {
            // 즉時 ON
            if (_punchSprite != null) _punchSprite.enabled = true;
            if (_holeSprite != null) _holeSprite.enabled = true;
            if (_blockerCollider != null) _blockerCollider.enabled = true;

            // パンチ拡大→復帰アニメーション。前回が残っていれば止めてから新規再生
            PlayPunchScaleAnim();

            SafeSE.Play(_punchSEPath);

            // _punchDuration 後にパンチだけ非表示。穴とコライダーは残す
            await UniTask.Delay(
                (int)(_punchDuration * 1000),
                cancellationToken: destroyCancellationToken
            );
            if (_punchSprite != null) _punchSprite.enabled = false;
        }

        /// <summary>
        /// 元サイズ→拡大→元サイズの2フェーズシーケンスを作って再生する
        /// </summary>
        private void PlayPunchScaleAnim()
        {
            if (_punchSprite == null) return;

            // 前回の拡大アニメが残っていればkillしてリセット
            if (_punchScaleSeq != null && _punchScaleSeq.IsActive())
            {
                _punchScaleSeq.Kill();
                _punchScaleSeq = null;
            }

            var t = _punchSprite.transform;
            t.localScale = _punchOriginalScale;

            float outDur = _punchScaleDuration * 0.4f; // 拡大フェーズ短め
            float inDur = _punchScaleDuration * 0.6f;  // 復帰フェーズ長め(余韻)

            _punchScaleSeq = DOTween.Sequence()
                .Append(t.DOScale(_punchOriginalScale * _punchScaleMultiplier, outDur).SetEase(_punchScaleEaseOut))
                .Append(t.DOScale(_punchOriginalScale, inDur).SetEase(_punchScaleEaseIn))
                .SetLink(gameObject); // GameObject破棄でtweenも自動停止
        }

        /// <summary>
        /// 死亡リセット時などに外部から呼び出し、状態を初期化する
        /// </summary>
        public void ResetState()
        {
            _hasFired = false;

            // 進行中の拡大アニメがあれば止めて元サイズに戻す
            if (_punchScaleSeq != null && _punchScaleSeq.IsActive())
            {
                _punchScaleSeq.Kill();
                _punchScaleSeq = null;
            }
            if (_punchSprite != null) _punchSprite.transform.localScale = _punchOriginalScale;

            if (_punchSprite != null) _punchSprite.enabled = false;
            if (_holeSprite != null) _holeSprite.enabled = false;
            if (_blockerCollider != null) _blockerCollider.enabled = false;
        }
    }
}
