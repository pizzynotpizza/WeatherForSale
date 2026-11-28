using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A pendulum swing with an irregular rhythm, for hanging bridges the player must read rather than count:
    /// angle = A(t) * sin(phase(t)), where the amplitude A(t) = base + variation * sin(2 pi t / amplitudePeriod)
    /// drifts between (base - variation) and (base + variation), and phase(t) = 2 pi t / period
    /// + wobble * sin(2 pi t / wobblePeriod) speeds up and slows down each swing. The phase always moves
    /// forward (the wobble is capped), so every swing still reaches its current amplitude at both ends: a
    /// boarding moment comes once per swing, at least at the smallest amplitude. Periods that do not divide
    /// each other keep the pattern from repeating soon.
    /// Same use as <see cref="PendulumSwing"/>: children hang from this transform, <c>keepUpright</c> keeps a
    /// standable deck level. Driven by absolute time, so the motion is deterministic and frame-rate independent.
    /// Implements: story-004, Valley 2 (long irregular suspension bridges).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class IrregularSwing : MonoBehaviour
    {
        /// <summary>Largest allowed wobble as a fraction of the one that would stop the phase (keeps it moving forward).</summary>
        private const float MaxWobbleFraction = 0.9f;

        [SerializeField, Tooltip("Local axis the swing rotates around. (1,0,0) swings front-to-back along local Z.")]
        private Vector3 swingAxis = Vector3.right;

        [SerializeField, Range(0f, 80f), Tooltip("Middle amplitude, degrees.")]
        private float baseAmplitudeDegrees = 14f;

        [SerializeField, Range(0f, 40f), Tooltip("How far the amplitude drifts above and below the middle, degrees.")]
        private float amplitudeVariationDegrees = 4f;

        [SerializeField, Min(0.1f), Tooltip("Seconds for the amplitude to drift up, down and back.")]
        private float amplitudePeriodSeconds = 9.7f;

        [SerializeField, Min(0.1f), Tooltip("Average seconds for one full swing (there and back).")]
        private float periodSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("How much each swing speeds up / slows down, radians of phase. Capped so the swing never stops or reverses.")]
        private float wobbleRadians = 0.5f;

        [SerializeField, Min(0.1f), Tooltip("Seconds for the speed-up / slow-down cycle.")]
        private float wobblePeriodSeconds = 6.1f;

        [SerializeField, Tooltip("Time offset, seconds.")]
        private float phaseSeconds;

        [SerializeField, Tooltip("Child kept at its starting world rotation while the rope swings (a standable deck). Optional.")]
        private Transform keepUpright;

        private Quaternion baseLocalRotation;
        private Quaternion uprightWorldRotation = Quaternion.identity;

        /// <summary>Widest swing reached, degrees (base + variation).</summary>
        public float MaxAmplitudeDegrees => baseAmplitudeDegrees + Mathf.Abs(amplitudeVariationDegrees);

        /// <summary>Smallest swing amplitude reached, degrees (base - variation, not below zero).</summary>
        public float MinAmplitudeDegrees => Mathf.Max(0f, baseAmplitudeDegrees - Mathf.Abs(amplitudeVariationDegrees));

        /// <summary>Swing angle in degrees at absolute time <paramref name="time"/> (seconds; the phase offset is added here).</summary>
        public float AngleAt(float time)
        {
            float t = time + phaseSeconds;
            float amplitude = baseAmplitudeDegrees + amplitudeVariationDegrees * Mathf.Sin(t * 2f * Mathf.PI / amplitudePeriodSeconds);
            // d(phase)/dt = 2pi/P + wobble * 2pi/Pw * cos(...) stays positive while wobble < Pw / P.
            float wobble = Mathf.Min(wobbleRadians, MaxWobbleFraction * wobblePeriodSeconds / periodSeconds);
            float phase = t * 2f * Mathf.PI / periodSeconds + wobble * Mathf.Sin(t * 2f * Mathf.PI / wobblePeriodSeconds);
            return amplitude * Mathf.Sin(phase);
        }

        private void Awake()
        {
            ObstacleUtility.EnsureKinematicBody(gameObject);
            baseLocalRotation = transform.localRotation;
            if (keepUpright != null) uprightWorldRotation = keepUpright.rotation;
        }

        private void Update()
        {
            transform.localRotation = baseLocalRotation * Quaternion.AngleAxis(AngleAt(Time.time), swingAxis.normalized);
            if (keepUpright != null) keepUpright.rotation = uprightWorldRotation;
        }
    }
}
