using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace SceneTransition
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [SerializeField] private CanvasGroup _fadePanel;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async UniTaskVoid SceneTransitionEnter()
        {
            await _fadePanel.DOFade(1f, 1f).ToUniTask();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public async UniTaskVoid SceneTransitionExit()
        {
            await _fadePanel.DOFade(0f, 1f).ToUniTask();
        }
    }
}
