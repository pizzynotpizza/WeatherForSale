using UnityEngine;

namespace WeatherForSale.Obby
{
    /// <summary>
    /// A hazard that rolls along a straight path again and again (tumbleweed, later snowballs): it starts at
    /// its home position, travels <c>travel</c> at <c>speed</c>, disappears, waits, and starts again. Put a
    /// trigger collider and a <see cref="KillVolume"/> on the same object so touching it respawns the
    /// player. The visual child rolls to match the distance travelled. Colliders and renderers are
    /// switched off while it waits. Driven by absolute time: every run has the same rhythm.
    /// Implements: story-004, Desert 1 and 2 (tumbleweeds rolling toward the player); reusable for Glacier.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class RollingHazard : MonoBehaviour
    {
        [SerializeField, Tooltip("Path from the home position, in the parent's space, metres. The hazard rolls along it.")]
        private Vector3 travel = new Vector3(0f, 0f, -15f);

        [SerializeField, Min(0.1f), Tooltip("Rolling speed, metres per second.")]
        private float speed = 5f;

        [SerializeField, Min(0f), Tooltip("Seconds it stays away after reaching the end, before it starts again.")]
        private float restartDelaySeconds = 2f;

        [SerializeField, Tooltip("Time offset of the cycle, seconds (to space several hazards on one path).")]
        private float phaseSeconds;

        [SerializeField, Tooltip("Visual that rolls (optional).")]
        private Transform spinVisual;

        [SerializeField, Tooltip("Local axis the visual rolls around: for travel toward -Z use (-1,0,0); toward +Z use (1,0,0).")]
        private Vector3 spinAxis = new Vector3(-1f, 0f, 0f);

        [SerializeField, Min(0.05f), Tooltip("Radius of the rolling visual, metres (sets how fast it spins).")]
        private float rollRadius = 0.6f;

        private Vector3 homeLocalPosition;
        private Quaternion baseSpinRotation = Quaternion.identity;
        private Collider[] colliders;
        private Renderer[] renderers;
        private bool active = true;

        private void Awake()
        {
            ObstacleUtility.EnsureKinematicBody(gameObject);
            homeLocalPosition = transform.localPosition;
            if (spinVisual != null) baseSpinRotation = spinVisual.localRotation;
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            float length = travel.magnitude;
            float runSeconds = length / speed;
            float cycle = runSeconds + restartDelaySeconds;
            float t = Mathf.Repeat(Time.time + phaseSeconds, cycle);
            bool running = t < runSeconds;

            if (running != active) SetActive(running);
            if (!running) return;

            float distance = t * speed;
            transform.localPosition = homeLocalPosition + travel.normalized * distance;
            if (spinVisual != null)
            {
                spinVisual.localRotation = baseSpinRotation * Quaternion.AngleAxis(distance / rollRadius * Mathf.Rad2Deg, spinAxis.normalized);
            }
        }

        private void SetActive(bool on)
        {
            active = on;
            foreach (Collider c in colliders)
            {
                if (c != null) c.enabled = on;
            }
            foreach (Renderer r in renderers)
            {
                if (r != null) r.enabled = on;
            }
            if (!on) transform.localPosition = homeLocalPosition;
        }
    }
}
