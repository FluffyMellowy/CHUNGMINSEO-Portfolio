using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dialogue
{
    /// <summary>
    /// Resources内のCSVファイルを読み込み、ID→DialogueDataの辞書として返すローダー
    /// CSVの1行目はヘッダーとしてスキップし、2行目以降をパースする
    /// </summary>
    public class DialogueLoader
    {
        /// <summary>
        /// 指定パスのCSVを読み込んでDialogueDataの辞書を返す
        /// 読み込み失敗時は空の辞書を返す
        /// </summary>
        public Dictionary<string, DialogueData> Load(string csvPath)
        {
            var table = new Dictionary<string, DialogueData>();

            TextAsset csvFile = Resources.Load<TextAsset>(csvPath);
            if (csvFile == null)
            {
                Debug.LogError($"CSV not found: {csvPath}");
                return table;
            }

            // 引用符内の改行を尊重して論理行に分割する
            List<string> logicalLines = SplitCsvIntoLogicalRows(csvFile.text);

            // 1行目はヘッダーなのでスキップ
            for (int i = 1; i < logicalLines.Count; i++)
            {
                string line = logicalLines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                DialogueData data = ParseLine(line);
                if (data == null) continue;
                if (string.IsNullOrEmpty(data.Id)) continue; // ID未指定行はスキップ
                table[data.Id] = data;
            }

            return table;
        }

        /// <summary>
        /// 引用符のバランスを保ったままCSV全文を行単位に分割する
        /// 引用符内の \r\n / \nはセル値の一部として保持し、行区切りに使わない
        /// </summary>
        private List<string> SplitCsvIntoLogicalRows(string text)
        {
            var rows = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    // 行区切り（引用符外のみ）。CRLFはまとめて1区切り扱い
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    rows.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            if (current.Length > 0) rows.Add(current.ToString());
            return rows;
        }

        /// <summary>
        /// CSV1行をDialogueDataに変換する
        /// 列順: Id, Speaker(未使用), TextJP, TextEN, NextId, ChoiceA, ChoiceAId, ChoiceB, ChoiceBId, VoiceJP, VoiceEN, Image, Trigger
        /// 話者名は画像側に焼き込むため列としては残すがパースしない
        /// 列数が9未満の場合はnullを返す（後方の列は欠けても動く）
        /// </summary>
        private DialogueData ParseLine(string line)
        {
            List<string> cols = ParseCsvColumns(line);
            if (cols.Count < 9) return null;

            return new DialogueData
            {
                Id = cols[0].Trim(),
                // cols[1] = Speaker（未使用）
                TextJP = cols[2].Trim().Replace("\\n", "\n"),
                TextEN = cols[3].Trim().Replace("\\n", "\n"),
                NextId = cols[4].Trim(),
                ChoiceA = cols[5].Trim(),
                ChoiceAId = cols[6].Trim(),
                ChoiceB = cols[7].Trim(),
                ChoiceBId = cols[8].Trim(),
                VoiceJP = cols.Count > 9 ? cols[9].Trim() : string.Empty,
                VoiceEN = cols.Count > 10 ? cols[10].Trim() : string.Empty,
                Image = cols.Count > 11 ? cols[11].Trim() : string.Empty,
                Trigger = cols.Count > 12 && Enum.TryParse(cols[12].Trim(), out TriggerType t) ? t : TriggerType.None,
            };
        }

        /// <summary>
        /// ダブルクオートで囲まれたカンマを正しく処理するCSVパーサー
        /// </summary>
        private List<string> ParseCsvColumns(string line)
        {
            var cols = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    cols.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            cols.Add(current.ToString());
            return cols;
        }
    }
}