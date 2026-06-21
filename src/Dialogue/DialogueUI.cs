using Cysharp.Threading.Tasks;
using DG.Tweening;
using Febucci.TextAnimatorForUnity;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        /// <summary>
        /// ダイアログのUI表示を担当するコンポーネント
        /// テキストのタイピング演出、選択肢パネル、フェードイン/アウトを制御する
        /// 話者名は画像（立ち絵）側に焼き込むため、ここでは扱わない
        /// </summary>
        [SerializeField] private TextMeshProUGUI _dialogueText;     // セリフテキスト

        [SerializeField] private GameObject _choicePanel;           // 選択肢パネル
        [SerializeField] private Button _choiceButtonA;             // 選択肢Aボタン
        [SerializeField] private Button _choiceButtonB;             // 選択肢Bボタン
        [SerializeField] private Image _choiceImageA;               // 選択肢Aボタンのイメージ
        [SerializeField] private Image _choiceImageB;               // 選択肢Bボタンのイメージ
        [SerializeField] private TextMeshProUGUI _choiceTextA;      // 選択肢Aテキスト
        [SerializeField] private TextMeshProUGUI _choiceTextB;      // 選択肢Bテキスト

        [SerializeField] private TypewriterComponent _typewriter;    // タイピング処理

        [SerializeField] private Image _portraitImage;              // 立ち絵Image。CSVのImage列に応じてspriteを差し替える

        /// <summary>
        /// 立ち絵スプライトを差し替える。nullを渡すと現状維持（変更しない）
        /// RectTransformの位置・サイズはインスペクター設定をそのまま使う
        /// </summary>
        public void SetPortrait(Sprite sprite)
        {
            if (_portraitImage == null) return;
            if (sprite == null) return; // 空セル時は前の立ち絵をそのまま保持
            _portraitImage.sprite = sprite;
        }

        /// <summary>
        /// セリフを表示する
        /// タイピングアニメーション付きで1文字ずつ表示する
        /// </summary>
        public async UniTask ShowText(string text, CancellationToken token)
        {
            bool finished = false;
            _typewriter.onTextShowed.AddListener(() => finished = true);
            _typewriter.ShowText(text);

            try
            {
                // 入力が残らないように1フレーム待機
                await UniTask.Yield(cancellationToken: token);
                while (!finished)
                {
                    if (DialogueInputs.IsAdvancePressed())
                    {
                        _typewriter.SkipTypewriter();
                    }
                    await UniTask.Yield(cancellationToken: token);
                }
            }
            finally
            {
                _typewriter.onTextShowed.RemoveAllListeners();
            }
        }

        public void ClearText()
        {
            _typewriter.ShowText("");
        }

        /// <summary>
        /// 言語切替時に現在表示中のテキストを新しい言語で即座に差し替える
        /// タイプライター演出は省略して全文瞬時表示（既に読んでいる行を再タイピングしないため）
        /// </summary>
        public void RefreshLanguage(string newText)
        {
            if (_typewriter == null) return;
            _typewriter.ShowText(newText);
            _typewriter.SkipTypewriter();
        }

        /// <summary>
        /// テキストを即座に全部表示する（スキップ時に使用）
        /// </summary>
        public void SkipTyping()
        {
            _typewriter.SkipTypewriter();
        }

        /// <summary>
        /// 選択肢パネルを表示してボタンテキストをセットする
        /// </summary>
        public void ShowChoices(string choiceA, string choiceB)
        {
            // 選択肢パネルを表示
            _choicePanel.SetActive(true);

            // 選択肢テキストをセット
            _choiceTextA.text = choiceA;
            _choiceTextB.text = choiceB;
        }

        /// <summary>
        /// プレイヤーが選択肢を選ぶまで待機する
        /// 選ばれた選択肢のIDを返す
        /// </summary>
        public async UniTask<string> WaitForChoice(string choiceAId, string choiceBId, CancellationToken token)
        {
            int selected = 0; // 0 = A, 1 = B

            // 初期選択のハイライト表示
            UpdateChoiceHighlight(selected);

            await UniTask.WaitUntil(() =>
            {
                // 上下移動入力（パッドD-pad orキーボード矢印）
                if (IsChoiceMovePressed())
                {
                    selected = selected == 0 ? 1 : 0;
                    UpdateChoiceHighlight(selected);
                }

                // 決定入力（A/B or Z/Space/Enter）
                return DialogueInputs.IsAdvancePressed();

            }, cancellationToken: token);

            return selected == 0 ? choiceAId : choiceBId;
        }

        private void UpdateChoiceHighlight(int selected)
        {
            _choiceImageA.color = selected == 0 ? Color.yellow : Color.white;
            _choiceImageB.color = selected == 1 ? Color.yellow : Color.white;
        }

        /// <summary>
        /// 選択肢の上下移動入力：D-pad上下またはキーボード矢印上下
        /// </summary>
        private static bool IsChoiceMovePressed()
        {
            var gp = Gamepad.current;
            if (gp != null && (gp.dpad.up.wasPressedThisFrame || gp.dpad.down.wasPressedThisFrame))
                return true;

            var kb = Keyboard.current;
            if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame))
                return true;

            return false;
        }

        /// <summary>
        /// テキストウィンドウ全体をフェードインさせる演出
        /// DOTweenを使用してアルファ値を0から1に変化させる
        /// </summary>
        public async UniTask FadeIn(CanvasGroup canvasGroup, CancellationToken token)
        {
            // アルファを0にリセットしてからフェードイン開始
            canvasGroup.alpha = 0f;

            // DOTweenでアルファを0から1に0.3秒かけて変化させる
            // ToUniTaskでasync/awaitに対応させる
            await canvasGroup
                .DOFade(1f, 0.3f)
                .ToUniTask(cancellationToken: token);
        }

        /// <summary>
        /// テキストウィンドウ全体をフェードアウトさせる演出
        /// DOTweenを使用してアルファ値を1から0に変化させる
        /// </summary>
        public async UniTask FadeOut(CanvasGroup canvasGroup, CancellationToken token)
        {
            // DOTweenでアルファを1から0に0.3秒かけて変化させる
            // ToUniTaskでasync/awaitに対応させる
            await canvasGroup
                .DOFade(0f, 0.3f)
                .ToUniTask(cancellationToken: token);
        }

        /// <summary>
        /// 選択肢パネルを非表示にする
        /// </summary>
        public void HideChoices()
        {
            _choicePanel.SetActive(false);
        }
    }
}
