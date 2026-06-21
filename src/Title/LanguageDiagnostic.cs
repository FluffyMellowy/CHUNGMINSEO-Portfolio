using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Language
{
    /// <summary>
    /// 言語が想定外にENへ切り替わる不具合の追跡用ロガー。
    /// ビルドのexeと同じフォルダ（Application.dataPathの親）にテキストファイルを追記する。
    /// テスターはビルドを配布されたフォルダをそのまま開けばログが見える。
    /// 出力先:
    ///   ビルド: &lt;buildFolder&gt;/language_diag.log  (exeと同じ階層)
    ///   Editor: &lt;projectRoot&gt;/language_diag.log  (Assets と同じ階層)
    /// 不具合が再現したらこのファイルを回収して原因のスタックを確認する
    /// </summary>
    public static class LanguageDiagnostic
    {
        private const string FileName = "language_diag.txt";
        private static string _cachedPath;
        private static bool _headerWritten;

        /// <summary>
        /// 出力先フルパスを返す（必要なら遅延でキャッシュ）。
        /// Application.dataPath はメインスレッドからのみ呼べる点に注意
        /// </summary>
        public static string GetLogPath()
        {
            if (string.IsNullOrEmpty(_cachedPath))
            {
                // Application.dataPath:
                //   Editor    -> <project>/Assets
                //   Windows   -> <game>_Data
                // どちらも 1階層上がるとビルド/プロジェクトのルートになる。そこに置けば exeと並べて配布できる
                string dir = Path.GetDirectoryName(Application.dataPath);
                _cachedPath = Path.Combine(dir, FileName);
            }
            return _cachedPath;
        }

        /// <summary>
        /// 1行ログを追記する。先頭タイムスタンプ＋呼び出し元タグ＋メッセージの形式。
        /// captureStackTrace=trueでスタックトレースも続けて書く。Set/Awake追跡用
        /// </summary>
        public static void Log(string tag, string message, bool captureStackTrace = false)
        {
            try
            {
                string path = GetLogPath();

                if (!_headerWritten)
                {
                    _headerWritten = true;
                    File.AppendAllText(path,
                        $"===== LanguageDiagnostic start ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) =====\n" +
                        $"Unity={Application.unityVersion}, Platform={Application.platform}, dataPath={Application.persistentDataPath}\n\n");
                }

                var sb = new StringBuilder();
                sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("] ");
                sb.Append('[').Append(tag).Append("] ");
                sb.Append(message);
                sb.Append('\n');

                if (captureStackTrace)
                {
                    // System.Environment.StackTraceは深め。先頭数フレームのみ取り出して書く
                    var raw = Environment.StackTrace;
                    sb.Append(raw);
                    sb.Append("\n");
                }

                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception e)
            {
                // ログ自体の失敗で本処理を止めないようにDebug.LogWarningで握りつぶす
                Debug.LogWarning($"[LanguageDiagnostic] failed to write: {e.Message}");
            }
        }
    }
}
