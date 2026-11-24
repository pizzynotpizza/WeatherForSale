using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A rolling log: the platform itself stays put but its surface drifts, pushing a rider back and
    /// forth along <c>driveDirection</c> (speed follows a sine wave, so it rolls one way, then the
    /// other). The visual cylinder rolls to match. The rider has to keep walking to stay on top.
    /// Collider should be a flat box on top of the log; only <c>rollVisual</c> rotates.
    /// Implements: story-004, Rainforest 2 (floating logs that rotate / roll). Reusable for conveyor-like hazards.
    /// </summary>
    public sealed class RollingSurface : RideableSurface
    {
        [SerializeField, Tooltip("Local direction the top of the log moves when the speed is positive. Must be perpendicular to the roll axis.")]
        private Vector3 driveDirection = Vector3.forward;

        [SerializeField, Min(0f), Tooltip("Peak speed of the surface, metres per second (the player's own run speed is 6).")]
        private float peakSurfaceSpeed = 2.5f;

        [SerializeField, Min(0.1f), Tooltip("Seconds for the surface to go one way and back.")]
        private float periodSeconds = 4f;

        [SerializeField, Tooltip("Time offset of the cycle, seconds.")]
        private float phaseSeconds;

        [SerializeField, Tooltip("Visual rolled around rollAxis to match the surface speed. Optional.")]
        private Transform rollVisual;

        [SerializeField, Tooltip("Local axis the visual rolls around. With driveDirection = +Z this should be +X (top of the log moves toward +Z for positive angles).")]
        private Vector3 rollAxis = Vector3.right;

        [SerializeField, Min(0.05f), Tooltip("Radius of the rolling visual, metres (sets how fast it appears to spin).")]
        private float rollRadius = 0.9f;

        private Quaternion baseVisualRotation = Quaternion.identity;

        /// <inheritdoc />
        public override Vector3 SurfaceVelocity =>
            transform.TransformDirection(driveDirection).normalized * CurrentSpeed;

        private float Omega => 2f * Mathf.PI / periodSeconds;

        private float CurrentSpeed => peakSurfaceSpeed * Mathf.Sin((Time.time + phaseSeconds) * Omega);

        private void Awake()
        {
            if (rollVisual != null) baseVisualRotation = rollVisual.localRotation;
        }

        private void Update()
        {
            if (rollVisual == null) return;
            // Integral of speed / radius over time, in closed form so the roll never drifts out of sync.
            float angleRadians = -peakSurfaceSpeed / (rollRadius * Omega) * Mathf.Cos((Time.time + phaseSeconds) * Omega);
            rollVisual.localRotation = baseVisualRotation * Quaternion.AngleAxis(angleRadians * Mathf.Rad2Deg, rollAxis.normalized);
        }
    }
}
