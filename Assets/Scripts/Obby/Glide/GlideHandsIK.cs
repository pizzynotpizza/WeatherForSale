using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// While the player glides, raises the character's arms up and out and puts the hands on the glider's two
    /// handle poles, one each side of the head (Sam's short arms cannot reach a bar over the big head), and
    /// lets the legs hang and swing loosely like a pendulum (no walk cycle: <see cref="PlayerAnimator"/> holds
    /// the idle pose while gliding). Animator humanoid IK; the controller's base layer needs IK Pass on. The poles
    /// cross the glider's local X axis at +-<see cref="gripHalfWidth"/> (<see cref="GlideMode.Glider"/>). Blends in and out.
    /// Purely visual. Lives on the character model next to its Animator.
    /// Implements: story-004, Valley 1 "Canyon glide" (Sam holds the glider).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class GlideHandsIK : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Distance of each handle pole from the glider's centre line, metres.")]
        private float gripHalfWidth = 0.32f;

        [SerializeField, Min(0.1f), Tooltip("How fast the arms blend onto / off the bar, weight per second.")]
        private float blendSpeed = 6f;

        [SerializeField, Tooltip("Elbow hint, model-local metres from the shoulder line centre (X is mirrored per arm): elbows out to the sides and slightly back.")]
        private Vector3 elbowHint = new Vector3(0.6f, 1.0f, 0f);

        [Header("Legs")]
        [SerializeField, Tooltip("Where each foot hangs at rest, model-local metres (X mirrored per leg): a little apart, lifted and behind the hips.")]
        private Vector3 footRest = new Vector3(0.09f, 0.1f, -0.1f);

        [SerializeField, Min(0f), Tooltip("Forward / back swing of each foot, metres (the legs swing in opposite phase).")]
        private float footSwing = 0.13f;

        [SerializeField, Min(0.01f), Tooltip("Seconds per full leg swing.")]
        private float swingPeriod = 1.6f;

        [SerializeField, Tooltip("Knee hint, model-local metres (X mirrored per leg): knees a little forward.")]
        private Vector3 kneeHint = new Vector3(0.12f, 0.35f, 0.45f);

        private Animator animator;
        private GlideMode glide;
        private float weight;

        [Header("Grip")]
        [SerializeField, Min(0f), Tooltip("Bar radius plus a little: the palm sits this far inside the pole's centre line (on Sam's side), metres.")]
        private float palmClearance = 0.035f;

        [SerializeField, Range(0f, 1f), Tooltip("Where the bar crosses the hand, as a fraction of the hand's length from the wrist (Sam's rig has no finger bones, so the flat hand is placed, not curled).")]
        private float barAcrossHand = 0.55f;

        private Hand left, right;

        /// <summary>A hand's bind-pose axes in the hand bone's local space, and its wrist-to-knuckle length.</summary>
        private struct Hand
        {
            public Quaternion LocalFrame; // forward = palm normal, up = finger direction (bone-local)
            public float Palm;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            left = MeasureHand(HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm);
            right = MeasureHand(HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm);
        }

        /// <summary>
        /// Reads a hand's axes from the bind pose (arms out, palms down). The rig has no finger bones, so the
        /// fingers point along the forearm and the palm faces down; hand length is about a third of the forearm.
        /// </summary>
        private Hand MeasureHand(HumanBodyBones hand, HumanBodyBones forearm)
        {
            Transform h = animator.GetBoneTransform(hand), f = animator.GetBoneTransform(forearm);
            if (h == null || f == null) return new Hand { LocalFrame = Quaternion.identity, Palm = 0.06f };
            Vector3 fingers = h.InverseTransformDirection(h.position - f.position).normalized;
            Vector3 palm = Vector3.ProjectOnPlane(h.InverseTransformDirection(Vector3.down), fingers).normalized;
            return new Hand { LocalFrame = Quaternion.LookRotation(palm, fingers), Palm = Vector3.Distance(h.position, f.position) * 0.4f };
        }

        /// <summary>
        /// Hand goal on a handle pole: fingers point up the glider, the palm faces outward and rests against the
        /// inside of the pole (Sam's side), the grip <see cref="barAcrossHand"/> of the way up the hand.
        /// <paramref name="side"/> is -1 for the left pole, +1 for the right.
        /// </summary>
        private void SetGrip(AvatarIKGoal goal, Hand hand, Transform bar, float side)
        {
            Vector3 up = bar.up, outward = bar.right * side;
            Quaternion world = Quaternion.LookRotation(outward, up) * Quaternion.Inverse(hand.LocalFrame);
            Vector3 wrist = bar.TransformPoint(side * gripHalfWidth, 0f, 0f) - up * hand.Palm * barAcrossHand - outward * palmClearance;
            animator.SetIKPosition(goal, wrist);
            animator.SetIKRotation(goal, world);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (glide == null) glide = GetComponentInParent<GlideMode>(); // added to the player on the first glide
            Transform bar = glide != null ? glide.Glider : null;
            weight = Mathf.MoveTowards(weight, bar != null ? 1f : 0f, blendSpeed * Time.deltaTime);

            if (weight <= 0f || bar == null)
            {
                SetWeights(0f);
                return;
            }

            SetWeights(weight);
            SetGrip(AvatarIKGoal.LeftHand, left, bar, -1f);
            SetGrip(AvatarIKGoal.RightHand, right, bar, 1f);

            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, transform.TransformPoint(new Vector3(-elbowHint.x, elbowHint.y, elbowHint.z)));
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, transform.TransformPoint(elbowHint));

            // Legs: loose pendulum swing, opposite phase, the second harmonic adds a little lift at each end.
            float phase = Time.time * 2f * Mathf.PI / swingPeriod;
            float swing = Mathf.Sin(phase) * footSwing;
            float lift = (1f - Mathf.Cos(2f * phase)) * 0.25f * footSwing;
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, transform.TransformPoint(new Vector3(-footRest.x, footRest.y + lift, footRest.z + swing)));
            animator.SetIKPosition(AvatarIKGoal.RightFoot, transform.TransformPoint(new Vector3(footRest.x, footRest.y + lift, footRest.z - swing)));
            animator.SetIKHintPosition(AvatarIKHint.LeftKnee, transform.TransformPoint(new Vector3(-kneeHint.x, kneeHint.y, kneeHint.z)));
            animator.SetIKHintPosition(AvatarIKHint.RightKnee, transform.TransformPoint(kneeHint));
        }

        private void SetWeights(float w)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, w);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, w);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, w);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, w);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, w);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, w);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, w);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, w);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, w);
        }
    }
}
