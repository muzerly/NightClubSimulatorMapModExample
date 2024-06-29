using UnityEngine;
using System.Collections;

namespace RootMotion.Dynamics
{

    /// <summary>
    /// Automatically generates a ragdoll for a Biped character.
    /// </summary>
    [HelpURL("https://www.youtube.com/watch?v=y-luLRVmL7E&index=1&list=PLVxSIA1OaTOuE2SB9NUbckQ9r2hTg4mvL")]
    [AddComponentMenu("Scripts/RootMotion.Dynamics/Ragdoll Manager/Biped Ragdoll Creator")]
    public class BipedRagdollCreator : RagdollCreator
    {

        // Open the User Manual URL
        [ContextMenu("User Manual")]
        void OpenUserManual()
        {
            Application.OpenURL("http://root-motion.com/puppetmasterdox/html/page1.html");
        }

        // Open the Script Reference URL
        [ContextMenu("Scrpt Reference")]
        void OpenScriptReference()
        {
            Application.OpenURL("http://root-motion.com/puppetmasterdox/html/class_root_motion_1_1_dynamics_1_1_biped_ragdoll_creator.html#details");
        }

        // Open a video tutorial about setting up the component
        [ContextMenu("TUTORIAL VIDEO")]
        void OpenTutorial()
        {
            Application.OpenURL("https://www.youtube.com/watch?v=y-luLRVmL7E&index=1&list=PLVxSIA1OaTOuE2SB9NUbckQ9r2hTg4mvL");
        }

        public bool canBuild;
        public BipedRagdollReferences references;
        public Options options = Options.Default;

        [System.Serializable]
        public struct Options
        {

            //[SpaceAttribute(10)]

            public float weight;

            [HeaderAttribute("Optional Bones")]
            public bool spine;
            public bool chest;
            public bool hands;
            public bool feet;

            [HeaderAttribute("Joints")]
            public JointType joints;
            public float jointRange;

            [HeaderAttribute("Colliders")]
            public float colliderLengthOverlap;
            public ColliderType torsoColliders;
            public ColliderType headCollider;
            public ColliderType armColliders;
            public ColliderType handColliders;
            public ColliderType legColliders;
            public ColliderType footColliders;
            public bool fixFootColliderRotation;

            public static Options Default
            {
                get
                {
                    Options o = new Options();

                    // General Settings
                    o.weight = 75f;
                    o.colliderLengthOverlap = 0.1f;
                    o.jointRange = 1f;

                    o.chest = true;
                    o.headCollider = ColliderType.Capsule;
                    o.armColliders = ColliderType.Capsule;
                    o.hands = true;
                    o.handColliders = ColliderType.Capsule;
                    o.legColliders = ColliderType.Capsule;
                    o.feet = true;
                    o.fixFootColliderRotation = true;
                    return o;
                }
            }
        }

        public static Options AutodetectOptions(BipedRagdollReferences r)
        {
            Options o = Options.Default;

            if (r.spine == null) o.spine = false;
            if (r.chest == null) o.chest = false;

            // If chest bone is too high
            if (o.chest && Vector3.Dot(r.root.up, r.chest.position - GetUpperArmCentroid(r)) > 0f)
            {
                o.chest = false;
                if (r.spine != null) o.spine = true;
            }

            return o;
        }

        public static void Create(BipedRagdollReferences r, Options options)
        {
            string msg = string.Empty;
            if (!r.IsValid(ref msg))
            {
                Debug.LogWarning(msg);
                return;
            }

            // Clean up
            ClearAll(r.root);

            // Colliders
            CreateColliders(r, options);

            // Rigidbodies
            MassDistribution(r, options);

            // Joints
            CreateJoints(r, options);
        }

