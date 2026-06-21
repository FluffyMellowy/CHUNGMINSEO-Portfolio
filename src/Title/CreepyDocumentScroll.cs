using Cysharp.Threading.Tasks;
using Febucci.TextAnimatorForUnity;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Title
{
    /// <summary>
    /// タイトル画面用・怪文書スクロール演出
    /// TextAnimator_TMPのAppendTextで1文字ずつ追加する（既存文字は再アニメーションされない）
    /// TMPのサイズをオーバーフローしたら先頭の文字を削除して継続する
    /// </summary>
    public class CreepyDocumentScroll : MonoBehaviour
    {
        [Header("テキスト参照")]
        [SerializeField] private TMP_Text _text; // 表示用TMP
        [SerializeField] private TextAnimatorComponentBase _textAnimator; // TMPと同じGameObjectのTextAnimator（_TMPの基底クラス）

        [Header("行データ")]
        [TextArea(1, 3)]
        [SerializeField] private string[] _lines; // 表示する行のプール
        [SerializeField] private bool _randomOrder = true; // ランダム順序で選ぶ

        [Header("タイミング")]
        [SerializeField] private float _charInterval = 0.05f; // 1文字あたりのタイピング間隔（秒）
        [SerializeField] private float _lineInterval = 0.6f; // 行と行の間の待機（秒）

        private CancellationTokenSource _cts;
        private readonly StringBuilder _buffer = new();
        private int _sequentialIndex;

        private void OnEnable()
        {
            if (_lines == null || _lines.Length == 0) return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _buffer.Clear();
            _textAnimator.SetText(string.Empty, false);

            RunAsync(_cts.Token).Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// 行を順次タイピング→次行、を無限ループする
        /// </summary>
        private async UniTaskVoid RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string line = GetNextLine();

                // 1文字ずつAppendText（既存文字は再アニメーションされない）
                for (int i = 0; i < line.Length; i++)
                {
                    string ch = line[i].ToString();
                    _buffer.Append(ch);
                    _textAnimator.AppendText(ch, false, true);

                    TrimFrontIfOverflowing();

                    await UniTask.Delay((int)(_charInterval * 1000), cancellationToken: token);
                }

                // 行末に改行を追加
                _buffer.Append('\n');
                _textAnimator.AppendText("\n", false, true);
                TrimFrontIfOverflowing();

                await UniTask.Delay((int)(_lineInterval * 1000), cancellationToken: token);
            }
        }

        /// <summary>
        /// TMPがオーバーフローしている間、バッファの先頭から1文字ずつ削除して反映する
        /// </summary>
        private void TrimFrontIfOverflowing()
        {
            if (_text == null) return;

            _text.ForceMeshUpdate();
            if (!_text.isTextOverflowing) return;

            int safety = 0;
            while (_text.isTextOverflowing && _buffer.Length > 0 && safety < 5000)
            {
                _buffer.Remove(0, 1);
                safety++;
                _textAnimator.SetText(_buffer.ToString(), false);
                _text.ForceMeshUpdate();
            }
        }

        /// <summary>
        /// ランダムまたは順次で次の行を返す
        /// </summary>
        private string GetNextLine()
        {
            if (_randomOrder)
                return _lines[Random.Range(0, _lines.Length)];
            return _lines[_sequentialIndex++ % _lines.Length];
        }
    }
}
