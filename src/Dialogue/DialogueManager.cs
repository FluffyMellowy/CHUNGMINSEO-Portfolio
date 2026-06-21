using Cysharp.Threading.Tasks;
using KanKikuchi.AudioManager;
using Language;
using SceneTransition;
using St;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Dialogue
{
    /// <summary>
    /// ダイアログシステムの中核マネージャー
    /// CSVから読み込んだデータをもとに、セリフ表示・選択肢分岐・トリガー実行を制御する
    /// </summary>
    public class DialogueManager : MonoBehaviour, IInitializable
    {
        [SerializeField] private DialogueUI _dialogueUI;
        [SerializeField] private DialogueSettings _settings;

        [SerializeField] private SectionTypeEvent _sectionTypeEvent;
        [SerializeField] private SectionTypeEvent _startTypeEvent;

        [Header("サウンド")]
        [SerializeField] private string _advanceSEPath = "SE/dialogue_advance"; // セリフ進行SE（TODO:実ファイル追加後調整）
        [SerializeField] private string _choiceSEPath = "SE/dialogue_choice"; // 選択肢決定SE

        [Header("ボイス")]
        [Tooltip("ボイス再生用AudioSource。CSVのVoiceJP/VoiceENの値に対し下記prefixを付加してResourcesから読み込む")]
        [SerializeField] private AudioSource _voiceSource;
        [Tooltip("JPボイスファイルのResourcesパスprefix。例: 'Voice/JP/'。CSVには拡張子なしのファイル名だけ入れればOK")]
        [SerializeField] private string _voicePathPrefixJP = "Voice/JP/";
        [Tooltip("ENボイスファイルのResourcesパスprefix。例: 'Voice/EN/'")]
        [SerializeField] private string _voicePathPrefixEN = "Voice/EN/";

        [Header("立ち絵")]
        [Tooltip("立ち絵スプライトのResourcesパスprefix。CSVのImage列にはファイル名のみ記入（拡張子なし）")]
        [SerializeField] private string _imagePathPrefix = "Portraits/";

        [Header("起動時に隠す表示物（ダイアログ開始時にまとめて表示）")]
        [Tooltip("シーン遷移時の画面フェード中にチラ見せしたくないGameObject群。" +
                 "ダイアログウィンドウ、立ち絵Canvas、3D背景などを入れる。" +
                 "Awakeで全てSetActive(false)、StartDialogue呼び出し時に全てSetActive(true)になる")]
        [SerializeField] private GameObject[] _visualRoots;

        [Header("デバッグ / 単独実行")]
        [Tooltip("trueにするとGameManager不在でもStart()でCSVを読み込み、_standaloneStartIdから再生する")]
        [SerializeField] private bool _runStandalone = false; // 単独再生フラグ
        [SerializeField] private string _standaloneStartId = "31"; // 単独実行時に最初に再生するID
        [Tooltip("単独実行時に強制する言語。LanguageManagerが居る場合のみ反映")]
        [SerializeField] private LanguageManager.Language _standaloneLanguage = LanguageManager.Language.JP;
        [Tooltip("リザルト分岐テスト用の擬似GameOverCount。負数で無効（実カウント or 0）")]
        [SerializeField] private int _standaloneGameOverCount = -1;

        [Header("リザルトシーン用")]
        [Tooltip("trueにするとStart()でCSV読み込み後、GameOverCountに応じて100/110/120のいずれかを自動再生する。ResultScene側のDialogueManagerでON")]
        [SerializeField] private bool _autoPlayResult = false; // リザルトシーン自動再生フラグ

        // ID → DialogueDataの辞書。CSVから一括読み込み
        private Dictionary<string, DialogueData> _dialogueTable;
        private DialogueData _current;
        private DialogueLoader _loader;

        // 非同期タスクのキャンセル用（シーン遷移・破棄時）
        private CancellationTokenSource _cts;

        /// <summary>現在再生中のダイアログID（デバッグ表示用）。再生していなければnull</summary>
        public string CurrentId => _current?.Id;

        /// <summary>CSVテーブルが読み込み済みか</summary>
        public bool IsTableLoaded => _dialogueTable != null;

        /// <summary>テーブル内の全ID（デバッグ用）。未ロード時はnull</summary>
        public ICollection<string> AvailableIds => _dialogueTable?.Keys;

        private void Awake()
        {
            // シーン遷移直後のフェード中に立ち絵・ウィンドウ・3D背景がチラ見えしないよう、
            // CSV再生開始(StartDialogue)まで全て非表示にしておく
            if (_visualRoots != null)
            {
                foreach (var go in _visualRoots)
                    if (go != null) go.SetActive(false);
            }
        }

        private void Start()
        {
            // _autoPlayResultはリザルトシーンの正規動作
            if (_autoPlayResult)
            {
                // 統合モード判定:
                //   _startTypeEvent が割り当てられている かつ ManagerScene 等が同時ロードされている
                //   (sceneCount > 1) ならGameManagerが画面フェード後にRaise(Result)してくる前提
                //   → CSVロードのみして待機。再生はOnSectionStart(Result)で実行される
                //
                // 単独モード判定:
                //   sceneCount <= 1 なら ResultScene 単体実行とみなして即時自動再生する
                //   (エディタ単独デバッグ用。_startTypeEvent がインスペクターに残っていても無視)
                bool isStandalone = SceneManager.sceneCount <= 1;
                if (!isStandalone && _startTypeEvent != null)
                {
                    InitializeAsync().Forget();
                    return;
                }

                // 単独モード:即時自動再生
                BootstrapResultAsync().Forget();
                return;
            }

            // DialogueScene向け:
            //   sceneCount <= 1 = DialogueScene 単体実行 → _standaloneStartIdから自動再生
            //   sceneCount > 1  = ManagerScene 等と統合実行 → GameManagerの _startEvent.Raise を待機（Start内では何もしない）
            // ビルドではManagerSceneが常に居るのでsceneCount<=1は発生しない → 統合動作のまま影響なし
            bool isStandaloneDialogue = SceneManager.sceneCount <= 1;
            if (isStandaloneDialogue)
            {
                BootstrapStandaloneAsync().Forget();
                return;
            }

            // インスペクターで _runStandalone を強制ONにしたエディタテスト用フォールバック。
            // ビルドでは #if UNITY_EDITOR ガードで完全に削除されるため強制ONでも無害
#if UNITY_EDITOR
            if (_runStandalone)
                BootstrapStandaloneAsync().Forget();
#endif
        }

        /// <summary>
        /// 単独シーン再生時の自動初期化フロー
        /// CSV読み込み → 言語設定 → 指定IDから再生
        /// </summary>
        private async UniTaskVoid BootstrapStandaloneAsync()
        {
            await InitializeAsync();
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.SetLanguage(_standaloneLanguage);
            if (!string.IsNullOrEmpty(_standaloneStartId))
                StartDialogue(_standaloneStartId).Forget();
        }

        /// <summary>
        /// ResultScene用の自動再生フロー
        /// CSV読み込み → GameOverCountに応じて100/110/120を自動再生
        /// エディタでは_standaloneLanguageで言語を強制（単独テスト用）
        /// ビルドではLanguageManagerの現在値（前シーンで設定された言語）を尊重
        /// </summary>
        private async UniTaskVoid BootstrapResultAsync()
        {
            await InitializeAsync();

#if UNITY_EDITOR
            // エディタ単独テスト用に強制言語適用
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.SetLanguage(_standaloneLanguage);
#endif

            string id = GetResultDialogueId();
            StartDialogue(id).Forget();
        }

        public async UniTask InitializeAsync()
        {
            _loader = new DialogueLoader();
            _dialogueTable = _loader.Load("dialogue");
            // Assets/Resources/dialogue.csv

            /*
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.SceneTransitionExit().Forget();
            StartDialogue("1").Forget();
            */

            await UniTask.CompletedTask;
        }

        private void OnEnable()
        {
            _startTypeEvent?.Register(OnSectionStart);
            // 言語切替時、表示中のセリフを新しい言語に即座に差し替える
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged += OnLanguageChangedRefresh;
        }

        private void OnDisable()
        {
            _startTypeEvent?.Unregister(OnSectionStart);
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged -= OnLanguageChangedRefresh;
        }

        /// <summary>
        /// 言語が切り替わった瞬間、表示中のセリフを新しい言語版でDialogueUIに差し替えさせる
        /// 選択肢が表示中ならChoiceテキストも更新する
        /// </summary>
        private void OnLanguageChangedRefresh(LanguageManager.Language lang)
        {
            if (_current == null || _dialogueUI == null) return;
            _dialogueUI.RefreshLanguage(_current.GetText());
            // ボイスも新言語版に切り替え（再生中なら止めて新クリップ再生）
            PlayVoiceFor(_current);
            // 選択肢ラベルは現状CSVが単一言語のため再ローカライズの仕組みは持っていない
            // 必要になった時点でChoiceA/BにもtextJP/textEN列を追加するのが正攻法
        }

        [Header("リザルト分岐")]
        [SerializeField] private string _goodEndDialogueId = "121"; // 失敗0回時に再生するダイアログID
        [SerializeField] private string _normalEndDialogueId = "111"; // 失敗が中間値の時のダイアログID
        [SerializeField] private string _badEndDialogueId = "101"; // 失敗が閾値以上の時のダイアログID
        [SerializeField] private int _normalEndThreshold = 1; // この値以上でノーマルエンド
        [SerializeField] private int _badEndThreshold = 3; // この値以上でバッドエンド

        /// <summary>
        /// 失敗回数に応じてリザルト時に再生するダイアログIDを返す
        /// エディタ単独テストのみ_standaloneGameOverCountを優先（ビルドでは無視）
        /// </summary>
        private string GetResultDialogueId()
        {
            int count = GameoverCounter.Instance != null ? GameoverCounter.Instance.GameOverCount : 0;

#if UNITY_EDITOR
            if (_standaloneGameOverCount >= 0)
                count = _standaloneGameOverCount;
#endif

            if (count >= _badEndThreshold) return _badEndDialogueId;
            if (count >= _normalEndThreshold) return _normalEndDialogueId;
            return _goodEndDialogueId;
        }

        private void OnSectionStart(SectionType type)
        {
            // 診断: ダイアログ開始時点でのLanguageManager状態を記録。
            // ここで意図と違う言語になっていたら、SetLanguageログのスタックトレースから犯人特定する
            var lmExists = LanguageManager.Instance != null;
            var lmLang = lmExists ? LanguageManager.Instance.CurrentLanguage.ToString() : "NO INSTANCE";
            Debug.Log($"[DialogueManager] OnSectionStart type={type}, LanguageManager.Instance={(lmExists ? "OK" : "NULL")}, CurrentLanguage={lmLang}", this);
            LanguageDiagnostic.Log("DialogueManager.OnSectionStart",
                $"type={type}, instance={(lmExists ? "OK" : "NULL")}, CurrentLanguage={lmLang}");

            switch (type)
            {
                case SectionType.UIMaze_Dialogue:
                    // ミニゲーム1（UIMaze）後のダイアログ
                    StartDialogue("31").Forget();
                    break;
                case SectionType.Action_Dialogue:
                    // ミニゲーム2（Action）後のダイアログ
                    StartDialogue("41").Forget();
                    break;
                case SectionType.Result:
                    StartDialogue(GetResultDialogueId()).Forget();
                    break;
            }
        }

        /// <summary>
        /// 指定IDからダイアログを開始する
        /// 選択肢の有無で分岐しながらNextIdが空になるまでループ
        /// </summary>
        public async UniTaskVoid StartDialogue(string startId)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // 直前のダイアログのボイスが残ってる可能性があるので明示的に止める
            StopVoice();

            if (!_dialogueTable.TryGetValue(startId, out _current))
            {
                Debug.LogError($"ダイアログID '{startId}' が見つかりません");
                return;
            }

            // CSV再生開始時点で隠していたビジュアル要素を一括表示
            // （Awakeで非表示にしていたウィンドウ・立ち絵・3D背景などをここで初めて見せる）
            if (_visualRoots != null)
            {
                foreach (var go in _visualRoots)
                    if (go != null && !go.activeSelf) go.SetActive(true);
            }

            try
            {
                while (_current != null)
                {
                    // 選択肢があれば分岐処理
                    if (_current.HasChoice)
                    {
                        await ShowChoice(_cts.Token);
                    }
                    else
                    {
                        // セリフ表示 → 入力待機 → トリガー発動 → 次へ
                        // await中に外部からStartDialogueが呼ばれて_currentが差し替わってもクラッシュしないようローカル変数で固定
                        var line = _current;
                        ApplyImageFor(line);
                        PlayVoiceFor(line);
                        await _dialogueUI.ShowText(line.GetText(), _cts.Token);
                        await WaitForInput(_cts.Token);

                        // await中に外部から差し替わっていたら以降の自走は中止
                        if (_current != line) break;

                        if (line.HasTrigger)
                        {
                            FireTrigger(line.Trigger);
                            print("トリガー");
                        }

                        if (string.IsNullOrEmpty(line.NextId))
                            break;

                        if (!_dialogueTable.TryGetValue(line.NextId, out _current))
                        {
                            Debug.LogError($"ダイアログID '{line.NextId}' が見つかりません");
                            break;
                        }
                    }
                }
            }
            finally
            {
                // 自然終了・キャンセル・例外いずれの経路でも必ずボイスを止める
                StopVoice();

                // キャンセルされず最後まで読み切ったかどうか（外部StartDialogueによる差し替えはキャンセル扱い）
                bool wasNaturalEnd = _cts != null && !_cts.IsCancellationRequested;
                _current = null;

                // _autoPlayResult時は、Result側ダイアログを最後まで読み切ったらタイトルへ戻すイベントを自動発火
                if (_autoPlayResult && wasNaturalEnd && _sectionTypeEvent != null)
                {
                    Debug.Log("[DialogueManager] Result dialogue 完了 → SectionType.Result 発火（タイトルへ戻る）");
                    _sectionTypeEvent.Raise(St.SectionType.Result);
                }
            }
        }

        /// <summary>
        /// 選択肢を表示して、プレイヤーの選択を待機する
        /// 選ばれた選択肢のIDを元に次のダイアログデータへ遷移する
        /// </summary>
        private async UniTask ShowChoice(CancellationToken token)
        {
            _dialogueUI.ShowChoices(_current.ChoiceA, _current.ChoiceB);

            ApplyImageFor(_current);
            PlayVoiceFor(_current);
            await _dialogueUI.ShowText(_current.GetText(), token);

            // 選ばれた方のIDが返ってくる
            string selectedId = await _dialogueUI.WaitForChoice(
                _current.ChoiceAId,
                _current.ChoiceBId,
                token
            );

            SafeSE.Play(_choiceSEPath);

            _dialogueUI.HideChoices();

            if (!_dialogueTable.TryGetValue(selectedId, out _current))
            {
                Debug.LogError($"選択肢のダイアログID '{selectedId}' が見つかりません");
            }
        }

        /// <summary>
        /// プレイヤーの入力を待機する
        /// AUTO・スキップモードの状態に応じて動作を変える
        /// </summary>
        private async UniTask WaitForInput(CancellationToken token)
        {
            // スキップ:待機なし
            if (_settings.IsSkip)
                return;

            // AUTO:設定時間だけ待ってから次へ
            if (_settings.IsAuto)
            {
                await _settings.WaitAuto(token);
                return;
            }

            // 通常:ゲームパッドのA/B、またはキーボードのZ/Space/Enterで進行
            await UniTask.WaitUntil(DialogueInputs.IsAdvancePressed, cancellationToken: token);

            SafeSE.Play(_advanceSEPath);
        }


        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            StopVoice();
        }

        /// <summary>
        /// 指定行のボイスを再生する。前のボイスは即座に停止し、新しいクリップに差し替える
        /// VoiceJP / VoiceENが空文字なら何もしない（無音セリフ扱い）
        /// </summary>
        private void PlayVoiceFor(DialogueData data)
        {
            StopVoice();
            if (data == null)
            {
                Debug.Log("[DialogueManager.PlayVoiceFor] data == null, skip");
                return;
            }
            if (_voiceSource == null)
            {
                Debug.LogWarning("[DialogueManager.PlayVoiceFor] _voiceSource not assigned in Inspector. ID=" + data.Id);
                return;
            }

            string filename = data.GetVoice();
            if (string.IsNullOrEmpty(filename))
            {
                Debug.Log($"[DialogueManager.PlayVoiceFor] ID={data.Id} の Voice{(LanguageManager.Instance?.CurrentLanguage)} 列が空文字。無音進行");
                return;
            }

            string prefix = (LanguageManager.Instance != null
                && LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.EN)
                ? _voicePathPrefixEN
                : _voicePathPrefixJP;
            string path = prefix + filename;
            Debug.Log($"[DialogueManager.PlayVoiceFor] ID={data.Id} loading: {path}");

            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[DialogueManager.PlayVoiceFor] ボイスクリップが見つかりません: {path}");
                return;
            }

            _voiceSource.clip = clip;
            _voiceSource.Play();
            Debug.Log($"[DialogueManager.PlayVoiceFor] Playing {clip.name}, length={clip.length:F2}s, source isActiveAndEnabled={_voiceSource.isActiveAndEnabled}");
        }

        /// <summary>再生中のボイスがあれば停止する</summary>
        private void StopVoice()
        {
            if (_voiceSource == null) return;
            if (_voiceSource.isPlaying) _voiceSource.Stop();
        }

        /// <summary>
        /// 指定行の立ち絵をDialogueUIに適用する
        /// Image列が空セルなら何もしない（前の立ち絵をそのまま保持）
        /// 値がある場合は_imagePathPrefix +値 をResourcesからロードして差し替える
        /// </summary>
        private void ApplyImageFor(DialogueData data)
        {
            if (data == null || _dialogueUI == null) return;
            if (string.IsNullOrEmpty(data.Image)) return; // 空セル=現状維持

            string path = _imagePathPrefix + data.Image;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[DialogueManager.ApplyImageFor] 立ち絵スプライトが見つかりません: {path} (ID={data.Id})");
                return;
            }

            _dialogueUI.SetPortrait(sprite);
        }


        // イベント終了を通知するトリガー
        private void FireTrigger(TriggerType trigger)
        {
            
            switch (trigger)
            {
                case TriggerType.FinishTitleScene:
                    // タイトル（イントロ含む）終了 → ミニゲーム1（UIMaze）へ
                    _sectionTypeEvent.Raise(St.SectionType.Title);
                    print("FinishTitleScene → Title イベントを発行");
                    break;
                case TriggerType.FinishDialogueScene1:
                    // ミニゲーム1後ダイアログ終了 → ミニゲーム2（Action）へ
                    _sectionTypeEvent.Raise(St.SectionType.UIMaze_Dialogue);
                    print("FinishDialogueScene1 → UIMaze_Dialogue イベントを発行");
                    break;
                case TriggerType.FinishDialogueScene2:
                    // ミニゲーム2後ダイアログ終了 → ミニゲーム3（DatingSim）へ
                    _sectionTypeEvent.Raise(St.SectionType.Action_Dialogue);
                    print("FinishDialogueScene2 → Action_Dialogue イベントを発行");
                    break;
            }
            
        }
    }
}