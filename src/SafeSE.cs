using System.Collections.Generic;
using KanKikuchi.AudioManager;
using UnityEngine;

/// <summary>
/// SEManager.Instance.Play(path) のラッパー。
/// パスが Resources にロード可能か初回呼び出し時に確認し、未登録なら以降スキップする。
/// これにより SE 未登録による LogError の大量出力や、見つからない AudioClip を渡し続ける
/// パフォーマンスロスを防ぐ。CHUNG 領域の SE 呼び出しは全てここを経由させる方針。
/// </summary>
public static class SafeSE
{
    // ロード試行に失敗したパスをキャッシュして二度目以降は即スキップ
    private static readonly HashSet<string> _missingPaths = new HashSet<string>();

    /// <summary>
    /// SEを再生する。空パス・未登録パスは安全にスキップする。
    /// </summary>
    public static void Play(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (_missingPaths.Contains(path)) return;

        // 初回チェック：Resources に存在しないクリップはここで弾いて記録、以降は即return
        if (Resources.Load<AudioClip>(path) == null)
        {
            _missingPaths.Add(path);
            return;
        }

        SEManager.Instance.Play(path);
    }
}
