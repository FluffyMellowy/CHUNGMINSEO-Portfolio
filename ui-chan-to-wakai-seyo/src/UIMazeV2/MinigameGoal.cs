using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム1・2のゴール判定
    /// プレイヤーが触れると演出（グリッチ等）を通知してから一定時間後に次のミニゲームへ遷移する
    /// </summary>
    public class MinigameGoal : MonoBehaviour
    {
        [SerializeField] private WindowManager _windowManager; // ウィンドウ遷移管理

        [Header("プレイヤー停止")]
        [Tooltip("到達時に無効化するTopViewPlayer（ミニゲーム1用）")]
        [SerializeField] private TopViewPlayer _topViewPlayer;
        [Tooltip("到達時に無効化するPlatformerPlayerA（ミニゲーム2A用）")]
        [SerializeField] private PlatformerPlayerA _platformerPlayerA;
        [Tooltip("到達時に無効化するPlatformerPlayerB（ミニゲーム2B用）")]
        [SerializeField] private PlatformerPlayerB _platformerPlayerB;
        [Tooltip("到達時に物理シミュレーションを止めるプレイヤーRigidbody2D（速度ゼロ + simulated=false）")]
        [SerializeField] private Rigidbody2D _playerRigidbody;

        [Header("ディゾルブ演出（上から消えて、次のミニゲームで上から現れる）")]
        [SerializeField] private PlayerDissolveEffect _playerDissolve;

        [Header("演出")]
        [Tooltip("ゴール到達時に追加で通知する演出（任意）")]
        [SerializeField] private UnityEvent _onReached;
        [Tooltip("ディゾルブIN完了後にプレイヤー操作を再開するまでの追加待機")]
        [SerializeField] private float _postInExtraDelay = 0.1f;

        private bool _isReached; // 到達済みフラグ

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isReached) return;
            if (!other.CompareTag("Player")) return;

            _isReached = true;

            // プレイヤー停止：登録された各コントローラーを無効化、Rigidbody2Dの動きも止める
            if (_topViewPlayer != null) _topViewPlayer.enabled = false;
            if (_platformerPlayerA != null) _platformerPlayerA.enabled = false;
            if (_platformerPlayerB != null) _platformerPlayerB.enabled = false;
            if (_playerRigidbody != null)
            {
                _playerRigidbody.linearVelocity = Vector2.zero;
                _playerRigidbody.angularVelocity = 0f;
                _playerRigidbody.simulated = false;
            }

            _onReached?.Invoke();

            RunSequenceAsync().Forget();
        }

        /// <summary>
        /// ディゾルブOUT → ウィンドウ切替 → ディゾルブIN → 物理再開、の順に実行
        /// アクティブなコントローラー切替はWindowManagerが行うので、ここでは触らない
        /// try-finallyで、どんな経路で抜けても最後に必ずsimulated=trueへ戻す
        /// （await中のキャンセル等でsimulated=falseのまま残り、プレイヤーが宙に浮き続けるバグ防止）
        /// </summary>
        private async UniTaskVoid RunSequenceAsync()
        {
            var token = destroyCancellationToken;

            try
            {
                // 1)上から消える
                float outDuration = 0f;
                if (_playerDissolve != null)
                {
                    _playerDissolve.PlayDissolveOut();
                    outDuration = _playerDissolve.OutDuration;
                }
                if (outDuration > 0f)
                    await UniTask.Delay((int)(outDuration * 1000), cancellationToken: token);

                // 2)次のウィンドウへ（位置移動+該当コントローラー有効化はWindowManagerが処理）
                if (_windowManager != null) _windowManager.NextWindow();

                // 1フレーム待ってからRigidbody2Dを再凍結（WindowManager側のOnEnableがsimulated=trueにするため上書き）
                await UniTask.Yield(token);
                if (_playerRigidbody != null)
                {
                    _playerRigidbody.linearVelocity = Vector2.zero;
                    _playerRigidbody.angularVelocity = 0f;
                    _playerRigidbody.simulated = false;
                }

                // 3)上から現れる（凍結状態のまま）
                float inDuration = 0f;
                if (_playerDissolve != null)
                {
                    _playerDissolve.PlayDissolveIn();
                    inDuration = _playerDissolve.InDuration;
                }
                if (inDuration > 0f)
                    await UniTask.Delay((int)(inDuration * 1000), cancellationToken: token);

                // 4)余韻 → 物理シミュレーション再開（重力は現在アクティブなコントローラーが既に設定済み）
                if (_postInExtraDelay > 0f)
                    await UniTask.Delay((int)(_postInExtraDelay * 1000), cancellationToken: token);

                // _playerControllerはあえて触らない（WindowManagerが正しいコントローラーを既に有効化済み）
            }
            finally
            {
                // 演出中のawaitがキャンセル/例外で中断されても、最終的に必ず物理シミュレーションを再開する
                // ここを抜けないと、プレイヤーがsimulated=falseのまま空中に固定されてリトライ不能になる
                if (_playerRigidbody != null) _playerRigidbody.simulated = true;
            }
        }
    }
}