        private static void CreateColliders(BipedRagdollReferences r, Options options)
        {
            // Torso
            Vector3 upperArmToHeadCentroid = GetUpperArmToHeadCentroid(r);

            if (r.spine == null) options.spine = false;
            if (r.chest == null) options.chest = false;

            Vector3 shoulderDirection = r.rightUpperArm.position - r.leftUpperArm.position;
            float torsoWidth = shoulderDirection.magnitude;
            float torsoProportionAspect = 0.6f;

            // Hips
            Vector3 hipsStartPoint = r.hips.position;

            // Making sure the hip bone is not at the feet
            float toHead = Vector3.Distance(r.head.position, r.root.position);
            float toHips = Vector3.Distance(r.hips.position, r.root.position);

            if (toHips < toHead * 0.2f)
            {
                hipsStartPoint = Vector3.Lerp(r.leftUpperLeg.position, r.rightUpperLeg.position, 0.5f);
            }

            Vector3 lastEndPoint = options.spine ? r.spine.position : (options.chest ? r.chest.position : upperArmToHeadCentroid);
            hipsStartPoint += (hipsStartPoint - upperArmToHeadCentroid) * 0.1f;
            float hipsWidth = options.spine || options.chest ? torsoWidth * 0.8f : torsoWidth;

            CreateCollider(r.hips, hipsStartPoint, lastEndPoint, options.torsoColliders, options.colliderLengthOverlap, hipsWidth, torsoProportionAspect, shoulderDirection);

            // Spine
            if (options.spine)
            {
                Vector3 spineStartPoint = lastEndPoint;
                lastEndPoint = options.chest ? r.chest.position : upperArmToHeadCentroid;

                float spineWidth = options.chest ? torsoWidth * 0.75f : torsoWidth;

                CreateCollider(r.spine, spineStartPoint, lastEndPoint, options.torsoColliders, options.colliderLengthOverlap, spineWidth, torsoProportionAspect, shoulderDirection);
            }

            if (options.chest)
            {
                Vector3 chestStartPoint = lastEndPoint;
                lastEndPoint = upperArmToHeadCentroid;

                CreateCollider(r.chest, chestStartPoint, lastEndPoint, options.torsoColliders, options.colliderLengthOverlap, torsoWidth, torsoProportionAspect, shoulderDirection);
            }

            // Head
            Vector3 headStartPoint = lastEndPoint;
            Vector3 headEndPoint = headStartPoint + (headStartPoint - hipsStartPoint) * 0.45f;
            Vector3 axis = r.head.TransformVector(AxisTools.GetAxisVectorToDirection(r.head, headEndPoint - headStartPoint));
            headEndPoint = headStartPoint + Vector3.Project(headEndPoint - headStartPoint, axis).normalized * (headEndPoint - headStartPoint).magnitude;

            CreateCollider(r.head, headStartPoint, headEndPoint, options.headCollider, options.colliderLengthOverlap, Vector3.Distance(headStartPoint, headEndPoint) * 0.8f);

            // Arms
            float armWidthAspect = 0.4f;

            float leftArmWidth = Vector3.Distance(r.leftUpperArm.position, r.leftLowerArm.position) * armWidthAspect;

            CreateCollider(r.leftUpperArm, r.leftUpperArm.position, r.leftLowerArm.position, options.armColliders, options.colliderLengthOverlap, leftArmWidth);
            CreateCollider(r.leftLowerArm, r.leftLowerArm.position, r.leftHand.position, options.armColliders, options.colliderLengthOverlap, leftArmWidth * 0.9f);

            float rightArmWidth = Vector3.Distance(r.rightUpperArm.position, r.rightLowerArm.position) * armWidthAspect;

            CreateCollider(r.rightUpperArm, r.rightUpperArm.position, r.rightLowerArm.position, options.armColliders, options.colliderLengthOverlap, rightArmWidth);
            CreateCollider(r.rightLowerArm, r.rightLowerArm.position, r.rightHand.position, options.armColliders, options.colliderLengthOverlap, rightArmWidth * 0.9f);

            // Legs
            float legWidthAspect = 0.3f;

            float leftLegWidth = Vector3.Distance(r.leftUpperLeg.position, r.leftLowerLeg.position) * legWidthAspect;

            CreateCollider(r.leftUpperLeg, r.leftUpperLeg.position, r.leftLowerLeg.position, options.legColliders, options.colliderLengthOverlap, leftLegWidth);
            CreateCollider(r.leftLowerLeg, r.leftLowerLeg.position, r.leftFoot.position, options.legColliders, options.colliderLengthOverlap, leftLegWidth * 0.9f);

            float rightLegWidth = Vector3.Distance(r.rightUpperLeg.position, r.rightLowerLeg.position) * legWidthAspect;

            CreateCollider(r.rightUpperLeg, r.rightUpperLeg.position, r.rightLowerLeg.position, options.legColliders, options.colliderLengthOverlap, rightLegWidth);
            CreateCollider(r.rightLowerLeg, r.rightLowerLeg.position, r.rightFoot.position, options.legColliders, options.colliderLengthOverlap, rightLegWidth * 0.9f);

            // Hands
            if (options.hands)
            {
                CreateHandCollider(r.leftHand, r.leftLowerArm, r.root, options);
                CreateHandCollider(r.rightHand, r.rightLowerArm, r.root, options);
            }

            // Feet
            if (options.feet)
            {
                CreateFootCollider(r.leftFoot, r.leftLowerLeg, r.leftUpperLeg, r.root, options);
                CreateFootCollider(r.rightFoot, r.rightLowerLeg, r.rightUpperLeg, r.root, options);
            }
        }

