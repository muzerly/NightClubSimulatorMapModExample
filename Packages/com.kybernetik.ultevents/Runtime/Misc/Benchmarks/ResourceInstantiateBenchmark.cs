// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using UnityEngine;

namespace UltEvents.Benchmarks
{
    /// <summary>
    /// A simple performance test that loads and instantiates a prefab to test how long it takes.
    /// </summary>
    [AddComponentMenu("")]// Don't show in the Add Component menu. You need to drag this script onto a prefab manually.
    [HelpURL(UltEventUtils.APIDocumentationURL + "/Behchmarks/ResourceInstantiateBenchmark")]
    public class ResourceInstantiateBenchmark : MonoBehaviour
    {
        /************************************************************************************************************************/

        [SerializeField] private string _PrefabResourcePath;
        [SerializeField] private int _InstanceCount = 10;

        /************************************************************************************************************************/

        /// <summary>Runs the test after the scene has been loaded for a minimum delay.</summary>
        protected virtual void Update()
        {
            // Wait a bit to avoid mixing this performance test in with the engine startup processes.
            if (Time.timeSinceLevelLoad < 1)
                return;

            var transform = this.transform;

            // Sleep to make this frame show up easily in the Unity Profiler.
            System.Threading.Thread.Sleep(100);

            var timer = SimpleTimer.Start();

            // Include the costs of loading and instantiating the prefab as well as the actual event invocation.
            var prefab = Resources.Load<GameObject>(_PrefabResourcePath);
            for (int i = 0; i < _InstanceCount; i++)
                Instantiate(prefab, transform);

            timer.Stop();

            Debug.Log(
                $"Instantiated {_InstanceCount} copies of '{_PrefabResourcePath}' in {timer.total * 1000} milliseconds.",
                this);

            gameObject.SetActive(false);
        }

        /************************************************************************************************************************/
    }
}