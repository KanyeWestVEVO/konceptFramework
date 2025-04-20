using System.Collections;
using UnityEngine;
using System.Linq;

/*
  _                              _   
 | |                            | |  
 | | _____  _ __   ___ ___ _ __ | |_ 
 | |/ / _ \| '_ \ / __/ _ \ '_ \| __| developed by Ben Nguyen (http://bennguyen.dx.am/)
 |   < (_) | | | | (_|  __/ |_) | |_ 
 |_|\_\___/|_| |_|\___\___| .__/ \__| For support, Twitter: @biggestboss__
                          | |        
                          |_|        
 */
namespace koncept
{
    /// <summary>
    /// A framework for developing apps in Unity.
    /// </summary>
    [DisallowMultipleComponent]
    public class koncept : MonoBehaviour
    {
        /// <summary>
        /// Singleton that allows access to all other functions within koncept Framework through various managers.
        /// </summary>
        private static koncept _instance;
        public static koncept Framework { get { return _instance; } }

        /// <summary>
        /// Name of this koncept App.
        /// </summary>
        [Header("Framework Configuration")]
        [Tooltip("Name of this koncept App.")]
        public string appName;
        /// <summary>
        /// A collection of all the koncept Managers in the pre-load scene.
        /// </summary>
        [Tooltip("A collection of all the koncept Managers in the pre-load scene.")]
        [SerializeField] Manager[] managers;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }

            StartCoroutine(InitializeManagers());
        }

        IEnumerator InitializeManagers()
        {
            foreach (Manager m in managers)
            {
                yield return m.Initialize();
            }

            Debug.Log("koncept.Framework - Managers initialized.");

            yield break;
        }

        /// <summary>
        /// Gets the designated koncept Manager.
        /// </summary>
        /// <typeparam name="T">Type of Manager to look for.</typeparam>
        /// <returns>A Manager of a certain type.</returns>
        public T GetManager<T>() where T : Manager
        {
            return managers.Where(m => m is T).FirstOrDefault() as T;
        }

        /// <summary>
        /// Gets the designated koncept Manager by its name.
        /// </summary>
        /// <param name="managerName">Name of Manager to look for.</param>
        /// <returns>A Manager with a certain name.</returns>
        public Manager GetManager(string managerName)
        {
            return managers.Where(m => m.GetType().ToString().Split('.').Last() == managerName).FirstOrDefault();
        }

        /// <summary>
        /// FOR USE ONLY by the koncept Framework Editor Window.
        /// </summary>
        /// <param name="updatedList">Just don't think about using this.</param>
        public void SetManagers(Manager[] updatedList)
        {
            managers = updatedList.OrderBy(m => m.ScriptExecutePriority).ToArray();
        }

        /// <summary>
        /// Exits the app to desktop.  In Unity Editor, it stops Play Mode.
        /// </summary>
        public void QuitApp()
        {
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}