        private static Collider CopyCollider(Collider c, GameObject destination)
        {
            if (c is CapsuleCollider)
            {
                var newCapsule = CopyCapsuleCollider(c as CapsuleCollider, destination);
                return newCapsule as Collider;
            }
            else if (c is SphereCollider)
            {
                var newSphere = CopySphereCollider(c as SphereCollider, destination);
                return newSphere as Collider;
            }
            else if (c is BoxCollider)
            {
                var newBox = CopyBoxCollider(c as BoxCollider, destination);
                return newBox as Collider;
            }
            return null;
        }

        private static CapsuleCollider CopyCapsuleCollider(CapsuleCollider o, GameObject destination)
        {
            CapsuleCollider d = destination.GetComponent<CapsuleCollider>();
            if (d == null) d = destination.AddComponent<CapsuleCollider>();

            d.isTrigger = o.isTrigger;
            d.sharedMaterial = o.sharedMaterial;
            d.center = o.center;
            d.radius = o.radius;
            d.height = o.height;
            d.direction = o.direction;

            return d;
        }

        private static SphereCollider CopySphereCollider(SphereCollider o, GameObject destination)
        {
            SphereCollider d = destination.GetComponent<SphereCollider>();
            if (d == null) d = destination.AddComponent<SphereCollider>();

            d.isTrigger = o.isTrigger;
            d.sharedMaterial = o.sharedMaterial;
            d.center = o.center;
            d.radius = o.radius;

            return d;
        }

        private static BoxCollider CopyBoxCollider(BoxCollider o, GameObject destination)
        {
            BoxCollider d = destination.GetComponent<BoxCollider>();
            if (d == null) d = destination.AddComponent<BoxCollider>();

            d.isTrigger = o.isTrigger;
            d.sharedMaterial = o.sharedMaterial;
            d.center = o.center;
            d.size = o.size;

            return d;
        }

        private static void CreateHandCollider(Transform hand, Transform lowerArm, Transform root, Options options)
        {
            Vector3 axis = hand.TransformVector(AxisTools.GetAxisVectorToPoint(hand, GetChildCentroid(hand, lowerArm.position)));

            Vector3 endPoint = hand.position - (lowerArm.position - hand.position) * 0.75f;
            endPoint = hand.position + Vector3.Project(endPoint - hand.position, axis).normalized * (endPoint - hand.position).magnitude;

            CreateCollider(hand, hand.position, endPoint, options.handColliders, options.colliderLengthOverlap, Vector3.Distance(endPoint, hand.position) * 0.5f);
        }

