using System.Linq;
using Cysharp.Threading.Tasks;
using St;
using UIMazeV2;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UIMazeV2の最上位マネージャー。IInitializableを実装し、GameManagerからの統一初期化を受け付ける
/// InitializeAsync内で配下の全IInitを収集して順次Init()を呼び出し、その後ShowWindow(0)で開始する
/// 単独シーンテスト用にStart()からもInitializeAsync()を呼ぶフォールバックを備える
/// </summary>
public class WindowManager : MonoBehaviour, IInitializable
{
    [SerializeField] private GameObject[] _windows; // ウィンドウ配列
    [SerializeField] private MonoBehaviour[] _controllers; // 各ウィンドウに対応するコントローラー
    [SerializeField] private Transform[] _spawnPoints; // 各ウィンドウのスポーン位置
    [SerializeField] private string[] _sortingLayers; // 各ウィンドウに対応するソートレイヤー名
    [SerializeField] private Transform _player; // 共有プレイヤー

    [Header("ゲームプレイ開始イベント")]
    [Tooltip("GameManager の StartEventSO を割り当てる。SectionType.UIMaze を受け取ったらゲーム開始。空ならスタンドアロンモードで即時開始")]
    [SerializeField] private SectionTypeEvent _startEvent;

    [Header("サウンド")]
    [Tooltip("次のステージへ移る瞬間に鳴らすSE(NextWindow時)。初回ShowWindow(0)では鳴らさない")]
    [SerializeField] private string _nextStageSEPath = "SE_Chung/SE4";

    [Header("ミニゲーム開始ボイス")]
    [Tooltip("ボイスID一括管理コンポーネント。1つだけ用意してここに刺す")]
    [SerializeField] private LocalizedVoicePlayer _voiceBank;
    [Tooltip("ShowWindow(i)時にBankへ投げるボイスID。配列インデックス=ウィンドウインデックス。" +
             "0以下なら再生スキップ。チェーン(138→141等)は Bank 側エントリで設定する")]
    [SerializeField] private int[] _voiceIdsPerWindow;

    [Header("ウィンドウ別 追加表示オブジェクト")]
    [Tooltip("インデックス=ウィンドウ番号。そのウィンドウがアクティブな時だけSetActive(true)になる。" +
             "ジャンルUI(RPG等)をミニゲーム1だけに見せたい時など、ウィンドウに紐づかない外部UIをここで制御する。" +
             "サイズが_windowsより小さくても可。配列外インデックスのオブジェクトは触らない")]
    [SerializeField] private GameObject[] _extrasPerWindow;

    [Header("操作ガイド矢印")]
    [Tooltip("各ミニゲーム開始時、プレイヤー頭上に表示する矢印GameObject。" +
             "MoveArrowIndicatorコンポーネント付きの想定。プレイヤーの子に置いて自動追従させる。" +
             "ShowWindow毎にSetActive(true)で都度表示し、本人が動いたら自動で消える(コンポーネント側で処理)")]
    [SerializeField] private GameObject _moveArrow;

    private SpriteRenderer _playerSR; // プレイヤーのSpriteRendererキャッシュ
    private int _currentIndex = 0; // 現在のウィンドウ番号
    private bool _initialized; // 二重初期化防止
    private bool _gameplayStarted; // ShowWindow(0)を呼んだか

    private void Awake()
    {
        _playerSR = _player.GetComponent<SpriteRenderer>();

        // 起動時に全コントローラーを一旦無効化する
        // 複数コントローラーのOnEnable（特に重力設定）が無秩序に上書きしあって、
        // ShowWindow(0)が呼ばれるまでに弱い重力が残るのを防ぐ
        if (_controllers != null)
        {
            foreach (var c in _controllers)
            {
                if (c != null) c.enabled = false;
            }
        }

        // 物理シミュレーションも一旦停止する
        // InitializeAsync(IInit初期化)のawait待機中にプレイヤーが重力で落下して死亡判定が出るのを防ぐ
        // StartGameplay内で再開する
        var rb = _player != null ? _player.GetComponent<Rigidbody2D>() : null;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    private void OnEnable()
    {
        _startEvent?.Register(OnSectionStart);
    }

    private void OnDisable()
    {
        _startEvent?.Unregister(OnSectionStart);
    }

    private void Start()
    {
        // 単独テスト用フォールバック。GameManager経由ならInitializeAsyncが既に呼ばれて_initialized=trueになっている
        // 単独モードではInitializeAsyncの完了を待ってからStartGameplayを呼ぶ必要があるので別メソッドに委譲
        StartAsync().Forget();
    }

    /// <summary>
    /// 単独テスト用の初期化→ゲーム開始フロー
    /// 統合モード/単独モードの判定:
    ///   - SceneManager.sceneCount > 1 → 統合モード（ManagerSceneと併用） → OnSectionStartに委ねる
    ///   - SceneManager.sceneCount == 1 → 単独モード → 即時ゲーム開始
    /// インスペクターの_startEventはどちらの場合も埋めたままで良い
    /// </summary>
    private async UniTaskVoid StartAsync()
    {
        if (!_initialized)
            await InitializeAsync();

        // ManagerScene等の追加シーンがロードされていなければ単独テストと判定
        bool isStandalone = SceneManager.sceneCount <= 1;
        if (isStandalone)
        {
            Debug.Log("[WindowManager] 単独モード検出（sceneCount<=1）→ 即時StartGameplay");
            StartGameplay();
        }
        // 統合モード: GameManagerの_startEvent.Raise(UIMaze)を待つ
    }

    /// <summary>
    /// GameManager.ExecuteInitializingから呼ばれる統一初期化エントリポイント
    /// IInitの子だけ初期化する。ShowWindow(0)はここで呼ばず、_startEventからStartGameplay()で開始する
    /// （画面フェードイン演出中に勝手にゲームが動き出すのを防ぐため）
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        Debug.Log("[WindowManager] InitializeAsync 開始");

        IInit[] allInits = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IInit>()
            .ToArray();
        foreach (var target in allInits)
        {
            await target.Init();
        }
        Debug.Log($"[WindowManager] IInit count = {allInits.Length}, 全て初期化完了。StartEvent を待機");
    }

