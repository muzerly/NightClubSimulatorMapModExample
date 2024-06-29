using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.Dynamics;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace RootMotion.Dynamics
{

    public class FixFootColliders : MonoBehaviour
    {
        public Transform root;
        public Transform leftFoot;
        public Transform rightFoot;

        [ContextMenu("Fix")]
        public void Fix()
        {
            var leftC = BipedRagdollCreator.FixFootCollider(leftFoot, root);
            var rightC = BipedRagdollCreator.FixFootCollider(rightFoot, root);

#if UNITY_EDITOR
            EditorUtility.SetDirty(leftC);
            EditorUtility.SetDirty(rightC);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
        }
    }
}