        private static void CreateFootCollider(Transform foot, Transform lowerLeg, Transform upperLeg, Transform root, Options options)
        {
            float legHeight = (upperLeg.position - foot.position).magnitude;
            Vector3 axis = foot.TransformVector(AxisTools.GetAxisVectorToPoint(foot, GetChildCentroid(foot, foot.position + root.forward) + root.forward * legHeight * 0.2f));

            Vector3 endPoint = foot.position + root.forward * legHeight * 0.25f;
            endPoint = foot.position + Vector3.Project(endPoint - foot.position, axis).normalized * (endPoint - foot.position).magnitude;

            float width = Vector3.Distance(endPoint, foot.position) * 0.5f;
            Vector3 startPoint = foot.position;

            bool footBelowRoot = Vector3.Dot(root.up, foot.position - root.position) < 0f;
            Vector3 heightOffset = footBelowRoot ? Vector3.zero : Vector3.Project((startPoint - root.up * width * 0.5f) - root.position, root.up);

            Vector3 direction = endPoint - startPoint;
            startPoint -= direction * 0.2f;

            if (options.fixFootColliderRotation)
            {
                Vector3 fAxis = AxisTools.GetAxisVectorToDirection(foot, root.forward);
                if (Vector3.Dot(foot.rotation * fAxis, root.forward) < 0f) fAxis = -fAxis;

                Vector3 normal = Vector3.up;
                Vector3 tangent = foot.rotation * fAxis;
                Vector3.OrthoNormalize(ref normal, ref tangent);

                Vector3 fallback = foot.position + tangent;
                Vector3 childCentroid = GetChildCentroidRecursive(foot, fallback);
                Vector3 toChildC = childCentroid - foot.position;

                var child = new GameObject("Foot Collider").transform;
                child.parent = foot;
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
                var collider = CreateCollider(child, startPoint - heightOffset, endPoint - heightOffset, options.footColliders, options.colliderLengthOverlap, width, foot);

                child.rotation = Quaternion.FromToRotation(child.rotation * fAxis, childCentroid - child.position) * child.rotation;
                Orthogonize(child, root.forward, root.up);
                Orthogonize(child, root.right, root.up);

                if (childCentroid != fallback)
                {
                    Vector3 center = Vector3.Lerp(foot.position, childCentroid, 0.5f);
                    Vector3 colliderCenter = GetColliderCenter(collider);
                    child.position += center - colliderCenter;

                    float bottomY = GetColliderBottom(collider, root.up);
                    child.position += Vector3.up * (root.position.y - bottomY);
                }
            }
            else
            {
                CreateCollider(foot, startPoint - heightOffset, endPoint - heightOffset, options.footColliders, options.colliderLengthOverlap, width);
            }
        }

        public static Collider FixFootCollider(Transform foot, Transform root)
        {
            Vector3 fAxis = AxisTools.GetAxisVectorToDirection(foot, root.forward);
            if (Vector3.Dot(foot.rotation * fAxis, root.forward) < 0f) fAxis = -fAxis;

            Vector3 normal = Vector3.up;
            Vector3 tangent = foot.rotation * fAxis;
            Vector3.OrthoNormalize(ref normal, ref tangent);

            Vector3 fallback = foot.position + tangent;
            Vector3 childCentroid = GetChildCentroidRecursive(foot, fallback);
            Vector3 toChildC = childCentroid - foot.position;

            var child = new GameObject("Foot Collider").transform;
            child.parent = foot;
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            var footCollider = foot.GetComponent<Collider>();
            
            var collider = CopyCollider(footCollider, child.gameObject);

            if (Application.isPlaying)
            {
                Destroy(footCollider);
            }
            else
            {
                DestroyImmediate(footCollider);
            }

            child.rotation = Quaternion.FromToRotation(child.rotation * fAxis, childCentroid - child.position) * child.rotation;
            Orthogonize(child, root.forward, root.up);
            Orthogonize(child, root.right, root.up);

            if (childCentroid != fallback)
            {
                Vector3 center = Vector3.Lerp(foot.position, childCentroid, 0.5f);
                Vector3 colliderCenter = GetColliderCenter(collider);
                child.position += center - colliderCenter;

                float bottomY = GetColliderBottom(collider, root.up);
                child.position += Vector3.up * (root.position.y - bottomY);
            }

            return collider;
        }

        private static Vector3 GetColliderCenter(Collider c)
        {
            if (c is BoxCollider)
            {
                return c.transform.TransformPoint((c as BoxCollider).center);
            }
            if (c is CapsuleCollider)
            {
                return c.transform.TransformPoint((c as CapsuleCollider).center);
            }
            return c.transform.position;
        }

