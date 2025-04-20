using System.Collections;
using UnityEngine;

namespace koncept.UIManagement
{
    /// <summary>
    /// Class that controls how the front end elements operate and animate.
    /// </summary>
    [DisallowMultipleComponent]
    public class UI : MonoBehaviour
    {
        /// <summary>
        /// Updates this UI's front end elements by binding data.
        /// </summary>
        /// <param name="uiData">An optional data package associated with this UI.</param>
        /// <returns>An updated app state for this UI.</returns>
        public virtual IEnumerator DataBind(UIData uiData = null) { yield break; }

        /// <summary>
        /// Behaviour that occurs when this UI loads into the scene.
        /// </summary>
        /// <param name="uiData">An optional data package associated with this UI.</param>
        /// <returns>A sequence of animations or similar behaviour for introducing this UI.</returns>
        public virtual IEnumerator Show(UIData uiData = null) { yield break; }

        /// <summary>
        /// Behaviour that occurs when this UI is removed from the scene.
        /// </summary>
        /// <returns>A sequence of animations or similar behaviour for dismissing this UI.</returns>
        public virtual IEnumerator Hide() { yield break; }

        /// <summary>
        /// Loads another UI from this one, generally used with the onClick event of a button.
        /// </summary>
        /// <param name="dataOfUIToNavigateTo">The data package of the UI to load.</param>
        public virtual void NavigateTo(UIData dataOfUIToNavigateTo)
        {
            koncept.Framework.GetManager<UIManager>().LoadUI(dataOfUIToNavigateTo);
        }

        /// <summary>
        /// Dismisses this UI, generally used with the onClick event of a button.
        /// </summary>
        public virtual void Dismiss()
        {
            koncept.Framework.GetManager<UIManager>().UnloadUI(this);
        }
    }
}