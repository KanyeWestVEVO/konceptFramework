using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace koncept.UIManagement
{
    /// <summary>
    /// Manager that controls the state of koncept UIs.
    /// </summary>
    public class UIManager : Manager
    {
        /// <summary>
        /// UIs to load when the app is first started.
        /// </summary>
        [Header("App Configuration")]
        [Tooltip("UIs to load when the app is first started.")]
        public UIData[] defaultUserInterfacesToLoad;
        /// <summary>
        /// The container for the Layers that UIs will be place in and ordered.
        /// </summary>
        [Tooltip("The container for the Layers that UIs will be place in and ordered.")]
        public Transform defaultContainer;

        // Current state of the UIs loaded into koncept
        List<UI> activeUI = new List<UI>();
        GameObject[] layers = new GameObject[101];

        public override IEnumerator Initialize()
        {
            if (defaultUserInterfacesToLoad.Length == 0)
            {
                Debug.LogWarning("koncept.UIManager - No default UIs to load.");
            }

            foreach (UIData u in defaultUserInterfacesToLoad)
            {
                LoadUI(u);
            }

            return base.Initialize();
        }

        /// <summary>
        /// Gets the first UI of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of UI to retrieve.</typeparam>
        /// <returns>A UI of the specified type.</returns>
        public T GetUI<T>() where T : UI
        {
            return activeUI.Where(ui => ui is T).FirstOrDefault() as T;
        }

        /// <summary>
        /// Gets all UIs of a specified type.  Only use if you know what you are doing.
        /// </summary>
        /// <typeparam name="T">The type of UI to retrieve.</typeparam>
        /// <returns>A list of UIs of a specified type.</returns>
        public List<T> GetUIs<T>() where T : UI
        {
            return new List<T>(activeUI.Cast<T>());
        }

        /// <summary>
        /// Loads UI into a Layer unelss another container is specified.
        /// </summary>
        /// <param name="u">The data package of the UI to load.</param>
        /// <param name="container">If the UI to load does not use the Layers system, another parent must be specified.</param>
        public Coroutine LoadUI(UIData u, Transform container = null)
        {
            if (!u.UseLayers && container == null)
            {
                Debug.LogError("koncept.UIManager - UI to load is set to not use layers but has no designated target container to be placed in.  To fix this, fill in the container parameter for this LoadUI() call.");
            }

            return StartCoroutine(LoadUICo(u, container));
        }

        int? dismissalLayer = null;
        IEnumerator LoadUICo(UIData u, Transform container)
        {
            if (u.Layer == dismissalLayer)
                yield break;

            if (u.UseLayers && layers[u.Layer] != null)
                yield return StartCoroutine(UnloadUICo(u.Layer, u.AllowNavDuringTransition));

            GameObject newUI = null;

            newUI = Instantiate(u.Prefab, container != null ? container : defaultContainer) as GameObject;

            newUI.SendMessage("Show", u, SendMessageOptions.DontRequireReceiver);
            layers[u.Layer] = newUI;
            activeUI.Add(newUI.GetComponent<UI>());

            for (int i = 0; i < layers.Length; i++)
            {
                GameObject currentUI = layers[i];

                if (currentUI != null)
                    currentUI.transform.SetSiblingIndex(i);
            }

            yield break;
        }

        /// <summary>
        /// Unloads a UI that exists in the scene.
        /// </summary>
        /// <param name="ui">The UI to unload - generally a GameObject in the scene with a UI component attached to its root.</param>
        public Coroutine UnloadUI(UI ui)
        {
            return StartCoroutine(UnloadUICo(ui));
        }

        /// <summary>
        /// Unloads the first UI found of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of UI to unload.</typeparam>
        public Coroutine UnloadUI<T>() where T : UI
        {
            Coroutine c = null;

            if (GetUI<T>() != null)
                c = StartCoroutine(UnloadUICo(GetUI<T>()));
            else
                Debug.LogWarning("koncept.UIManager - A UI of this type was not found.");

            return c;
        }

        IEnumerator UnloadUICo(int layer, bool allowNavDuringTransition = false)
        {
            if (layers[layer] != null && layers[layer].TryGetComponent(out UI cachedUIToDispose))
            {
                dismissalLayer = layer;
                layers[layer] = null;

                if (activeUI.Contains(cachedUIToDispose))
                    activeUI.Remove(cachedUIToDispose);

                if (!allowNavDuringTransition)
                    yield return StartCoroutine(cachedUIToDispose.Hide());

                Destroy(cachedUIToDispose.gameObject);
                dismissalLayer = null;
            }
            else
                Debug.LogWarning("koncept.UIManager - No UI was found in Layer " + layer);

            yield break;
        }

        IEnumerator UnloadUICo(UI ui, bool allowNavDuringTransition = false)
        {
            if (ui.TryGetComponent(out UI cachedUIToDispose))
            {
                if (activeUI.Contains(cachedUIToDispose))
                    activeUI.Remove(cachedUIToDispose);
                else
                    Debug.LogError("koncept.UIManager - UI was not found in list of Active UIs.");

                if (!allowNavDuringTransition)
                    yield return StartCoroutine(cachedUIToDispose.Hide());

                Destroy(cachedUIToDispose.gameObject);
            }
            else
                Debug.LogError("koncept.UIManager - GameObject attempting to unload does not have a UI component attached to it.");

            yield break;
        }
    }
}