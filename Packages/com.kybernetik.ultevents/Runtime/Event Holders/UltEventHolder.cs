// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using UnityEngine;

namespace UltEvents
{
    /// <summary>
    /// A component which encapsulates a single <see cref="UltEvent"/>.
    /// </summary>
    [AddComponentMenu(UltEventUtils.ComponentMenuPrefix + "Ult Event Holder")]
    [HelpURL(UltEventUtils.APIDocumentationURL + "/UltEventHolder")]
    public class UltEventHolder : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField]
        private UltEvent _Event;

        /// <summary>The encapsulated event.</summary>
        public UltEvent Event
        {
            get => _Event ??= new UltEvent();
            set => _Event = value;
        }

        /// <summary>Invoked the <see cref="Event"/>.</summary>
        public virtual void Invoke()
            => _Event?.Invoke();

        /************************************************************************************************************************/
    }
}