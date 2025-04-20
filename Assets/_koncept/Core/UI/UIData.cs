using UnityEngine;

namespace koncept.UIManagement
{
    /// <summary>
    /// A data package to be binded to an associated UI when it loads up.
    /// </summary>
    public class UIData : ScriptableObject
    {
        /// <summary>
        /// If set to true, the UI prefab will be loaded into the koncept Layers system, which works like Photoshop Layers.
        /// </summary>
        [Header("UI Properties")]
        [Tooltip("If set to true, the UI prefab will be loaded into the koncept Layers system, which works like Photoshop Layers.")]
        public bool UseLayers = true;

        /// <summary>
        /// The Layer that the UI Prefab will be assigned to.  Higher Layer appears in front of Lower Layers.
        /// </summary>
        [Tooltip("The Layer that the UI Prefab will be assigned to.  Higher Layer appears in front of Lower Layers.")]
        [Range(1, 100)]
        public int Layer = 1;

        /// <summary>
        /// The Prefab to be instantiated when loading the associated UI - specifically a prefab with a UI component attached to its root.
        /// </summary>
        [Header("UI Configuration")]
        [Tooltip("The Prefab to be instantiated when loading the associated UI - specifically a prefab with a UI component attached to its root.")]
        public GameObject Prefab;

        /// <summary>
        /// If true, UI will bind data when it is loaded into the scene.  On by default.
        /// </summary>
        [Tooltip("If true, UI will bind data when it is loaded into the scene.  On by default.")]
        public bool DataBindOnShow = true;

        /// <summary>
        /// Allows navigation to another UI while another UI is transitioning on the same layer.  May cause issues if not managed properly!
        /// </summary>
        [Tooltip("Allows navigation to another UI while another UI is transitioning on the same layer.  May cause issues if not managed properly!")]
        public bool AllowNavDuringTransition = false;
    }
}