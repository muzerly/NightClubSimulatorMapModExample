// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltEvents
{
    /// <summary>Caches types loaded via reflection for better performance on subsequent lookups.</summary>
    public static class ReflectionCache
    {
        /************************************************************************************************************************/

        private static readonly Dictionary<string, Type>
            AssemblyQualifiedNameToType = new();

        /************************************************************************************************************************/

        /// <summary>Ensures that this cache is initialized to reduce the cost of first access.</summary>
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize() { }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Type.GetType(string)"/> and caches the result for subsequent calls.</summary>
        public static Type GetType(string assemblyQualifiedName)
        {
            if (!AssemblyQualifiedNameToType.TryGetValue(assemblyQualifiedName, out var type))
                AssemblyQualifiedNameToType.Add(assemblyQualifiedName, type = Type.GetType(assemblyQualifiedName));

            return type;
        }

        /************************************************************************************************************************/

        /// <summary>Sets a cached type for <see cref="GetType"/>.</summary>
        public static void SetType(string assemblyQualifiedName, Type type)
            => AssemblyQualifiedNameToType[assemblyQualifiedName] = type;

        /// <summary>Sets a cached type for <see cref="GetType"/>.</summary>
        public static void SetType(Type type)
            => AssemblyQualifiedNameToType[type.AssemblyQualifiedName] = type;

        /************************************************************************************************************************/
    }
}
