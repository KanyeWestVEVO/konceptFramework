using System.Collections;
using UnityEngine;

namespace koncept
{
    /// <summary>
    /// A class that provides standard koncept Framework functionalities through inheritance.
    /// </summary>
    [DisallowMultipleComponent]
    public class Manager : MonoBehaviour
    {
        /// <summary>
        /// The priority in which this Manager will initialize in relation to other Managers in the koncept Framework.  Lower values initialize first.
        /// </summary>
        [Header("koncept Configuration")]
        [Range(1, 9999)]
        [Tooltip("The priority in which this Manager will initialize in relation to other Managers in the koncept Framework.  Lower values initialize first.")]
        public int ScriptExecutePriority = 9999;

        /// <summary>
        /// Behaviour that performs when this koncept Manager starts up with the app.
        /// </summary>
        /// <returns>The initalization of this koncept Manager.</returns>
        public virtual IEnumerator Initialize() { yield break; }
    }
}