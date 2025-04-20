using koncept.Tools;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace koncept.SceneManagement
{
    public class SceneManager : Manager
    {
        [Header("App Configuration")]
        /// <summary>
        /// Name of the scene to load upon startup.  This is optional and generally used if you structure your koncept App to utilize a pre-load scene.
        /// </summary>
        [Tooltip("Name of the scene to load upon startup.  This is optional and generally used if you structure your koncept App to utilize a pre-load scene.")]
        public string startupSceneName;
        public string StartupSceneName
        {
            get
            {
                return startupSceneName;
            }
            set
            {
                if (startupSceneName == value)
                    return;

                startupSceneName = value;
            }
        }

        /// <summary>
        /// UnityEvent that handles behaviour that occurs when a new scene is about to load - ex: Make a loading screen or icon appear.
        /// </summary>
        [Header("Triggered Events")]
        [Tooltip("UnityEvent that handles behaviour that occurs when a new scene is about to load - ex: Make a loading screen or icon appear.")]
        public SceneEvent OnSceneLoadBegin;
        /// <summary>
        /// UnityEvent that handles behaviour that occurs when a new scene has finished loading - ex: Dismiss a loading screen or icon.
        /// </summary>
        [Tooltip("UnityEvent that handles behaviour that occurs when a new scene has finished loading - ex: Dismiss a loading screen or icon.")]
        public SceneEvent OnSceneLoadComplete;

        public override IEnumerator Initialize()
        {
            if (Helpers.IsStringBlank(startupSceneName))
                Debug.LogWarning("koncept.SceneManager - No default scene assigned.  Ignore this if you are not using a pre-load scene.");
            else
                LoadScene(startupSceneName);

            return base.Initialize();
        }

        Coroutine loadSceneCo;
        /// <summary>
        /// Loads a Unity scene by its name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="onSceneLoadBegin">Optional behaviour to trigger when the scene is about to load - otherwise, the SceneEvent defined in the Scene Manager will be used.</param>
        /// <param name="onSceneLoadComplete">Optional behaviour to trigger when the scene has finished loading - otherwise, the SceneEvent defined in the Scene Manager will be used.</param>
        public void LoadScene(string sceneName, SceneEvent onSceneLoadBegin = null, SceneEvent onSceneLoadComplete = null)
        {
            if (loadSceneCo == null)
            {
                if (onSceneLoadBegin == null)
                    onSceneLoadBegin = OnSceneLoadBegin;

                if (onSceneLoadComplete == null)
                    onSceneLoadComplete = OnSceneLoadComplete;

                StartCoroutine(LoadSceneAsync(sceneName, onSceneLoadBegin, onSceneLoadComplete));
            }
            else
                Debug.LogError("koncept.SceneManager - Another scene is already in the process of loading.  Load aborted.");
        }

        IEnumerator LoadSceneAsync(string sceneName, SceneEvent onSceneLoadBegin, SceneEvent onSceneLoadComplete)
        {
            onSceneLoadBegin.unityEvent?.Invoke();

            yield return new WaitForSeconds(OnSceneLoadBegin.durationForThisEvent);

            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
                yield return null;

            // Set new scene as the active scene to override original scene's lighting
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName));

            onSceneLoadComplete.unityEvent?.Invoke();

            yield return new WaitForSeconds(onSceneLoadComplete.durationForThisEvent);

            loadSceneCo = null;

            yield break;
        }
    }

    /// <summary>
    /// A UnityEvent that tells the koncept SceneManager to yield for a specified duration.
    /// </summary>
    [System.Serializable]
    public class SceneEvent
    {
        /// <summary>
        /// Event that will trigger when the Scene Manager invokes it.
        /// </summary>
        [Tooltip("Event that will trigger when the Scene Manager invokes it.")]
        public UnityEvent unityEvent;
        /// <summary>
        /// The duration that the Scene Manager will wait before proceeding to the next event.
        /// </summary>
        [Tooltip("The duration that the Scene Manager will wait before proceeding to the next event.")]
        public float durationForThisEvent;
    }
}