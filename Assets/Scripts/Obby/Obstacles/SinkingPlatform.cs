using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A platform that slowly sinks while the player stands on it and rises back when they leave. Stand
    /// too long and it goes under the surface of the quicksand (the level's kill slab), which respawns the
    /// player. Needs a <see cref="RideableSurface"/> on the same object; the rider is carried down with it.
    /// Implements: story-004, Desert 2 (stone slabs that slowly sink over the quicksand); reusable for Glacier.
    /// </summary>
    [RequireComponent(typeof(RideableSurface))]
    [DefaultExecutionOrder(-50)]
    public sealed class SinkingPlatform : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Sinking speed while the player stands on it, metres per second.")]
        private float sinkSpeed = 0.7f;

        [SerializeField, Min(0f), Tooltip("Rising speed after the player leaves, metres per second.")]
        private float riseSpeed = 0.9f;

        [SerializeField, Min(0f), Tooltip("Deepest it can sink below its start position, metres. Make it deeper than the platform's height above the surface.")]
        private float maxDepth = 4f;

        private RideableSurface surface;
        private Vector3 homeLocalPosition;
        private float depth;

        /// <summary>How far below its start position the platform is, metres.</summary>
        public float Depth => depth;

        private void Awake()
        {
            surface = GetComponent<RideableSurface>();
            ObstacleUtility.EnsureKinematicBody(gameObject);
            homeLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (surface.HasRider) depth = Mathf.Min(maxDepth, depth + sinkSpeed * dt);
            else depth = Mathf.Max(0f, depth - riseSpeed * dt);
            transform.localPosition = homeLocalPosition + Vector3.down * depth;
        }
    }
}
