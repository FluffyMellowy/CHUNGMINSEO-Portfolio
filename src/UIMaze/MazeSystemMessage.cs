using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace UIMaze
{
    public class MazeSystemMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text; // メッセージテキスト参照
        [SerializeField] private int _maxLines = 5;     // 最大表示行数
        private Queue<string> _messageQueue = new Queue<string>(); // メッセージキュー
        private List<string> _activeMessages = new List<string>(); // 表示中メッセージ
        private CancellationTokenSource _cts;           // 非同期タスクのキャンセル用

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        public void ShowMessage(string message)
        {
            _messageQueue.Enqueue(message);
            ProcessQueue().Forget();
        }

        private async UniTaskVoid ProcessQueue()
        {
            while (_messageQueue.Count > 0)
            {
                string message = _messageQueue.Dequeue();

                // 最大行数超えたら古いメッセージを削除
                if (_activeMessages.Count >= _maxLines)
                    _activeMessages.RemoveAt(0);

                _activeMessages.Add(message);
                UpdateDisplay();

                // 2秒後にこのメッセージを削除
                await UniTask.Delay(2000);
                _activeMessages.Remove(message);
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            _text.text = string.Join("\n", _activeMessages);
            _text.gameObject.SetActive(_activeMessages.Count > 0);
        }
    }
}