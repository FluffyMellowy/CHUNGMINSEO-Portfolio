using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Dialogue
{
    /// <summary>
    /// ダイアログのオート・スキップモードの状態と設定値を管理する
    /// 両モードは排他的で同時に有効にならない
    /// </summary>
    public class DialogueSettings : MonoBehaviour
    {
        public bool IsAuto { get; private set; } = false;
        public bool IsSkip { get; private set; } = false;

        [SerializeField] private float _autoInterval = 2.0f;  // AUTO時の待機時間（秒）
        [SerializeField] private float _skipInterval = 0.01f;  // スキップ時のテキスト表示速度（秒）

        public float AutoInterval => _autoInterval;
        public float SkipInterval => _skipInterval;

        // オートON → スキップOFF（排他制御）
        public void ToggleAuto()
        {
            IsAuto = !IsAuto;
            if (IsAuto) IsSkip = false;
            Debug.Log($"AutoMode: {IsAuto}");
        }

        // スキップON → オートOFF（排他制御）
        public void ToggleSkip()
        {
            IsSkip = !IsSkip;
            if (IsSkip) IsAuto = false;
            Debug.Log($"SkipMode: {IsSkip}");
        }

        // AUTO時の待機処理。_autoInterval秒後に次へ進む
        public async UniTask WaitAuto(CancellationToken token)
        {
            await UniTask.Delay(
                (int)(_autoInterval * 1000),
                cancellationToken: token
            );
        }
    }
}