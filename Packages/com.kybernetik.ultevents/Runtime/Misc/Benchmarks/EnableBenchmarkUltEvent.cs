// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using UnityEngine;

namespace UltEvents.Benchmarks
{
    /// <summary>
    /// An <see cref="EnableBenchmark"/> which invokes an <see cref="UltEvent"/>.
    /// </summary>
    [AddComponentMenu("")]// Don't show in the Add Component menu. You need to drag this script onto a prefab manually.
    [HelpURL(UltEventUtils.APIDocumentationURL + "/Behchmarks/EnableBenchmarkUltEvent")]
    public class EnableBenchmarkUltEvent : EnableBenchmark
    {
        /************************************************************************************************************************/

        /// <summary>The event to test.</summary>
        public UltEvent ultEvent;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void Test()
            => ultEvent.Invoke();

        /************************************************************************************************************************/
    }
}