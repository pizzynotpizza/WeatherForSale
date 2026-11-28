using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>The flight path a <see cref="Hawk"/> repeats.</summary>
    public enum HawkPattern
    {
        /// <summary>An oval across the canyon (x = sin, z = cos), tilted by the Y radius (high on one side, low on the other).</summary>
        Loop = 0,
        /// <summary>A figure-8 across the canyon (x = sin, z = sin 2x), bobbing by the Y radius.</summary>
        FigureEight = 1,
        /// <summary>A fast swoop across the path (high, low in the middle, high on the far side), then a slow high return arc.</summary>
        Dive = 2,
        /// <summary>A back-and-forth line across the canyon with a small wave in Y and Z.</summary>
        Sweep = 3,
    }

    /// <summary>
    /// A hawk hazard for the glide: flies a repeating path around its start position (in the parent's
    /// space) and faces where it is going, flapping its wings (wings held still during a dive's swoop).
    /// Put a trigger collider and a <see cref="KillVolume"/> on the same object: touching it respawns the
    /// player. Driven by absolute time, so every run has the same rhythm and it is frame-rate independent.
    /// Implements: story-004, Valley 1 "Canyon glide" (hawks: patrols, figure-8s, dives, stacked pairs).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DefaultExecutionOrder(-50)]
    public sealed class Hawk : MonoBehaviour
    {
        /// <summary>Seconds ahead sampled to find the facing direction.</summary>
        private const float LookAheadSeconds = 0.05f;

        [SerializeField, Tooltip("Path shape.")]
        private HawkPattern pattern = HawkPattern.Loop;

        [SerializeField, Tooltip("Path half-size in the parent's space, metres: x = across the canyon, y = up, z = along the canyon. For a Dive, y is the swoop depth and z the return arc (negative = back toward the player).")]
        private Vector3 radii = new Vector3(4f, 1f, 2.5f);

        [SerializeField, Min(0.2f), Tooltip("Seconds for one full path.")]
        private float periodSeconds = 4f;

        [SerializeField, Tooltip("Time offset of the path, seconds (half a period = the opposite side).")]
        private float phaseSeconds;

        [SerializeField, Range(0.05f, 0.95f), Tooltip("Dive only: fraction of the period spent on the fast swoop.")]
        private float diveFraction = 0.45f;

        [SerializeField, Tooltip("Dive only: extra height of the return arc above the swoop's ends, metres (keep it above the glide ceiling).")]
        private float returnLift = 6f;

        [SerializeField, Tooltip("Left wing pivot (flaps around its local Z). Optional.")]
        private Transform leftWing;

        [SerializeField, Tooltip("Right wing pivot (flaps around its local Z). Optional.")]
        private Transform rightWing;

        [SerializeField, Range(0f, 80f), Tooltip("Wing flap angle each way, degrees.")]
        private float flapDegrees = 28f;

        [SerializeField, Min(0.05f), Tooltip("Seconds per wing beat.")]
        private float flapPeriodSeconds = 0.5f;

        private Vector3 homeLocalPosition;
        private Quaternion leftBase = Quaternion.identity;
        private Quaternion rightBase = Quaternion.identity;

        /// <summary>True while a Dive hawk is on its fast swoop (at the current time).</summary>
        public bool IsDiving => pattern == HawkPattern.Dive && Mathf.Repeat((Time.time + phaseSeconds) / periodSeconds, 1f) < diveFraction;

        /// <summary>
        /// Offset from the start position (parent space, metres) at path time <paramref name="time"/>
        /// (seconds, phase included). Pure function of its inputs.
        /// </summary>
        public Vector3 OffsetAt(float time)
        {
            float u = Mathf.Repeat(time / periodSeconds, 1f);
            float a = u * 2f * Mathf.PI;
            switch (pattern)
            {
                case HawkPattern.FigureEight:
                    return new Vector3(radii.x * Mathf.Sin(a), radii.y * Mathf.Cos(2f * a), radii.z * Mathf.Sin(2f * a));
                case HawkPattern.Dive:
                    return DiveOffset(u);
                case HawkPattern.Sweep:
                    return new Vector3(radii.x * Mathf.Sin(a), radii.y * Mathf.Sin(2f * a), radii.z * Mathf.Sin(2f * a));
                default:
                    return new Vector3(radii.x * Mathf.Sin(a), radii.y * Mathf.Sin(a), radii.z * Mathf.Cos(a));
            }
        }

        private Vector3 DiveOffset(float u)
        {
            float d = Mathf.Clamp(diveFraction, 0.05f, 0.95f);
            if (u < d)
            {
                // Swoop: from -x high, down through the middle, up to +x high.
                float p = u / d;
                return new Vector3(-radii.x * Mathf.Cos(Mathf.PI * p), radii.y * Mathf.Cos(2f * Mathf.PI * p), 0f);
            }
            // Return: back from +x to -x on a high arc that bends along z.
            float q = (u - d) / (1f - d);
            float s = Mathf.Sin(Mathf.PI * q);
            return new Vector3(radii.x * Mathf.Cos(Mathf.PI * q), radii.y + returnLift * s, radii.z * s);
        }

        private void Awake()
        {
            ObstacleUtility.EnsureKinematicBody(gameObject);
            homeLocalPosition = transform.localPosition;
            if (leftWing != null) leftBase = leftWing.localRotation;
            if (rightWing != null) rightBase = rightWing.localRotation;
        }

        private void Update()
        {
            float t = Time.time + phaseSeconds;
            Vector3 offset = OffsetAt(t);
            Vector3 ahead = OffsetAt(t + LookAheadSeconds) - offset;
            transform.localPosition = homeLocalPosition + offset;
            if (ahead.sqrMagnitude > 1e-8f) transform.localRotation = Quaternion.LookRotation(ahead, Vector3.up);

            float flap = IsDiving ? 0f : flapDegrees * Mathf.Sin(t * 2f * Mathf.PI / flapPeriodSeconds);
            if (leftWing != null) leftWing.localRotation = leftBase * Quaternion.Euler(0f, 0f, -flap);
            if (rightWing != null) rightWing.localRotation = rightBase * Quaternion.Euler(0f, 0f, flap);
        }
    }
}
