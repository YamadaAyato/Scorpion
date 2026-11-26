using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Scorpion.UI
{
    /// <summary>
    ///     暗転してから指定シーンを読み込み、画面を明るく戻す
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SceneFadeTransition : MonoBehaviour
    {
        [SerializeField, Tooltip("Build Settingsに登録した遷移先シーンのパス")]
        private string _targetScenePath;
        [SerializeField, Min(0f), Tooltip("暗転と明転にそれぞれかける時間（秒）")]
        private float _fadeSeconds = 0.4f;

        private CanvasGroup _overlay;
        private bool _isTransitioning;

        /// <summary>
        ///     フェードを開始し、読み込み中の重複操作を受け付けない
        /// </summary>
        public void LoadTargetScene()
        {
            if (_isTransitioning)
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(_targetScenePath))
            {
                Debug.LogError($"[{nameof(SceneFadeTransition)}] 遷移先がビルドのシーン一覧にありません: {_targetScenePath}", this);
                return;
            }

            _isTransitioning = true;
            _overlay.blocksRaycasts = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            StartCoroutine(LoadWithFade());
        }

        private void Awake()
        {
            _overlay = GetComponent<CanvasGroup>();
            _overlay.alpha = 0f;
            _overlay.blocksRaycasts = false;
        }

        /// <summary>
        ///     暗転を維持したまま遷移し、明転が終わったらオーバーレイを片付ける
        /// </summary>
        private IEnumerator LoadWithFade()
        {
            yield return FadeTo(1f);
            yield return null;

            DontDestroyOnLoad(gameObject);
            yield return SceneManager.LoadSceneAsync(_targetScenePath, LoadSceneMode.Single);
            yield return FadeTo(0f);

            _overlay.blocksRaycasts = false;
            Destroy(gameObject);
        }

        /// <summary>
        ///     ゲーム内時間に依存せず、指定した不透明度へ移る
        /// </summary>
        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = _overlay.alpha;
            float elapsed = 0f;
            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _overlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeSeconds);
                yield return null;
            }

            _overlay.alpha = targetAlpha;
        }
    }
}