        private static float GetColliderBottom(Collider c, Vector3 up)
        {
            var t = c.transform;

            if (c is BoxCollider)
            {
                var box = c as BoxCollider;
                Vector3 axis = AxisTools.GetAxisVectorToDirection(t, -up);
                if (Vector3.Dot(t.rotation * axis, -up) < 0f) axis = -axis;

                var scaled = Vector3.Scale(box.size, axis * 0.5f);

                return (t.TransformPoint(box.center) + t.rotation * scaled).y;
            }

            if (c is CapsuleCollider)
            {
                var capsule = c as CapsuleCollider;
                Vector3 axis = AxisTools.GetAxisVectorToDirection(t, -up);
                if (Vector3.Dot(t.rotation * axis, -up) < 0f) axis = -axis;

                var scaled = capsule.radius * axis * 0.5f;

                return (t.TransformPoint(capsule.center) + t.rotation * scaled).y;
            }

            return GetColliderCenter(c).y;
        }

        private static void Orthogonize(Transform t, Vector3 direction, Vector3 normal)
        {
            Vector3 axis = AxisTools.GetAxisVectorToDirection(t, direction);
            if (Vector3.Dot(t.rotation * axis, direction) < 0f) axis = -axis;

            Vector3 tangent = t.rotation * axis;
            Vector3.OrthoNormalize(ref normal, ref tangent);
            t.rotation = Quaternion.FromToRotation(t.rotation * axis, tangent) * t.rotation;
        }

        private static Vector3 GetChildCentroidRecursive(Transform t, Vector3 fallback)
        {
            var children = t.GetComponentsInChildren<Transform>();
            if (children.Length < 2) return fallback;

            Vector3 c = Vector3.zero;
            for (int i = 1; i < children.Length; i++)
            {
                c += children[i].position;
            }
            c /= children.Length - 1;

            return c;
        }

        private static Vector3 GetChildCentroid(Transform t, Vector3 fallback)
        {
            if (t.childCount == 0) return fallback;

            Vector3 c = Vector3.zero;
            for (int i = 0; i < t.childCount; i++)
            {
                c += t.GetChild(i).position;
            }
            c /= (float)t.childCount;

            return c;
        }

        private static void MassDistribution(BipedRagdollReferences r, Options o)
        {
            int torsoBones = 3;
            if (r.spine == null)
            {
                o.spine = false;
                torsoBones--;
            }
            if (r.chest == null)
            {
                o.chest = false;
                torsoBones--;
            }

            float torsoPrc = 0.508f / (float)torsoBones;
            float headPrc = 0.0732f;
            float upperArmPrc = 0.027f;
            float lowerArmPrc = 0.016f;
            float handPrc = 0.0066f;
            float upperLegPrc = 0.0988f;
            float lowerLegPrc = 0.0465f;
            float footPrc = 0.0145f;

            r.hips.GetComponent<Rigidbody>().mass = torsoPrc * o.weight;
            if (o.spine) r.spine.GetComponent<Rigidbody>().mass = torsoPrc * o.weight;
            if (o.chest) r.chest.GetComponent<Rigidbody>().mass = torsoPrc * o.weight;

            r.head.GetComponent<Rigidbody>().mass = headPrc * o.weight;

            r.leftUpperArm.GetComponent<Rigidbody>().mass = upperArmPrc * o.weight;
            r.rightUpperArm.GetComponent<Rigidbody>().mass = r.leftUpperArm.GetComponent<Rigidbody>().mass;

            r.leftLowerArm.GetComponent<Rigidbody>().mass = lowerArmPrc * o.weight;
            r.rightLowerArm.GetComponent<Rigidbody>().mass = r.leftLowerArm.GetComponent<Rigidbody>().mass;

            if (o.hands)
            {
                r.leftHand.GetComponent<Rigidbody>().mass = handPrc * o.weight;
                r.rightHand.GetComponent<Rigidbody>().mass = r.leftHand.GetComponent<Rigidbody>().mass;
            }

            r.leftUpperLeg.GetComponent<Rigidbody>().mass = upperLegPrc * o.weight;
            r.rightUpperLeg.GetComponent<Rigidbody>().mass = r.leftUpperLeg.GetComponent<Rigidbody>().mass;

            r.leftLowerLeg.GetComponent<Rigidbody>().mass = lowerLegPrc * o.weight;
            r.rightLowerLeg.GetComponent<Rigidbody>().mass = r.leftLowerLeg.GetComponent<Rigidbody>().mass;

            if (o.feet)
            {
                r.leftFoot.GetComponent<Rigidbody>().mass = footPrc * o.weight;
                r.rightFoot.GetComponent<Rigidbody>().mass = r.leftFoot.GetComponent<Rigidbody>().mass;
            }
        }

