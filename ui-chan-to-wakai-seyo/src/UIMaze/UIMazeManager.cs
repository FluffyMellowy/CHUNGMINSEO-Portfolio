using Cysharp.Threading.Tasks;
using SceneTransition;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using St;
using System.Threading.Tasks;

namespace UIMaze
{
    public class MazeMinigameManager : MonoBehaviour, IMiniGame, IInitializable
    {
        // カーソルの参照
        [SerializeField] private MazeCursor _cursor;
        // ミニゲームの制限時間（秒）
        [SerializeField] private float _timeLimit = 10f;

        [SerializeField] private RectTransform _wallsParent;  // 壁
        private MazeWall[] _walls;
        [SerializeField] private MazeGoal _goal;    // ゴール
        [SerializeField] private MazeFakeGoal _fakeGoal;
        [SerializeField] private RectTransform _saveLoadParent;
        private MazeSaveLoadPoint[] _saveLoadPoints;

        public Vector2 SavedPosition { get; private set; } // セーブ位置

        [SerializeField] private SectionTypeEvent _startEvent;
        private Action<SectionType> _startGameAction;

        // クリア時に通知するイベント
        public static event Action OnClear;
        // 失敗時に通知するイベント
        public static event Action OnFail;

        // 非同期タスクのキャンセル用トークンソース
        private CancellationTokenSource _cts;

        /// <summary>
        /// オブジェクト破棄時にタスクをキャンセルしてリソースを解放する
        /// </summary>
        private void OnDestroy()
        {
            _startEvent?.Unregister(_startGameAction);
            _cts?.Cancel();
            _cts?.Dispose();
        }

        /*
        public void Start()
        {
            InitializeAsync().Forget();
        }
        */

        public async UniTask InitializeAsync()
        {
            Debug.Log("InitializeAsync 呼ばれた");

            // シーン内のIInitを持つコンポーネントを順番に初期化
            IInit[] allInits = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IInit>()
                .ToArray();

            foreach (var target in allInits)
            {
                await target.Init();
            }

            Debug.Log($"IInit count: {allInits.Length}");

            _walls = _wallsParent.GetComponentsInChildren<MazeWall>();
            _saveLoadPoints = _saveLoadParent.GetComponentsInChildren<MazeSaveLoadPoint>();
            StartGame();
            await UniTask.CompletedTask;
        }

        public async UniTask Init()
        {
            _startGameAction = value => StartMinigame(value);
            _startEvent?.Register(_startGameAction);
            await UniTask.CompletedTask;
        }

        public void StartMinigame(SectionType type)
        {
            StartGame();
        }

        /// <summary>
        /// ミニゲームを開始する
        /// カーソルを表示し、制限時間のカウントを開始する
        /// </summary>
        public void StartGame()
        {
            Debug.Log("startgame");

            _cts = new CancellationTokenSource();
            _cursor.gameObject.SetActive(true); // カーソルを表示
            Debug.Log($"cursor active: {_cursor.gameObject.activeSelf}");
            _cursor.SetWalls(_walls);           // カーソルに壁リストを渡す
            Debug.Log($"walls count: {_walls.Length}");
            _goal.SetCursor(_cursor);           // ゴールにカーソルを渡す
            _fakeGoal.SetCursor(_cursor);       // 偽ゴールにカーソルを渡す
            foreach (var point in _saveLoadPoints)
            {
                point.SetCursor(_cursor);
            }
            RunGame(_cts.Token).Forget();       // 制限時間カウント開始
        }
        /// <summary>
        /// ゲームのメインループ
        /// 制限時間が経過したら自動的に失敗扱いにする
        /// </summary>
        private async UniTaskVoid RunGame(CancellationToken token)
        {
            // 制限時間が経過するまで待機
            await UniTask.Delay((int)(_timeLimit * 1000), cancellationToken: token);
            Fail();
        }

        public void SavePosition(Vector2 position)
        {
            SavedPosition = position;
        }

        /// <summary>
        /// クリア処理
        /// タスクをキャンセルしてクリアイベントを通知する
        /// ゴールボタンに到達した際に呼び出す
        /// </summary>
        public void Clear()
        {
            _cts?.Cancel();
            // クリアイベントを通知する
            OnClear?.Invoke();
        }

        /// <summary>
        /// 失敗処理
        /// タスクをキャンセルして失敗イベントを通知する
        /// 制限時間超過時または外部から失敗を通知する際に呼び出す
        /// </summary>
        public void Fail()
        {
            _cts?.Cancel();
            // 失敗イベントを通知する
            OnFail?.Invoke();
        }
    }
}