namespace Colorless.UI
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// CmdLogPanel のタブコントローラ。ブラウザのタブのように
    /// 上部のタブを切替えると下部の対応する View が active になる。
    /// 任意のタブ数を扱えるよう設計。Input / History の 2 タブが初期構成。
    /// </summary>
    public sealed class CmdTabsUI : MonoBehaviour
    {
        [Serializable]
        public sealed class TabEntry
        {
            [InfoBox("API でタブを指定するための識別子（例: \"Input\", \"History\"）")]
            public string Id;

            [Required] public Button TabButton;
            [InfoBox("選択中ハイライト用 Image。未指定なら色操作なし。")]
            public Image TabBackground;
            public TextMeshProUGUI TabLabel;
            [Required] public GameObject View;
        }

        [Title("Tabs")]
        [SerializeField] private List<TabEntry> _tabs = new();
        [SerializeField, MinValue(0)] private int _defaultTabIndex = 0;

        [Title("Highlight")]
        [SerializeField] private Color _activeBackgroundColor = new(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color _inactiveBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.8f);
        [SerializeField] private Color _activeLabelColor = Color.white;
        [SerializeField] private Color _inactiveLabelColor = new(0.65f, 0.65f, 0.65f, 1f);

        private int _currentIndex = -1;

        private void Start()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i].TabButton == null) continue;
                int captured = i;
                _tabs[i].TabButton.onClick.AddListener(() => Select(captured));
            }
            int idx = Mathf.Clamp(_defaultTabIndex, 0, Mathf.Max(0, _tabs.Count - 1));
            Select(idx);
        }

        /// <summary>インデックス指定でタブ切替。</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            if (_currentIndex == index) return;
            _currentIndex = index;

            for (int i = 0; i < _tabs.Count; i++)
            {
                TabEntry t = _tabs[i];
                bool active = (i == index);

                if (t.View != null) t.View.SetActive(active);

                if (t.TabBackground != null)
                    t.TabBackground.color = active ? _activeBackgroundColor : _inactiveBackgroundColor;

                if (t.TabLabel != null)
                    t.TabLabel.color = active ? _activeLabelColor : _inactiveLabelColor;
            }
        }

        /// <summary>Id 指定でタブ切替（例: "Input", "History"）。</summary>
        public void SelectById(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < _tabs.Count; i++)
                if (_tabs[i].Id == id) { Select(i); return; }
        }

        public int CurrentIndex => _currentIndex;
    }
}
