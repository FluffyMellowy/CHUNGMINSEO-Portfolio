using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace UIMazeV2
{
    /// <summary>
    /// 3D背景内のモニターに寄っていく導入カメラ演出
    /// 開始ポーズ → 少し引いたポーズ → モニターを画面いっぱいに収める最終ポーズ、の順で位置と向きを同時補間する
    /// 各ポーズはシーン上の空のTransformで指定する（Sceneビューで位置と向きを直接配置可能）
    /// </summary>
    public class MonitorIntroCamera : MonoBehaviour
    {
        [Header("対象カメラ")]
        [SerializeField] private Transform _cameraTransform; // アニメーション対象のカメラTransform（未指定時はCamera.mainを使用）

        [Header("カメラポーズ")]
        [SerializeField] private Transform _startPose; // 開始時のポーズ（モニターから少し離れた位置）
        [SerializeField] private Transform _pullBackPose; // 一旦少し引いた中間ポーズ
        [SerializeField] private Transform _monitorClosePose; // モニターに寄って画面いっぱいにする最終ポーズ

        [Header("タイミング（秒）")]
        [SerializeField] private float _pullBackDuration = 0.6f; // 引きフェーズの所要時間
        [SerializeField] private float _holdBetween = 0.15f; // 引き終わりから接近開始までのタメ
        [SerializeField] private float _approachDuration = 1.4f; // 接近フェーズの所要時間
        [SerializeField] private float _holdAfter = 0.05f; // 到達後の余韻

        [Header("イージング")]
        [SerializeField] private Ease _pullBackEase = Ease.OutSine;
        [SerializeField] private Ease _approachEase = Ease.InOutCubic;

        [Header("制御")]
        [SerializeField] private bool _playOnStart = true; // Start時に自動再生するか
        [SerializeField] private MonoBehaviour[] _disableDuringIntro; // 導入中に無効化したいコンポーネント（プレイヤー操作系等）

        [Header("コールバック")]
        [SerializeField] private UnityEvent _onIntroComplete; // 導入完了時に呼ばれるイベント（ゲームプレイ開始の起点に使う）

        /// <summary>
        /// 導入演出再生中かどうか（外部から参照用）
        /// </summary>
        public bool IsPlaying { get; private set; }

        private void Start()
        {
            if (_playOnStart) PlayIntro().Forget();
        }

        /// <summary>
        /// 導入演出を再生する。多重呼び出しは無視される
        /// 完了時のみ_onIntroCompleteを通知し、キャンセル時には通知しない
        /// </summary>
        public async UniTask PlayIntro()
        {
            if (IsPlaying) return;

            // 対象カメラ：明示指定> Camera.mainの順で解決
            var cam = _cameraTransform != null
                ? _cameraTransform
                : (Camera.main != null ? Camera.main.transform : null);

            if (cam == null || _startPose == null || _pullBackPose == null || _monitorClosePose == null)
            {
                Debug.LogError("[MonitorIntroCamera] カメラまたはポーズの参照が不足しています");
                return;
            }

            IsPlaying = true;
            SetControllersEnabled(false);

            // 開始ポーズに即時スナップ
            cam.SetPositionAndRotation(_startPose.position, _startPose.rotation);

            var token = destroyCancellationToken;
            bool completed = false;

            try
            {
                // フェーズ1：引きの演出（位置と向きをSequenceで同時補間）
                var pullSeq = DOTween.Sequence();
                pullSeq.Join(cam.DOMove(_pullBackPose.position, _pullBackDuration).SetEase(_pullBackEase));
                pullSeq.Join(cam.DORotateQuaternion(_pullBackPose.rotation, _pullBackDuration).SetEase(_pullBackEase));
                await pullSeq.ToUniTask(cancellationToken: token);

                if (_holdBetween > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(_holdBetween), cancellationToken: token);

                // フェーズ2：接近してモニターを画面いっぱいにする
                var approachSeq = DOTween.Sequence();
                approachSeq.Join(cam.DOMove(_monitorClosePose.position, _approachDuration).SetEase(_approachEase));
                approachSeq.Join(cam.DORotateQuaternion(_monitorClosePose.rotation, _approachDuration).SetEase(_approachEase));
                await approachSeq.ToUniTask(cancellationToken: token);

                if (_holdAfter > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(_holdAfter), cancellationToken: token);

                completed = true;
            }
            catch (OperationCanceledException)
            {
                // 破棄等によるキャンセル時は最終ポーズに揃えずそのまま終了する（残ったTweenはGameObject破棄でDOTween側が片付ける）
            }
            finally
            {
                SetControllersEnabled(true);
                IsPlaying = false;
            }

            if (completed)
                _onIntroComplete?.Invoke();
        }

        /// <summary>
        /// _disableDuringIntroで指定されたコンポーネントの有効/無効を一括切り替えする
        /// </summary>
        private void SetControllersEnabled(bool value)
        {
            if (_disableDuringIntro == null) return;
            for (int i = 0; i < _disableDuringIntro.Length; i++)
            {
                var c = _disableDuringIntro[i];
                if (c != null) c.enabled = value;
            }
        }
    }
}
