using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Swings this transform back and forth around a local axis like a pendulum (angle = amplitude *
    /// sin(time)). Children hang from it: a hazard (trigger collider + <see cref="KillVolume"/>) for a
    /// swinging Rainbell flower, or a foothold (collider + <see cref="RideableSurface"/>) for a vine the
    /// player rides. <c>keepUpright</c> stops a foothold from tilting with the rope.
    /// Driven by absolute time, so the motion is deterministic and frame-rate independent.
    /// Implements: story-004, Rainforest 1 (swinging Rainbell flowers), Rainforest 2 (swinging vines);
    /// reusable for Valley (hanging bridge) and Desert.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PendulumSwing : MonoBehaviour
    {
        [SerializeField, Tooltip("Local axis the pendulum rotates around. (1,0,0) swings front-to-back along local Z; (0,0,1) swings left-to-right along local X.")]
        private Vector3 swingAxis = Vector3.right;

        [SerializeField, Range(0f, 80f), Tooltip("Peak angle from hanging straight down, degrees.")]
        private float amplitudeDegrees = 35f;

        [SerializeField, Min(0.1f), Tooltip("Seconds for one full swing (there and back).")]
        private float periodSeconds = 3f;

        [SerializeField, Tooltip("Time offset of the swing, seconds (half a period = the opposite phase).")]
        private float phaseSeconds;

        [SerializeField, Tooltip("Child kept at its starting world rotation while the rope swings (a standable foothold). Optional.")]
        private Transform keepUpright;

        private Quaternion baseLocalRotation;
        private Quaternion uprightWorldRotation = Quaternion.identity;

        private void Awake()
        {
            ObstacleUtility.EnsureKinematicBody(gameObject);
            baseLocalRotation = transform.localRotation;
            if (keepUpright != null) uprightWorldRotation = keepUpright.rotation;
        }

        private void Update()
        {
            float angle = amplitudeDegrees * Mathf.Sin((Time.time + phaseSeconds) * (2f * Mathf.PI / periodSeconds));
            transform.localRotation = baseLocalRotation * Quaternion.AngleAxis(angle, swingAxis.normalized);
            if (keepUpright != null) keepUpright.rotation = uprightWorldRotation;
        }
    }
}
