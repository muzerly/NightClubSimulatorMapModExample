// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace UltEvents
{
    /// <summary>
    /// Allows you to expose the add and remove methods of an <see cref="UltEvent{T0, T1, T2, T3}"/>
    /// without exposing the rest of its members such as the ability to invoke it.
    /// </summary>
    public interface IUltEvent<T0, T1, T2, T3> : IUltEventBase
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Delegates registered here are invoked by <see cref="UltEvent{T0, T1, T2, T3}.Invoke"/> after all
        /// <see cref="UltEvent{T0, T1, T2, T3}.PersistentCalls"/>.
        /// </summary>
        event Action<T0, T1, T2, T3> DynamicCalls;

        /// <summary>
        /// Invokes all <see cref="UltEvent.PersistentCalls"/> then all <see cref="DynamicCalls"/>.
        /// </summary>
        void Invoke(T0 parameter0, T1 parameter1, T2 parameter2, T3 parameter3);

        /************************************************************************************************************************/
    }

    /// <summary>A serializable event with 4 parameters which can be viewed and configured in the inspector.</summary>
    /// <remarks>This is a more versatile and user friendly implementation than <see cref="UnityEvent{T0, T1, T2, T3}"/>.</remarks>
    [Serializable]
    public class UltEvent<T0, T1, T2, T3> : UltEventBase, IUltEvent<T0, T1, T2, T3>
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int ParameterCount => 4;

        /************************************************************************************************************************/

        /// <summary>
        /// Delegates registered to this event are serialized as <see cref="PersistentCall"/>s and are invoked by
        /// <see cref="Invoke"/> before all <see cref="DynamicCalls"/>.
        /// </summary>
        public event Action<T0, T1, T2, T3> PersistentCalls
        {
            add => AddPersistentCall(value);
            remove => RemovePersistentCall(value);
        }

        /************************************************************************************************************************/

        private Action<T0, T1, T2, T3> _DynamicCalls;

        /// <summary>
        /// Delegates registered here are invoked by <see cref="Invoke"/> after all <see cref="PersistentCalls"/>.
        /// </summary>
        public event Action<T0, T1, T2, T3> DynamicCalls
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
            set => _DynamicCalls = value as Action<T0, T1, T2, T3>;
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
        public static UltEvent<T0, T1, T2, T3> operator +(UltEvent<T0, T1, T2, T3> ultEvent, Action<T0, T1, T2, T3> method)
        {
            ultEvent ??= new UltEvent<T0, T1, T2, T3>();

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
        public static UltEvent<T0, T1, T2, T3> operator -(UltEvent<T0, T1, T2, T3> ultEvent, Action<T0, T1, T2, T3> method)
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
        public static implicit operator UltEvent<T0, T1, T2, T3>(Action<T0, T1, T2, T3> method)
        {
            if (method == null)
                return null;

            var ultEvent = new UltEvent<T0, T1, T2, T3>();
            ultEvent += method;
            return ultEvent;
        }

        /************************************************************************************************************************/

        /// <summary>Ensures that `ultEvent` isn't null and adds `method` to its <see cref="DynamicCalls"/>.</summary>
        public static void AddDynamicCall(ref UltEvent<T0, T1, T2, T3> ultEvent, Action<T0, T1, T2, T3> method)
        {
            ultEvent ??= new UltEvent<T0, T1, T2, T3>();

            ultEvent.DynamicCalls += method;
        }

        /// <summary>If `ultEvent` isn't null, this method removes `method` from its <see cref="DynamicCalls"/>.</summary>
        public static void RemoveDynamicCall(ref UltEvent<T0, T1, T2, T3> e, Action<T0, T1, T2, T3> method)
        {
            if (e != null)
                e.DynamicCalls -= method;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] The types of each of this event's parameters.</summary>
        public override Type[] ParameterTypes => _ParameterTypes;
        private static readonly Type[] _ParameterTypes = { typeof(T0), typeof(T1), typeof(T2), typeof(T3) };
#endif

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void Invoke(T0 parameter0, T1 parameter1, T2 parameter2, T3 parameter3)
        {
            CacheParameter(parameter0);
            CacheParameter(parameter1);
            CacheParameter(parameter2);
            CacheParameter(parameter3);
            InvokePersistentCalls();
            _DynamicCalls?.Invoke(parameter0, parameter1, parameter2, parameter3);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Invokes all <see cref="PersistentCalls"/> then all <see cref="DynamicCalls"/>
        /// inside a try/catch block which logs any exceptions that are thrown.
        /// </summary>
        public virtual void InvokeSafe(T0 parameter0, T1 parameter1, T2 parameter2, T3 parameter3)
        {
            try
            {
                Invoke(parameter0, parameter1, parameter2, parameter3);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /************************************************************************************************************************/
    }
}