    /// <summary>
    /// GameManagerのStartEventSOからSectionType.UIMazeを受け取ったらゲームを開始する
    /// 画面フェードイン完了後の発火なので、このタイミングでウィンドウ表示＋コントローラー有効化
    /// </summary>
    private void OnSectionStart(SectionType type)
    {
        if (type == SectionType.UIMaze)
            StartGameplay();
    }

    /// <summary>
    /// ShowWindow(0)を一度だけ呼ぶ。多重呼び出しはガード
    /// Awakeで停止していた物理シミュレーションを再開してからShowWindowを実行
    /// </summary>
    private void StartGameplay()
    {
        if (_gameplayStarted) return;
        _gameplayStarted = true;

        // Awakeでsimulated=falseにしていた物理シミュレーションを再開
        // この時点でコントローラーのOnEnableがgravityを適切に設定する
        var rb = _player != null ? _player.GetComponent<Rigidbody2D>() : null;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        Debug.Log("[WindowManager] StartGameplay → ShowWindow(0)");
        ShowWindow(0);
    }

    public void NextWindow()
    {
        _currentIndex++;
        if (_currentIndex >= _windows.Length) return;
        // 次のステージへ移る瞬間のSE(SE4)。初回ShowWindow(0)はStartGameplay経由なので
        // ここを通らず、ステージ"切替"時だけ鳴る設計
        SafeSE.Play(_nextStageSEPath);
        ShowWindow(_currentIndex);
    }

    /// <summary>デバッグ用：任意のウィンドウインデックスへ即時ジャンプ</summary>
    public void JumpToWindow(int index)
    {
        if (index < 0 || index >= _windows.Length) return;
        _currentIndex = index;
        ShowWindow(_currentIndex);
    }

    /// <summary>現在表示中のウィンドウインデックス（デバッグ表示用）</summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>登録されているウィンドウの総数（デバッグ表示用）</summary>
    public int WindowCount => _windows != null ? _windows.Length : 0;

    private void ShowWindow(int index)
    {
        // ウィンドウ切り替え
        for (int i = 0; i < _windows.Length; i++)
        {
            _windows[i].SetActive(i == index);
        }

        // コントローラー切り替え（全disable→対象のみenableでOnEnableを確実に通知）
        if (_controllers != null && index < _controllers.Length)
        {
            for (int i = 0; i < _controllers.Length; i++)
            {
                _controllers[i].enabled = false;
            }
            _controllers[index].enabled = true;
        }

        // スポーン位置に移動
        if (_spawnPoints != null && index < _spawnPoints.Length)
        {
            _player.position = _spawnPoints[index].position;
        }

        // ソートレイヤー切り替え
        if (_sortingLayers != null && index < _sortingLayers.Length)
        {
            _playerSR.sortingLayerName = _sortingLayers[index];
        }

        // ミニゲーム開始ボイス。Bank に ID を投げる。
        // 138→141 のような連鎖は Bank 側エントリの nextId で設定済みなのでここでは1IDだけ渡せばよい
        if (_voiceBank != null && _voiceIdsPerWindow != null && index < _voiceIdsPerWindow.Length)
        {
            int vid = _voiceIdsPerWindow[index];
            if (vid > 0) _voiceBank.Play(vid);
        }

        // ウィンドウ別の追加表示オブジェクトをトグル。
        // 例: ジャンルUI(RPG)をミニゲーム1だけ見せたい時、_extrasPerWindow[0]に刺しておけば自動でON/OFFされる
        if (_extrasPerWindow != null)
        {
            for (int i = 0; i < _extrasPerWindow.Length; i++)
            {
                var go = _extrasPerWindow[i];
                if (go != null) go.SetActive(i == index);
            }
        }

        // 操作ガイド矢印を毎ミニゲームで再表示。前のミニゲームでフェード消えた状態でも、
        // SetActive(false)→(true)のトグルでMoveArrowIndicator.OnEnableが走り、alphaと位置がリセットされて再開する
        if (_moveArrow != null)
        {
            _moveArrow.SetActive(false);
            _moveArrow.SetActive(true);
        }
    }
}
