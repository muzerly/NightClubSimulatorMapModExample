// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#if UNITY_PHYSICS_3D

using UnityEngine;

namespace UltEvents
{
    /// <summary>
    /// An event that takes a single <see cref="Collision"/> parameter.
    /// </summary>
    [System.Serializable]
    public sealed class CollisionEvent3D : UltEvent<Collision> { }

    /************************************************************************************************************************/

    /// <summary>
    /// Holds <see cref="UltEvent"/>s which are called by various <see cref="MonoBehaviour"/> collision events:
    /// <see cref="OnCollisionEnter"/>, <see cref="OnCollisionStay"/>, and <see cref="OnCollisionExit"/>.
    /// </summary>
    [AddComponentMenu(UltEventUtils.ComponentMenuPrefix + "Collision Events 3D")]
    [HelpURL(UltEventUtils.APIDocumentationURL + "/CollisionEvents3D")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class CollisionEvents3D : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField]
        private CollisionEvent3D _CollisionEnterEvent;

        /// <summary>Invoked by <see cref="OnCollisionEnter"/>.</summary>
        public CollisionEvent3D CollisionEnterEvent
        {
            get => _CollisionEnterEvent ??= new CollisionEvent3D();
            set => _CollisionEnterEvent = value;
        }

        /// <summary>Invokes <see cref="CollisionEnterEvent"/>.</summary>
        public virtual void OnCollisionEnter(Collision collision)
            => _CollisionEnterEvent?.Invoke(collision);

        /************************************************************************************************************************/

        [SerializeField]
        private CollisionEvent3D _CollisionStayEvent;

        /// <summary>Invoked by <see cref="OnCollisionStay"/>.</summary>
        public CollisionEvent3D CollisionStayEvent
        {
            get => _CollisionStayEvent ??= new CollisionEvent3D();
            set => _CollisionStayEvent = value;
        }

        /// <summary>Invokes <see cref="CollisionStayEvent"/>.</summary>
        public virtual void OnCollisionStay(Collision collision)
            => _CollisionStayEvent?.Invoke(collision);

        /************************************************************************************************************************/

        [SerializeField]
        private CollisionEvent3D _CollisionExitEvent;

        /// <summary>Invoked by <see cref="OnCollisionExit"/>.</summary>
        public CollisionEvent3D CollisionExitEvent
        {
            get => _CollisionExitEvent ??= new CollisionEvent3D();
            set => _CollisionExitEvent = value;
        }

        /// <summary>Invokes <see cref="CollisionExitEvent"/>.</summary>
        public virtual void OnCollisionExit(Collision collision)
            => _CollisionExitEvent?.Invoke(collision);

        /************************************************************************************************************************/
    }
}

#endif