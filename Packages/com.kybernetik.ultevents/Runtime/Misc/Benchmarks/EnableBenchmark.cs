// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using UnityEngine;

namespace UltEvents.Benchmarks
{
    /// <summary>
    /// A simple performance test which calls <see cref="Test"/> a specified number of times
    /// in <see cref="OnEnable"/> and logs the amount of time it takes.
    /// </summary>
    [AddComponentMenu("")]// Don't show in the Add Component menu. You need to drag this script onto a prefab manually.
    [HelpURL(UltEventUtils.APIDocumentationURL + "/Behchmarks/EnableBenchmark")]
    public abstract class EnableBenchmark : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField] private int _TestCount = 1;

        /************************************************************************************************************************/

        /// <summary>Executes the test and disables the <see cref="gameObject"/>.</summary>
        protected virtual void OnEnable()
        {
            var timer = SimpleTimer.Start();

            Test();

            timer.Stop();

            Debug.Log(
                $"Invoked {this} {_TestCount} times in {timer.total * 1000} milliseconds.",
                this);

            gameObject.SetActive(false);
        }

        /************************************************************************************************************************/

        /// <summary>Called by <see cref="OnEnable"/>.</summary>
        protected abstract void Test();

        /************************************************************************************************************************/
    }
}