using Cysharp.Threading.Tasks;
using St;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UIMaze
{
    public class MazeVignette : MonoBehaviour, IInit
    {
        [SerializeField] private Volume _volume;        // Volumeの参照
        [SerializeField] private MazeCursor _cursor;    // カーソルの参照
        [SerializeField] private Camera _camera;        // メインカメラの参照

        private Vignette _vignette;                     // Vignetteエフェクトのキャッシュ

        public async UniTask Init()
        {
            _volume.profile.TryGet(out _vignette);
            _vignette.center.value = new Vector2(0.5f, 0.5f);
            await UniTask.CompletedTask;
        }

        public void SetVignette(bool isOn)
        {
            _vignette.active = isOn;
        }
    }
}