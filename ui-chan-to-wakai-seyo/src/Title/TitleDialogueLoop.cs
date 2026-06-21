using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Febucci.TextAnimatorForUnity;
using St;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Title
{
    /// <summary>
    /// タイトル画面で指定範囲のCSV行（既定では1〜10）を順番に表示し、無限ループするコンポーネント
    /// 外部からRequestSkip()が呼ばれた時点で、現在行の次から1周分（行数- 1）を高速再生してから次シーンに遷移する
    /// 例: 6表示中にスキップ → 7,8,9,10,1,2,3,4,5を高速再生 → 遷移
    /// </summary>
    public class TitleDialogueLoop : MonoBehaviour, St.IInitializable
    {
        [Header("表示先")]
        [SerializeField] private TMP_Text _bodyText; // 本文を表示するTMP_Text（タイプライター未使用時のフォールバック）
        [Tooltip("DialogueScene と同じタイプライター演出で表示するためのTypewriterComponent。設定すると_bodyText より優先される")]
        [SerializeField] private TypewriterComponent _typewriter; // タイプライター演出（オプション）

        [Header("CSV / 表示ID範囲")]
        [SerializeField] private string _csvPath = "dialogue"; // Resources下のCSVパス（拡張子なし）
        [SerializeField] private int _startId = 1; // ループ対象の開始ID（含む）
        [SerializeField] private int _endId = 10; // ループ対象の終了ID（含む）

        [Header("再生速度")]
        [SerializeField] private float _normalDurationPerLine = 4.0f; // 通常再生時の1行あたり保持秒
        [SerializeField] private float _fastDurationPerLine = 0.35f; // 高速再生時の1行あたり保持秒

        [Header("高速再生中の演出（TextAnimatorタグ）")]
        [Tooltip("高速再生中のテキストをこのタグで包む。空文字なら無効。例: 'shake a=2' / 'rainb' / 'wave a=1'")]
        [SerializeField] private string _fastPlaybackTextTag = "shake a=1.5";

        [Header("Intro（高速再生後の自動再生フェーズ）")]
        [Tooltip("0以下ならIntroフェーズをスキップ。CSVは _csvPath と同じ")]
        [SerializeField] private int _introStartId = 0;
        [SerializeField] private int _introEndId = 0;
        [Tooltip("Introフェーズの1行あたり保持秒（オート進行のみ）")]
        [SerializeField] private float _introDurationPerLine = 3.0f;

        [Tooltip("ボイス再生がある行で、ボイス終了後に追加で待機する秒数。通常ループとIntro両方に適用される")]
        [SerializeField] private float _voiceTailPaddingSec = 1.0f;

        [Header("ボイス")]
        [Tooltip("ボイス再生用AudioSource（任意）。通常ループとIntroの両フェーズで使用、CSVのVoiceJP/VoiceENに下記prefixを付加してResourcesから読み込む")]
        [SerializeField] private AudioSource _voiceSource;
        [Tooltip("JPボイスファイルのResourcesパスprefix。例: 'Voice/JP/'")]
        [SerializeField] private string _voicePathPrefixJP = "Voice/JP/";
        [Tooltip("ENボイスファイルのResourcesパスprefix。例: 'Voice/EN/'")]
        [SerializeField] private string _voicePathPrefixEN = "Voice/EN/";

        [Header("立ち絵")]
        [Tooltip("立ち絵表示用Image（任意）。CSVのImage列に応じてspriteを差し替える")]
        [SerializeField] private Image _portraitImage;
        [Tooltip("立ち絵スプライトのResourcesパスprefix。CSVのImage列にはファイル名のみ記入（拡張子なし）")]
        [SerializeField] private string _imagePathPrefix = "Portraits/";

        [Header("遷移")]
        [Tooltip("Introフェーズ中の行にTrigger列が設定されていれば、このSectionTypeEventで通知する（DialogueManagerと同じ流儀）")]
        [SerializeField] private SectionTypeEvent _sectionTypeEvent;
        [Tooltip("単独テスト用フォールバック。Trigger通知が一度も発生しなかった時のみSceneManagerで直接ロード")]
        [SerializeField] private string _nextSceneName = "DialogueScene";
        [Tooltip("シーン遷移の瞬間に鳴らすSEパス(画面遷移音)。Trigger発火 or SceneManager.LoadScene直前で1回再生")]
        [SerializeField] private string _transitionSEPath = "SE_Chung/SE1";
        [SerializeField] private UnityEvent _onFinished; // 高速再生完了時のコールバック（遷移以外の追加処理用）
        [SerializeField] private UnityEvent _onSkipRequested; // スキップ受付時のコールバック（演出用 — TitleSkipButtonのOnClick等を繋ぐ）
        [Tooltip("高速再生フェーズが終了しIntroフェーズに入る瞬間に通知（グリッチ演出停止等のフック）")]
        [SerializeField] private UnityEvent _onFastPlaybackEnded;

        [Header("入力")]
        [Tooltip("ゲームパッドA/Bボタンでスキップを発動する")]
        [SerializeField] private bool _enableGamepadSkip = true;
        [Tooltip("キーボードSpace/Enterでもスキップを発動する")]
        [SerializeField] private bool _enableKeyboardSkip = true;
        [Tooltip("アトラクト動画再生中はスキップ入力を無効化する。割り当てない場合は無効化なし")]
        [SerializeField] private TitleAttractVideo _attractVideo;

        private readonly List<Dialogue.DialogueData> _lines = new List<Dialogue.DialogueData>();
        private CancellationTokenSource _lifeCts; // 本コンポーネントの寿命CTS
        private CancellationTokenSource _holdCts; // 通常再生時、各行の保持時間用CTS（スキップ要求でキャンセル）
        private bool _skipRequested;
        private bool _finished;
        private bool _triggerFired; // Introフェーズ中にTrigger通知を行ったか（フォールバックLoadSceneを抑制）
        private bool _initialized; // 二重初期化防止
        private Dialogue.DialogueData _currentLine; // 現在表示中の行（言語切替時に再描画するため保持）

        private void Start()
        {
            // 単独テスト用フォールバック。GameManager経由ならInitializeAsyncが先に呼ばれて_initialized=trueになっている
            if (!_initialized)
                InitializeAsync().Forget();
        }

        private void OnEnable()
        {
            // 言語切替時、現在表示中の行を新しい言語版で即座に再描画する
            if (Language.LanguageManager.Instance != null)
                Language.LanguageManager.Instance.OnLanguageChanged += OnLanguageChangedRefresh;
        }

        private void OnDisable()
        {
            if (Language.LanguageManager.Instance != null)
                Language.LanguageManager.Instance.OnLanguageChanged -= OnLanguageChangedRefresh;
        }

        /// <summary>
        /// LanguageManager.OnLanguageChanged 受信時、表示中の行を新言語版で再表示。
        /// タイプライターは新しい言語のテキストで再開（進行位置はリセットされるが、フォントと内容の不整合を防ぐ）
        /// </summary>
        [Header("言語別 TMP 設定（任意。空ならLocalizedFont/FontManager等の自動システム任せ）")]
        [Tooltip("JP表示時に強制適用するTMP_FontAsset")]
        [SerializeField] private TMPro.TMP_FontAsset _bodyFontJP;
        [Tooltip("EN表示時に強制適用するTMP_FontAsset")]
        [SerializeField] private TMPro.TMP_FontAsset _bodyFontEN;
        [Tooltip("JP表示時に強制適用するMaterial（アウトライン等込み）")]
        [SerializeField] private Material _bodyMaterialJP;
        [Tooltip("EN表示時に強制適用するMaterial")]
        [SerializeField] private Material _bodyMaterialEN;
        [Tooltip("JP表示時に強制適用するfontSize。0以下なら触らない")]
        [SerializeField] private float _bodyFontSizeJP = 0f;
        [Tooltip("EN表示時に強制適用するfontSize。0以下なら触らない")]
        [SerializeField] private float _bodyFontSizeEN = 0f;

        private void OnLanguageChangedRefresh(Language.LanguageManager.Language lang)
        {
            // _bodyTextのfont/materialを言語に合わせてここで一括差し替え。
            // 自動システム(LocalizedFont/FontManager)が後から上書きしないよう、
            // テキスト再描画(Display)より前にfont/materialを確定させる
            ApplyLanguageStyle(lang);

            if (_currentLine == null) return;
            // 高速再生フェーズ中の差し替えは演出を崩すため、通常再生中のみ反映
            Display(_currentLine, instant: false);
            // ボイスも新言語版に切り替え
            PlayVoiceFor(_currentLine);
        }

        /// <summary>
        /// _bodyTextのfont/material/fontSizeを直接書き換える。他コンポーネントに依存しない最終手段。
        /// 比較ガード付きで同じ値の場合はset呼び出しを省略する（無駄なTMPダーティ通知を防ぐ）
        /// </summary>
        private void ApplyLanguageStyle(Language.LanguageManager.Language lang)
        {
            if (_bodyText == null) return;
            bool isEN = lang == Language.LanguageManager.Language.EN;
            var font = isEN ? _bodyFontEN : _bodyFontJP;
            var mat = isEN ? _bodyMaterialEN : _bodyMaterialJP;
            var size = isEN ? _bodyFontSizeEN : _bodyFontSizeJP;
            if (font != null && _bodyText.font != font) _bodyText.font = font;
            if (mat != null && _bodyText.fontSharedMaterial != mat) _bodyText.fontSharedMaterial = mat;
            // fontSize は 0 以下なら触らない（LocalizedFontSize 等の他システムに任せる）
            if (size > 0f && !Mathf.Approximately(_bodyText.fontSize, size))
                _bodyText.fontSize = size;
        }

        /// <summary>
        /// LateUpdate で毎フレーム ApplyLanguageStyle を呼ぶ。
        /// OnLanguageChangedRefresh / Display での適用が同一フレーム内で他システムや Febucci の
        /// mesh rebuild に巻き戻される事象（特に切替直後の最初の1行）の保険として常時同期させる。
        /// 比較ガード付きなので、値が同じ間は実setは走らずコストは比較のみ
        /// </summary>
        private void LateUpdate()
        {
            if (_bodyText == null) return;
            if (_finished) return;
            var lang = Language.LanguageManager.Instance != null
                ? Language.LanguageManager.Instance.CurrentLanguage
                : Language.LanguageManager.Language.JP;
            ApplyLanguageStyle(lang);
        }

        /// <summary>
        /// GameManager.ExecuteInitializingから呼ばれる統一初期化エントリポイント
        /// CSVをロードしてループを開始する
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            LoadLines();
            if (_lines.Count == 0)
            {
                Debug.LogError("[TitleDialogueLoop] 表示対象の行がありません");
                return;
            }
            // 初期言語に対応するfont/material/fontSizeを最初に強制適用しておく。
            // これにより最初のJP表示時点でインスペクター値が反映され、ループ開始後の見た目が一定になる
            var initialLang = Language.LanguageManager.Instance != null
                ? Language.LanguageManager.Instance.CurrentLanguage
                : Language.LanguageManager.Language.JP;
            ApplyLanguageStyle(initialLang);

            _lifeCts = new CancellationTokenSource();
            RunLoop(_lifeCts.Token).Forget();

            await UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            _lifeCts?.Cancel();
            _lifeCts?.Dispose();
            _holdCts?.Dispose();
            StopVoice();
        }

        /// <summary>
        /// 毎フレーム、ゲームパッドのA/Bボタンまたはキーボードのスペース/EnterでRequestSkipを呼ぶ
        /// 多重呼び出しはRequestSkip側のガードで弾かれる
        /// </summary>
        private void Update()
        {
            if (_skipRequested || _finished) return;

            // アトラクト動画再生中は入力をスキップ判定に流さない。
            // この間の入力はTitleAttractVideo側が「動画を閉じてタイトル復帰」用に消費する
            if (_attractVideo != null && _attractVideo.IsPlaying) return;

            if (_enableGamepadSkip && Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    RequestSkip();
                    return;
                }
            }

            if (_enableKeyboardSkip && Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    RequestSkip();
                }
            }
        }

        /// <summary>
        /// 指定IDの範囲をDialogueLoader経由で読み込み、_linesに格納する
        /// 見つからないIDはWarningだけ出してスキップする
        /// </summary>
        private void LoadLines()
        {
            var loader = new Dialogue.DialogueLoader();
            var table = loader.Load(_csvPath);
            for (int i = _startId; i <= _endId; i++)
            {
                if (table.TryGetValue(i.ToString(), out var data))
                    _lines.Add(data);
                else
                    Debug.LogWarning($"[TitleDialogueLoop] ID '{i}' がCSVに見つかりません");
            }
        }

        /// <summary>
        /// 通常ループ → スキップ受付 → 高速1周 → 次シーン遷移という流れを担当するメインループ
        /// </summary>
        private async UniTask RunLoop(CancellationToken token)
        {
            int index = 0;

            // 通常再生：スキップ要求が来るまで_linesを巡回
            // ボイスがあればクリップ長+ 1秒、なければ_normalDurationPerLineをフォールバック
            while (!_skipRequested && !token.IsCancellationRequested)
            {
                Display(_lines[index], instant: false);
                float voiceLen = PlayVoiceFor(_lines[index]);
                float hold = voiceLen > 0f ? voiceLen + _voiceTailPaddingSec : _normalDurationPerLine;
                await Hold(hold, token);
                if (_skipRequested) break;
                index = (index + 1) % _lines.Count;
            }

            if (token.IsCancellationRequested) return;

            // 高速再生：現在の次の行から開始し、(行数- 1)行を瞬時表示でめくる（ボイスは混ざるので止める）
            // shakeタグ等を被せてグリッチ感を出す
            StopVoice();
            int fastStart = (index + 1) % _lines.Count;
            for (int i = 0; i < _lines.Count - 1; i++)
            {
                int target = (fastStart + i) % _lines.Count;
                Display(_lines[target], instant: true, wrapWithFastTag: true);
                await UniTask.Delay(TimeSpan.FromSeconds(_fastDurationPerLine), cancellationToken: token);
            }

            // 高速再生フェーズ完了通知（グリッチ等の演出フック）
            _onFastPlaybackEnded?.Invoke();

            // Introフェーズ（オート再生のみ、スキップ受付なし — _skipRequested=trueのままなのでUpdateも反応しない）
            if (_introStartId > 0 && _introEndId >= _introStartId)
                await RunIntroAsync(token);

            _finished = true;
            _onFinished?.Invoke();

            // Trigger通知を行った場合は外部の遷移システムに任せる。フォールバックは未通知時のみ
            if (!_triggerFired && !string.IsNullOrEmpty(_nextSceneName))
                SceneManager.LoadScene(_nextSceneName);
        }

        /// <summary>
        /// 高速再生後のIntroフェーズ：指定範囲を1行ずつタイプライターで表示し_introDurationPerLineで自動進行
        /// ボイスSourceが設定されていれば各行のボイスも再生する
        /// </summary>
        private async UniTask RunIntroAsync(CancellationToken token)
        {
            var loader = new Dialogue.DialogueLoader();
            var table = loader.Load(_csvPath);

            for (int id = _introStartId; id <= _introEndId; id++)
            {
                if (token.IsCancellationRequested) return;

                if (!table.TryGetValue(id.ToString(), out var data))
                {
                    Debug.LogWarning($"[TitleDialogueLoop] Intro ID '{id}' がCSVに見つかりません");
                    continue;
                }

                Display(data, instant: false);
                float voiceLen = PlayVoiceFor(data);
                float hold = voiceLen > 0f ? voiceLen + _voiceTailPaddingSec : _introDurationPerLine;
                await UniTask.Delay(TimeSpan.FromSeconds(hold), cancellationToken: token);

                // Trigger列が設定されていればSectionTypeEventで通知し、即時終了（外部が遷移を担当）
                if (data.HasTrigger)
                {
                    FireTrigger(data.Trigger);
                    _triggerFired = true;
                    StopVoice();
                    return;
                }
            }

            StopVoice();
        }

        /// <summary>
        /// CSVのTriggerをSectionTypeEventで通知する。DialogueManager.FireTriggerと同じマッピング
        /// </summary>
        private void FireTrigger(Dialogue.TriggerType trigger)
        {
            if (_sectionTypeEvent == null)
            {
                Debug.LogWarning("[TitleDialogueLoop] SectionTypeEventが未設定。Trigger通知できません");
                return;
            }
            switch (trigger)
            {
                case Dialogue.TriggerType.FinishTitleScene:
                    _sectionTypeEvent.Raise(SectionType.Title);
                    break;
                case Dialogue.TriggerType.FinishDialogueScene1:
                    _sectionTypeEvent.Raise(SectionType.UIMaze_Dialogue);
                    break;
                case Dialogue.TriggerType.FinishDialogueScene2:
                    _sectionTypeEvent.Raise(SectionType.Action_Dialogue);
                    break;
            }
        }

        /// <summary>
        /// 行のボイスを再生する（通常ループ・Intro両方で使用）。前のボイスは停止して差し替え
        /// CSVには拡張子なしのファイル名だけ入っている前提で、言語別prefixを付加して読み込む
        /// </summary>
        /// <returns>再生したクリップの長さ（秒）。ボイスなし/読み込み失敗時は0</returns>
        private float PlayVoiceFor(Dialogue.DialogueData data)
        {
            StopVoice();
            if (_voiceSource == null || data == null) return 0f;

            string filename = data.GetVoice();
            if (string.IsNullOrEmpty(filename)) return 0f;

            string prefix = (Language.LanguageManager.Instance != null
                && Language.LanguageManager.Instance.CurrentLanguage == Language.LanguageManager.Language.EN)
                ? _voicePathPrefixEN
                : _voicePathPrefixJP;
            string path = prefix + filename;

            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[TitleDialogueLoop] ボイスが見つかりません: {path}");
                return 0f;
            }
            _voiceSource.clip = clip;
            _voiceSource.Play();
            return clip.length;
        }

        private void StopVoice()
        {
            if (_voiceSource == null) return;
            if (_voiceSource.isPlaying) _voiceSource.Stop();
        }

        /// <summary>
        /// 1行のテキストを画面に反映する
        /// Typewriterがあればタイプライター演出（既存テキストはShowTextが自動でクリア）
        /// なければTMP_Textに直接代入
        /// </summary>
        /// <param name="instant">true時は瞬時表示（高速再生フェーズ用）</param>
        /// <param name="wrapWithFastTag">true時は_fastPlaybackTextTagでテキスト全体を包む（演出用）</param>
        private void Display(Dialogue.DialogueData data, bool instant = false, bool wrapWithFastTag = false)
        {
            // 言語切替時の再描画用に現在行を記録
            _currentLine = data;
            ApplyImageFor(data);

            string text = data.GetText();

            if (wrapWithFastTag && !string.IsNullOrEmpty(_fastPlaybackTextTag))
            {
                // TextAnimatorタグの仕様:開始タグはパラメータ込み、終了タグはタグ名のみ
                string tagName = _fastPlaybackTextTag.Split(' ')[0];
                text = $"<{_fastPlaybackTextTag}>{text}</{tagName}>";
            }

            if (_typewriter != null)
            {
                _typewriter.ShowText(text);
                if (instant) _typewriter.SkipTypewriter();
            }
            else if (_bodyText != null)
            {
                _bodyText.text = text;
            }

            // Febucci Typewriter/TMP の mesh rebuild時に font/material/fontSize がリセットされる
            // ケースがあるため、ShowText / text 設定後に毎回 ApplyLanguageStyle を強制再適用する。
            // これにより2行目以降に fontSize や Material が消える事故を防ぐ
            var lang = Language.LanguageManager.Instance != null
                ? Language.LanguageManager.Instance.CurrentLanguage
                : Language.LanguageManager.Language.JP;
            ApplyLanguageStyle(lang);
        }

        /// <summary>
        /// 行のImage列に応じて立ち絵スプライトを差し替える
        /// 空セルなら何もしない（前の立ち絵を保持）。DialogueManager.ApplyImageForと同じ流儀
        /// </summary>
        private void ApplyImageFor(Dialogue.DialogueData data)
        {
            if (data == null)
            {
                Debug.Log("[TitleDialogueLoop.ApplyImageFor] data == null, skip");
                return;
            }
            if (_portraitImage == null)
            {
                Debug.LogWarning($"[TitleDialogueLoop.ApplyImageFor] _portraitImage slot not assigned (ID={data.Id}, Image='{data.Image}')");
                return;
            }
            if (string.IsNullOrEmpty(data.Image))
            {
                Debug.Log($"[TitleDialogueLoop.ApplyImageFor] ID={data.Id} の Image 列が空。現状維持");
                return;
            }

            string path = _imagePathPrefix + data.Image;
            Debug.Log($"[TitleDialogueLoop.ApplyImageFor] ID={data.Id} loading: {path}");
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[TitleDialogueLoop] 立ち絵スプライトが見つかりません: {path} (ID={data.Id})");
                return;
            }

            _portraitImage.sprite = sprite;
            Debug.Log($"[TitleDialogueLoop.ApplyImageFor] Applied sprite '{sprite.name}' on _portraitImage='{_portraitImage.name}'");
        }

        /// <summary>
        /// 通常再生中の保持待機
        /// _holdCtsを_lifeCtsとリンクして毎行作り直すことで、RequestSkip呼び出しで即座に抜けられるようにする
        /// </summary>
        private async UniTask Hold(float duration, CancellationToken life)
        {
            _holdCts?.Dispose();
            _holdCts = CancellationTokenSource.CreateLinkedTokenSource(life);
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: _holdCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 寿命CTSが死んでいる場合だけ伝播。スキップ起因のキャンセルは握りつぶして高速再生に進む
                if (life.IsCancellationRequested) throw;
            }
        }

        /// <summary>
        /// 外部（スキップボタン等）から呼ばれ、通常再生を即時中断して高速再生フェーズへ移行させる
        /// 多重呼び出しおよび完了後の呼び出しは無視される
        /// </summary>
        public void RequestSkip()
        {
            Debug.Log($"[TitleDialogueLoop] RequestSkip called. skipRequested={_skipRequested}, finished={_finished}, onSkipRequested listeners={_onSkipRequested?.GetPersistentEventCount() ?? 0}");
            if (_skipRequested || _finished) return;
            _skipRequested = true;
            _holdCts?.Cancel();
            // 画面遷移音(SE1)。ボタン押下→グリッチ→大セリフスキップ演出開始の瞬間に1回だけ再生
            SafeSE.Play(_transitionSEPath);
            _onSkipRequested?.Invoke();
            Debug.Log("[TitleDialogueLoop] _onSkipRequested.Invoke() done");
        }
    }
}
