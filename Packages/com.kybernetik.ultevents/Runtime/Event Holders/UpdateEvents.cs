// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using UnityEngine;

namespace UltEvents
{
    /// <summary>
    /// Holds <see cref="UltEvent"/>s which are called by various <see cref="MonoBehaviour"/> update events:
    /// <see cref="Update"/>, <see cref="LateUpdate"/>, and <see cref="FixedUpdate"/>.
    /// </summary>
    [AddComponentMenu(UltEventUtils.ComponentMenuPrefix + "Update Events")]
    [HelpURL(UltEventUtils.APIDocumentationURL + "/UpdateEvents")]
    [DisallowMultipleComponent]
    public class UpdateEvents : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField]
        private UltEvent _UpdateEvent;

        /// <summary>Invoked by <see cref="Update"/>.</summary>
        public UltEvent UpdateEvent
        {
            get => _UpdateEvent ??= new UltEvent();
            set => _UpdateEvent = value;
        }

        /// <summary>Invokes <see cref="UpdateEvent"/>.</summary>
        public virtual void Update()
            => _UpdateEvent?.Invoke();

        /************************************************************************************************************************/

        [SerializeField]
        private UltEvent _LateUpdateEvent;

        /// <summary>Invoked by <see cref="LateUpdate"/>.</summary>
        public UltEvent LateUpdateEvent
        {
            get => _LateUpdateEvent ??= new UltEvent();
            set => _LateUpdateEvent = value;
        }

        /// <summary>Invokes <see cref="LateUpdateEvent"/>.</summary>
        public virtual void LateUpdate()
            => _LateUpdateEvent?.Invoke();

        /************************************************************************************************************************/

        [SerializeField]
        private UltEvent _FixedUpdateEvent;

        /// <summary>Invoked by <see cref="FixedUpdate"/>.</summary>
        public UltEvent FixedUpdateEvent
        {
            get => _FixedUpdateEvent ??= new UltEvent();
            set => _FixedUpdateEvent = value;
        }

        /// <summary>Invokes <see cref="FixedUpdateEvent"/>.</summary>
        public virtual void FixedUpdate()
            => _FixedUpdateEvent?.Invoke();

        /************************************************************************************************************************/
    }
}