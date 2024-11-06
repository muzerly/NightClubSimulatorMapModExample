// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace UltEvents
{
    /// <summary>
    /// Allows you to expose the add and remove methods of an <see cref="UltEvent"/>
    /// without exposing the rest of its members such as the ability to invoke it.
    /// </summary>
    public interface IUltEvent : IUltEventBase
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Delegates registered here are invoked by <see cref="UltEvent.Invoke"/> after all
        /// <see cref="UltEvent.PersistentCalls"/>.
        /// </summary>
        event Action DynamicCalls;

        /// <summary>
        /// Invokes all <see cref="UltEvent.PersistentCalls"/> then all <see cref="DynamicCalls"/>.
        /// </summary>
        void Invoke();

        /************************************************************************************************************************/
    }

    /// <summary>A serializable event with no parameters which can be viewed and configured in the inspector.</summary>
    /// <remarks>This is a more versatile and user friendly implementation than <see cref="UnityEvent"/>.</remarks>
    [Serializable]
    public class UltEvent : UltEventBase, IUltEvent
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int ParameterCount => 0;

        /************************************************************************************************************************/

        /// <summary>
        /// Delegates registered to this event are serialized as <see cref="PersistentCall"/>s and are invoked by
        /// <see cref="Invoke"/> before all <see cref="DynamicCalls"/>.
        /// </summary>
        public event Action PersistentCalls
        {
            add => AddPersistentCall(value);
            remove => RemovePersistentCall(value);
        }

        /************************************************************************************************************************/

        private Action _DynamicCalls;

        /// <summary>
        /// Delegates registered here are invoked by <see cref="Invoke"/> after all <see cref="PersistentCalls"/>.
        /// </summary>
        public event Action DynamicCalls
        {
            add
            {
                _DynamicCalls += value;
                OnDynamicCallsChanged();
            }
            remove
            {
                _DynamicCalls -= value;
                OnDynamicCallsChanged();
            }
        }

        /// <summary>
        /// The non-serialized method and parameter details of this event.
        /// Delegates registered here are called by <see cref="Invoke"/> after all <see cref="PersistentCalls"/>.
        /// </summary>
        protected override Delegate DynamicCallsBase
        {
            get => _DynamicCalls;
            set => _DynamicCalls = value as Action;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Operators and Call Registration
        /************************************************************************************************************************/

        /// <summary>
        /// Ensures that `ultEvent` isn't null and adds `method` to its <see cref="PersistentCalls"/> (if in Edit Mode) or
        /// <see cref="DynamicCalls"/> (in Play Mode and at runtime).
        /// </summary>
        public static UltEvent operator +(UltEvent ultEvent, Action method)
        {
            ultEvent ??= new UltEvent();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && method.Target is Object)
            {
                ultEvent.PersistentCalls += method;
                return ultEvent;
            }
#endif

            ultEvent.DynamicCalls += method;
            return ultEvent;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If `ultEvent` isn't null, this method removes `method` from its <see cref="PersistentCalls"/> (if in Edit Mode) or
        /// <see cref="DynamicCalls"/> (in Play Mode and at runtime).
        /// </summary>
        public static UltEvent operator -(UltEvent ultEvent, Action method)
        {
            if (ultEvent == null)
                return null;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && method.Target is Object)
            {
                ultEvent.PersistentCalls -= method;
                return ultEvent;
            }
#endif

            ultEvent.DynamicCalls -= method;
            return ultEvent;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="UltEventBase"/> and adds `method` to its <see cref="PersistentCalls"/> (if in edit
        /// mode), or <see cref="DynamicCalls"/> (in Play Mode and at runtime).
        /// </summary>
        public static implicit operator UltEvent(Action method)
        {
            if (method != null)
            {
                var ultEvent = new UltEvent();
                ultEvent += method;
                return ultEvent;
            }
            else return null;
        }

        /************************************************************************************************************************/

        /// <summary>Ensures that `ultEvent` isn't null and adds `method` to its <see cref="DynamicCalls"/>.</summary>
        public static void AddDynamicCall(ref UltEvent ultEvent, Action method)
        {
            ultEvent ??= new UltEvent();

            ultEvent.DynamicCalls += method;
        }

        /// <summary>If `ultEvent` isn't null, this method removes `method` from its <see cref="DynamicCalls"/>.</summary>
        public static void RemoveDynamicCall(ref UltEvent ultEvent, Action method)
        {
            if (ultEvent != null)
                ultEvent.DynamicCalls -= method;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] The types of each of this event's parameters.</summary>
        public override Type[] ParameterTypes
            => Type.EmptyTypes;
#endif

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public void Invoke()
        {
            InvokePersistentCalls();
            _DynamicCalls?.Invoke();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Invokes all <see cref="PersistentCalls"/> then all <see cref="DynamicCalls"/>
        /// inside a try/catch block which logs any exceptions that are thrown.
        /// </summary>
        public void InvokeSafe()
        {
            try
            {
                Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /************************************************************************************************************************/
    }
}
