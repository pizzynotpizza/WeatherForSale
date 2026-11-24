using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// Moves a transform back and forth (sine wave) around its start position: a bobbing log, a lift,
    /// a floating stone. Put a <see cref="RideableSurface"/> on the same object so the player is carried.
    /// Driven by absolute time, so the motion is the same on every run and frame-rate independent.
    /// Implements: story-004 shared obstacle toolkit (Rainforest 2 floating logs; Valley floating rocks).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlatformMover : MonoBehaviour
    {
        [SerializeField, Tooltip("Peak offset from the start position, in the parent's space, metres.")]
        private Vector3 travel = new Vector3(0f, 0.1f, 0f);

        [SerializeField, Min(0.1f), Tooltip("Seconds for one full back-and-forth cycle.")]
        private float periodSeconds = 2.5f;

        [SerializeField, Tooltip("Time offset of the cycle, seconds (to desynchronise neighbours).")]
        private float phaseSeconds;

        private Vector3 homeLocalPosition;

        private void Awake()
        {
            ObstacleUtility.EnsureKinematicBody(gameObject);
            homeLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            float wave = Mathf.Sin((Time.time + phaseSeconds) * (2f * Mathf.PI / periodSeconds));
            transform.localPosition = homeLocalPosition + travel * wave;
        }
    }
}
