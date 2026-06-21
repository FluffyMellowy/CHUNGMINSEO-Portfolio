using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace Title
{
    /// <summary>
    /// タイトル画面のアトラクト動画コントローラー。
    /// 一定時間入力が無いと指定の VideoPlayer を再生して動画を全画面表示する。
    /// 再生中に入力があれば動画を止めてタイトルに戻る。
    /// 入力検出は Keyboard.anyKey / マウスクリック / Gamepad 主要ボタン+D-pad。
    /// </summary>
    public class TitleAttractVideo : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("再生する VideoPlayer。url または clip にmp4を割り当てておく")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [Tooltip("動画再生時にアクティブ化する表示ルート。VideoPlayerが描画するRawImage/Canvas等を入れる")]
        [SerializeField] private GameObject _displayRoot;

        [Header("挙動")]
        [Tooltip("この秒数だけ入力が無いと動画再生を開始する")]
        [SerializeField] private float _idleTimeoutSec = 30f;
        [Tooltip("動画をループ再生するか")]
        [SerializeField] private bool _loop = true;

        private float _idleTimer;
        private bool _playing;
        private bool _audioMuted; // AudioListener.pauseを書き換えたか

        /// <summary>
        /// アトラクト動画再生中か（外部から参照用）。
        /// TitleDialogueLoopがスキップ入力を抑止する判定に使う。
        /// </summary>
        public bool IsPlaying => _playing;

        private void OnEnable()
        {
            ResetToIdle();
        }

        private void OnDisable()
        {
            // シーン遷移時の取り残し対策
            StopVideo();
        }

        private void Update()
        {
            // 動画再生中の入力消費は LateUpdate に移譲する。
            // 理由: 同一フレームで TitleAttractVideo.Update が先に走って _playing=false にしてしまうと、
            // 後から走る TitleDialogueLoop.Update 等の IsPlaying ガードが外れて入力が漏れる。
            // LateUpdate で閉じれば、その frame の全 Update が _playing=true の状態で完走できる。
            if (_playing) return;

            // 動画が出ていない素のタイトル状態での初入力 = グリッチ+ID 11への進行トリガー。
            // L/C/X/Yは言語/クレジット専用なのでアトラクト判定からは除外する
            if (AnyInputForAttractGate())
            {
                enabled = false;
                return;
            }

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleTimeoutSec)
                StartVideo();
        }

        private void LateUpdate()
        {
            // 動画再生中はあらゆる入力で閉じる。Update を全コンポーネントが走り終わった後に処理することで、
            // 他コンポーネントが _playing=true を確実に観測してガード処理を完了させる
            if (!_playing) return;
            if (AnyInputForVideoClose()) ResetToIdle();
        }

        /// <summary>
        /// 動画を再生開始。表示ルートをアクティブ化し、最初から再生する。
        /// 同時にAudioListener.pauseでBGM/SE/その他AudioSourceを一時停止し、
        /// VideoPlayerはDirect出力でAudioListenerを経由しないため動画音声だけが鳴る
        /// </summary>
        private void StartVideo()
        {
            _playing = true;
            if (_displayRoot != null) _displayRoot.SetActive(true);
            if (_videoPlayer != null)
            {
                // 動画音声は AudioListener を経由しない直接出力にする。
                // これによりAudioListenerをpauseしても動画音声だけは鳴り続ける
                _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                _videoPlayer.isLooping = _loop;
                _videoPlayer.Stop();
                _videoPlayer.Play();
            }

            // 他の全AudioSource (BGM/SE/ボイス等) を一時停止
            AudioListener.pause = true;
            _audioMuted = true;
        }

        /// <summary>
        /// 動画を停止して非表示にし、タイトル状態へ戻す。
        /// 一時停止していた他オーディオを復帰させる
        /// </summary>
        private void StopVideo()
        {
            if (_videoPlayer != null) _videoPlayer.Stop();
            if (_displayRoot != null) _displayRoot.SetActive(false);

            // 一時停止していたオーディオを復帰
            if (_audioMuted)
            {
                AudioListener.pause = false;
                _audioMuted = false;
            }
        }

        /// <summary>
        /// アトラクト解除 + アイドルタイマーリセット
        /// </summary>
        private void ResetToIdle()
        {
            _playing = false;
            _idleTimer = 0f;
            StopVideo();
        }

        /// <summary>
        /// 動画再生中の「動画閉じる」入力判定。あらゆる入力(L/C/X/Y含む)を消費する。
        /// この間のL/C/X/Y押下はLanguageToggleButton/CreditModalがIsPlayingで自分の処理を抑止するので、
        /// ここで動画閉じる方に振っても二重発動にならない
        /// </summary>
        private static bool AnyInputForVideoClose()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame))
                return true;

            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame
                || gp.buttonEast.wasPressedThisFrame
                || gp.buttonWest.wasPressedThisFrame
                || gp.buttonNorth.wasPressedThisFrame
                || gp.startButton.wasPressedThisFrame
                || gp.selectButton.wasPressedThisFrame
                || gp.dpad.up.wasPressedThisFrame
                || gp.dpad.down.wasPressedThisFrame
                || gp.dpad.left.wasPressedThisFrame
                || gp.dpad.right.wasPressedThisFrame))
                return true;

            return false;
        }

        /// <summary>
        /// アトラクト発動可能なidle状態での入力判定。
        /// L/C/X/Y は言語/クレジット専用UIショートカットなのでアトラクト解除トリガーから除外。
        /// マウス移動は無視（誤発動防止）
        /// </summary>
        private static bool AnyInputForAttractGate()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame)
            {
                if (!kb.lKey.wasPressedThisFrame && !kb.cKey.wasPressedThisFrame)
                    return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame))
                return true;

            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame
                || gp.buttonEast.wasPressedThisFrame
                // buttonWest(X) / buttonNorth(Y) はUI専用なので除外
                || gp.startButton.wasPressedThisFrame
                || gp.selectButton.wasPressedThisFrame
                || gp.dpad.up.wasPressedThisFrame
                || gp.dpad.down.wasPressedThisFrame
                || gp.dpad.left.wasPressedThisFrame
                || gp.dpad.right.wasPressedThisFrame))
                return true;

            return false;
        }
    }
}