        private static void CreateJoints(BipedRagdollReferences r, Options o)
        {
            // Torso
            if (r.spine == null) o.spine = false;
            if (r.chest == null) o.chest = false;

            float spineMinSwing = -30f * o.jointRange;
            float spineMaxSwing = 10f * o.jointRange;
            float spineSwing2 = 25f * o.jointRange;
            float spineTwist = 25f * o.jointRange;

            CreateJoint(new CreateJointParams(
                r.hips.GetComponent<Rigidbody>(),
                null,
                (o.spine ? r.spine : (o.chest ? r.chest : r.head)),
                r.root.right,
                new CreateJointParams.Limits(0f, 0f, 0f, 0f),
                o.joints
                ));

            if (o.spine)
            {
                CreateJoint(new CreateJointParams(
                    r.spine.GetComponent<Rigidbody>(),
                    r.hips.GetComponent<Rigidbody>(),
                    (o.chest ? r.chest : r.head),
                    r.root.right,
                    new CreateJointParams.Limits(spineMinSwing, spineMaxSwing, spineSwing2, spineTwist),
                    o.joints
                    ));
            }

            if (o.chest)
            {
                CreateJoint(new CreateJointParams(
                    r.chest.GetComponent<Rigidbody>(),
                    (o.spine ? r.spine.GetComponent<Rigidbody>() : r.hips.GetComponent<Rigidbody>()),
                    r.head,
                    r.root.right,
                    new CreateJointParams.Limits(spineMinSwing, spineMaxSwing, spineSwing2, spineTwist),
                    o.joints
                    ));
            }

            // Head
            Transform lastTorsoBone = o.chest ? r.chest : (o.spine ? r.spine : r.hips);

            CreateJoint(new CreateJointParams(
                r.head.GetComponent<Rigidbody>(),
                lastTorsoBone.GetComponent<Rigidbody>(),
                null,
                r.root.right,
                new CreateJointParams.Limits(-30f, 30f, 30f, 85f),
                o.joints
                ));

            // Arms
            CreateJointParams.Limits upperArmLimits = new CreateJointParams.Limits(-35f * o.jointRange, 120f * o.jointRange, 85f * o.jointRange, 45 * o.jointRange);
            CreateJointParams.Limits lowerArmLimits = new CreateJointParams.Limits(0f, 140f * o.jointRange, 10f * o.jointRange, 45f * o.jointRange);
            CreateJointParams.Limits handLimits = new CreateJointParams.Limits(-50f * o.jointRange, 50f * o.jointRange, 50f * o.jointRange, 25f * o.jointRange);

            // Left Arm
            CreateLimbJoints(
                lastTorsoBone,
                r.leftUpperArm,
                r.leftLowerArm,
                r.leftHand,
                r.root,
                -r.root.right,
                o.joints,
                upperArmLimits,
                lowerArmLimits,
                handLimits);

            // Right Arm
            CreateLimbJoints(
                lastTorsoBone,
                r.rightUpperArm,
                r.rightLowerArm,
                r.rightHand,
                r.root,
                r.root.right,
                o.joints,
                upperArmLimits,
                lowerArmLimits,
                handLimits);

            // Legs
            CreateJointParams.Limits upperLegLimits = new CreateJointParams.Limits(-120f * o.jointRange, 35f * o.jointRange, 85f * o.jointRange, 45 * o.jointRange);
            CreateJointParams.Limits lowerLegLimits = new CreateJointParams.Limits(0f, 140f * o.jointRange, 10f * o.jointRange, 45f * o.jointRange);
            CreateJointParams.Limits footLimits = new CreateJointParams.Limits(-50f * o.jointRange, 50f * o.jointRange, 50f * o.jointRange, 25f * o.jointRange);

            // Left Leg
            CreateLimbJoints(
                r.hips,
                r.leftUpperLeg,
                r.leftLowerLeg,
                r.leftFoot,
                r.root,
                -r.root.up,
                o.joints,
                upperLegLimits,
                lowerLegLimits,
                footLimits);

            // Right Leg
            CreateLimbJoints(
                r.hips,
                r.rightUpperLeg,
                r.rightLowerLeg,
                r.rightFoot,
                r.root,
                -r.root.up,
                o.joints,
                upperLegLimits,
                lowerLegLimits,
                footLimits);
        }

