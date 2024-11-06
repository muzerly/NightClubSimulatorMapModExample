// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#if UNITY_PHYSICS_2D

using UnityEngine;

namespace UltEvents
{
    /// <summary>
    /// An event that takes a single <see cref="Collider2D"/> parameter.
    /// </summary>
    [System.Serializable]
    public sealed class TriggerEvent2D : UltEvent<Collider2D> { }

    /************************************************************************************************************************/

    /// <summary>
    /// Holds <see cref="UltEvent"/>s which are called by various <see cref="MonoBehaviour"/> 2D trigger events:
    /// <see cref="OnTriggerEnter2D"/>, <see cref="OnTriggerStay2D"/>, and <see cref="OnTriggerExit2D"/>.
    /// </summary>
    [AddComponentMenu(UltEventUtils.ComponentMenuPrefix + "Trigger Events 2D")]
    [HelpURL(UltEventUtils.APIDocumentationURL + "/TriggerEvents2D")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class TriggerEvents2D : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField]
        private TriggerEvent2D _TriggerEnterEvent;

        /// <summary>Invoked by <see cref="OnTriggerEnter2D"/>.</summary>
        public TriggerEvent2D TriggerEnterEvent
        {
            get => _TriggerEnterEvent ??= new TriggerEvent2D();
            set => _TriggerEnterEvent = value;
        }

        /// <summary>Invokes <see cref="TriggerEnterEvent"/>.</summary>
        public virtual void OnTriggerEnter2D(Collider2D collider)
            => _TriggerEnterEvent?.Invoke(collider);

        /************************************************************************************************************************/

        [SerializeField]
        private TriggerEvent2D _TriggerStayEvent;

        /// <summary>Invoked by <see cref="OnTriggerStay2D"/>.</summary>
        public TriggerEvent2D TriggerStayEvent
        {
            get => _TriggerStayEvent ??= new TriggerEvent2D();
            set => _TriggerStayEvent = value;
        }

        /// <summary>Invokes <see cref="TriggerStayEvent"/>.</summary>
        public virtual void OnTriggerStay2D(Collider2D collider)
            => _TriggerStayEvent?.Invoke(collider);

        /************************************************************************************************************************/

        [SerializeField]
        private TriggerEvent2D _TriggerExitEvent;

        /// <summary>Invoked by <see cref="OnTriggerExit2D"/>.</summary>
        public TriggerEvent2D TriggerExitEvent
        {
            get => _TriggerExitEvent ??= new TriggerEvent2D();
            set => _TriggerExitEvent = value;
        }

        /// <summary>Invokes <see cref="TriggerExitEvent"/>.</summary>
        public virtual void OnTriggerExit2D(Collider2D collider)
            => _TriggerExitEvent?.Invoke(collider);

        /************************************************************************************************************************/
    }
}

#endif