// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using System;

namespace UltEvents
{
    /// <summary>
    /// Extension methods to give <see cref="UltEvents"/>
    /// similar APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    public static class UnityEventCompatibility
    {
        /************************************************************************************************************************/

        /// <summary>Adds the `action` to the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void AddListener(this IUltEvent ultEvent, Action action)
            => ultEvent.DynamicCalls += action;

        /// <summary>Removes the `action` from the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void RemoveListener(this IUltEvent ultEvent, Action action)
            => ultEvent.DynamicCalls -= action;

        /************************************************************************************************************************/

        /// <summary>Adds the `action` to the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void AddListener<T0>(this IUltEvent<T0> ultEvent, Action<T0> action)
            => ultEvent.DynamicCalls += action;

        /// <summary>Removes the `action` from the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void RemoveListener<T0>(this IUltEvent<T0> ultEvent, Action<T0> action)
            => ultEvent.DynamicCalls -= action;

        /************************************************************************************************************************/

        /// <summary>Adds the `action` to the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void AddListener<T0, T1>(this IUltEvent<T0, T1> ultEvent, Action<T0, T1> action)
            => ultEvent.DynamicCalls += action;

        /// <summary>Removes the `action` from the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void RemoveListener<T0, T1>(this IUltEvent<T0, T1> ultEvent, Action<T0, T1> action)
            => ultEvent.DynamicCalls -= action;

        /************************************************************************************************************************/

        /// <summary>Adds the `action` to the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void AddListener<T0, T1, T2>(this IUltEvent<T0, T1, T2> ultEvent, Action<T0, T1, T2> action)
            => ultEvent.DynamicCalls += action;

        /// <summary>Removes the `action` from the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void RemoveListener<T0, T1, T2>(this IUltEvent<T0, T1, T2> ultEvent, Action<T0, T1, T2> action)
            => ultEvent.DynamicCalls -= action;

        /************************************************************************************************************************/

        /// <summary>Adds the `action` to the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void AddListener<T0, T1, T2, T3>(this IUltEvent<T0, T1, T2, T3> ultEvent, Action<T0, T1, T2, T3> action)
            => ultEvent.DynamicCalls += action;

        /// <summary>Removes the `action` from the <see cref="IUltEvent.DynamicCalls"/>.</summary>
        public static void RemoveListener<T0, T1, T2, T3>(this IUltEvent<T0, T1, T2, T3> ultEvent, Action<T0, T1, T2, T3> action)
            => ultEvent.DynamicCalls -= action;

        /************************************************************************************************************************/
    }
}
