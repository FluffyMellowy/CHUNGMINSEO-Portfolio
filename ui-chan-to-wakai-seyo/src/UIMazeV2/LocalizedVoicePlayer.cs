using Cysharp.Threading.Tasks;
using Language;
using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// 言語別ボイスを1つのGameObjectで一括管理する。
    /// _entries にCSV IDごとの設定(チェーン先, 追加遅延)を並べ、Play(int id) で外部から呼び出す。
    /// 命名規約:
    ///   JP: Resources/Voice/JP/jvoice_{ID}.mp3
    ///   EN: Resources/Voice/EN/evoice_{ID}.mp3
    /// LanguageManager.CurrentLanguage で自動切替
    /// </summary>
    public class LocalizedVoicePlayer : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("ボイスID。CSV行番号と同じ(例: 131)")]
            public int id;
            [Tooltip("このボイス再生完了後、自動で続けて再生する次のID。0以下なら連鎖なし")]
            public int nextId;
            [Tooltip("チェーン再生時の追加待機(秒)。0なら直結")]
            public float chainExtraDelay;
        }

        [Header("ボイスエントリ")]
        [Tooltip("再生対象のボイスID一覧。チェーンしたいなら nextId に次のIDを入れる")]
        [SerializeField] private Entry[] _entries;

        [Header("AudioSource")]
        [Tooltip("ボイス再生用AudioSource。空ならこのGameObjectから取得、それも無ければAddComponent")]
        [SerializeField] private AudioSource _audioSource;

        [Header("挙動")]
        [Tooltip("trueなら再生中の呼び出しは無視。falseなら新しいクリップで上書き")]
        [SerializeField] private bool _suppressWhilePlaying = true;

        [Header("パス設定")]
        [Tooltip("JP用Resourcesパスprefix(末尾スラッシュ込み)。jvoice_/evoice_でJP/ENを区別するためフォルダは共通")]
        [SerializeField] private string _pathPrefixJP = "Voice/";
        [Tooltip("EN用Resourcesパスprefix")]
        [SerializeField] private string _pathPrefixEN = "Voice/";
        [Tooltip("JPファイル名プリフィックス(IDの前)")]
        [SerializeField] private string _filePrefixJP = "jvoice_";
        [Tooltip("ENファイル名プリフィックス")]
        [SerializeField] private string _filePrefixEN = "evoice_";

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// 指定IDのボイスを現在言語で再生する。エントリにマッチがあれば nextId を見てチェーンも自動発火
        /// </summary>
        public void Play(int id)
        {
            if (id <= 0) return;
            if (_audioSource == null) return;
            if (_suppressWhilePlaying && _audioSource.isPlaying) return;

            var clip = ResolveClip(id);
            if (clip == null) return;

            _audioSource.clip = clip;
            _audioSource.Play();

            // チェーン先IDがあれば、現在クリップ長 + 追加遅延後に再帰的に Play(nextId)
            var entry = FindEntry(id);
            if (entry != null && entry.nextId > 0)
                ChainNextAsync(clip.length + Mathf.Max(0f, entry.chainExtraDelay), entry.nextId).Forget();
        }

        /// <summary>
        /// 再生中のボイスを停止する。死亡やシーン遷移時の打ち切りに使う
        /// </summary>
        public void Stop()
        {
            if (_audioSource != null && _audioSource.isPlaying) _audioSource.Stop();
        }

        /// <summary>
        /// チェーン用：指定秒数待ってから次IDを Play する。
        /// destroyCancellationToken で GameObject破棄時に自動中断
        /// </summary>
        private async UniTaskVoid ChainNextAsync(float waitSec, int nextId)
        {
            try
            {
                await UniTask.Delay((int)(waitSec * 1000), cancellationToken: destroyCancellationToken);
            }
            catch (System.OperationCanceledException) { return; }

            Play(nextId);
        }

        /// <summary>
        /// 配列線形探索で id を持つエントリを返す。エントリ数は小さい想定(数個〜数十個)なのでDict不要
        /// </summary>
        private Entry FindEntry(int id)
        {
            if (_entries == null) return null;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i] != null && _entries[i].id == id) return _entries[i];
            }
            return null;
        }

        /// <summary>
        /// 言語に応じてパスを組み立て、Resourcesからクリップをロードして返す。
        /// 命名規約: prefix + filePrefix + id 。例えばJP+131なら Voice/JP/jvoice_131
        /// </summary>
        private AudioClip ResolveClip(int id)
        {
            bool isEN = LanguageManager.Instance != null
                        && LanguageManager.Instance.CurrentLanguage == LanguageManager.Language.EN;

            string prefix = isEN ? _pathPrefixEN : _pathPrefixJP;
            string filePrefix = isEN ? _filePrefixEN : _filePrefixJP;
            string path = prefix + filePrefix + id;

            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[LocalizedVoicePlayer] ボイスが見つかりません: {path}", this);
            return clip;
        }
    }
}
