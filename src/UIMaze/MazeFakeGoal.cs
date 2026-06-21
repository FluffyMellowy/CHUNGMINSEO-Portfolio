using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using St;

namespace UIMaze
{
    public class MazeFakeGoal : MazeUIElement, IInit
    {
        [SerializeField] private TextAnimator_TMP _textAnimator;  // TextAnimator_TMP参照

        [SerializeField] private float _chaseRange = 150f;        // 追跡開始距離
        [SerializeField] private float _stopRange = 300f;         // 追跡停止距離
        [SerializeField] private float _chaseSpeed = 200f;        // 追跡速度

        [SerializeField] private RectTransform _shakeTarget; // ShakeTarget参照

        private RectTransform _cursorRect;                        // カーソルのRectTransform（キャッシュ）
        private bool _isChasing = false;                          // 追跡中フラグ
        private CancellationTokenSource _cts;                     // 非同期タスクのキャンセル用
        private Tween _shakeTween;                                // 振動Tweenのキャッシュ

        public async UniTask Init()
        {
            _textAnimator.SetText("Goal");
            _cts = new CancellationTokenSource();
            GlitchLoop(_cts.Token).Forget();
            await UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            _shakeTween?.Kill();
            _cts?.Cancel();
            _cts?.Dispose();
        }

        public void SetCursor(MazeCursor cursor)
        {
            _cursorRect = cursor.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_cursorRect == null) return;

            float distance = Vector2.Distance(_rect.anchoredPosition, _cursorRect.anchoredPosition);

            if (!_isChasing && distance < _chaseRange)
            {
                // 追跡開始
                StartChasing();
            }
            else if (_isChasing && distance > _stopRange)
            {
                // 追跡停止
                StopChasing();
            }

            // 追跡中はカーソルに向かって移動
            if (_isChasing)
            {
                _rect.anchoredPosition = Vector2.MoveTowards(
                    _rect.anchoredPosition,
                    _cursorRect.anchoredPosition,
                    _chaseSpeed * Time.deltaTime
                );
            }
        }

        private void StartChasing()
        {
            _isChasing = true;
            _cts?.Cancel();
            _textAnimator.SetText("<color=red>ERROR</color>");

            // ShakeTargetを振動させる
            _shakeTween = _shakeTarget.DOShakeAnchorPos(
                duration: 99f,
                strength: 15f,
                vibrato: 30,
                randomness: 90f
            ).SetLoops(-1);
        }

        private void StopChasing()
        {
            _isChasing = false;
            _shakeTween?.Kill();
            _shakeTarget.anchoredPosition = Vector2.zero; // ShakeTarget位置リセット
            _textAnimator.SetText("Goal");

            // グリッチループ再開
            _cts = new CancellationTokenSource();
            GlitchLoop(_cts.Token).Forget();
        }

        private async UniTaskVoid GlitchLoop(CancellationToken token)
        {
            while (true)
            {
                _textAnimator.SetText("Goal");
                await UniTask.Delay(UnityEngine.Random.Range(1000, 3000), cancellationToken: token);
                _textAnimator.SetText("Ǥ0@1");
                await UniTask.Delay(500, cancellationToken: token);
            }
        }
    }
}