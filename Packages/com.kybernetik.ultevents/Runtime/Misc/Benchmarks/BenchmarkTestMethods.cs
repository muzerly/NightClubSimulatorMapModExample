// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member.

using UnityEngine;

namespace UltEvents.Benchmarks
{
    /// <summary>Dummy methods for performance testing events.</summary>
    [AddComponentMenu("")]// Don't show in the Add Component menu. You need to drag this script onto a prefab manually.
    [HelpURL(UltEventUtils.APIDocumentationURL + "/Behchmarks/BenchmarkTestMethods")]
    public class BenchmarkTestMethods : MonoBehaviour
    {
        /************************************************************************************************************************/

        public void Parameterless() { }

        public void OneParameter(float value) { }

        public static void StaticMethod(float value) { }

        /************************************************************************************************************************/
    }
}