using UnityEngine;

namespace koncept.SceneManagement
{
    /// <summary>
    /// The root instantiates this koncept app's Framework Prefab to apply a Singleton pattern.
    /// </summary>
    public class Root : MonoBehaviour
    {
        public GameObject frameworkPrefab;

        private void Start()
        {
            Debug.Assert(frameworkPrefab != null);

            if (koncept.Framework == null)
            {
                Debug.Log("koncept.Root - Management Singleton not detected in scene.  Instantiating Framework Prefab...");
                GameObject newSingletonInstance = Instantiate(frameworkPrefab);
                koncept konceptInstance = newSingletonInstance.GetComponent<koncept>();
                konceptInstance.GetManager<SceneManager>().StartupSceneName = string.Empty;
            }
        }
    }
}