        private static void CreateLimbJoints(Transform connectedBone, Transform bone1, Transform bone2, Transform bone3, Transform root, Vector3 defaultWorldDirection, JointType jointType, CreateJointParams.Limits limits1, CreateJointParams.Limits limits2, CreateJointParams.Limits limits3)
        {
            Quaternion bone1DefaultLocalRotation = bone1.localRotation;

            bone1.rotation = Quaternion.FromToRotation(bone1.rotation * (bone2.position - bone1.position), defaultWorldDirection) * bone1.rotation;

            Vector3 bone1Dir = (bone2.position - bone1.position).normalized;
            Vector3 bone2Dir = (bone3.position - bone2.position).normalized;

            Vector3 bendPlaneNormal = -Vector3.Cross(bone1Dir, bone2Dir);
            float bone2PoseAngleOffset = Vector3.Angle(bone1Dir, bone2Dir);

            bool isVertical = Mathf.Abs(Vector3.Dot(bone1Dir, root.up)) > 0.5f;
            float verticalAngleOffsetMlp = isVertical ? 100f : 1f; // Fixing Mixamo's inverted legs

            // Fixing straight limbs
            if (bone2PoseAngleOffset < 0.01f * verticalAngleOffsetMlp)
            {
                if (isVertical) bendPlaneNormal = Vector3.Dot(bone1Dir, root.up) > 0f ? root.right : -root.right;
                else bendPlaneNormal = Vector3.Dot(bone1Dir, root.right) > 0f ? root.up : -root.up;

                //Debug.LogWarning("Limb " + bone1.name + ", " + bone2.name + ", " + bone3.name + " appears to be completely stretched out, Ragdoll Creator can not know how to assign joint limits. Please rotate the elbow/knee bone slightly towards its natural bending direction.");
            }

            CreateJoint(new CreateJointParams(
                bone1.GetComponent<Rigidbody>(),
                connectedBone.GetComponent<Rigidbody>(),
                bone2,
                bendPlaneNormal,
                limits1,
                jointType
                ));

            CreateJoint(new CreateJointParams(
                bone2.GetComponent<Rigidbody>(),
                bone1.GetComponent<Rigidbody>(),
                bone3,
                bendPlaneNormal,
                new CreateJointParams.Limits(limits2.minSwing - bone2PoseAngleOffset, limits2.maxSwing - bone2PoseAngleOffset, limits2.swing2, limits2.twist),
                jointType
                ));

            if (bone3.GetComponent<Rigidbody>() != null)
            {
                CreateJoint(new CreateJointParams(
                    bone3.GetComponent<Rigidbody>(),
                    bone2.GetComponent<Rigidbody>(),
                    null,
                    bendPlaneNormal,
                    limits3,
                    jointType
                    ));
            }

            bone1.localRotation = bone1DefaultLocalRotation;
        }

        public static void ClearBipedRagdoll(BipedRagdollReferences r)
        {
            var transforms = r.GetRagdollTransforms();
            foreach (Transform t in transforms) ClearTransform(t);
        }

        public static bool IsClear(BipedRagdollReferences r)
        {
            var transforms = r.GetRagdollTransforms();
            foreach (Transform t in transforms)
            {
                if (t.GetComponent<Rigidbody>() != null) return false;
            }
            return true;
        }

        private static Vector3 GetUpperArmToHeadCentroid(BipedRagdollReferences r)
        {
            return Vector3.Lerp(GetUpperArmCentroid(r), r.head.position, 0.5f);
        }

        private static Vector3 GetUpperArmCentroid(BipedRagdollReferences r)
        {
            return Vector3.Lerp(r.leftUpperArm.position, r.rightUpperArm.position, 0.5f);
        }
    }
}
